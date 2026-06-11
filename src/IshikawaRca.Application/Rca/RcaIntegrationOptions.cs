namespace IshikawaRca.Application.Rca;

public class RcaIntegrationOptions
{
    public const string SectionName = "RcaIntegration";

    public int PublishBatchSize { get; set; } = 50;
    public int MaxPublishAttempts { get; set; } = 5;
    public int PublishTimeoutSeconds { get; set; } = 5;
    public List<RcaWebhookOptions> Webhooks { get; set; } = [];
}

public class RcaWebhookOptions
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Secret { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = [];
}
