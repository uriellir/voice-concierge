using System.Text.Json;
using VoiceConcierge.Models;

namespace VoiceConcierge.Services;

public class KnowledgeItemTransformer
{
    public KnowledgeItem Transform(RawPropertyRecord record)
    {
        return new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            Title = record.Title,
            Category = record.Section,
            CanonicalQuestion = BuildCanonicalQuestion(record),
            AnswerText = BuildAnswerText(record),
            FactsJson = JsonSerializer.Serialize(new
            {
                record.Id,
                record.Section,
                record.Title,
                record.SubTitleLabel,
                record.SubTitleValue,
                record.DetailsLabel,
                record.DetailsValue,
                record.SearchText
            }),
            RawPropertyRecordId = record.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string BuildCanonicalQuestion(RawPropertyRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.SubTitleValue))
        {
            return $"What should a guest know about {record.Title} for {record.SubTitleValue}?";
        }

        return $"What should a guest know about {record.Title}?";
    }

    private static string BuildAnswerText(RawPropertyRecord record)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(record.SubTitleValue))
        {
            var label = string.IsNullOrWhiteSpace(record.SubTitleLabel) ? "Sub-title" : record.SubTitleLabel;
            lines.Add($"{label}: {record.SubTitleValue}");
        }

        if (!string.IsNullOrWhiteSpace(record.DetailsValue))
        {
            var label = string.IsNullOrWhiteSpace(record.DetailsLabel) ? "Details" : record.DetailsLabel;
            lines.Add($"{label}: {record.DetailsValue}");
        }

        if (lines.Count == 0)
        {
            lines.Add(record.SearchText);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
