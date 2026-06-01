namespace IshikawaRca.Contracts.Rca;

public class EscalateRcaIncidentTo8DRequest
{
    public string? EscalatedByUserId { get; set; }

    public string EscalationReason { get; set; } = string.Empty;
}
