using VoiceConcierge.Models;
using Microsoft.EntityFrameworkCore;

namespace VoiceConcierge.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<KnowledgeEmbedding> KnowledgeEmbeddings => Set<KnowledgeEmbedding>();
    public DbSet<UnansweredQuestion> UnansweredQuestions => Set<UnansweredQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KnowledgeItem>(entity =>
        {
            entity.ToTable("knowledge_items");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<KnowledgeEmbedding>(entity =>
        {
            entity.ToTable("knowledge_embeddings");
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
    }
}