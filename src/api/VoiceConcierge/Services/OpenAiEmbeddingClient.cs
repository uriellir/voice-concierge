using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace VoiceConcierge.Services;

public class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;

    public OpenAiEmbeddingClient(HttpClient httpClient, IOptions<EmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<EmbeddingVector> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Embedding API key is missing. Set Embedding:ApiKey in configuration before generating embeddings.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new EmbeddingRequest(_options.Model, input));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The embedding service returned an empty response.");

        var vector = payload.Data.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("The embedding service response did not include vector data.");

        return new EmbeddingVector(payload.Model ?? _options.Model, vector);
    }

    private sealed record EmbeddingRequest(string Model, string Input);

    private sealed class EmbeddingResponse
    {
        public string? Model { get; set; }
        public List<EmbeddingData> Data { get; set; } = new();
    }

    private sealed class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
