using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;

namespace IshikawaRca.Application.Rca;

public interface IRcaOutboxService
{
    Task<RcaOutboxEvent> EnqueueAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RcaOutboxEvent>> ListPendingAsync(int take = 100, CancellationToken cancellationToken = default);

    Task<RcaOutboxStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RcaOutboxEventDto>> ListDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid id, string error, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default);
}
