namespace IshikawaRca.Contracts.Rca;

public class RcaAiSummaryResultDto
{
    public Guid IncidentId { get; set; }

    public string ExecutiveSummary { get; set; } = string.Empty;

    public string RiskAssessment { get; set; } = string.Empty;

    public IReadOnlyList<string> RecommendedNextSteps { get; set; } = [];

    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
