namespace IshikawaRca.Contracts.Rca;

public class UpdateCorrectiveActionStatusRequest
{
    public string Status { get; set; } = string.Empty;

    public string? CompletedByUserId { get; set; }

    public string? ValidationNotes { get; set; }
}
