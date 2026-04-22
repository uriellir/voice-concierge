using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoiceConcierge.Data;
using VoiceConcierge.Models;

namespace VoiceConcierge.Services;

public class UnansweredQuestionService
{
    private readonly AppDbContext _dbContext;

    public UnansweredQuestionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UnansweredQuestion> RecordAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        var normalizedQuestion = Normalize(question);
        var existing = await _dbContext.UnansweredQuestions
            .SingleOrDefaultAsync(x => x.NormalizedQuestion == normalizedQuestion, cancellationToken);

        if (existing is null)
        {
            existing = new UnansweredQuestion
            {
                Id = Guid.NewGuid(),
                NormalizedQuestion = normalizedQuestion,
                OriginalText = question.Trim(),
                Count = 1,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            };

            _dbContext.UnansweredQuestions.Add(existing);
        }
        else
        {
            existing.Count += 1;
            existing.LastSeenAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public static string Normalize(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = Regex.Replace(normalized, @"[^\w\s]", string.Empty);
        return normalized;
    }
}
