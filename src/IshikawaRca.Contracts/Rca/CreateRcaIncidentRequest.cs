namespace IshikawaRca.Contracts.Rca;

public class CreateRcaIncidentRequest
{
    public Guid TenantId { get; set; }

    public string SourceSystem { get; set; } = "MANUAL";

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ProblemDescription { get; set; }

    public string Severity { get; set; } = "Medium";

    public string ClaimScope { get; set; } = "Internal";

    public string? ClaimActorType { get; set; }

    public string? ClaimOwnerName { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string? MachineCode { get; set; }

    public string? LineCode { get; set; }

    public string? WorkOrderCode { get; set; }

    public string? ReportedBy { get; set; }

    public string? TaskSnapshotJson { get; set; }

    public string? ContextSnapshotJson { get; set; }
}
