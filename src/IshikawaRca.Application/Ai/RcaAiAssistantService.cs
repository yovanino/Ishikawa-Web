using System.Text.Json;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Ai;

public class RcaAiAssistantService : IRcaAiAssistantService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaAiGatewayClient _aiGatewayClient;
    private readonly IRcaAiSuggestionStore _suggestionStore;

    public RcaAiAssistantService(IRcaIncidentService rcaIncidentService, IRcaAiGatewayClient aiGatewayClient, IRcaAiSuggestionStore suggestionStore)
    {
        _rcaIncidentService = rcaIncidentService;
        _aiGatewayClient = aiGatewayClient;
        _suggestionStore = suggestionStore;
    }

    public async Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiCauseSuggestionResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        var result = await _aiGatewayClient.SuggestCausesAsync(contextResult.Data, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await SavePendingBatchAsync(
                contextResult.Data,
                result.Data.Suggestions.Select(x => new RcaAiPendingSuggestionInput(
                    RcaAiSuggestionType.Cause,
                    x.Title,
                    result.Data.Summary,
                    x,
                    result.Data.Metadata)).ToList(),
                cancellationToken);
        }

        return result;
    }

    public async Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiActionSuggestionResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        var result = await _aiGatewayClient.SuggestActionsAsync(contextResult.Data, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await SavePendingBatchAsync(
                contextResult.Data,
                result.Data.Suggestions.Select(x => new RcaAiPendingSuggestionInput(
                    RcaAiSuggestionType.Action,
                    x.Title,
                    result.Data.Summary,
                    x,
                    result.Data.Metadata)).ToList(),
                cancellationToken);
        }

        return result;
    }

    public async Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiSummaryResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        var result = await _aiGatewayClient.SummarizeAsync(contextResult.Data, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await SavePendingBatchAsync(
                contextResult.Data,
                [new RcaAiPendingSuggestionInput(RcaAiSuggestionType.Summary, "Resumen ejecutivo IA", result.Data.ExecutiveSummary, result.Data, result.Data.Metadata)],
                cancellationToken);
        }

        return result;
    }

    public async Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiRecurrenceResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        var result = await _aiGatewayClient.DetectRecurrenceAsync(contextResult.Data, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await SavePendingBatchAsync(
                contextResult.Data,
                [new RcaAiPendingSuggestionInput(RcaAiSuggestionType.Recurrence, "Deteccion de recurrencia IA", result.Data.Rationale, result.Data, result.Data.Metadata)],
                cancellationToken);
        }

        return result;
    }

    public async Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiEightDDraftResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        var result = await _aiGatewayClient.GenerateEightDDraftAsync(contextResult.Data, cancellationToken);
        if (result.Success && result.Data is not null)
        {
            await SavePendingBatchAsync(
                contextResult.Data,
                [new RcaAiPendingSuggestionInput(RcaAiSuggestionType.EightD, "Borrador 8D IA", result.Data.ProblemStatement, result.Data, result.Data.Metadata)],
                cancellationToken);
        }

        return result;
    }

    public async Task<ApiResult<IReadOnlyList<RcaAiSuggestionDto>>> ListSuggestionsAsync(Guid incidentId, string? status, CancellationToken cancellationToken = default)
    {
        var incidentResult = await _rcaIncidentService.GetByIdAsync(incidentId, cancellationToken);
        if (!incidentResult.Success || incidentResult.Data is null)
        {
            return ApiResult<IReadOnlyList<RcaAiSuggestionDto>>.Fail(incidentResult.Message ?? "No se encontro el incidente RCA.", incidentResult.Errors.ToArray());
        }

        if (!TryParseSuggestionStatus(status, out var parsedStatus))
        {
            return ApiResult<IReadOnlyList<RcaAiSuggestionDto>>.Fail(
                "El estado de sugerencia IA no es valido.",
                new ApiError { Field = nameof(status), Code = "AI_SUGGESTION_STATUS_INVALID", Message = "El estado informado no corresponde a una sugerencia IA." });
        }

        var suggestions = await _suggestionStore.ListAsync(incidentResult.Data.TenantId, incidentId, parsedStatus, cancellationToken);
        return ApiResult<IReadOnlyList<RcaAiSuggestionDto>>.Ok(suggestions);
    }

    public async Task<ApiResult<RcaAiSuggestionDto>> AcceptSuggestionAsync(Guid incidentId, Guid suggestionId, AcceptRcaAiSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        var incidentResult = await _rcaIncidentService.GetByIdAsync(incidentId, cancellationToken);
        if (!incidentResult.Success || incidentResult.Data is null)
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(incidentResult.Message ?? "No se encontro el incidente RCA.", incidentResult.Errors.ToArray());
        }

        var suggestion = await _suggestionStore.GetAsync(incidentResult.Data.TenantId, incidentId, suggestionId, cancellationToken);
        if (suggestion is null)
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(
                "No se encontro la sugerencia IA.",
                new ApiError { Field = nameof(suggestionId), Code = "AI_SUGGESTION_NOT_FOUND", Message = "La sugerencia IA no corresponde al incidente RCA." });
        }

        if (suggestion.Status != RcaAiSuggestionStatus.Pending.ToString())
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(
                "La sugerencia IA ya fue revisada.",
                new ApiError { Field = nameof(suggestionId), Code = "AI_SUGGESTION_NOT_PENDING", Message = "Solo se pueden aceptar sugerencias pendientes." });
        }

        return await _suggestionStore.ExecuteReviewTransactionAsync(async transactionCancellationToken =>
        {
            var applied = await ApplySuggestionAsync(incidentId, suggestion, request, transactionCancellationToken);
            if (!applied.Success || applied.Data is null)
            {
                return ApiResult<RcaAiSuggestionDto>.Fail(applied.Message ?? "No se pudo aplicar la sugerencia IA.", applied.Errors.ToArray());
            }

            var accepted = await _suggestionStore.MarkAcceptedAsync(
                incidentResult.Data.TenantId,
                incidentId,
                suggestionId,
                NormalizeUserId(request.ReviewedByUserId),
                request.ReviewNotes ?? string.Empty,
                applied.Data.EntityType,
                applied.Data.EntityId,
                transactionCancellationToken);

            return accepted is null
                ? SuggestionNotPending()
                : ApiResult<RcaAiSuggestionDto>.Ok(accepted, "Sugerencia IA aceptada.");
        }, cancellationToken);
    }

    public async Task<ApiResult<RcaAiSuggestionDto>> RejectSuggestionAsync(Guid incidentId, Guid suggestionId, RejectRcaAiSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        var incidentResult = await _rcaIncidentService.GetByIdAsync(incidentId, cancellationToken);
        if (!incidentResult.Success || incidentResult.Data is null)
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(incidentResult.Message ?? "No se encontro el incidente RCA.", incidentResult.Errors.ToArray());
        }

        var suggestion = await _suggestionStore.GetAsync(incidentResult.Data.TenantId, incidentId, suggestionId, cancellationToken);
        if (suggestion is null)
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(
                "No se encontro la sugerencia IA.",
                new ApiError { Field = nameof(suggestionId), Code = "AI_SUGGESTION_NOT_FOUND", Message = "La sugerencia IA no corresponde al incidente RCA." });
        }

        if (suggestion.Status != RcaAiSuggestionStatus.Pending.ToString())
        {
            return ApiResult<RcaAiSuggestionDto>.Fail(
                "La sugerencia IA ya fue revisada.",
                new ApiError { Field = nameof(suggestionId), Code = "AI_SUGGESTION_NOT_PENDING", Message = "Solo se pueden rechazar sugerencias pendientes." });
        }

        return await _suggestionStore.ExecuteReviewTransactionAsync(async transactionCancellationToken =>
        {
            var rejected = await _suggestionStore.MarkRejectedAsync(
                incidentResult.Data.TenantId,
                incidentId,
                suggestionId,
                NormalizeUserId(request.ReviewedByUserId),
                request.ReviewNotes ?? string.Empty,
                transactionCancellationToken);

            return rejected is null
                ? SuggestionNotPending()
                : ApiResult<RcaAiSuggestionDto>.Ok(rejected, "Sugerencia IA rechazada.");
        }, cancellationToken);
    }

    private async Task SavePendingBatchAsync(RcaAiContextDto context, IReadOnlyList<RcaAiPendingSuggestionInput> suggestions, CancellationToken cancellationToken)
    {
        if (suggestions.Count == 0)
        {
            return;
        }

        await _suggestionStore.SavePendingBatchAsync(
            context.Incident.TenantId,
            context.Incident.Id,
            suggestions,
            "ai-request",
            cancellationToken);
    }

    private async Task<ApiResult<AppliedAiSuggestion>> ApplySuggestionAsync(Guid incidentId, RcaAiSuggestionDto suggestion, AcceptRcaAiSuggestionRequest request, CancellationToken cancellationToken)
    {
        if (suggestion.SuggestionType == RcaAiSuggestionType.Cause.ToString())
        {
            if (!request.TargetBranchId.HasValue)
            {
                return ApiResult<AppliedAiSuggestion>.Fail(
                    "La sugerencia de causa requiere rama destino.",
                    new ApiError { Field = nameof(request.TargetBranchId), Code = "AI_SUGGESTION_BRANCH_REQUIRED", Message = "Seleccione la rama Ishikawa donde aplicar la causa." });
            }

            var payload = DeserializePayload<RcaAiCauseSuggestionDto>(suggestion.PayloadJson);
            if (payload is null)
            {
                return InvalidPayload();
            }

            var causeResult = await _rcaIncidentService.AddCauseAsync(incidentId, new AddIshikawaCauseRequest
            {
                BranchId = request.TargetBranchId.Value,
                Title = payload.Title,
                Description = payload.Reasoning,
                ProbabilityScore = payload.SuggestedProbabilityScore,
                ImpactScore = payload.SuggestedImpactScore,
                FrequencyScore = payload.SuggestedFrequencyScore,
                EvidenceSummary = suggestion.Summary
            }, cancellationToken);

            return causeResult.Success && causeResult.Data is not null
                ? ApiResult<AppliedAiSuggestion>.Ok(new AppliedAiSuggestion(nameof(IshikawaCauseDto), causeResult.Data.Id))
                : ApiResult<AppliedAiSuggestion>.Fail(causeResult.Message ?? "No se pudo crear la causa desde la sugerencia IA.", causeResult.Errors.ToArray());
        }

        if (suggestion.SuggestionType == RcaAiSuggestionType.Action.ToString())
        {
            var payload = DeserializePayload<RcaAiActionSuggestionDto>(suggestion.PayloadJson);
            if (payload is null)
            {
                return InvalidPayload();
            }

            var actionResult = await _rcaIncidentService.AddCorrectiveActionAsync(incidentId, new AddCorrectiveActionRequest
            {
                Title = payload.Title,
                Description = payload.Description,
                AssignedToUserId = payload.SuggestedOwnerRole,
                DueDate = payload.SuggestedDueDays > 0
                    ? DateTimeOffset.UtcNow.AddDays(payload.SuggestedDueDays)
                    : null
            }, cancellationToken);

            return actionResult.Success && actionResult.Data is not null
                ? ApiResult<AppliedAiSuggestion>.Ok(new AppliedAiSuggestion(nameof(CorrectiveActionDto), actionResult.Data.Id))
                : ApiResult<AppliedAiSuggestion>.Fail(actionResult.Message ?? "No se pudo crear la accion desde la sugerencia IA.", actionResult.Errors.ToArray());
        }

        return ApiResult<AppliedAiSuggestion>.Ok(new AppliedAiSuggestion(string.Empty, null));
    }

    private static bool TryParseSuggestionStatus(string? status, out RcaAiSuggestionStatus? parsedStatus)
    {
        parsedStatus = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<RcaAiSuggestionStatus>(status, true, out var value) ||
            !Enum.IsDefined(value))
        {
            return false;
        }

        parsedStatus = value;
        return true;
    }

    private static T? DeserializePayload<T>(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static ApiResult<AppliedAiSuggestion> InvalidPayload()
    {
        return ApiResult<AppliedAiSuggestion>.Fail(
            "El payload de la sugerencia IA no es valido.",
            new ApiError { Field = nameof(RcaAiSuggestionDto.PayloadJson), Code = "AI_SUGGESTION_PAYLOAD_INVALID", Message = "No se pudo interpretar el payload de la sugerencia IA." });
    }

    private static ApiResult<RcaAiSuggestionDto> SuggestionNotPending()
    {
        return ApiResult<RcaAiSuggestionDto>.Fail(
            "La sugerencia IA ya fue revisada.",
            new ApiError { Code = "AI_SUGGESTION_NOT_PENDING", Message = "Solo se pueden revisar sugerencias pendientes." });
    }

    private static string NormalizeUserId(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "ai-review"
            : value.Trim();
    }

    private sealed record AppliedAiSuggestion(string EntityType, Guid? EntityId);

    private async Task<ApiResult<RcaAiContextDto>> BuildContextAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var incidentResult = await _rcaIncidentService.GetByIdAsync(incidentId, cancellationToken);
        if (!incidentResult.Success || incidentResult.Data is null)
        {
            return ApiResult<RcaAiContextDto>.Fail(incidentResult.Message ?? "No se encontro el incidente RCA.", incidentResult.Errors.ToArray());
        }

        var canvasResult = await _rcaIncidentService.GetCanvasAsync(incidentId, cancellationToken);
        if (!canvasResult.Success || canvasResult.Data is null)
        {
            return ApiResult<RcaAiContextDto>.Fail(canvasResult.Message ?? "No se encontro el canvas Ishikawa.", canvasResult.Errors.ToArray());
        }

        var actionsResult = await _rcaIncidentService.ListCorrectiveActionsAsync(incidentId, cancellationToken);
        if (!actionsResult.Success)
        {
            return ApiResult<RcaAiContextDto>.Fail(actionsResult.Message ?? "No se encontraron acciones correctivas.", actionsResult.Errors.ToArray());
        }

        return ApiResult<RcaAiContextDto>.Ok(new RcaAiContextDto
        {
            Incident = incidentResult.Data,
            Canvas = canvasResult.Data,
            CorrectiveActions = actionsResult.Data ?? []
        });
    }
}
