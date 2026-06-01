namespace IshikawaRca.Contracts.Rca;

public class RejectExternalIntakeRequest
{
    public string RejectionReason { get; set; } = string.Empty;

    public string? RejectedByUserId { get; set; }
}
