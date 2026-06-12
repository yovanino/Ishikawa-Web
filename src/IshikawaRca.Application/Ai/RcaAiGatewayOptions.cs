namespace IshikawaRca.Application.Ai;

public class RcaAiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public string Mode { get; set; } = "Stub";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseFallbackOnFailure { get; set; } = true;
}
