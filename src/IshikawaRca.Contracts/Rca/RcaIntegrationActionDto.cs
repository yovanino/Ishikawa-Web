namespace IshikawaRca.Contracts.Rca;

public class RcaIntegrationActionDto
{
    public Guid Id { get; set; }

    public Guid? CauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
