namespace IshikawaRca.Contracts.Rca;

public class ReviewExternalIntakeRequest
{
    public Guid BranchId { get; set; }

    public bool ImportCause { get; set; } = true;

    public bool MarkCauseAsRoot { get; set; }

    public bool ImportCorrectiveAction { get; set; } = true;

    public string? ReviewedByUserId { get; set; }
}
