using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceConcierge.Models;

public class KnowledgeItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string CanonicalQuestion { get; set; } = string.Empty;

    [Required]
    public string AnswerText { get; set; } = string.Empty;

    public string? FactsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public KnowledgeEmbedding? Embedding { get; set; }
}