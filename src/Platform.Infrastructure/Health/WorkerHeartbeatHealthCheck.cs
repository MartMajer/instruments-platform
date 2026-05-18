using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure.Operations;

namespace Platform.Infrastructure.Health;

public sealed class WorkerHeartbeatHealthCheck(
    IWorkerHeartbeatStore heartbeatStore,
    IOptions<WorkerHeartbeatReadinessOptions> options)
    : IPlatformHealthCheck
{
    public string Name => "worker_heartbeat";

    public async Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return PlatformHealthCheckResult.Ok(Name);
        }

        try
        {
            var heartbeat = await heartbeatStore.GetLatestHeartbeatAsync(
                currentOptions.ExpectedWorkerName,
                cancellationToken);

            if (heartbeat is null)
            {
                return PlatformHealthCheckResult.Unready(Name);
            }

            var heartbeatAge = DateTimeOffset.UtcNow - heartbeat.LastSeenAt;

            return heartbeatAge > currentOptions.StaleAfter
                ? PlatformHealthCheckResult.Unready(Name)
                : PlatformHealthCheckResult.Ok(Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return PlatformHealthCheckResult.Unready(Name);
        }
    }
}
