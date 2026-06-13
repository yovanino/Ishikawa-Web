using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Ai;

public interface IRcaAiGatewayClient
{
    Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(RcaAiContextDto context, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(RcaAiContextDto context, CancellationToken cancellationToken = default);
}
