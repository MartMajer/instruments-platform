using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;

namespace Platform.Workers.Operations;

public sealed class WorkerHeartbeatWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerHeartbeatWorkerOptions> options,
    ILogger<WorkerHeartbeatWorker> logger)
    : BackgroundService
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    public async Task<bool> RecordOnceAsync(CancellationToken cancellationToken = default)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return false;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatStore>();
            await store.RecordHeartbeatAsync(
                new WorkerHeartbeatRecordRequest(
                    currentOptions.WorkerName,
                    _instanceId,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Worker heartbeat failed with {ExceptionType}.",
                exception.GetType().Name);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Worker heartbeat is disabled by configuration.");
            return;
        }

        await Task.Delay(currentOptions.InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RecordOnceAsync(stoppingToken);
            await Task.Delay(currentOptions.Interval, stoppingToken);
        }
    }
}
