namespace IshikawaRca.Contracts.Rca;

public class RcaAiRecurrenceResultDto
{
    public Guid IncidentId { get; set; }

    public bool IsLikelyRecurring { get; set; }

    public int ConfidenceScore { get; set; }

    public string Rationale { get; set; } = string.Empty;

    public IReadOnlyList<string> SimilarSignals { get; set; } = [];

    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
