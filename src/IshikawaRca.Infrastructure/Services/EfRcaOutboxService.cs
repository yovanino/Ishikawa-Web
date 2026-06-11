using System.Text.Json;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaOutboxService : IRcaOutboxService
{
    private const int MaxTake = 500;
    private const int MaxErrorLength = 2000;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RcaDbContext _dbContext;

    public EfRcaOutboxService(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RcaOutboxEvent> EnqueueAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var existing = await _dbContext.RcaOutboxEvents
            .FirstOrDefaultAsync(
                x => x.TenantId == integrationEvent.TenantId &&
                    x.EventId == integrationEvent.Id &&
                    !x.IsDeleted,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var outboxEvent = new RcaOutboxEvent
        {
            TenantId = integrationEvent.TenantId,
            EventId = integrationEvent.Id,
            EventType = integrationEvent.Type,
            OccurredAt = integrationEvent.OccurredAt,
            IncidentId = integrationEvent.IncidentId,
            SourceSystem = integrationEvent.SourceSystem,
            ExternalTaskId = integrationEvent.ExternalTaskId,
            ExternalEventId = integrationEvent.ExternalEventId,
            ExternalWorkOrderId = integrationEvent.ExternalWorkOrderId,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            Status = RcaOutboxEventStatus.Pending
        };

        _dbContext.RcaOutboxEvents.Add(outboxEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return outboxEvent;
    }

    public async Task<IReadOnlyList<RcaOutboxEvent>> ListPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var boundedTake = Math.Clamp(take, 1, MaxTake);

        return await _dbContext.RcaOutboxEvents
            .Where(x => !x.IsDeleted &&
                (x.Status == RcaOutboxEventStatus.Pending || x.Status == RcaOutboxEventStatus.Failed) &&
                (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= now))
            .OrderBy(x => x.NextAttemptAt ?? x.CreatedAt)
            .ThenBy(x => x.CreatedAt)
            .Take(boundedTake)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        var outboxEvent = await GetRequiredAsync(id, cancellationToken);

        outboxEvent.Status = RcaOutboxEventStatus.Published;
        outboxEvent.PublishedAt = publishedAt;
        outboxEvent.LastAttemptAt = publishedAt;
        outboxEvent.NextAttemptAt = null;
        outboxEvent.LastError = null;
        outboxEvent.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default)
    {
        var outboxEvent = await GetRequiredAsync(id, cancellationToken);

        outboxEvent.Status = RcaOutboxEventStatus.Failed;
        outboxEvent.AttemptCount++;
        outboxEvent.LastAttemptAt = DateTimeOffset.UtcNow;
        outboxEvent.NextAttemptAt = nextAttemptAt;
        outboxEvent.LastError = Truncate(error, MaxErrorLength);
        outboxEvent.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RcaOutboxEvent> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.RcaOutboxEvents
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el evento outbox RCA.");
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
