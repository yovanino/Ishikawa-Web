namespace IshikawaRca.Contracts.Rca;

public class RcaAiEightDDraftResultDto
{
    public Guid IncidentId { get; set; }

    public string ProblemStatement { get; set; } = string.Empty;

    public string ContainmentActions { get; set; } = string.Empty;

    public string RootCauseAnalysis { get; set; } = string.Empty;

    public string CorrectiveActions { get; set; } = string.Empty;

    public string VerificationPlan { get; set; } = string.Empty;

    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
