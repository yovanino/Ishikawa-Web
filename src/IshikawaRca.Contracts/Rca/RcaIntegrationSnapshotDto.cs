namespace IshikawaRca.Contracts.Rca;

public class RcaIntegrationSnapshotDto
{
    public Guid IncidentId { get; set; }

    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string ClaimScope { get; set; } = string.Empty;

    public string ClaimActorType { get; set; } = string.Empty;

    public string? ClaimOwnerName { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset? LastUpdatedAt { get; set; }

    public string SourceSystem { get; set; } = string.Empty;

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string? MachineCode { get; set; }

    public string? LineCode { get; set; }

    public string? WorkOrderCode { get; set; }

    public bool EscalatedTo8D { get; set; }

    public string? RootCauseTitle { get; set; }

    public string? RootCauseEvidenceSummary { get; set; }

    public int CauseCount { get; set; }

    public int EvidenceCount { get; set; }

    public int OpenCorrectiveActionsCount { get; set; }

    public int OverdueCorrectiveActionsCount { get; set; }

    public DateTimeOffset? NextActionDueAt { get; set; }

    public IReadOnlyList<RcaIntegrationActionDto> OpenActions { get; set; } = [];
}
