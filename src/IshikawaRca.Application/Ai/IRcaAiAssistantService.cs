using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Ai;

public interface IRcaAiAssistantService
{
    Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
