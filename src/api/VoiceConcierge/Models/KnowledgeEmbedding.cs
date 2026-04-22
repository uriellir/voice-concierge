using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace VoiceConcierge.Models;

public class KnowledgeEmbedding
{
    [Key, ForeignKey(nameof(KnowledgeItem))]
    public Guid KnowledgeItemId { get; set; }

    [Required]
    public string EmbeddingText { get; set; } = string.Empty;

    [Required]
    public string VectorJson { get; set; } = "[]";

    [MaxLength(100)]
    public string EmbeddingModel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public KnowledgeItem KnowledgeItem { get; set; } = null!;

    public float[] GetVector()
    {
        return JsonSerializer.Deserialize<float[]>(VectorJson) ?? Array.Empty<float>();
    }

    public void SetVector(IReadOnlyList<float> values)
    {
        VectorJson = JsonSerializer.Serialize(values);
    }
}
