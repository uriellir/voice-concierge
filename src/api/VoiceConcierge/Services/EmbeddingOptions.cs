namespace VoiceConcierge.Services;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "text-embedding-3-small";
    public string ApiKey { get; set; } = string.Empty;
}
