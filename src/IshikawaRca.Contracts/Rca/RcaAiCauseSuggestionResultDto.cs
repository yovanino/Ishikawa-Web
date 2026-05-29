namespace IshikawaRca.Contracts.Rca;

public class RcaAiCauseSuggestionResultDto
{
    public Guid IncidentId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<RcaAiCauseSuggestionDto> Suggestions { get; set; } = [];

    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
