using Microsoft.EntityFrameworkCore;
using VoiceConcierge.Data;
using VoiceConcierge.Models;

namespace VoiceConcierge.Services;

public class VoiceConfigurationService
{
    private readonly AppDbContext _dbContext;

    public VoiceConfigurationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.VoiceConfigurations.AnyAsync(cancellationToken))
        {
            return;
        }

        _dbContext.VoiceConfigurations.Add(new VoiceConfiguration
        {
            Id = Guid.NewGuid(),
            ActiveVoiceId = VoiceCatalog.DefaultVoiceId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminVoiceOptionsResponse> GetAdminVoiceOptionsAsync(CancellationToken cancellationToken = default)
    {
        var activeVoice = await GetActiveVoiceAsync(cancellationToken);
        var options = VoiceCatalog.Options
            .Select(option => new AdminVoiceOption(
                option.Id,
                option.Name,
                option.Description,
                option.Id == activeVoice.Id))
            .ToList();

        return new AdminVoiceOptionsResponse(
            activeVoice.Id,
            activeVoice.Name,
            options);
    }

    public async Task<AgentVoiceResponse> GetAgentVoiceAsync(CancellationToken cancellationToken = default)
    {
        var activeVoice = await GetActiveVoiceAsync(cancellationToken);
        return new AgentVoiceResponse(
            activeVoice.Id,
            activeVoice.Name,
            activeVoice.ProviderVoiceId);
    }

    public async Task<AdminVoiceOptionsResponse> SetActiveVoiceAsync(string voiceId, CancellationToken cancellationToken = default)
    {
        var selectedVoice = VoiceCatalog.GetById(voiceId);
        if (selectedVoice is null)
        {
            throw new ArgumentException("Invalid voice selection.", nameof(voiceId));
        }

        var configuration = await GetConfigurationAsync(cancellationToken);
        configuration.ActiveVoiceId = selectedVoice.Id;
        configuration.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminVoiceOptionsAsync(cancellationToken);
    }

    private async Task<VoiceConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);

        return await _dbContext.VoiceConfigurations
            .OrderBy(x => x.CreatedAt)
            .FirstAsync(cancellationToken);
    }

    private async Task<VoiceCatalogItem> GetActiveVoiceAsync(CancellationToken cancellationToken)
    {
        var configuration = await GetConfigurationAsync(cancellationToken);
        return VoiceCatalog.GetById(configuration.ActiveVoiceId) ?? VoiceCatalog.Default;
    }
}

public static class VoiceCatalog
{
    public const string DefaultVoiceId = "james";

    public static readonly VoiceCatalogItem Default = new(
        "james",
        "James",
        "Male, mature, warm British accent. Professional and refined.",
        Environment.GetEnvironmentVariable("VOICE_JAMES_PROVIDER_ID") ?? "9626c31c-bec5-4cca-baa8-f8ba9e84c8bc");

    public static readonly IReadOnlyList<VoiceCatalogItem> Options = new List<VoiceCatalogItem>
    {
        Default,
        new(
            "sofia",
            "Sofia",
            "Female, friendly, subtle European accent. Welcoming and elegant.",
            Environment.GetEnvironmentVariable("VOICE_SOFIA_PROVIDER_ID") ?? "9626c31c-bec5-4cca-baa8-f8ba9e84c8bc"),
        new(
            "marcus",
            "Marcus",
            "Male, American, confident and energetic. Modern and approachable.",
            Environment.GetEnvironmentVariable("VOICE_MARCUS_PROVIDER_ID") ?? "9626c31c-bec5-4cca-baa8-f8ba9e84c8bc"),
        new(
            "elena",
            "Elena",
            "Female, American, calm and reassuring. Sophisticated and clear.",
            Environment.GetEnvironmentVariable("VOICE_ELENA_PROVIDER_ID") ?? "9626c31c-bec5-4cca-baa8-f8ba9e84c8bc")
    };

    public static VoiceCatalogItem? GetById(string? voiceId)
    {
        return Options.FirstOrDefault(option => string.Equals(option.Id, voiceId?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record VoiceCatalogItem(string Id, string Name, string Description, string ProviderVoiceId);

public sealed record AdminVoiceOption(string Id, string Name, string Description, bool IsActive);

public sealed record AdminVoiceOptionsResponse(string ActiveVoiceId, string ActiveVoiceName, IReadOnlyList<AdminVoiceOption> Items);

public sealed record AgentVoiceResponse(string VoiceId, string VoiceName, string ProviderVoiceId);
