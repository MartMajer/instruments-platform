using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Tenancy;

public sealed class TenantDbScope(ApplicationDbContext db) : ITenantDbScope
{
    public async Task<IDbContextTransaction> BeginTransactionAsync(
        Guid tenantId,
        Guid? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await SetTenantAsync(tenantId, userId, correlationId, cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task SetTenantAsync(
        Guid tenantId,
        Guid? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Tenant database settings must be applied inside an active transaction.");
        }

        await SetTransactionLocalAsync("app.current_tenant_id", tenantId.ToString(), cancellationToken);

        if (userId.HasValue)
        {
            await SetTransactionLocalAsync("app.current_user_id", userId.Value.ToString(), cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            await SetTransactionLocalAsync("app.correlation_id", correlationId, cancellationToken);
        }
    }

    private Task SetTransactionLocalAsync(
        string name,
        string value,
        CancellationToken cancellationToken)
    {
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config({name}, {value}, true)",
            cancellationToken);
    }
}
