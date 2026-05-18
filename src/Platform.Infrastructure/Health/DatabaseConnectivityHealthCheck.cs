using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Health;

public sealed class DatabaseConnectivityHealthCheck(ApplicationDbContext dbContext) : IPlatformHealthCheck
{
    public string Name => "database";

    public async Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? PlatformHealthCheckResult.Ok(Name)
                : PlatformHealthCheckResult.Unready(Name);
        }
        catch
        {
            return PlatformHealthCheckResult.Unready(Name);
        }
    }
}
