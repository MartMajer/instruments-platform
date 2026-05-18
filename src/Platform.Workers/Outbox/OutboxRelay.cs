using Microsoft.EntityFrameworkCore;
using Platform.Application.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;

namespace Platform.Workers.Outbox;

public sealed class OutboxRelay(
    ApplicationDbContext dbContext,
    IOutboxEventDispatcher dispatcher,
    ITenantDbScope tenantDbScope,
    ICurrentTenant currentTenant)
{
    public async Task<int> ProcessDueAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        if (batchSize > OutboxRelayWorkerOptions.MaxBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                $"Batch size must be less than or equal to {OutboxRelayWorkerOptions.MaxBatchSize}.");
        }

        var dueTenantIds = await ListDueTenantIdsAsync(batchSize, cancellationToken);
        var processed = 0;
        foreach (var tenantId in dueTenantIds)
        {
            if (processed >= batchSize)
            {
                break;
            }

            processed += await ProcessTenantDueAsync(
                tenantId,
                batchSize - processed,
                cancellationToken);
        }

        return processed;
    }

    private async Task<IReadOnlyList<Guid>> ListDueTenantIdsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await dbContext.OutboxEvents
            .AsNoTracking()
            .Where(outboxEvent =>
                outboxEvent.PublishedAt == null &&
                (outboxEvent.NextRetryAt == null || outboxEvent.NextRetryAt <= now) &&
                (outboxEvent.LastError == null ||
                    !EF.Functions.Like(outboxEvent.LastError, "DEAD_LETTER:%")))
            .Select(outboxEvent => outboxEvent.TenantId)
            .Distinct()
            .OrderBy(tenantId => tenantId)
            .Take(1)
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ProcessTenantDueAsync(
        Guid tenantId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        currentTenant.SetTenant(tenantId, "outbox_relay");

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var dueEvents = await dbContext.OutboxEvents
            .FromSqlRaw(
                """
                SELECT *
                FROM outbox_event
                WHERE tenant_id = {0}
                  AND published_at IS NULL
                  AND (next_retry_at IS NULL OR next_retry_at <= now())
                  AND (last_error IS NULL OR left(last_error, 12) <> 'DEAD_LETTER:')
                ORDER BY aggregate_id, created_at
                LIMIT {1}
                FOR UPDATE SKIP LOCKED
                """,
                tenantId,
                batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var outboxEvent in dueEvents)
        {
            try
            {
                await dispatcher.DispatchAsync(outboxEvent, cancellationToken);
                outboxEvent.MarkPublished(DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                outboxEvent.MarkFailed(
                    OutboxFailureDiagnostics.DispatchFailed(outboxEvent, exception),
                    DateTimeOffset.UtcNow);
            }

            processed++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return processed;
    }
}
