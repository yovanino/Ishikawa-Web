using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaOutboxEvent : TenantEntity
{
    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public Guid IncidentId { get; set; }

    public string? SourceSystem { get; set; }

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public RcaOutboxEventStatus Status { get; set; } = RcaOutboxEventStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? LastError { get; set; }
}
