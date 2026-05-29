namespace IshikawaRca.Contracts.Rca;

public class RcaAiActionSuggestionResultDto
{
    public Guid IncidentId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<RcaAiActionSuggestionDto> Suggestions { get; set; } = [];

    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
