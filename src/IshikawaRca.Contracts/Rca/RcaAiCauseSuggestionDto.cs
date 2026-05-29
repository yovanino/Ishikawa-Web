namespace IshikawaRca.Contracts.Rca;

public class RcaAiCauseSuggestionDto
{
    public string BranchName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Reasoning { get; set; } = string.Empty;

    public int ConfidenceScore { get; set; }

    public int SuggestedImpactScore { get; set; }

    public int SuggestedProbabilityScore { get; set; }

    public int SuggestedFrequencyScore { get; set; }
}
