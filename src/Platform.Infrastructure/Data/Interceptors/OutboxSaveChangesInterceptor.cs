using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Platform.Application.Auditing;
using Platform.Application.Outbox;
using Platform.Application.Tenancy;
using Platform.Domain.Outbox;

namespace Platform.Infrastructure.Data.Interceptors;

public sealed class OutboxSaveChangesInterceptor(
    ICurrentTenant currentTenant,
    ICurrentAuditContext currentAuditContext,
    IOutboxEventBuffer outboxEventBuffer) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddOutboxEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddOutboxEvents(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        outboxEventBuffer.Clear();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        outboxEventBuffer.Clear();
        return ValueTask.FromResult(result);
    }

    private void AddOutboxEvents(DbContext? dbContext)
    {
        if (dbContext is null || outboxEventBuffer.PendingMessages.Count == 0)
        {
            return;
        }

        if (!currentTenant.HasTenant)
        {
            throw new InvalidOperationException("Tenant context is required to write outbox events.");
        }

        var trackedOutboxIds = dbContext.ChangeTracker.Entries<OutboxEvent>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity.AggregateId)
            .ToHashSet();

        var outboxEvents = outboxEventBuffer.PendingMessages
            .Where(message => !trackedOutboxIds.Contains(message.AggregateId))
            .Select(message => OutboxEvent.Create(
                currentTenant.TenantId,
                message.AggregateId,
                message.AggregateType,
                message.EventType,
                message.Payload,
                currentAuditContext.CorrelationId))
            .ToList();

        if (outboxEvents.Count > 0)
        {
            dbContext.Set<OutboxEvent>().AddRange(outboxEvents);
        }
    }
}
