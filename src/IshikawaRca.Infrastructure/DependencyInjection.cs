using IshikawaRca.Application.Ai;
using IshikawaRca.Application.Rca;
using IshikawaRca.Infrastructure.Ai;
using IshikawaRca.Infrastructure.Data;
using IshikawaRca.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IshikawaRca.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MySqlMaxRetryDelay = TimeSpan.FromSeconds(2);

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
        services.AddScoped<IRcaAiAssistantService, RcaAiAssistantService>();
        services.AddScoped<IRcaAiGatewayClient, StubRcaAiGatewayClient>();

        return services;
    }
}
