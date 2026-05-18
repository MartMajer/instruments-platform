using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Platform.Application.Auditing;
using Platform.Application.Tenancy;
using Platform.Domain.Auditing;
using Platform.Domain.Auth;
using Platform.Domain.Outbox;
using Platform.Domain.Operations;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Data.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    ICurrentTenant currentTenant,
    ICurrentAuditContext currentAuditContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEvents(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AddAuditEvents(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();

        var auditEvents = dbContext.ChangeTracker.Entries()
            .Where(IsAuditableChange)
            .Select(CreateAuditEvent)
            .OfType<AuditEvent>()
            .ToList();

        if (auditEvents.Count > 0)
        {
            dbContext.Set<AuditEvent>().AddRange(auditEvents);
        }
    }

    private static bool IsAuditableChange(EntityEntry entry)
    {
        return entry.Entity is not AuditEvent &&
               entry.Entity is not OutboxEvent &&
               entry.Entity is not Permission &&
               entry.Entity is not RegistrationIntent &&
               entry.Entity is not WorkerHeartbeat &&
               entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
    }

    private AuditEvent? CreateAuditEvent(EntityEntry entry)
    {
        var changeKind = entry.State switch
        {
            EntityState.Added => AuditChangeKinds.Added,
            EntityState.Modified => AuditChangeKinds.Modified,
            EntityState.Deleted => AuditChangeKinds.Deleted,
            _ => null
        };

        if (changeKind is null)
        {
            return null;
        }

        var before = entry.State is EntityState.Modified or EntityState.Deleted
            ? Snapshot(entry, before: true, changedOnly: entry.State == EntityState.Modified)
            : null;
        var after = entry.State is EntityState.Added or EntityState.Modified
            ? Snapshot(entry, before: false, changedOnly: entry.State == EntityState.Modified)
            : null;

        if (entry.State == EntityState.Modified &&
            before is null &&
            after is null)
        {
            return null;
        }

        if (!currentTenant.HasTenant)
        {
            throw new InvalidOperationException("Tenant context is required to write audit events.");
        }

        return new AuditEvent(
            PlatformIds.NewId(),
            DateTimeOffset.UtcNow,
            currentTenant.TenantId,
            currentAuditContext.ActorType,
            currentAuditContext.ActorId,
            currentAuditContext.CorrelationId,
            entry.Metadata.ClrType.Name,
            BuildEntityId(entry),
            changeKind,
            before,
            after,
            currentAuditContext.Reason);
    }

    private static System.Text.Json.JsonDocument? Snapshot(
        EntityEntry entry,
        bool before,
        bool changedOnly)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty())
            {
                continue;
            }

            if (changedOnly && !property.IsModified)
            {
                continue;
            }

            var propertyName = property.Metadata.Name;
            var value = before
                ? property.OriginalValue
                : property.CurrentValue;
            values[propertyName] = AuditSnapshotRedactionPolicy.Redact(
                entry.Metadata.ClrType,
                propertyName,
                value);
        }

        return values.Count == 0
            ? null
            : AuditJson.Create(values);
    }

    private static string BuildEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
        {
            return string.Empty;
        }

        var keyValues = primaryKey.Properties
            .Select(property =>
            {
                var value = entry.Property(property.Name).CurrentValue
                    ?? entry.Property(property.Name).OriginalValue;

                return primaryKey.Properties.Count == 1
                    ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                    : $"{property.Name}={Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)}";
            });

        return string.Join(";", keyValues);
    }
}
