using IshikawaRca.Application.Ai;
using IshikawaRca.Application.Rca;
using IshikawaRca.Infrastructure.Ai;
using IshikawaRca.Infrastructure.Data;
using IshikawaRca.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MySqlMaxRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly HttpClient SharedAiGatewayHttpClient = new();

    public static IServiceCollection AddIshikawaRcaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IshikawaRca");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:IshikawaRca is required for MySQL persistence.");
        }

        services.AddDbContext<RcaDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 36)),
                mysqlOptions => mysqlOptions
                    .EnableRetryOnFailure(3, MySqlMaxRetryDelay, null)
                    .CommandTimeout(15)));

        services.AddScoped<IRcaIncidentService, EfRcaIncidentService>();
        services.AddScoped<IRcaExternalIntakeService, EfRcaExternalIntakeService>();
        services.AddScoped<IRcaOutboxService, EfRcaOutboxService>();
        services.AddScoped<IRcaOutboxPublisher, RcaOutboxPublisher>();
        services.AddScoped<IRcaWebhookSender>(provider => new RcaHttpWebhookSender(
            new HttpClient(),
            provider.GetRequiredService<IOptions<RcaIntegrationOptions>>()));
        services.AddScoped<IRcaAiAssistantService, RcaAiAssistantService>();
        services.AddScoped<StubRcaAiGatewayClient>();
        services.AddScoped<HttpRcaAiGatewayClient>(provider => new HttpRcaAiGatewayClient(
            SharedAiGatewayHttpClient,
            provider.GetRequiredService<IOptions<RcaAiGatewayOptions>>()));
        services.AddScoped<IRcaAiGatewayClient>(provider => new ConfiguredRcaAiGatewayClient(
            provider.GetRequiredService<HttpRcaAiGatewayClient>(),
            provider.GetRequiredService<StubRcaAiGatewayClient>(),
            provider.GetRequiredService<IOptions<RcaAiGatewayOptions>>()));
        services.Configure<RcaAiGatewayOptions>(options =>
        {
            var section = configuration.GetSection(RcaAiGatewayOptions.SectionName);

            options.Mode = section["Mode"] ?? options.Mode;
            options.BaseUrl = section["BaseUrl"] ?? options.BaseUrl;
            options.ApiKey = section["ApiKey"] ?? options.ApiKey;
            options.TimeoutSeconds = ReadInt(section["TimeoutSeconds"], options.TimeoutSeconds);
            options.UseFallbackOnFailure = ReadBool(section["UseFallbackOnFailure"], options.UseFallbackOnFailure);
        });
        services.Configure<RcaIntegrationOptions>(options =>
        {
            var section = configuration.GetSection(RcaIntegrationOptions.SectionName);

            options.PublishBatchSize = ReadInt(section["PublishBatchSize"], options.PublishBatchSize);
            options.MaxPublishAttempts = ReadInt(section["MaxPublishAttempts"], options.MaxPublishAttempts);
            options.PublishTimeoutSeconds = ReadInt(section["PublishTimeoutSeconds"], options.PublishTimeoutSeconds);
            options.Webhooks = section.GetSection("Webhooks")
                .GetChildren()
                .Select(webhook => new RcaWebhookOptions
                {
                    Name = webhook["Name"] ?? string.Empty,
                    Url = webhook["Url"] ?? string.Empty,
                    Enabled = bool.TryParse(webhook["Enabled"], out var enabled) && enabled,
                    Secret = webhook["Secret"] ?? string.Empty,
                    EventTypes = webhook.GetSection("EventTypes")
                        .GetChildren()
                        .Select(x => x.Value)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x!)
                        .ToList()
                })
                .ToList();
        });

        return services;
    }

    private static int ReadInt(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    private static bool ReadBool(string? value, bool fallback)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
