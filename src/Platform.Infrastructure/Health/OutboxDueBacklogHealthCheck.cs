using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure.Operations;

namespace Platform.Infrastructure.Health;

public sealed class OutboxDueBacklogHealthCheck(
    IOutboxOperationalSnapshotStore snapshotStore,
    IOptions<OutboxOperationalReadinessOptions> options)
    : IPlatformHealthCheck
{
    public string Name => "outbox_due_backlog";

    public async Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await snapshotStore.GetSnapshotAsync(cancellationToken);
            if (snapshot.OldestDueCreatedAt is null)
            {
                return PlatformHealthCheckResult.Ok(Name);
            }

            var dueAge = DateTimeOffset.UtcNow - snapshot.OldestDueCreatedAt.Value;

            return dueAge > options.Value.DueBacklogUnreadyAfter
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
