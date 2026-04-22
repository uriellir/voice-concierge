namespace VoiceConcierge.Services;

public interface IEmbeddingClient
{
    bool IsConfigured { get; }

    Task<EmbeddingVector> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}

public sealed record EmbeddingVector(string Model, float[] Values);
