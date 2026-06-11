namespace IshikawaRca.Contracts.Rca;

public class RcaOutboxEventDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid IncidentId { get; set; }
    public string? SourceSystem { get; set; }
    public string? ExternalTaskId { get; set; }
    public string? ExternalEventId { get; set; }
    public string? ExternalWorkOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? LastError { get; set; }
}
