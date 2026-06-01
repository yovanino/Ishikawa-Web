using IshikawaRca.Domain.Common;

namespace IshikawaRca.Domain.Entities;

public class RcaEvidence : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public Guid? CauseId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CapturedByUserId { get; set; }
}
