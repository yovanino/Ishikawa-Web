namespace IshikawaRca.Contracts.Rca;

public class RcaAiSuggestionMetadataDto
{
    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool IsFallback { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
