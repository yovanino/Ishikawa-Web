namespace IshikawaRca.Contracts.Rca;

public class RcaDomainEventDto
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public Guid IncidentId { get; set; }

    public Guid TenantId { get; set; }

    public string SourceSystem { get; set; } = string.Empty;

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public Dictionary<string, string?> Data { get; set; } = new();
}
