using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/rca/incidents/{id:guid}/ai")]
public class RcaAiController : ControllerBase
{
    private readonly IRcaAiAssistantService _aiAssistantService;

    public RcaAiController(IRcaAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    [HttpPost("suggest-causes")]
    [ProducesResponseType(typeof(ApiResult<RcaAiCauseSuggestionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiCauseSuggestionResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<RcaAiCauseSuggestionResultDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResult<RcaAiCauseSuggestionResultDto>>> SuggestCauses(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.SuggestCausesAsync(id, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("suggest-actions")]
    [ProducesResponseType(typeof(ApiResult<RcaAiActionSuggestionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiActionSuggestionResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<RcaAiActionSuggestionResultDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResult<RcaAiActionSuggestionResultDto>>> SuggestActions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.SuggestActionsAsync(id, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("summarize")]
    [ProducesResponseType(typeof(ApiResult<RcaAiSummaryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSummaryResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSummaryResultDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResult<RcaAiSummaryResultDto>>> Summarize(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.SummarizeAsync(id, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("detect-recurrence")]
    [ProducesResponseType(typeof(ApiResult<RcaAiRecurrenceResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiRecurrenceResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<RcaAiRecurrenceResultDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResult<RcaAiRecurrenceResultDto>>> DetectRecurrence(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.DetectRecurrenceAsync(id, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("generate-8d-draft")]
    [ProducesResponseType(typeof(ApiResult<RcaAiEightDDraftResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiEightDDraftResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<RcaAiEightDDraftResultDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResult<RcaAiEightDDraftResultDto>>> GenerateEightDDraft(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.GenerateEightDDraftAsync(id, cancellationToken);
        return ToAiActionResult(result);
    }

    private ActionResult<ApiResult<T>> ToAiActionResult<T>(ApiResult<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors.Any(x => x.Code == "RCA_NOT_FOUND"))
        {
            return NotFound(result);
        }

        if (result.Errors.Any(x => x.Code.StartsWith("AI_GATEWAY_", StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        return BadRequest(result);
    }
}
