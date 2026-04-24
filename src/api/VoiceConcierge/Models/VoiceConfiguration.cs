using System.ComponentModel.DataAnnotations;

namespace VoiceConcierge.Models;

public class VoiceConfiguration
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActiveVoiceId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
