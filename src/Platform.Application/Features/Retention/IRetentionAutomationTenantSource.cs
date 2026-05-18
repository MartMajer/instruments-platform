namespace Platform.Application.Features.Retention;

public interface IRetentionAutomationTenantSource
{
    Task<IReadOnlyList<Guid>> ListEligibleTenantIdsAsync(
        DateTimeOffset asOf,
        int maxTenantsPerTick,
        CancellationToken cancellationToken);
}
