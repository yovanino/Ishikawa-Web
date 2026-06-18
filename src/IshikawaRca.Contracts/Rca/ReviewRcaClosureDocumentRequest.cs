namespace IshikawaRca.Contracts.Rca;

public class ReviewRcaClosureDocumentRequest
{
    public string ReviewedByUserId { get; set; } = string.Empty;

    public string ReviewNotes { get; set; } = string.Empty;
}
