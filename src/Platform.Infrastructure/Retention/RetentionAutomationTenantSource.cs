using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Retention;
using Platform.Domain.Consent;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Retention;

public sealed class RetentionAutomationTenantSource(ApplicationDbContext db) : IRetentionAutomationTenantSource
{
    public async Task<IReadOnlyList<Guid>> ListEligibleTenantIdsAsync(
        DateTimeOffset asOf,
        int maxTenantsPerTick,
        CancellationToken cancellationToken)
    {
        if (maxTenantsPerTick <= 0)
        {
            return [];
        }

        var policyTenantIds = db.RetentionPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.CreatedAt <= asOf &&
                (!policy.RetiredAt.HasValue || policy.RetiredAt.Value > asOf))
            .Select(policy => policy.TenantId);

        var dueBatchTenantIds = db.RetentionDueBatches
            .AsNoTracking()
            .Where(batch =>
                batch.Status == RetentionDueBatchStatuses.Planned ||
                batch.Status == RetentionDueBatchStatuses.Processing)
            .Select(batch => batch.TenantId);

        return await policyTenantIds
            .Concat(dueBatchTenantIds)
            .Distinct()
            .OrderBy(tenantId => tenantId)
            .Take(maxTenantsPerTick)
            .ToListAsync(cancellationToken);
    }
}
