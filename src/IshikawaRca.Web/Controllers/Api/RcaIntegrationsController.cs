using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/integrations/rca")]
public class RcaIntegrationsController : ControllerBase
{
    private readonly IRcaIncidentService _rcaIncidentService;

    public RcaIntegrationsController(IRcaIncidentService rcaIncidentService)
    {
        _rcaIncidentService = rcaIncidentService;
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
}
