using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Ai;

public class RcaAiAssistantService : IRcaAiAssistantService
{
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
