using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace IshikawaRca.Infrastructure.Data;

public class DesignTimeRcaDbContextFactory : IDesignTimeDbContextFactory<RcaDbContext>
{
    private static readonly TimeSpan MySqlMaxRetryDelay = TimeSpan.FromSeconds(2);

    public RcaDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<RcaDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)),
            mysqlOptions => mysqlOptions
                .EnableRetryOnFailure(3, MySqlMaxRetryDelay, null)
                .CommandTimeout(15));

        return new RcaDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ISHIKAWA_RCA_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var localSettingsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "src",
            "IshikawaRca.Web",
            "appsettings.Local.json");

        var localConnectionString = TryReadConnectionString(localSettingsPath);
        if (!string.IsNullOrWhiteSpace(localConnectionString))
        {
            return localConnectionString;
        }

        var defaultSettingsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "src",
            "IshikawaRca.Web",
            "appsettings.json");

        var defaultConnectionString = TryReadConnectionString(defaultSettingsPath);
        if (!string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            return defaultConnectionString;
        }

        return "Server=localhost;Port=3306;Database=ishikawa_rca;User=ishikawa_user;Password=change_me;TreatTinyAsBoolean=true;SslMode=None;AllowPublicKeyRetrieval=True;Connection Timeout=5;Default Command Timeout=15;";
    }

    private static string? TryReadConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        return connectionStrings.TryGetProperty("IshikawaRca", out var connectionString)
            ? connectionString.GetString()
            : null;
    }
}
