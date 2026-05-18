using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Retention;

namespace Platform.Workers.Retention;

public sealed class RetentionAutomationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionAutomationWorkerOptions> options,
    ILogger<RetentionAutomationWorker> logger,
    TimeProvider? timeProvider = null)
    : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<RetentionAutomationWorkerTickResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return RetentionAutomationWorkerTickResult.Empty;
        }

        var asOf = _timeProvider.GetUtcNow();
        using var scope = scopeFactory.CreateScope();
        var tenantSource = scope.ServiceProvider.GetRequiredService<IRetentionAutomationTenantSource>();
        var dueBatchStore = scope.ServiceProvider.GetRequiredService<IRetentionDueBatchStore>();

        IReadOnlyList<Guid> tenantIds;
        try
        {
            tenantIds = await tenantSource.ListEligibleTenantIdsAsync(
                asOf,
                currentOptions.MaxTenantsPerTick,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Retention automation tenant enumeration failed with {ExceptionType}.",
                exception.GetType().Name);
            return RetentionAutomationWorkerTickResult.Empty;
        }

        var succeededTenantCount = 0;
        var failedTenantCount = 0;
        var seriesScannedCount = 0;
        var dueBatchCount = 0;
        var claimedBatchCount = 0;
        var completedBatchCount = 0;
        var failedBatchCount = 0;
        var skippedBatchCount = 0;

        for (var index = 0; index < tenantIds.Count; index++)
        {
            var tenantOrdinal = index + 1;
            var tenantId = tenantIds[index];

            try
            {
                var run = await dueBatchStore.RunDueBatchAutomationAsync(
                    tenantId,
                    asOf,
                    currentOptions.MaxBatchesPerTenant,
                    cancellationToken);

                if (run.IsFailure)
                {
                    failedTenantCount++;
                    logger.LogWarning(
                        "Retention automation tenant {TenantOrdinal} of {TenantCount} failed with {ErrorCode}.",
                        tenantOrdinal,
                        tenantIds.Count,
                        run.Error.Code);
                    continue;
                }

                var value = run.Value;
                succeededTenantCount++;
                seriesScannedCount += value.SeriesScannedCount;
                dueBatchCount += value.DueBatchCount;
                claimedBatchCount += value.ClaimedBatchCount;
                completedBatchCount += value.CompletedBatchCount;
                failedBatchCount += value.FailedBatchCount;
                skippedBatchCount += value.SkippedBatchCount;

                if (value.DueBatchCount > 0 || value.CompletedBatchCount > 0 || value.FailedBatchCount > 0)
                {
                    logger.LogInformation(
                        "Retention automation tenant {TenantOrdinal} of {TenantCount} scanned {SeriesScannedCount} series, found {DueBatchCount} due batch(es), claimed {ClaimedBatchCount}, completed {CompletedBatchCount}, failed {FailedBatchCount}, skipped {SkippedBatchCount}.",
                        tenantOrdinal,
                        tenantIds.Count,
                        value.SeriesScannedCount,
                        value.DueBatchCount,
                        value.ClaimedBatchCount,
                        value.CompletedBatchCount,
                        value.FailedBatchCount,
                        value.SkippedBatchCount);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedTenantCount++;
                logger.LogError(
                    "Retention automation tenant {TenantOrdinal} of {TenantCount} failed with {ExceptionType}.",
                    tenantOrdinal,
                    tenantIds.Count,
                    exception.GetType().Name);
            }
        }

        if (tenantIds.Count > 0)
        {
            logger.LogInformation(
                "Retention automation tick finished for {TenantCount} tenant(s): succeeded {SucceededTenantCount}, failed {FailedTenantCount}, scanned {SeriesScannedCount} series, due {DueBatchCount}, claimed {ClaimedBatchCount}, completed {CompletedBatchCount}, failed batches {FailedBatchCount}, skipped {SkippedBatchCount}.",
                tenantIds.Count,
                succeededTenantCount,
                failedTenantCount,
                seriesScannedCount,
                dueBatchCount,
                claimedBatchCount,
                completedBatchCount,
                failedBatchCount,
                skippedBatchCount);
        }

        return new RetentionAutomationWorkerTickResult(
            tenantIds.Count,
            succeededTenantCount,
            failedTenantCount,
            seriesScannedCount,
            dueBatchCount,
            claimedBatchCount,
            completedBatchCount,
            failedBatchCount,
            skippedBatchCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Retention automation worker is disabled by configuration.");
            return;
        }

        await Task.Delay(currentOptions.InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(currentOptions.Interval, stoppingToken);
        }
    }
}

public sealed record RetentionAutomationWorkerTickResult(
    int TenantCount,
    int SucceededTenantCount,
    int FailedTenantCount,
    int SeriesScannedCount,
    int DueBatchCount,
    int ClaimedBatchCount,
    int CompletedBatchCount,
    int FailedBatchCount,
    int SkippedBatchCount)
{
    public static RetentionAutomationWorkerTickResult Empty { get; } = new(
        TenantCount: 0,
        SucceededTenantCount: 0,
        FailedTenantCount: 0,
        SeriesScannedCount: 0,
        DueBatchCount: 0,
        ClaimedBatchCount: 0,
        CompletedBatchCount: 0,
        FailedBatchCount: 0,
        SkippedBatchCount: 0);
}
