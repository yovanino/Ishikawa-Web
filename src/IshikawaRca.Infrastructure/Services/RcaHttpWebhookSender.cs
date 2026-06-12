using System.Text;
using System.Security.Cryptography;
using IshikawaRca.Application.Rca;
using IshikawaRca.Domain.Entities;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Services;

public class RcaHttpWebhookSender : IRcaWebhookSender
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _publishTimeout;

    public RcaHttpWebhookSender(HttpClient httpClient)
        : this(httpClient, Options.Create(new RcaIntegrationOptions()))
    {
    }

    public RcaHttpWebhookSender(HttpClient httpClient, IOptions<RcaIntegrationOptions> options)
    {
        _httpClient = httpClient;
        var timeoutSeconds = options.Value.PublishTimeoutSeconds > 0
            ? options.Value.PublishTimeoutSeconds
            : new RcaIntegrationOptions().PublishTimeoutSeconds;
        _publishTimeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<RcaWebhookSendResult> SendAsync(
        RcaWebhookOptions webhook,
        RcaOutboxEvent outboxEvent,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(webhook.Url, UriKind.Absolute, out var uri))
        {
            return RcaWebhookSendResult.Failed("Webhook URL is not valid.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(outboxEvent.PayloadJson, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("X-RCA-Event-Id", outboxEvent.EventId);
        request.Headers.TryAddWithoutValidation("X-RCA-Event-Type", outboxEvent.EventType);
        request.Headers.TryAddWithoutValidation("X-RCA-Outbox-Id", outboxEvent.Id.ToString());

        if (!string.IsNullOrWhiteSpace(webhook.Secret))
        {
            var signature = Convert.ToHexString(
                HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes(webhook.Secret),
                    Encoding.UTF8.GetBytes(outboxEvent.PayloadJson)))
                .ToLowerInvariant();
            request.Headers.TryAddWithoutValidation("X-RCA-Signature", "sha256=" + signature);
        }

        using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(_publishTimeout);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeoutTokenSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RcaWebhookSendResult.Failed("Webhook delivery timed out.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return RcaWebhookSendResult.Succeeded();
            }

            return RcaWebhookSendResult.Failed($"Webhook responded with HTTP {(int)response.StatusCode}.");
        }
    }
}
