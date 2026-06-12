namespace IshikawaRca.Contracts.Rca;

public class RcaOutboxPublishResultDto
{
    public int EnabledWebhookCount { get; set; }
    public int AttemptedEventCount { get; set; }
    public int PublishedEventCount { get; set; }
    public int FailedEventCount { get; set; }
    public int DeadLetterEventCount { get; set; }
}
