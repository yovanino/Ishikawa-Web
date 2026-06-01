using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaExternalIntakeRequest : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public RcaClaimActorType ActorType { get; set; } = RcaClaimActorType.Supplier;

    public string? ActorName { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public RcaExternalIntakeStatus Status { get; set; } = RcaExternalIntakeStatus.Sent;

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ClaimReference { get; set; }

    public string? MaterialCode { get; set; }

    public string? BatchOrLot { get; set; }

    public string? Description { get; set; }

    public string? ContainmentResponse { get; set; }

    public string? ProposedRootCause { get; set; }

    public string? ProposedCorrectiveAction { get; set; }

    public string? EvidenceSummary { get; set; }
}
