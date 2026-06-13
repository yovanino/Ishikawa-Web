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
            foreach (var suggestion in result.Data.Suggestions)
            {
                await _suggestionStore.SavePendingAsync(
                    contextResult.Data.Incident.TenantId,
                    incidentId,
                    RcaAiSuggestionType.Cause,
                    suggestion.Title,
                    result.Data.Summary,
                    suggestion,
                    result.Data.Metadata,
                    "ai-request",
                    cancellationToken);
            }
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
            foreach (var suggestion in result.Data.Suggestions)
            {
                await _suggestionStore.SavePendingAsync(
                    contextResult.Data.Incident.TenantId,
                    incidentId,
                    RcaAiSuggestionType.Action,
                    suggestion.Title,
                    result.Data.Summary,
                    suggestion,
                    result.Data.Metadata,
                    "ai-request",
                    cancellationToken);
            }
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
            await _suggestionStore.SavePendingAsync(
                contextResult.Data.Incident.TenantId,
                incidentId,
                RcaAiSuggestionType.Summary,
                "Resumen ejecutivo IA",
                result.Data.ExecutiveSummary,
                result.Data,
                result.Data.Metadata,
                "ai-request",
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
            await _suggestionStore.SavePendingAsync(
                contextResult.Data.Incident.TenantId,
                incidentId,
                RcaAiSuggestionType.Recurrence,
                "Deteccion de recurrencia IA",
                result.Data.Rationale,
                result.Data,
                result.Data.Metadata,
                "ai-request",
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
            await _suggestionStore.SavePendingAsync(
                contextResult.Data.Incident.TenantId,
                incidentId,
                RcaAiSuggestionType.EightD,
                "Borrador 8D IA",
                result.Data.ProblemStatement,
                result.Data,
                result.Data.Metadata,
                "ai-request",
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

        var suggestions = await _suggestionStore.ListAsync(incidentId, status, cancellationToken);
        return ApiResult<IReadOnlyList<RcaAiSuggestionDto>>.Ok(suggestions);
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
