using IshikawaRca.Domain.Entities;

namespace IshikawaRca.Application.Rca;

public interface IRcaWebhookSender
{
    Task<RcaWebhookSendResult> SendAsync(RcaWebhookOptions webhook, RcaOutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
