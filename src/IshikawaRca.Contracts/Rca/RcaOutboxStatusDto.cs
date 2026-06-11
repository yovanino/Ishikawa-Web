namespace IshikawaRca.Contracts.Rca;

public class RcaOutboxStatusDto
{
    public int TotalEvents { get; set; }

    public int PendingCount { get; set; }

    public int PublishingCount { get; set; }

    public int PublishedCount { get; set; }

    public int FailedCount { get; set; }

    public int DeadLetterCount { get; set; }

    public DateTimeOffset? OldestPendingAt { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? LastPublishedAt { get; set; }
}
