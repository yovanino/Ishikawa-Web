using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class CorrectiveAction : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public Guid? CauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CorrectiveActionType ActionType { get; set; } = CorrectiveActionType.Corrective;

    public RcaResolutionScope ResolutionScope { get; set; } = RcaResolutionScope.RootCause;

    public CorrectiveActionStatus Status { get; set; } = CorrectiveActionStatus.Open;

    public string? AssignedToUserId { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? ValidationNotes { get; set; }
}
