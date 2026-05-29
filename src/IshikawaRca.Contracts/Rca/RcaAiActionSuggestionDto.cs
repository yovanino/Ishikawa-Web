namespace IshikawaRca.Contracts.Rca;

public class RcaAiActionSuggestionDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? RelatedCauseTitle { get; set; }

    public string SuggestedOwnerRole { get; set; } = string.Empty;

    public int SuggestedDueDays { get; set; }
}
