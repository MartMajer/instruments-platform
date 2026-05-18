using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;

namespace Platform.Workers.Reports;

public sealed class ReportPdfArtifactWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportPdfArtifactWorkerOptions> options,
    ILogger<ReportPdfArtifactWorker> logger)
    : BackgroundService
{
    public async Task<ReportPdfArtifactWorkerTickResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return ReportPdfArtifactWorkerTickResult.Empty;
        }

        using var scope = scopeFactory.CreateScope();
        var tenantSource = scope.ServiceProvider.GetRequiredService<IReportPdfArtifactWorkerTenantSource>();
        var exportStore = scope.ServiceProvider.GetRequiredService<IReportProofExportStore>();

        IReadOnlyList<Guid> tenantIds;
        var staleRenderingBefore = DateTimeOffset.UtcNow.Subtract(currentOptions.RenderingTimeout);
        try
        {
            tenantIds = await tenantSource.ListTenantIdsWithQueuedReportPdfArtifactsAsync(
                currentOptions.MaxTenantsPerTick,
                staleRenderingBefore,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Report PDF artifact tenant enumeration failed with {ExceptionType}.",
                exception.GetType().Name);
            return ReportPdfArtifactWorkerTickResult.Empty;
        }

        var succeededTenantCount = 0;
        var failedTenantCount = 0;
        var staleFailedArtifactCount = 0;
        var processedArtifactCount = 0;

        for (var index = 0; index < tenantIds.Count; index++)
        {
            var tenantOrdinal = index + 1;
            var tenantId = tenantIds[index];

            try
            {
                var staleRun = await exportStore.FailStaleCampaignSeriesReportPdfArtifactsAsync(
                    tenantId,
                    staleRenderingBefore,
                    currentOptions.MaxArtifactsPerTenant,
                    cancellationToken);

                if (staleRun.IsFailure)
                {
                    failedTenantCount++;
                    logger.LogWarning(
                        "Report PDF artifact stale recovery tenant {TenantOrdinal} of {TenantCount} failed with {ErrorCode}.",
                        tenantOrdinal,
                        tenantIds.Count,
                        staleRun.Error.Code);
                    continue;
                }

                staleFailedArtifactCount += staleRun.Value.ProcessedArtifactCount;

                var run = await exportStore.ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
                    tenantId,
                    currentOptions.MaxArtifactsPerTenant,
                    cancellationToken);

                if (run.IsFailure)
                {
                    failedTenantCount++;
                    logger.LogWarning(
                        "Report PDF artifact tenant {TenantOrdinal} of {TenantCount} failed with {ErrorCode}.",
                        tenantOrdinal,
                        tenantIds.Count,
                        run.Error.Code);
                    continue;
                }

                succeededTenantCount++;
                processedArtifactCount += run.Value.ProcessedArtifactCount;

                if (run.Value.ProcessedArtifactCount > 0)
                {
                    logger.LogInformation(
                        "Report PDF artifact tenant {TenantOrdinal} of {TenantCount} processed {ProcessedArtifactCount} artifact(s) and recovered {StaleFailedArtifactCount} stale artifact(s).",
                        tenantOrdinal,
                        tenantIds.Count,
                        run.Value.ProcessedArtifactCount,
                        staleRun.Value.ProcessedArtifactCount);
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
                    "Report PDF artifact tenant {TenantOrdinal} of {TenantCount} failed with {ExceptionType}.",
                    tenantOrdinal,
                    tenantIds.Count,
                    exception.GetType().Name);
            }
        }

        if (tenantIds.Count > 0)
        {
            logger.LogInformation(
                "Report PDF artifact worker tick finished for {TenantCount} tenant(s): succeeded {SucceededTenantCount}, failed {FailedTenantCount}, processed {ProcessedArtifactCount} artifact(s).",
                tenantIds.Count,
                succeededTenantCount,
                failedTenantCount,
                processedArtifactCount);
        }

        return new ReportPdfArtifactWorkerTickResult(
            tenantIds.Count,
            succeededTenantCount,
            failedTenantCount,
            staleFailedArtifactCount,
            processedArtifactCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Report PDF artifact worker is disabled by configuration.");
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

public sealed record ReportPdfArtifactWorkerTickResult(
    int TenantCount,
    int SucceededTenantCount,
    int FailedTenantCount,
    int StaleFailedArtifactCount,
    int ProcessedArtifactCount)
{
    public static ReportPdfArtifactWorkerTickResult Empty { get; } = new(
        TenantCount: 0,
        SucceededTenantCount: 0,
        FailedTenantCount: 0,
        StaleFailedArtifactCount: 0,
        ProcessedArtifactCount: 0);
}
