using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.Workers.Operations;

public static class WorkerHeartbeatServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerHeartbeat(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<WorkerHeartbeatWorkerOptions>()
            .Bind(configuration.GetSection(WorkerHeartbeatWorkerOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.WorkerName),
                "WorkerHeartbeat:WorkerName must not be empty.")
            .Validate(
                options => options.WorkerName.Length <= 128,
                "WorkerHeartbeat:WorkerName must be 128 characters or fewer.")
            .Validate(
                options => options.WorkerName.All(character =>
                    char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'),
                "WorkerHeartbeat:WorkerName contains unsupported characters.")
            .Validate(
                options => options.InitialDelaySeconds >= 0,
                "WorkerHeartbeat:InitialDelaySeconds must be greater than or equal to zero.")
            .Validate(
                options => options.IntervalSeconds > 0,
                "WorkerHeartbeat:IntervalSeconds must be greater than zero.");

        services.AddHostedService<WorkerHeartbeatWorker>();

        return services;
    }
}
