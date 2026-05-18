using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;
using Platform.Workers.Reports;

namespace Platform.UnitTests.Workers;

public sealed class ReportPdfArtifactWorkerTests
{
    private static readonly string[] SensitiveSentinels =
    [
        "wdr_sensitive_token",
        "<!doctype html>",
        "raw free-text answer with identifiable detail",
        "Host=db.example;Username=platform_app;Password=super-secret",
        "smtp-provider-token-secret",
        "subject@example.com",
        "raw object path"
    ];

    [Fact]
    public void Options_disable_report_pdf_artifact_worker_by_default()
    {
        var options = new ReportPdfArtifactWorkerOptions();

        Assert.False(options.Enabled);
        Assert.True(options.InitialDelay > TimeSpan.Zero);
        Assert.True(options.Interval > TimeSpan.Zero);
        Assert.True(options.RenderingTimeout > TimeSpan.Zero);
        Assert.True(options.MaxArtifactsPerTenant > 0);
        Assert.True(options.MaxTenantsPerTick > 0);
    }

    [Fact]
    public async Task RunOnceAsync_does_not_enumerate_tenants_or_process_artifacts_when_disabled()
    {
        var tenantSource = new RecordingReportPdfArtifactTenantSource([Guid.NewGuid()]);
        var exportStore = new RecordingReportProofExportStore();
        using var provider = CreateProvider(tenantSource, exportStore);
        var worker = CreateWorker(
            provider,
            new ReportPdfArtifactWorkerOptions
            {
                Enabled = false,
                MaxArtifactsPerTenant = 7,
                MaxTenantsPerTick = 3
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(ReportPdfArtifactWorkerTickResult.Empty, result);
        Assert.Equal(0, tenantSource.Calls);
        Assert.Empty(exportStore.ProcessCalls);
    }

    [Fact]
    public async Task RunOnceAsync_passes_max_tenant_and_artifact_caps_to_sources_when_enabled()
    {
        var tenantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var tenantSource = new RecordingReportPdfArtifactTenantSource(tenantIds);
        var exportStore = new RecordingReportProofExportStore(processedCount: 2);
        using var provider = CreateProvider(tenantSource, exportStore);
        var worker = CreateWorker(
            provider,
            new ReportPdfArtifactWorkerOptions
            {
                Enabled = true,
                MaxArtifactsPerTenant = 7,
                MaxTenantsPerTick = 3
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(3, tenantSource.MaxTenantsPerTick);
        Assert.Equal(2, result.TenantCount);
        Assert.Equal(2, result.SucceededTenantCount);
        Assert.Equal(0, result.FailedTenantCount);
        Assert.Equal(4, result.ProcessedArtifactCount);
        Assert.Equal(tenantIds, exportStore.ProcessCalls.Select(call => call.TenantId));
        Assert.All(exportStore.ProcessCalls, call => Assert.Equal(7, call.MaxArtifacts));
        Assert.Equal(tenantIds, exportStore.StaleCalls.Select(call => call.TenantId));
        Assert.All(exportStore.StaleCalls, call => Assert.Equal(7, call.MaxArtifacts));
        Assert.All(exportStore.StaleCalls, call => Assert.True(call.StaleBefore < DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task RunOnceAsync_isolates_one_tenant_failure_from_later_tenants()
    {
        var failingTenantId = Guid.NewGuid();
        var succeedingTenantId = Guid.NewGuid();
        var tenantSource = new RecordingReportPdfArtifactTenantSource([failingTenantId, succeedingTenantId]);
        var exportStore = new RecordingReportProofExportStore(
            processedCount: 1,
            failingTenantIds: new HashSet<Guid> { failingTenantId });
        using var provider = CreateProvider(tenantSource, exportStore);
        var worker = CreateWorker(
            provider,
            new ReportPdfArtifactWorkerOptions
            {
                Enabled = true,
                MaxArtifactsPerTenant = 5,
                MaxTenantsPerTick = 5
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(2, result.TenantCount);
        Assert.Equal(1, result.SucceededTenantCount);
        Assert.Equal(1, result.FailedTenantCount);
        Assert.Equal(1, result.ProcessedArtifactCount);
        Assert.Equal(2, result.StaleFailedArtifactCount);
        Assert.Equal([failingTenantId, succeedingTenantId], exportStore.ProcessCalls.Select(call => call.TenantId));
    }

    [Fact]
    public async Task RunOnceAsync_logs_exception_type_without_sensitive_exception_message()
    {
        var tenantSource = new RecordingReportPdfArtifactTenantSource([Guid.NewGuid()]);
        var exportStore = new RecordingReportProofExportStore(
            throwMessage: string.Join(" | ", SensitiveSentinels));
        using var provider = CreateProvider(tenantSource, exportStore);
        using var capturedLogs = new CapturedLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(capturedLogs));
        var worker = CreateWorker(
            provider,
            new ReportPdfArtifactWorkerOptions
            {
                Enabled = true,
                MaxArtifactsPerTenant = 5,
                MaxTenantsPerTick = 5
            },
            loggerFactory.CreateLogger<ReportPdfArtifactWorker>());

        var result = await worker.RunOnceAsync();

        Assert.Equal(1, result.FailedTenantCount);
        Assert.DoesNotContain(
            capturedLogs.Flatten(),
            value => SensitiveSentinels.Any(sentinel => value.Contains(sentinel, StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(capturedLogs.Flatten(), value => value.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
    }

    private static ServiceProvider CreateProvider(
        IReportPdfArtifactWorkerTenantSource tenantSource,
        IReportProofExportStore exportStore)
    {
        return new ServiceCollection()
            .AddScoped(_ => tenantSource)
            .AddScoped(_ => exportStore)
            .BuildServiceProvider();
    }

    private static ReportPdfArtifactWorker CreateWorker(
        ServiceProvider provider,
        ReportPdfArtifactWorkerOptions options,
        ILogger<ReportPdfArtifactWorker>? logger = null)
    {
        return new ReportPdfArtifactWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<ReportPdfArtifactWorker>.Instance);
    }

    private sealed class RecordingReportPdfArtifactTenantSource(
        IReadOnlyList<Guid> tenantIds) : IReportPdfArtifactWorkerTenantSource
    {
        public int Calls { get; private set; }

        public int? MaxTenantsPerTick { get; private set; }

        public Task<IReadOnlyList<Guid>> ListTenantIdsWithQueuedReportPdfArtifactsAsync(
            int maxTenantsPerTick,
            DateTimeOffset staleRenderingBefore,
            CancellationToken cancellationToken)
        {
            Calls++;
            MaxTenantsPerTick = maxTenantsPerTick;
            return Task.FromResult<IReadOnlyList<Guid>>(tenantIds.Take(maxTenantsPerTick).ToArray());
        }
    }

    private sealed class RecordingReportProofExportStore(
        int processedCount = 0,
        int staleFailedCount = 1,
        IReadOnlySet<Guid>? failingTenantIds = null,
        string? throwMessage = null) : IReportProofExportStore
    {
        private readonly IReadOnlySet<Guid> _failingTenantIds = failingTenantIds ?? new HashSet<Guid>();
        private readonly List<ProcessCall> _processCalls = [];

        public IReadOnlyList<ProcessCall> ProcessCalls => _processCalls;

        private readonly List<StaleCall> _staleCalls = [];

        public IReadOnlyList<StaleCall> StaleCalls => _staleCalls;

        public Task<Result<ReportPdfArtifactWorkerRunResponse>> FailStaleCampaignSeriesReportPdfArtifactsAsync(
            Guid tenantId,
            DateTimeOffset staleBefore,
            int maxArtifacts,
            CancellationToken cancellationToken)
        {
            _staleCalls.Add(new StaleCall(tenantId, staleBefore, maxArtifacts));

            return Task.FromResult(Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                staleFailedCount)));
        }

        public Task<Result<ReportPdfArtifactWorkerRunResponse>> ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
            Guid tenantId,
            int maxArtifacts,
            CancellationToken cancellationToken)
        {
            _processCalls.Add(new ProcessCall(tenantId, maxArtifacts));

            if (throwMessage is not null || _failingTenantIds.Contains(tenantId))
            {
                throw new InvalidOperationException(throwMessage ?? "tenant failed");
            }

            return Task.FromResult(Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                processedCount)));
        }

        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignReportProofExportAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResponseExportAsync(Guid tenantId, Guid campaignSeriesId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportHtmlArtifactAsync(Guid tenantId, Guid campaignSeriesId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportPdfArtifactAsync(Guid tenantId, Guid campaignSeriesId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> QueueCampaignSeriesReportPdfArtifactAsync(Guid tenantId, Guid campaignSeriesId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> ProcessCampaignSeriesReportPdfArtifactAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> RetryCampaignSeriesReportPdfArtifactAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ReportProofExportArtifactResponse>> GetExportArtifactAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ExportArtifactDownloadResponse>> GetExportArtifactDownloadAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<ExportArtifactSignedDownloadUrlResponse>> GetExportArtifactSignedDownloadUrlAsync(Guid tenantId, Guid artifactId, TimeSpan expiresIn, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed record ProcessCall(Guid TenantId, int MaxArtifacts);

    private sealed record StaleCall(Guid TenantId, DateTimeOffset StaleBefore, int MaxArtifacts);

    private sealed class CapturedLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _captured = [];

        public IReadOnlyList<string> Flatten() => _captured;

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturedLogger(_captured);
        }

        public void Dispose()
        {
        }

        private sealed class CapturedLogger(List<string> captured) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                captured.Add(logLevel.ToString());
                captured.Add(formatter(state, exception));
                captured.Add(exception?.GetType().Name ?? string.Empty);
                captured.Add(exception?.Message ?? string.Empty);

                if (state is IEnumerable<KeyValuePair<string, object?>> structured)
                {
                    captured.AddRange(structured.Select(pair => $"{pair.Key}={pair.Value}"));
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
