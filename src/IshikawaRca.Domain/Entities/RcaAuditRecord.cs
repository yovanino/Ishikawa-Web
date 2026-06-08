using IshikawaRca.Domain.Common;

namespace IshikawaRca.Domain.Entities;

public class RcaAuditRecord : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string Summary { get; set; } = string.Empty;

    public string? DataJson { get; set; }
}

