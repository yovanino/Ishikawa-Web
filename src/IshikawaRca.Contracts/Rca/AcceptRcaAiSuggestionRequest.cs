namespace IshikawaRca.Contracts.Rca;

public class AcceptRcaAiSuggestionRequest
{
    public string ReviewedByUserId { get; set; } = string.Empty;

    public string ReviewNotes { get; set; } = string.Empty;

    public Guid? TargetBranchId { get; set; }
}
