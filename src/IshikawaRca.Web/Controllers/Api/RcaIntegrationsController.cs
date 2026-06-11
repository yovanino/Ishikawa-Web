using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/integrations/rca")]
public class RcaIntegrationsController : ControllerBase
{
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaOutboxService _rcaOutboxService;

    public RcaIntegrationsController(IRcaIncidentService rcaIncidentService, IRcaOutboxService rcaOutboxService)
    {
        _rcaIncidentService = rcaIncidentService;
        _rcaOutboxService = rcaOutboxService;
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
}
