using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Operations;
using Platform.Domain.Outbox;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Operations;

public sealed class OutboxOperationalSnapshotStore(ApplicationDbContext dbContext)
    : IOutboxOperationalSnapshotStore
{
    private Task<OutboxOperationalSnapshotResponse>? _snapshotTask;

    public async Task<OutboxOperationalSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        _snapshotTask ??= QuerySnapshotAsync(cancellationToken);

        return await _snapshotTask;
    }

    private async Task<OutboxOperationalSnapshotResponse> QuerySnapshotAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var unpublished = dbContext.OutboxEvents
            .AsNoTracking()
            .Where(outboxEvent => outboxEvent.PublishedAt == null);

        var active = unpublished
            .Where(outboxEvent =>
                outboxEvent.LastError == null ||
                !outboxEvent.LastError.StartsWith(OutboxEvent.DeadLetterPrefix));

        var due = active
            .Where(outboxEvent =>
                outboxEvent.NextRetryAt == null ||
                outboxEvent.NextRetryAt <= now);

        var dueCount = await due.CountAsync(cancellationToken);
        var scheduledRetryCount = await active
            .CountAsync(outboxEvent => outboxEvent.NextRetryAt > now, cancellationToken);
        var deadLetterCount = await unpublished
            .CountAsync(
                outboxEvent =>
                    outboxEvent.LastError != null &&
                    outboxEvent.LastError.StartsWith(OutboxEvent.DeadLetterPrefix),
                cancellationToken);
        var oldestDueCreatedAt = await due
            .OrderBy(outboxEvent => outboxEvent.CreatedAt)
            .Select(outboxEvent => (DateTimeOffset?)outboxEvent.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new OutboxOperationalSnapshotResponse(
            dueCount,
            scheduledRetryCount,
            deadLetterCount,
            oldestDueCreatedAt);
    }
}
