using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Rca;

public interface IRcaOutboxPublisher
{
    Task<ApiResult<RcaOutboxPublishResultDto>> PublishPendingAsync(CancellationToken cancellationToken = default);
}
