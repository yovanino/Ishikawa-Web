namespace IshikawaRca.Contracts.Rca;

public class RcaExternalIntakeDto
{
    public Guid Id { get; set; }

    public Guid RcaIncidentId { get; set; }

    public string IncidentTitle { get; set; } = string.Empty;

    public string ActorType { get; set; } = string.Empty;

    public string? ActorName { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ClaimReference { get; set; }

    public string? MaterialCode { get; set; }

    public string? BatchOrLot { get; set; }

    public string? Description { get; set; }

    public string? ContainmentResponse { get; set; }

    public string? ProposedRootCause { get; set; }

    public string? ProposedCorrectiveAction { get; set; }

    public string? EvidenceSummary { get; set; }
}
