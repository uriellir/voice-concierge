using Microsoft.EntityFrameworkCore;
using VoiceConcierge.Models;

namespace VoiceConcierge.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RawPropertyRecord> RawPropertyRecords => Set<RawPropertyRecord>();
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<KnowledgeEmbedding> KnowledgeEmbeddings => Set<KnowledgeEmbedding>();
    public DbSet<UnansweredQuestion> UnansweredQuestions => Set<UnansweredQuestion>();
    public DbSet<VoiceConfiguration> VoiceConfigurations => Set<VoiceConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RawPropertyRecord>(entity =>
        {
            entity.ToTable("raw_property_records");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(x => new { x.Section, x.Title });
        });

        modelBuilder.Entity<KnowledgeItem>(entity =>
        {
            entity.ToTable("knowledge_items");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(x => x.RawPropertyRecordId)
                .IsUnique();

            entity.HasOne(x => x.RawPropertyRecord)
                .WithOne()
                .HasForeignKey<KnowledgeItem>(x => x.RawPropertyRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeEmbedding>(entity =>
        {
            entity.ToTable("knowledge_embeddings");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.KnowledgeItem)
                .WithOne(x => x.Embedding)
                .HasForeignKey<KnowledgeEmbedding>(x => x.KnowledgeItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UnansweredQuestion>(entity =>
        {
            entity.ToTable("unanswered_questions");

            entity.HasIndex(x => x.NormalizedQuestion)
                .IsUnique();

            entity.Property(x => x.FirstSeenAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(x => x.LastSeenAt)
                .HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<VoiceConfiguration>(entity =>
        {
            entity.ToTable("voice_configurations");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW()");
        });
    }
}
