using System.ComponentModel.DataAnnotations;

namespace VoiceConcierge.Models;

public class RawPropertyRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Section { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SubTitleLabel { get; set; }

    public string? SubTitleValue { get; set; }

    [MaxLength(100)]
    public string? DetailsLabel { get; set; }

    public string? DetailsValue { get; set; }

    [Required]
    public string SearchText { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
