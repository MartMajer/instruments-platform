using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Platform.Workers.Outbox;

public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxRelayWorkerOptions> options,
    ILogger<OutboxRelayWorker> logger)
    : BackgroundService
{
    public async Task<int> ProcessOnceAsync(CancellationToken cancellationToken = default)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return 0;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxRelayTickProcessor>();
            var processed = await processor.ProcessDueAsync(currentOptions.BatchSize, cancellationToken);

            if (processed > 0)
            {
                logger.LogInformation("Outbox relay processed {OutboxEventCount} event(s).", processed);
            }

            return processed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Outbox relay tick failed with {ExceptionType}.",
                exception.GetType().Name);
            return 0;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Outbox relay worker is disabled by configuration.");
            return;
        }

        if (currentOptions.ProcessOnStartup)
        {
            await ProcessOnceAsync(stoppingToken);
        }

        using var timer = new PeriodicTimer(currentOptions.PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }
}
