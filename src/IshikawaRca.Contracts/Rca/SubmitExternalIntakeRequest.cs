namespace IshikawaRca.Contracts.Rca;

public class SubmitExternalIntakeRequest
{
    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string? ClaimReference { get; set; }

    public string? MaterialCode { get; set; }

    public string? BatchOrLot { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ContainmentResponse { get; set; }

    public string? ProposedRootCause { get; set; }

    public string? ProposedCorrectiveAction { get; set; }

    public string? EvidenceSummary { get; set; }
}
