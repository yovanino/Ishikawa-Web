namespace IshikawaRca.Contracts.Rca;

public class CreateExternalIntakeRequest
{
    public string ActorType { get; set; } = "Supplier";

    public string? ActorName { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
