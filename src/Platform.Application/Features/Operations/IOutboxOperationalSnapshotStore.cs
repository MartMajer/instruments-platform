namespace Platform.Application.Features.Operations;

public interface IOutboxOperationalSnapshotStore
{
    Task<OutboxOperationalSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record OutboxOperationalSnapshotResponse(
    int DueCount,
    int ScheduledRetryCount,
    int DeadLetterCount,
    DateTimeOffset? OldestDueCreatedAt);
