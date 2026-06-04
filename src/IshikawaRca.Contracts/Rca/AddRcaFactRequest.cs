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

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    public string? CapturedByUserId { get; set; }
}
