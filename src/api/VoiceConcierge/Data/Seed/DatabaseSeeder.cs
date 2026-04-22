using VoiceConcierge.Services;
using Microsoft.EntityFrameworkCore;

namespace VoiceConcierge.Data.Seed;

public class DatabaseSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly RawPropertyCsvLoader _csvLoader;
    private readonly KnowledgeItemTransformer _transformer;
    private readonly IWebHostEnvironment _environment;

    public DatabaseSeeder(
        AppDbContext dbContext,
        RawPropertyCsvLoader csvLoader,
        KnowledgeItemTransformer transformer,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _csvLoader = csvLoader;
        _transformer = transformer;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.RawPropertyRecords.AnyAsync(cancellationToken))
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "Seed", "meridian-property-data.csv");
            var rawRecords = await _csvLoader.LoadAsync(filePath);

            _dbContext.RawPropertyRecords.AddRange(rawRecords);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await _dbContext.KnowledgeItems.AnyAsync(cancellationToken))
        {
            var rawRows = await _dbContext.RawPropertyRecords.ToListAsync(cancellationToken);
            var knowledgeItems = rawRows.Select(_transformer.Transform).ToList();

            _dbContext.KnowledgeItems.AddRange(knowledgeItems);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
