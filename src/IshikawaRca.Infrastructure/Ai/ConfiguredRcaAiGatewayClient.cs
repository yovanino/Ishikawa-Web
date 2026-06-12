using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Ai;

public class ConfiguredRcaAiGatewayClient : IRcaAiGatewayClient
{
    private readonly IRcaAiGatewayClient _httpClient;
    private readonly IRcaAiGatewayClient _stubClient;
    private readonly RcaAiGatewayOptions _options;

    public ConfiguredRcaAiGatewayClient(
        IRcaAiGatewayClient httpClient,
        IRcaAiGatewayClient stubClient,
        IOptions<RcaAiGatewayOptions> options)
    {
        _httpClient = httpClient;
        _stubClient = stubClient;
        _options = options.Value;
    }

    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            httpClient => httpClient.SuggestCausesAsync(context, cancellationToken),
            stubClient => stubClient.SuggestCausesAsync(context, cancellationToken));
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            httpClient => httpClient.SuggestActionsAsync(context, cancellationToken),
            stubClient => stubClient.SuggestActionsAsync(context, cancellationToken));
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            httpClient => httpClient.SummarizeAsync(context, cancellationToken),
            stubClient => stubClient.SummarizeAsync(context, cancellationToken));
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(
        Func<IRcaAiGatewayClient, Task<ApiResult<T>>> runHttp,
        Func<IRcaAiGatewayClient, Task<ApiResult<T>>> runStub)
    {
        if (!string.Equals(_options.Mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            return await runStub(_stubClient);
        }

        ApiResult<T> result;
        try
        {
            result = await runHttp(_httpClient);
        }
        catch (HttpRequestException)
        {
            result = GatewayUnavailable<T>();
        }

        if (result.Success || !_options.UseFallbackOnFailure)
        {
            return result;
        }

        return await runStub(_stubClient);
    }

    private static ApiResult<T> GatewayUnavailable<T>()
    {
        return ApiResult<T>.Fail(
            "AI Gateway no esta disponible.",
            new ApiError
            {
                Code = "AI_GATEWAY_UNAVAILABLE",
                Message = "AI Gateway no esta disponible.",
                Field = "AiGateway"
            });
    }
}
