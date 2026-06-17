using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/v1/rca/incidents/{id:guid}/ai")]
public class RcaAiController : ControllerBase
{
    private readonly IRcaAiAssistantService _aiAssistantService;
    private readonly ICurrentRcaUserContext _currentUserContext;

    public RcaAiController(IRcaAiAssistantService aiAssistantService, ICurrentRcaUserContext currentUserContext)
    {
        _aiAssistantService = aiAssistantService;
        _currentUserContext = currentUserContext;
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

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaAiSuggestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaAiSuggestionDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaAiSuggestionDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaAiSuggestionDto>>>> ListSuggestions(Guid id, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.ListSuggestionsAsync(id, status, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("suggestions/{suggestionId:guid}/accept")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaAiSuggestionDto>>> AcceptSuggestion(Guid id, Guid suggestionId, AcceptRcaAiSuggestionRequest request, CancellationToken cancellationToken)
    {
        request.ReviewedByUserId = _currentUserContext.UserId;
        var result = await _aiAssistantService.AcceptSuggestionAsync(id, suggestionId, request, cancellationToken);
        return ToAiActionResult(result);
    }

    [HttpPost("suggestions/{suggestionId:guid}/reject")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaAiSuggestionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaAiSuggestionDto>>> RejectSuggestion(Guid id, Guid suggestionId, RejectRcaAiSuggestionRequest request, CancellationToken cancellationToken)
    {
        request.ReviewedByUserId = _currentUserContext.UserId;
        var result = await _aiAssistantService.RejectSuggestionAsync(id, suggestionId, request, cancellationToken);
        return ToAiActionResult(result);
    }

    private ActionResult<ApiResult<T>> ToAiActionResult<T>(ApiResult<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors.Any(x => x.Code is "RCA_NOT_FOUND" or "AI_SUGGESTION_NOT_FOUND"))
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
