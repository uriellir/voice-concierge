using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoiceConcierge.Data;
using VoiceConcierge.Models;

namespace VoiceConcierge.Services;

public class KnowledgeSearchService
{
    private const double DefaultMinimumConfidence = 0.55;
    private const double MinimumKeywordScoreForVectorOnlyMatch = 0.45;

    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingClient _embeddingClient;

    public KnowledgeSearchService(AppDbContext dbContext, IEmbeddingClient embeddingClient)
    {
        _dbContext = dbContext;
        _embeddingClient = embeddingClient;
    }

    public async Task<int> GenerateEmbeddingsAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (!_embeddingClient.IsConfigured)
        {
            throw new InvalidOperationException("Embedding generation is not configured. Set Embedding:ApiKey before running indexing.");
        }

        var items = await _dbContext.KnowledgeItems
            .Include(x => x.Embedding)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var item in items)
        {
            if (!force && item.Embedding is not null && item.Embedding.GetVector().Length > 0)
            {
                continue;
            }

            var embeddingText = BuildEmbeddingText(item);
            var result = await _embeddingClient.CreateEmbeddingAsync(embeddingText, cancellationToken);

            if (item.Embedding is null)
            {
                item.Embedding = new KnowledgeEmbedding
                {
                    KnowledgeItemId = item.Id,
                    EmbeddingText = embeddingText,
                    EmbeddingModel = result.Model,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }
            else
            {
                item.Embedding.EmbeddingText = embeddingText;
                item.Embedding.EmbeddingModel = result.Model;
            }

            item.Embedding.SetVector(result.Values);
            processed++;
        }

        if (processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    public async Task<KnowledgeAnswerResponse> FindBestAnswerAsync(
        string query,
        int topK = 5,
        double? minimumConfidence = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return KnowledgeAnswerResponse.NoMatch("Please provide a question to search.", 0);
        }

        var expandedQuery = ExpandGuestQuery(query);
        var queryTokens = Tokenize($"{query} {expandedQuery}");
        float[]? queryEmbedding = null;

        if (_embeddingClient.IsConfigured)
        {
            queryEmbedding = (await _embeddingClient.CreateEmbeddingAsync(expandedQuery, cancellationToken)).Values;
        }

        var knowledgeItems = await _dbContext.KnowledgeItems
            .Include(x => x.Embedding)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var candidates = knowledgeItems
            .Select(item => BuildCandidate(item, queryTokens, queryEmbedding))
            .OrderByDescending(candidate => candidate.CombinedScore)
            .Take(Math.Max(topK, 1))
            .ToList();

        var bestMatch = candidates.FirstOrDefault();
        var threshold = minimumConfidence ?? DefaultMinimumConfidence;

        if (bestMatch is null || bestMatch.CombinedScore < threshold)
        {
            return KnowledgeAnswerResponse.NoMatch(
                "I couldn't find a confident answer in the knowledge base.",
                threshold,
                candidates);
        }

        if (bestMatch.MeaningfulMatchedTokens.Count == 0 && bestMatch.KeywordScore < MinimumKeywordScoreForVectorOnlyMatch)
        {
            return KnowledgeAnswerResponse.NoMatch(
                "I couldn't find a grounded answer in the knowledge base.",
                threshold,
                candidates);
        }

        return KnowledgeAnswerResponse.Match(bestMatch, threshold, candidates);
    }

    private KnowledgeSearchCandidate BuildCandidate(KnowledgeItem item, HashSet<string> queryTokens, float[]? queryEmbedding)
    {
        var facts = KnowledgeFacts.FromJson(item.FactsJson);
        var searchableText = string.Join(
            ' ',
            new[]
            {
                item.Title,
                item.Category,
                item.CanonicalQuestion,
                item.AnswerText,
                facts.SearchText,
                facts.SubTitleValue,
                facts.DetailsValue
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        var searchableTokens = Tokenize(searchableText);
        var matchedTokens = queryTokens
            .Where(searchableTokens.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(token => token)
            .ToList();
        var meaningfulMatchedTokens = matchedTokens
            .Where(token => !GenericHospitalityTokens.Contains(token))
            .ToList();
        var keywordScore = CalculateKeywordScore(queryTokens, searchableTokens, searchableText, item, facts);
        var vectorScore = queryEmbedding is not null && item.Embedding is not null
            ? NormalizeCosineSimilarity(CosineSimilarity(queryEmbedding, item.Embedding.GetVector()))
            : 0d;

        var combinedScore = queryEmbedding is not null
            ? (vectorScore * 0.65) + (keywordScore * 0.35)
            : keywordScore;

        return new KnowledgeSearchCandidate(
            item.Id,
            item.Title,
            item.Category,
            item.AnswerText,
            item.FactsJson,
            combinedScore,
            keywordScore,
            vectorScore,
            matchedTokens,
            meaningfulMatchedTokens);
    }

    private static double CalculateKeywordScore(
        HashSet<string> queryTokens,
        HashSet<string> searchableTokens,
        string searchableText,
        KnowledgeItem item,
        KnowledgeFacts facts)
    {
        if (queryTokens.Count == 0 || searchableTokens.Count == 0)
        {
            return 0d;
        }

        var overlapCount = queryTokens.Count(searchableTokens.Contains);
        var overlapScore = (double)overlapCount / queryTokens.Count;

        double exactBoost = 0;
        var normalizedQuery = string.Join(' ', queryTokens);

        if (ContainsPhrase(searchableText, normalizedQuery))
        {
            exactBoost += 0.2;
        }

        if (ContainsPhrase(item.Title, normalizedQuery))
        {
            exactBoost += 0.2;
        }

        if (ContainsPhrase(facts.SubTitleValue, normalizedQuery))
        {
            exactBoost += 0.15;
        }

        if (ContainsPhrase(item.Category, normalizedQuery))
        {
            exactBoost += 0.1;
        }

        foreach (var token in queryTokens)
        {
            if (ContainsPhrase(item.Title, token))
            {
                exactBoost += 0.08;
            }

            if (ContainsPhrase(facts.SubTitleValue, token))
            {
                exactBoost += 0.05;
            }
        }

        return Math.Min(1d, overlapScore + exactBoost);
    }

    private static bool ContainsPhrase(string? source, string phrase)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        return source.Contains(phrase, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExpandGuestQuery(string query)
    {
        var variations = new List<string> { query.Trim() };
        var normalized = query.ToLowerInvariant();

        if (normalized.Contains("discount"))
        {
            variations.Add("partner discounts room key benefits show room key offers");
        }

        if (normalized.Contains("restaurant") || normalized.Contains("eat") || normalized.Contains("food") || normalized.Contains("dining"))
        {
            variations.Add("restaurants dining food places to eat on-property restaurants");
        }

        if (normalized.Contains("check in") || normalized.Contains("check-in") || normalized.Contains("arrival"))
        {
            variations.Add("hotel check-in arrival time early check-in");
        }

        if (normalized.Contains("check out") || normalized.Contains("check-out") || normalized.Contains("departure"))
        {
            variations.Add("hotel check-out departure time late checkout");
        }

        if (normalized.Contains("parking") || normalized.Contains("valet"))
        {
            variations.Add("parking valet self parking car parking parking fee");
        }

        if (normalized.Contains("pool"))
        {
            variations.Add("pool cabana swimming outdoor amenities");
        }

        if (normalized.Contains("spa"))
        {
            variations.Add("spa massage treatments wellness");
        }

        if (normalized.Contains("suite") || normalized.Contains("villa") || normalized.Contains("accommodation") || normalized.Contains("stay"))
        {
            variations.Add("accommodations hotel room suite villa stay options");
        }

        return string.Join(". ", variations.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildEmbeddingText(KnowledgeItem item)
    {
        var facts = KnowledgeFacts.FromJson(item.FactsJson);
        var alternatePhrasings = BuildAlternatePhrasings(item, facts);

        return string.Join(
            Environment.NewLine,
            new List<string?>
            {
                $"Category: {item.Category}",
                $"Title: {item.Title}",
                $"Guest-facing summary: {BuildGuestFacingSummary(item, facts)}",
                $"Question: {item.CanonicalQuestion}",
                $"Answer: {item.AnswerText}",
                $"Search text: {facts.SearchText}",
                $"Guest intents: {string.Join("; ", alternatePhrasings)}",
                $"Keywords: {string.Join(", ", BuildKeywordSet(item, facts))}"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildGuestFacingSummary(KnowledgeItem item, KnowledgeFacts facts)
    {
        var subject = item.Title;

        if (!string.IsNullOrWhiteSpace(facts.SubTitleValue))
        {
            return $"Guests asking about {item.Category} should know that {subject} includes {facts.SubTitleValue}. {facts.DetailsValue}".Trim();
        }

        return $"Guests asking about {subject} should know that {facts.DetailsValue}".Trim();
    }

    private static IReadOnlyList<string> BuildAlternatePhrasings(KnowledgeItem item, KnowledgeFacts facts)
    {
        var phrases = new List<string>
        {
            item.CanonicalQuestion,
            $"Tell me about {item.Title}.",
            $"What should a guest know about {item.Title}?",
            $"Do you have information about {item.Title}?"
        };

        AddIfPresent(phrases, BuildCategoryQuestion(item.Category, item.Title));
        AddIfPresent(phrases, BuildSubTitleQuestion(item.Title, facts.SubTitleValue));

        foreach (var synonym in BuildKeywordSet(item, facts))
        {
            phrases.Add($"Guest question variant: {synonym}");
        }

        return phrases
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> BuildKeywordSet(KnowledgeItem item, KnowledgeFacts facts)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            item.Category,
            item.Title
        };

        AddIfPresent(keywords, facts.SubTitleValue);
        AddIfPresent(keywords, facts.DetailsValue);

        foreach (var synonym in GetSynonyms(item.Category, item.Title))
        {
            keywords.Add(synonym);
        }

        return keywords.ToList();
    }

    private static IEnumerable<string> GetSynonyms(string category, string title)
    {
        var text = $"{category} {title}".ToLowerInvariant();
        var synonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (text.Contains("check-in"))
        {
            synonyms.Add("arrival time");
            synonyms.Add("when can I arrive");
            synonyms.Add("hotel arrival");
        }

        if (text.Contains("check-out"))
        {
            synonyms.Add("departure time");
            synonyms.Add("late checkout");
            synonyms.Add("when do I leave");
        }

        if (text.Contains("parking") || text.Contains("valet"))
        {
            synonyms.Add("car parking");
            synonyms.Add("where do I park");
            synonyms.Add("parking fee");
        }

        if (text.Contains("partner discounts") || text.Contains("show room key"))
        {
            synonyms.Add("room key benefits");
            synonyms.Add("show my key for discounts");
            synonyms.Add("hotel guest discounts");
        }

        if (text.Contains("restaurant") || text.Contains("dining") || text.Contains("steakhouse") || text.Contains("cafe") || text.Contains("aurelia") || text.Contains("silk road"))
        {
            synonyms.Add("places to eat");
            synonyms.Add("food options");
            synonyms.Add("where can I dine");
        }

        if (text.Contains("poker") || text.Contains("blackjack") || text.Contains("baccarat") || text.Contains("roulette") || text.Contains("craps") || text.Contains("slot"))
        {
            synonyms.Add("casino games");
            synonyms.Add("gaming options");
            synonyms.Add("table games");
        }

        if (text.Contains("spa"))
        {
            synonyms.Add("massage");
            synonyms.Add("treatments");
            synonyms.Add("wellness");
        }

        if (text.Contains("pool"))
        {
            synonyms.Add("swimming pool");
            synonyms.Add("cabana");
            synonyms.Add("outdoor amenities");
        }

        if (text.Contains("nightclub") || text.Contains("lounge") || text.Contains("bar"))
        {
            synonyms.Add("nightlife");
            synonyms.Add("drinks");
            synonyms.Add("cocktails");
        }

        if (text.Contains("room") || text.Contains("suite") || text.Contains("villa") || text.Contains("accommodations"))
        {
            synonyms.Add("stay options");
            synonyms.Add("hotel room");
            synonyms.Add("where can I stay");
        }

        return synonyms;
    }

    private static string? BuildCategoryQuestion(string category, string title)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return $"What does the hotel offer for {category.ToLowerInvariant()} related to {title}?";
    }

    private static string? BuildSubTitleQuestion(string title, string? subTitleValue)
    {
        if (string.IsNullOrWhiteSpace(subTitleValue))
        {
            return null;
        }

        return $"How does {title} apply to {subTitleValue}?";
    }

    private static void AddIfPresent(ICollection<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }

    private static double NormalizeCosineSimilarity(double cosineSimilarity)
    {
        if (cosineSimilarity == double.MinValue)
        {
            return 0d;
        }

        return Math.Clamp((cosineSimilarity + 1d) / 2d, 0d, 1d);
    }

    private sealed class KnowledgeFacts
    {
        public string? SearchText { get; init; }
        public string? SubTitleValue { get; init; }
        public string? DetailsValue { get; init; }

        public static KnowledgeFacts FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new KnowledgeFacts();
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new KnowledgeFacts
            {
                SearchText = GetString(root, "SearchText"),
                SubTitleValue = GetString(root, "SubTitleValue"),
                DetailsValue = GetString(root, "DetailsValue")
            };
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
        }
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
        {
            return double.MinValue;
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Count; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return double.MinValue;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "at", "be", "do", "for", "from", "get", "have", "how", "i", "in",
        "is", "it", "me", "my", "of", "on", "or", "the", "to", "what", "when", "where", "with", "you"
    };

    private static readonly HashSet<string> GenericHospitalityTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "guest",
        "guests",
        "hotel",
        "property",
        "room",
        "rooms",
        "information",
        "details",
        "available",
        "book",
        "booking",
        "stay"
    };
}

public sealed record KnowledgeSearchCandidate(
    Guid KnowledgeItemId,
    string Title,
    string Category,
    string AnswerText,
    string? FactsJson,
    double CombinedScore,
    double KeywordScore,
    double VectorScore,
    IReadOnlyList<string> MatchedTokens,
    IReadOnlyList<string> MeaningfulMatchedTokens);

public sealed record KnowledgeAnswerResponse(
    bool MatchFound,
    string Query,
    double MinimumConfidence,
    string? Answer,
    string SpokenResponse,
    double? Confidence,
    string ConfidenceLabel,
    bool ShouldRecordUnanswered,
    string FallbackMessage,
    Guid? UnansweredQuestionId,
    int? UnansweredQuestionCount,
    KnowledgeSearchCandidate? BestMatch,
    IReadOnlyList<KnowledgeSearchCandidate> Candidates,
    string Message)
{
    public static KnowledgeAnswerResponse Match(
        KnowledgeSearchCandidate bestMatch,
        double minimumConfidence,
        IReadOnlyList<KnowledgeSearchCandidate> candidates)
    {
        return new KnowledgeAnswerResponse(
            true,
            string.Empty,
            minimumConfidence,
            bestMatch.AnswerText,
            BuildSpokenMatchResponse(bestMatch),
            bestMatch.CombinedScore,
            ToConfidenceLabel(bestMatch.CombinedScore),
            false,
            string.Empty,
            null,
            null,
            bestMatch,
            candidates,
            "A matching answer was found.");
    }

    public static KnowledgeAnswerResponse NoMatch(
        string message,
        double minimumConfidence,
        IReadOnlyList<KnowledgeSearchCandidate>? candidates = null)
    {
        return new KnowledgeAnswerResponse(
            false,
            string.Empty,
            minimumConfidence,
            null,
            "I'm sorry, but I don't have a reliable answer for that right now. I've noted the question for our team to review.",
            0,
            "none",
            true,
            "I'm sorry, but I don't have a reliable answer for that right now. I've noted the question for our team to review.",
            null,
            null,
            null,
            candidates ?? Array.Empty<KnowledgeSearchCandidate>(),
            message);
    }

    public KnowledgeAnswerResponse WithQuery(string query) => this with { Query = query };

    public KnowledgeAnswerResponse WithUnansweredQuestion(UnansweredQuestion unansweredQuestion)
    {
        return this with
        {
            UnansweredQuestionId = unansweredQuestion.Id,
            UnansweredQuestionCount = unansweredQuestion.Count
        };
    }

    private static string BuildSpokenMatchResponse(KnowledgeSearchCandidate bestMatch)
    {
        return bestMatch.AnswerText
            .Replace("Sub-title:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Details:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\r", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\n", ". ", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string ToConfidenceLabel(double score)
    {
        if (score >= 0.8)
        {
            return "high";
        }

        if (score >= 0.65)
        {
            return "medium";
        }

        if (score > 0)
        {
            return "low";
        }

        return "none";
    }
}
