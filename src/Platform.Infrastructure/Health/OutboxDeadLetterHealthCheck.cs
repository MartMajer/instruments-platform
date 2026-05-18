using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;

namespace Platform.Infrastructure.Health;

public sealed class OutboxDeadLetterHealthCheck(IOutboxOperationalSnapshotStore snapshotStore) : IPlatformHealthCheck
{
    public string Name => "outbox_dead_letters";

    public async Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await snapshotStore.GetSnapshotAsync(cancellationToken);

            return snapshot.DeadLetterCount > 0
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
