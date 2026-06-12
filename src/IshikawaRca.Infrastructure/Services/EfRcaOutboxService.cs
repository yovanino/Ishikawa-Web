using System.Text.Json;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
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

    public async Task<RcaOutboxStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.RcaOutboxEvents
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                x.Status,
                x.CreatedAt,
                x.NextAttemptAt,
                x.LastAttemptAt,
                x.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var pendingRows = rows
            .Where(x => x.Status is RcaOutboxEventStatus.Pending or RcaOutboxEventStatus.Failed)
            .ToList();

        return new RcaOutboxStatusDto
        {
            TotalEvents = rows.Count,
            PendingCount = rows.Count(x => x.Status == RcaOutboxEventStatus.Pending),
            PublishingCount = rows.Count(x => x.Status == RcaOutboxEventStatus.Publishing),
            PublishedCount = rows.Count(x => x.Status == RcaOutboxEventStatus.Published),
            FailedCount = rows.Count(x => x.Status == RcaOutboxEventStatus.Failed),
            DeadLetterCount = rows.Count(x => x.Status == RcaOutboxEventStatus.DeadLetter),
            OldestPendingAt = pendingRows
                .OrderBy(x => x.CreatedAt)
                .Select(x => (DateTimeOffset?)x.CreatedAt)
                .FirstOrDefault(),
            NextAttemptAt = pendingRows
                .Where(x => x.NextAttemptAt.HasValue)
                .OrderBy(x => x.NextAttemptAt)
                .Select(x => x.NextAttemptAt)
                .FirstOrDefault(),
            LastAttemptAt = rows
                .Where(x => x.LastAttemptAt.HasValue)
                .OrderByDescending(x => x.LastAttemptAt)
                .Select(x => x.LastAttemptAt)
                .FirstOrDefault(),
            LastPublishedAt = rows
                .Where(x => x.PublishedAt.HasValue)
                .OrderByDescending(x => x.PublishedAt)
                .Select(x => x.PublishedAt)
                .FirstOrDefault()
        };
    }

    public async Task<IReadOnlyList<RcaOutboxEventDto>> ListDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var boundedTake = Math.Clamp(take, 1, 500);

        return await _dbContext.RcaOutboxEvents
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == RcaOutboxEventStatus.DeadLetter)
            .OrderByDescending(x => x.LastAttemptAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(boundedTake)
            .Select(x => new RcaOutboxEventDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                EventId = x.EventId,
                EventType = x.EventType,
                OccurredAt = x.OccurredAt,
                IncidentId = x.IncidentId,
                SourceSystem = x.SourceSystem,
                ExternalTaskId = x.ExternalTaskId,
                ExternalEventId = x.ExternalEventId,
                ExternalWorkOrderId = x.ExternalWorkOrderId,
                Status = x.Status.ToString(),
                AttemptCount = x.AttemptCount,
                NextAttemptAt = x.NextAttemptAt,
                LastAttemptAt = x.LastAttemptAt,
                PublishedAt = x.PublishedAt,
                LastError = x.LastError
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiResult<RcaOutboxEventDto>> ScheduleRetryAsync(
        Guid id,
        RetryRcaOutboxEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outboxEvent = await _dbContext.RcaOutboxEvents
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (outboxEvent is null)
        {
            return ApiResult<RcaOutboxEventDto>.Fail(
                "No se encontro el evento outbox RCA.",
                new ApiError
                {
                    Field = nameof(id),
                    Code = "OUTBOX_EVENT_NOT_FOUND",
                    Message = "No se encontro el evento outbox RCA."
                });
        }

        if (outboxEvent.Status is not (RcaOutboxEventStatus.Failed or RcaOutboxEventStatus.DeadLetter))
        {
            return ApiResult<RcaOutboxEventDto>.Fail(
                "Solo se pueden reprogramar eventos outbox fallidos o en dead-letter.",
                new ApiError
                {
                    Field = nameof(outboxEvent.Status),
                    Code = "OUTBOX_EVENT_NOT_RETRYABLE",
                    Message = "Solo se pueden reprogramar eventos outbox fallidos o en dead-letter."
                });
        }

        outboxEvent.Status = RcaOutboxEventStatus.Pending;
        outboxEvent.NextAttemptAt = request.NextAttemptAt ?? DateTimeOffset.UtcNow;
        outboxEvent.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaOutboxEventDto>.Ok(ToDto(outboxEvent));
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

    public async Task MarkDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var outboxEvent = await GetRequiredAsync(id, cancellationToken);

        outboxEvent.Status = RcaOutboxEventStatus.DeadLetter;
        outboxEvent.AttemptCount++;
        outboxEvent.LastAttemptAt = DateTimeOffset.UtcNow;
        outboxEvent.NextAttemptAt = null;
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

    private static RcaOutboxEventDto ToDto(RcaOutboxEvent outboxEvent)
    {
        return new RcaOutboxEventDto
        {
            Id = outboxEvent.Id,
            TenantId = outboxEvent.TenantId,
            EventId = outboxEvent.EventId,
            EventType = outboxEvent.EventType,
            OccurredAt = outboxEvent.OccurredAt,
            IncidentId = outboxEvent.IncidentId,
            SourceSystem = outboxEvent.SourceSystem,
            ExternalTaskId = outboxEvent.ExternalTaskId,
            ExternalEventId = outboxEvent.ExternalEventId,
            ExternalWorkOrderId = outboxEvent.ExternalWorkOrderId,
            Status = outboxEvent.Status.ToString(),
            AttemptCount = outboxEvent.AttemptCount,
            NextAttemptAt = outboxEvent.NextAttemptAt,
            LastAttemptAt = outboxEvent.LastAttemptAt,
            PublishedAt = outboxEvent.PublishedAt,
            LastError = outboxEvent.LastError
        };
    }
}
