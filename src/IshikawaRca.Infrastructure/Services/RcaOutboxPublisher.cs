using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Services;

public class RcaOutboxPublisher : IRcaOutboxPublisher
{
    private readonly IRcaOutboxService _outboxService;
    private readonly RcaIntegrationOptions _options;

    public RcaOutboxPublisher(IRcaOutboxService outboxService, IOptions<RcaIntegrationOptions> options)
    {
        _outboxService = outboxService;
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

        return ApiResult<RcaOutboxPublishResultDto>.Ok(result);
    }
}
