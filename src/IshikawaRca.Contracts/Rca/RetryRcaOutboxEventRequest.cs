namespace IshikawaRca.Contracts.Rca;

public class RetryRcaOutboxEventRequest
{
    public DateTimeOffset? NextAttemptAt { get; set; }
}
