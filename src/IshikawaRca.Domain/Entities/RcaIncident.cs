using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaIncident : TenantEntity
{
    public string Title { get; set; } = string.Empty;

    public string? ProblemDescription { get; set; }

    public RcaSeverity Severity { get; set; } = RcaSeverity.Medium;

    public RcaIncidentStatus Status { get; set; } = RcaIncidentStatus.Open;

    public RcaClaimScope ClaimScope { get; set; } = RcaClaimScope.Internal;

    public RcaClaimActorType ClaimActorType { get; set; } = RcaClaimActorType.InternalArea;

    public string? ClaimOwnerName { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAt { get; set; }

    public string? ClosedByUserId { get; set; }

    public string? ClosureSummary { get; set; }

    public string SourceSystem { get; set; } = "MANUAL";

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string? MachineCode { get; set; }

    public string? LineCode { get; set; }

    public string? WorkOrderCode { get; set; }

    public string? ReportedBy { get; set; }

    public string? TaskSnapshotJson { get; set; }

    public string? ContextSnapshotJson { get; set; }

    public bool EscalatedTo8D { get; set; }

    public DateTimeOffset? EscalatedTo8DAt { get; set; }

    public string? EscalatedTo8DByUserId { get; set; }

    public string? EscalationReason { get; set; }

    public RcaWizardStep WizardStep { get; set; } = RcaWizardStep.Problem;

    public DateTimeOffset? WizardStepCompletedAt { get; set; }

    public string? WizardStepCompletedByUserId { get; set; }

    public string? WizardStepNotes { get; set; }

    public ICollection<IshikawaBranch> Branches { get; set; } = new List<IshikawaBranch>();

    public ICollection<CorrectiveAction> CorrectiveActions { get; set; } = new List<CorrectiveAction>();

    public ICollection<RcaEvidence> Evidence { get; set; } = new List<RcaEvidence>();
}
