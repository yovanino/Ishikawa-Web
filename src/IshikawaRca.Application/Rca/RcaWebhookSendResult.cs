namespace IshikawaRca.Application.Rca;

public class RcaWebhookSendResult
{
    private RcaWebhookSendResult(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public bool Success { get; }
    public string? Error { get; }

    public static RcaWebhookSendResult Succeeded()
    {
        return new RcaWebhookSendResult(true, null);
    }

    public static RcaWebhookSendResult Failed(string error)
    {
        return new RcaWebhookSendResult(false, error);
    }
}
