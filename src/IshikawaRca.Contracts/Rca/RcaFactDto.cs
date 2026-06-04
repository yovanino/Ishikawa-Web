namespace IshikawaRca.Contracts.Rca;

public class RcaFactDto
{
    public Guid Id { get; set; }

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

    public DateTimeOffset OccurredAt { get; set; }

    public string? CapturedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
