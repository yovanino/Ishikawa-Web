using IshikawaRca.Domain.Common;

namespace IshikawaRca.Domain.Entities;

public class RcaFact : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public Guid? CauseId { get; set; }

    public Guid? EvidenceId { get; set; }

    public Guid? CorrectiveActionId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string FactType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    public string? SourceDetail { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CapturedByUserId { get; set; }
}
