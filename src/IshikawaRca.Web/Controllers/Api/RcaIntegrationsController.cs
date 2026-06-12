using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/integrations/rca")]
public class RcaIntegrationsController : ControllerBase
{
    private static readonly JsonSerializerOptions ServerSentEventJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaOutboxService _rcaOutboxService;
    private readonly IRcaOutboxPublisher _rcaOutboxPublisher;

    public RcaIntegrationsController(
        IRcaIncidentService rcaIncidentService,
        IRcaOutboxService rcaOutboxService,
        IRcaOutboxPublisher rcaOutboxPublisher)
    {
        _rcaIncidentService = rcaIncidentService;
        _rcaOutboxService = rcaOutboxService;
        _rcaOutboxPublisher = rcaOutboxPublisher;
    }

    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>>> ListSnapshots(
        [FromQuery] string? sourceSystem,
        [FromQuery] string? externalTaskId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListIntegrationSnapshotsAsync(sourceSystem, externalTaskId, status, cancellationToken);

        return Ok(result);
    }

    [HttpGet("incidents/{id:guid}/snapshot")]
    [ProducesResponseType(typeof(ApiResult<RcaIntegrationSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIntegrationSnapshotDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIntegrationSnapshotDto>>> GetSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetIntegrationSnapshotAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaDomainEventDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaDomainEventDto>>>> ListEvents(
        [FromQuery] Guid? incidentId,
        [FromQuery] DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListIntegrationEventsAsync(incidentId, since, cancellationToken);

        return Ok(result);
    }

    [HttpGet("events/live")]
    [Produces("text/event-stream")]
    public async Task<IActionResult> StreamEvents(
        [FromQuery] Guid? incidentId,
        [FromQuery] DateTimeOffset? since,
        [FromQuery] int pollIntervalSeconds = 5,
        [FromQuery] int? maxBatches = null,
        CancellationToken cancellationToken = default)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var cursor = since;
        var interval = TimeSpan.FromSeconds(Math.Clamp(pollIntervalSeconds, 1, 30));
        var batchLimit = maxBatches.HasValue
            ? Math.Clamp(maxBatches.Value, 1, 100)
            : int.MaxValue;

        try
        {
            for (var batch = 0; batch < batchLimit && !cancellationToken.IsCancellationRequested; batch++)
            {
                var result = await _rcaIncidentService.ListIntegrationEventsAsync(incidentId, cursor, cancellationToken);
                var events = result.Data?
                    .OrderBy(x => x.OccurredAt)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .ToList() ?? [];

                foreach (var integrationEvent in events)
                {
                    await Response.WriteAsync($"id: {integrationEvent.Id}\n", cancellationToken);
                    await Response.WriteAsync($"event: {integrationEvent.Type}\n", cancellationToken);
                    await Response.WriteAsync(
                        $"data: {JsonSerializer.Serialize(integrationEvent, ServerSentEventJsonOptions)}\n\n",
                        cancellationToken);
                }

                if (events.Count > 0)
                {
                    cursor = events.Max(x => x.OccurredAt).AddTicks(1);
                }

                await Response.Body.FlushAsync(cancellationToken);

                if (batch + 1 < batchLimit)
                {
                    await Task.Delay(interval, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        return new EmptyResult();
    }

    [HttpGet("outbox/status")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaOutboxStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<RcaOutboxStatusDto>>> GetOutboxStatus(CancellationToken cancellationToken)
    {
        var status = await _rcaOutboxService.GetStatusAsync(cancellationToken);

        return Ok(ApiResult<RcaOutboxStatusDto>.Ok(status));
    }

    [HttpGet("outbox/dead-letter")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaOutboxEventDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaOutboxEventDto>>>> ListOutboxDeadLetters(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var events = await _rcaOutboxService.ListDeadLettersAsync(take, cancellationToken);

        return Ok(ApiResult<IReadOnlyList<RcaOutboxEventDto>>.Ok(events));
    }

    [HttpPost("outbox/publish")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaOutboxPublishResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<RcaOutboxPublishResultDto>>> PublishOutbox(CancellationToken cancellationToken)
    {
        var result = await _rcaOutboxPublisher.PublishPendingAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("outbox/{id:guid}/retry")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaOutboxEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaOutboxEventDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaOutboxEventDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaOutboxEventDto>>> RetryOutboxEvent(
        Guid id,
        RetryRcaOutboxEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rcaOutboxService.ScheduleRetryAsync(id, request, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        return result.Errors.Any(x => x.Code == "OUTBOX_EVENT_NOT_FOUND")
            ? NotFound(result)
            : BadRequest(result);
    }
}
