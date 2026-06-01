namespace IshikawaRca.Contracts.Rca;

public class CloseRcaIncidentRequest
{
    public string? ClosedByUserId { get; set; }

    public string ClosureSummary { get; set; } = string.Empty;
}
