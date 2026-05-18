using Microsoft.EntityFrameworkCore.Storage;

namespace Platform.Infrastructure.Tenancy;

public interface ITenantDbScope
{
    Task<IDbContextTransaction> BeginTransactionAsync(
        Guid tenantId,
        Guid? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task SetTenantAsync(
        Guid tenantId,
        Guid? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
