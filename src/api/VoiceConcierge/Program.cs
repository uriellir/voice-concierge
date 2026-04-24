using Microsoft.EntityFrameworkCore;
using VoiceConcierge.Data;
using VoiceConcierge.Data.Seed;
using VoiceConcierge.Services;

LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminUi", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection(EmbeddingOptions.SectionName));
builder.Services.AddHttpClient<IEmbeddingClient, OpenAiEmbeddingClient>();

builder.Services.AddScoped<RawPropertyCsvLoader>();
builder.Services.AddScoped<KnowledgeItemTransformer>();
builder.Services.AddScoped<KnowledgeSearchService>();
builder.Services.AddScoped<UnansweredQuestionService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AdminUi");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/knowledge/items", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var items = await dbContext.KnowledgeItems
        .Include(x => x.Embedding)
        .OrderBy(x => x.Category)
        .ThenBy(x => x.Title)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Category,
            x.CanonicalQuestion,
            x.AnswerText,
            x.FactsJson,
            HasEmbedding = x.Embedding != null
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(items);
})
.WithName("GetKnowledgeItems")
.WithTags("Knowledge");

app.MapPost("/api/knowledge/seed", async (DatabaseSeeder seeder, CancellationToken cancellationToken) =>
{
    await seeder.SeedAsync(cancellationToken);
    return Results.Ok(new { Seeded = true });
})
.WithName("SeedKnowledge")
.WithTags("Knowledge");

app.MapPost("/api/knowledge/reindex", async (ReindexEmbeddingsRequest request, KnowledgeSearchService searchService, CancellationToken cancellationToken) =>
{
    var processed = await searchService.GenerateEmbeddingsAsync(request.Force, cancellationToken);
    return Results.Ok(new { Processed = processed });
})
.WithName("ReindexKnowledgeEmbeddings")
.WithTags("Knowledge");

app.MapPost("/api/knowledge/search", async (
    KnowledgeSearchRequest request,
    KnowledgeSearchService searchService,
    UnansweredQuestionService unansweredQuestionService,
    CancellationToken cancellationToken) =>
{
    var result = await searchService.FindBestAnswerAsync(
        request.Query,
        request.TopK,
        request.MinimumConfidence,
        cancellationToken)
        .ConfigureAwait(false);

    if (!result.MatchFound && result.ShouldRecordUnanswered && request.AutoRecordUnansweredWhenNoMatch)
    {
        var recorded = await unansweredQuestionService.RecordAsync(request.Query, cancellationToken);
        result = result.WithUnansweredQuestion(recorded);
    }

    return Results.Ok(result.WithQuery(request.Query));
})
.WithName("SearchKnowledge")
.WithTags("Knowledge");

app.MapPost("/api/concierge/ask", async (
    KnowledgeSearchRequest request,
    KnowledgeSearchService searchService,
    UnansweredQuestionService unansweredQuestionService,
    CancellationToken cancellationToken) =>
{
    var effectiveRequest = request with { AutoRecordUnansweredWhenNoMatch = true };
    var result = await searchService.FindBestAnswerAsync(
        effectiveRequest.Query,
        effectiveRequest.TopK,
        effectiveRequest.MinimumConfidence,
        cancellationToken);

    if (!result.MatchFound && result.ShouldRecordUnanswered)
    {
        var recorded = await unansweredQuestionService.RecordAsync(effectiveRequest.Query, cancellationToken);
        result = result.WithUnansweredQuestion(recorded);
    }

    return Results.Ok(result.WithQuery(effectiveRequest.Query));
})
.WithName("AskConcierge")
.WithTags("Concierge");

app.MapPost("/api/unanswered", async (UnansweredQuestionRequest request, UnansweredQuestionService unansweredQuestionService, CancellationToken cancellationToken) =>
{
    var recorded = await unansweredQuestionService.RecordAsync(request.Question, cancellationToken);

    return Results.Ok(new
    {
        recorded.Id,
        recorded.OriginalText,
        recorded.NormalizedQuestion,
        recorded.Count,
        recorded.FirstSeenAt,
        recorded.LastSeenAt
    });
})
.WithName("RecordUnansweredQuestion")
.WithTags("Knowledge");

app.MapGet("/api/admin/unanswered", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var items = await dbContext.UnansweredQuestions
        .OrderByDescending(x => x.Count)
        .ThenByDescending(x => x.LastSeenAt)
        .Select(x => new
        {
            x.Id,
            x.OriginalText,
            x.NormalizedQuestion,
            x.Count,
            x.FirstSeenAt,
            x.LastSeenAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        TotalQuestions = items.Count,
        TotalMentions = items.Sum(x => x.Count),
        Items = items
    });
})
.WithName("GetUnansweredQuestionsQueue")
.WithTags("Admin");

app.MapGet("/api/admin/faqs", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var items = await dbContext.KnowledgeItems
        .OrderBy(x => x.Category)
        .ThenBy(x => x.Title)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Category,
            x.CanonicalQuestion,
            x.AnswerText,
            x.CreatedAt,
            x.UpdatedAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        TotalFaqs = items.Count,
        TotalCategories = items.Select(x => x.Category).Distinct().Count(),
        Items = items
    });
})
.WithName("GetAdminFaqs")
.WithTags("Admin");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

static void LoadDotEnv(string filePath)
{
    if (!File.Exists(filePath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(filePath))
    {
        var line = rawLine.Trim();

        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(key) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            continue;
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}

public sealed record KnowledgeSearchRequest(
    string Query,
    int TopK = 5,
    double? MinimumConfidence = null,
    bool AutoRecordUnansweredWhenNoMatch = false);
public sealed record ReindexEmbeddingsRequest(bool Force = false);
public sealed record UnansweredQuestionRequest(string Question);
