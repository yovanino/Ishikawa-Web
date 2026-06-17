namespace IshikawaRca.Contracts.Rca;

public class RejectRcaAiSuggestionRequest
{
    public string ReviewedByUserId { get; set; } = string.Empty;

    public string ReviewNotes { get; set; } = string.Empty;
}
