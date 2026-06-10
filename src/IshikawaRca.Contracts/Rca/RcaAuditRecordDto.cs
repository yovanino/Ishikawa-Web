namespace IshikawaRca.Contracts.Rca;

public class RcaAuditRecordDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid RcaIncidentId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? DataJson { get; set; }
}
