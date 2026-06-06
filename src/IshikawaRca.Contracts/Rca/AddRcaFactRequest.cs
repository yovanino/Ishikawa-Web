namespace IshikawaRca.Contracts.Rca;

public class AddRcaFactRequest
{
    public Guid? CauseId { get; set; }

    public Guid? EvidenceId { get; set; }

    public Guid? CorrectiveActionId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string? FactType { get; set; }

    public string? Source { get; set; }

    public string? SourceDetail { get; set; }

    public string? ExternalSourceSystem { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalRecordUri { get; set; }

    public string? FactSeverity { get; set; }

    public string? ShiftCode { get; set; }

    public string? MachineCode { get; set; }

    public string? LineCode { get; set; }

    public string? WorkOrderCode { get; set; }

    public string? MaterialCode { get; set; }

    public string? BatchOrLot { get; set; }

    public string? AlarmCode { get; set; }

    public string? MeasurementName { get; set; }

    public decimal? MeasurementValue { get; set; }

    public string? MeasurementUnit { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    public string? CapturedByUserId { get; set; }
}
