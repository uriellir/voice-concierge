using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceConcierge.Models;

public class KnowledgeEmbedding
{
    [Key, ForeignKey(nameof(KnowledgeItem))]
    public Guid KnowledgeItemId { get; set; }

    [Required]
    public string EmbeddingText { get; set; } = string.Empty;

    public KnowledgeItem KnowledgeItem { get; set; } = null!;
}