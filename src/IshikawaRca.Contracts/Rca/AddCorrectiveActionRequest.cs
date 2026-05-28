namespace IshikawaRca.Contracts.Rca;

public class AddCorrectiveActionRequest
{
    public Guid? CauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AssignedToUserId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
