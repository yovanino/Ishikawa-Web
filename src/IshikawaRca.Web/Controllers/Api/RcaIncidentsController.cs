using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/rca/incidents")]
public class RcaIncidentsController : ControllerBase
{
    private readonly IRcaIncidentService _rcaIncidentService;

    public RcaIncidentsController(IRcaIncidentService rcaIncidentService)
    {
        _rcaIncidentService = rcaIncidentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> Create(CreateRcaIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.CreateAsync(request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaIncidentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaIncidentDto>>>> List(
        [FromQuery] string? sourceSystem,
        [FromQuery] string? externalTaskId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListAsync(sourceSystem, externalTaskId, status, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/canvas")]
    [ProducesResponseType(typeof(ApiResult<IshikawaCanvasDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IshikawaCanvasDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IshikawaCanvasDto>>> GetCanvas(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetCanvasAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/causes")]
    [ProducesResponseType(typeof(ApiResult<IshikawaCauseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<IshikawaCauseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<IshikawaCauseDto>>> AddCause(Guid id, AddIshikawaCauseRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddCauseAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCanvas), new { id }, result);
    }

    [HttpGet("{id:guid}/actions")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<CorrectiveActionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<CorrectiveActionDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CorrectiveActionDto>>>> ListCorrectiveActions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListCorrectiveActionsAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/actions")]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<CorrectiveActionDto>>> AddCorrectiveAction(Guid id, AddCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddCorrectiveActionAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(ListCorrectiveActions), new { id }, result);
    }
}
