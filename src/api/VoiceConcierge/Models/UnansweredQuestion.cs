using System.ComponentModel.DataAnnotations;

namespace VoiceConcierge.Models;

public class UnansweredQuestion
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string NormalizedQuestion { get; set; } = string.Empty;

    [Required]
    public string OriginalText { get; set; } = string.Empty;

    [Required]
    public int Count { get; set; } = 1;

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}