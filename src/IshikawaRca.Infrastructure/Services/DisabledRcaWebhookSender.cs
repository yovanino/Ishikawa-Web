using IshikawaRca.Application.Rca;
using IshikawaRca.Domain.Entities;

namespace IshikawaRca.Infrastructure.Services;

public class DisabledRcaWebhookSender : IRcaWebhookSender
{
    public Task<RcaWebhookSendResult> SendAsync(RcaWebhookOptions webhook, RcaOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RcaWebhookSendResult.Failed("Webhook HTTP delivery is not configured yet."));
    }
}
