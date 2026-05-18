using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.Workers.Outbox;

public static class OutboxRelayWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxRelayWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OutboxRelayWorkerOptions>()
            .Bind(configuration.GetSection(OutboxRelayWorkerOptions.SectionName))
            .Validate(options => options.BatchSize > 0, "OutboxRelay:BatchSize must be greater than zero.")
            .Validate(
                options => options.BatchSize <= OutboxRelayWorkerOptions.MaxBatchSize,
                $"OutboxRelay:BatchSize must be less than or equal to {OutboxRelayWorkerOptions.MaxBatchSize}.")
            .Validate(
                options => options.PollIntervalSeconds > 0,
                "OutboxRelay:PollIntervalSeconds must be greater than zero.");

        services.AddScoped<OutboxRelay>();
        services.AddScoped<IOutboxRelayTickProcessor, OutboxRelayTickProcessor>();
        services.AddHostedService<OutboxRelayWorker>();

        return services;
    }
}
