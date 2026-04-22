using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using VoiceConcierge.Models;

namespace VoiceConcierge.Services;

public class RawPropertyCsvLoader
{
    public async Task<List<RawPropertyRecord>> LoadAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var rows = new List<RawPropertyRecord>();

        await foreach (var record in csv.GetRecordsAsync<RawPropertyCsvRow>())
        {
            var rawRecord = new RawPropertyRecord
            {
                Id = Guid.NewGuid(),
                Section = Clean(record.Section),
                Title = Clean(record.Title),
                SubTitleLabel = CleanNullable(record.SubTitleLabel),
                SubTitleValue = CleanNullable(record.SubTitleValue),
                DetailsLabel = CleanNullable(record.DetailsLabel),
                DetailsValue = CleanNullable(record.DetailsValue),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            rawRecord.SearchText = BuildSearchText(rawRecord);
            rows.Add(rawRecord);
        }

        return rows;
    }

    private static string BuildSearchText(RawPropertyRecord record)
    {
        var lines = new List<string>
        {
            $"Section: {record.Section}",
            $"Title: {record.Title}"
        };

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

        return string.Join(Environment.NewLine, lines);
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? CleanNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private sealed class RawPropertyCsvRow
    {
        [Name("Section")]
        public string? Section { get; set; }

        [Name("Title")]
        public string? Title { get; set; }

        [Name("SubTitleLabel", "SubtitleLabel", "Sub-title Label")]
        public string? ExplicitSubTitleLabel { get; set; }

        [Name("SubTitleValue", "SubtitleValue", "Sub-title Value", "Sub-title", "Subtitle")]
        public string? ExplicitSubTitleValue { get; set; }

        [Name("DetailsLabel", "DetailLabel", "Details Label")]
        public string? ExplicitDetailsLabel { get; set; }

        [Name("DetailsValue", "DetailValue", "Details")]
        public string? ExplicitDetailsValue { get; set; }

        public string? SubTitleLabel => ExplicitSubTitleLabel ?? (ExplicitSubTitleValue is null ? null : "Sub-title");
        public string? SubTitleValue => ExplicitSubTitleValue;
        public string? DetailsLabel => ExplicitDetailsLabel ?? (ExplicitDetailsValue is null ? null : "Details");
        public string? DetailsValue => ExplicitDetailsValue;
    }
}
