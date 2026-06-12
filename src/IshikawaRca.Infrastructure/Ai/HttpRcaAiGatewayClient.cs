using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Ai;

public class HttpRcaAiGatewayClient : IRcaAiGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly RcaAiGatewayOptions _options;

    public HttpRcaAiGatewayClient(HttpClient httpClient, IOptions<RcaAiGatewayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiCauseSuggestionResultDto>("/ai/rca/suggest-causes", context, cancellationToken);
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiActionSuggestionResultDto>("/ai/rca/suggest-actions", context, cancellationToken);
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiSummaryResultDto>("/ai/rca/summarize", context, cancellationToken);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string path, RcaAiContextDto context, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ApiResult<T>.Fail(
                "AI Gateway BaseUrl no es valido.",
                new ApiError
                {
                    Code = "AI_GATEWAY_CONFIGURATION_INVALID",
                    Message = "AiGateway:BaseUrl debe ser una URL absoluta.",
                    Field = "AiGateway.BaseUrl"
                });
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, path))
        {
            Content = JsonContent.Create(context)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeout.Token);
        }
        catch (HttpRequestException)
        {
            return GatewayUnavailable<T>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GatewayUnavailable<T>();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return GatewayUnavailable<T>($"AI Gateway respondio HTTP {(int)response.StatusCode}.");
            }

            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: timeout.Token);
                return data is null
                    ? ApiResult<T>.Fail(
                        "AI Gateway devolvio una respuesta vacia.",
                        new ApiError
                        {
                            Code = "AI_GATEWAY_INVALID_RESPONSE",
                            Message = "Respuesta IA vacia.",
                            Field = "AiGateway"
                        })
                    : ApiResult<T>.Ok(data);
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail(
                    "AI Gateway devolvio JSON invalido.",
                    new ApiError
                    {
                        Code = "AI_GATEWAY_INVALID_RESPONSE",
                        Message = "Respuesta IA invalida.",
                        Field = "AiGateway"
                    });
            }
        }
    }

    private static ApiResult<T> GatewayUnavailable<T>(string message = "AI Gateway no esta disponible.")
    {
        return ApiResult<T>.Fail(
            message,
            new ApiError
            {
                Code = "AI_GATEWAY_UNAVAILABLE",
                Message = "AI Gateway no esta disponible.",
                Field = "AiGateway"
            });
    }
}
