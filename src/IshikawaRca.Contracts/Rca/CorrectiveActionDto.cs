namespace IshikawaRca.Contracts.Rca;

public class CorrectiveActionDto
{
    public Guid Id { get; set; }

    public Guid RcaIncidentId { get; set; }

    public Guid? CauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ResolutionScope { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? ValidationNotes { get; set; }
}
