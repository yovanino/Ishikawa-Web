using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class AddRcaFactViewModel
{
    public Guid? CauseId { get; set; }

    public Guid? EvidenceId { get; set; }

    public Guid? CorrectiveActionId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string FactType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    [StringLength(220)]
    public string? SourceDetail { get; set; }

    [StringLength(80)]
    public string? ExternalSourceSystem { get; set; }

    [StringLength(160)]
    public string? ExternalEventId { get; set; }

    [StringLength(500)]
    public string? ExternalRecordUri { get; set; }

    public string FactSeverity { get; set; } = "Info";

    [StringLength(80)]
    public string? ShiftCode { get; set; }

    [StringLength(80)]
    public string? MachineCode { get; set; }

    [StringLength(80)]
    public string? LineCode { get; set; }

    [StringLength(120)]
    public string? WorkOrderCode { get; set; }

    [StringLength(120)]
    public string? MaterialCode { get; set; }

    [StringLength(120)]
    public string? BatchOrLot { get; set; }

    [StringLength(120)]
    public string? AlarmCode { get; set; }

    [StringLength(160)]
    public string? MeasurementName { get; set; }

    public decimal? MeasurementValue { get; set; }

    [StringLength(40)]
    public string? MeasurementUnit { get; set; }

    [Required]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    [StringLength(160)]
    public string? CapturedByUserId { get; set; }
}
