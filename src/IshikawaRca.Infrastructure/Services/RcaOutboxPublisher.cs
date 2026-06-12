using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Services;

public class RcaOutboxPublisher : IRcaOutboxPublisher
{
    private readonly IRcaOutboxService _outboxService;
    private readonly IRcaWebhookSender _webhookSender;
    private readonly RcaIntegrationOptions _options;

    public RcaOutboxPublisher(
        IRcaOutboxService outboxService,
        IRcaWebhookSender webhookSender,
        IOptions<RcaIntegrationOptions> options)
    {
        _outboxService = outboxService;
        _webhookSender = webhookSender;
        _options = options.Value;
    }

    public async Task<ApiResult<RcaOutboxPublishResultDto>> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var enabledWebhooks = _options.Webhooks
            .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        var result = new RcaOutboxPublishResultDto
        {
            EnabledWebhookCount = enabledWebhooks.Count
        };

        if (enabledWebhooks.Count == 0)
        {
            return ApiResult<RcaOutboxPublishResultDto>.Ok(result);
        }

        var pendingEvents = await _outboxService.ListPendingAsync(_options.PublishBatchSize, cancellationToken);
        result.AttemptedEventCount = pendingEvents.Count;

        foreach (var pendingEvent in pendingEvents)
        {
            var matchingWebhooks = enabledWebhooks
                .Where(x => x.EventTypes.Count == 0 || x.EventTypes.Contains(pendingEvent.EventType, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (matchingWebhooks.Count == 0)
            {
                continue;
            }

            var allSucceeded = true;
            foreach (var webhook in matchingWebhooks)
            {
                var sendResult = await _webhookSender.SendAsync(webhook, pendingEvent, cancellationToken);
                allSucceeded &= sendResult.Success;
            }

            if (allSucceeded)
            {
                await _outboxService.MarkPublishedAsync(pendingEvent.Id, DateTimeOffset.UtcNow, cancellationToken);
                result.PublishedEventCount++;
            }
        }

        return ApiResult<RcaOutboxPublishResultDto>.Ok(result);
    }
}
