using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Ai;

public class RcaAiAssistantService : IRcaAiAssistantService
{
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaAiGatewayClient _aiGatewayClient;

    public RcaAiAssistantService(IRcaIncidentService rcaIncidentService, IRcaAiGatewayClient aiGatewayClient)
    {
        _rcaIncidentService = rcaIncidentService;
        _aiGatewayClient = aiGatewayClient;
    }

    public async Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiCauseSuggestionResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        return await _aiGatewayClient.SuggestCausesAsync(contextResult.Data, cancellationToken);
    }

    public async Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiActionSuggestionResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        return await _aiGatewayClient.SuggestActionsAsync(contextResult.Data, cancellationToken);
    }

    public async Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var contextResult = await BuildContextAsync(incidentId, cancellationToken);
        if (!contextResult.Success || contextResult.Data is null)
        {
            return ApiResult<RcaAiSummaryResultDto>.Fail(contextResult.Message ?? "No se pudo armar el contexto RCA.", contextResult.Errors.ToArray());
        }

        return await _aiGatewayClient.SummarizeAsync(contextResult.Data, cancellationToken);
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
