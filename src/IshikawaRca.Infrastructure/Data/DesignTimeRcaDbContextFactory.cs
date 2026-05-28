using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IshikawaRca.Infrastructure.Data;

public class DesignTimeRcaDbContextFactory : IDesignTimeDbContextFactory<RcaDbContext>
{
    public RcaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ISHIKAWA_RCA_CONNECTION")
            ?? "Server=localhost;Port=3306;Database=ishikawa_rca;User=ishikawa_user;Password=change_me;TreatTinyAsBoolean=true;SslMode=None;";

        var optionsBuilder = new DbContextOptionsBuilder<RcaDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)),
            mysqlOptions => mysqlOptions.EnableRetryOnFailure());

        return new RcaDbContext(optionsBuilder.Options);
    }
}
