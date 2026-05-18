using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Platform.Infrastructure.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public const string DesignTimeConnectionStringEnvironmentVariable = "PLATFORM_DESIGN_TIME_CONNECTION";

    private const string DefaultDesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=instruments_platform;Username=platform_migrator;Password=platform_migrator";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(DesignTimeConnectionStringEnvironmentVariable)
            ?? DefaultDesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
