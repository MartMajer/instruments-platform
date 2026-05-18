using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Platform.Workers.Retention;

public static class RetentionAutomationWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddRetentionAutomationWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RetentionAutomationWorkerOptions>()
            .Bind(configuration.GetSection(RetentionAutomationWorkerOptions.SectionName))
            .Validate(
                options => options.InitialDelaySeconds > 0,
                "RetentionAutomation:InitialDelaySeconds must be greater than zero.")
            .Validate(
                options => options.IntervalSeconds > 0,
                "RetentionAutomation:IntervalSeconds must be greater than zero.")
            .Validate(
                options => options.MaxBatchesPerTenant > 0,
                "RetentionAutomation:MaxBatchesPerTenant must be greater than zero.")
            .Validate(
                options => options.MaxBatchesPerTenant <= RetentionAutomationWorkerOptions.MaxAllowedBatchesPerTenant,
                $"RetentionAutomation:MaxBatchesPerTenant must be less than or equal to {RetentionAutomationWorkerOptions.MaxAllowedBatchesPerTenant}.")
            .Validate(
                options => options.MaxTenantsPerTick > 0,
                "RetentionAutomation:MaxTenantsPerTick must be greater than zero.")
            .Validate(
                options => options.MaxTenantsPerTick <= RetentionAutomationWorkerOptions.MaxAllowedTenantsPerTick,
                $"RetentionAutomation:MaxTenantsPerTick must be less than or equal to {RetentionAutomationWorkerOptions.MaxAllowedTenantsPerTick}.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<RetentionAutomationWorker>();

        return services;
    }
}
