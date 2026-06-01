namespace IshikawaRca.Contracts.Rca;

public class RcaIncidentDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ProblemDescription { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ClaimScope { get; set; } = string.Empty;

    public string ClaimActorType { get; set; } = string.Empty;

    public string? ClaimOwnerName { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? ClosedByUserId { get; set; }

    public string? ClosureSummary { get; set; }

    public string SourceSystem { get; set; } = string.Empty;

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string? MachineCode { get; set; }

    public string? LineCode { get; set; }

    public string? WorkOrderCode { get; set; }

    public bool EscalatedTo8D { get; set; }

    public DateTimeOffset? EscalatedTo8DAt { get; set; }

    public string? EscalatedTo8DByUserId { get; set; }

    public string? EscalationReason { get; set; }
}
