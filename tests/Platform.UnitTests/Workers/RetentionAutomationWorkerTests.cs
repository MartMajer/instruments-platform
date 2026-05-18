using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Retention;
using Platform.SharedKernel;
using Platform.Workers.Retention;

namespace Platform.UnitTests.Workers;

public sealed class RetentionAutomationWorkerTests
{
    private static readonly string[] SensitiveSentinels =
    [
        "alpha-raw-participant-code-2026",
        "inv_11111111111141118111111111111111_sensitiveINV",
        "raw free-text answer with identifiable detail",
        "Host=db.example;Username=platform_app;Password=super-secret",
        "smtp-provider-token-secret",
        "series-salt-raw-value",
        "subject@example.com",
        "provider-message-123",
        "public-handle-123"
    ];

    [Fact]
    public void Options_disable_retention_automation_by_default()
    {
        var options = new RetentionAutomationWorkerOptions();

        Assert.False(options.Enabled);
        Assert.True(options.InitialDelay > TimeSpan.Zero);
        Assert.True(options.Interval > TimeSpan.Zero);
        Assert.True(options.MaxBatchesPerTenant > 0);
        Assert.True(options.MaxTenantsPerTick > 0);
    }

    [Fact]
    public async Task RunOnceAsync_does_not_enumerate_tenants_or_run_batches_when_disabled()
    {
        var tenantSource = new RecordingRetentionAutomationTenantSource([Guid.NewGuid()]);
        var batchStore = new RecordingRetentionDueBatchStore();
        using var provider = CreateProvider(tenantSource, batchStore);
        var worker = CreateWorker(
            provider,
            new RetentionAutomationWorkerOptions
            {
                Enabled = false,
                MaxBatchesPerTenant = 7,
                MaxTenantsPerTick = 3
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(0, result.TenantCount);
        Assert.Equal(0, result.SucceededTenantCount);
        Assert.Equal(0, result.FailedTenantCount);
        Assert.Equal(0, tenantSource.Calls);
        Assert.Empty(batchStore.Calls);
    }

    [Fact]
    public async Task RunOnceAsync_passes_max_tenant_and_batch_caps_to_sources_when_enabled()
    {
        var tenantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var tenantSource = new RecordingRetentionAutomationTenantSource(tenantIds);
        var batchStore = new RecordingRetentionDueBatchStore();
        using var provider = CreateProvider(tenantSource, batchStore);
        var worker = CreateWorker(
            provider,
            new RetentionAutomationWorkerOptions
            {
                Enabled = true,
                MaxBatchesPerTenant = 7,
                MaxTenantsPerTick = 3
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(3, tenantSource.MaxTenantsPerTick);
        Assert.Equal(2, result.TenantCount);
        Assert.Equal(2, result.SucceededTenantCount);
        Assert.Equal(0, result.FailedTenantCount);
        Assert.Equal(tenantIds, batchStore.Calls.Select(call => call.TenantId));
        Assert.All(batchStore.Calls, call => Assert.Equal(7, call.MaxBatches));
    }

    [Fact]
    public async Task RunOnceAsync_isolates_one_tenant_failure_from_later_tenants()
    {
        var failingTenantId = Guid.NewGuid();
        var succeedingTenantId = Guid.NewGuid();
        var tenantSource = new RecordingRetentionAutomationTenantSource([failingTenantId, succeedingTenantId]);
        var batchStore = new RecordingRetentionDueBatchStore(
            failingTenantIds: new HashSet<Guid> { failingTenantId });
        using var provider = CreateProvider(tenantSource, batchStore);
        var worker = CreateWorker(
            provider,
            new RetentionAutomationWorkerOptions
            {
                Enabled = true,
                MaxBatchesPerTenant = 5,
                MaxTenantsPerTick = 5
            });

        var result = await worker.RunOnceAsync();

        Assert.Equal(2, result.TenantCount);
        Assert.Equal(1, result.SucceededTenantCount);
        Assert.Equal(1, result.FailedTenantCount);
        Assert.Equal([failingTenantId, succeedingTenantId], batchStore.Calls.Select(call => call.TenantId));
    }

    [Fact]
    public async Task RunOnceAsync_logs_exception_type_without_sensitive_exception_message()
    {
        var tenantSource = new RecordingRetentionAutomationTenantSource([Guid.NewGuid()]);
        var batchStore = new RecordingRetentionDueBatchStore(
            throwMessage: string.Join(" | ", SensitiveSentinels));
        using var provider = CreateProvider(tenantSource, batchStore);
        using var capturedLogs = new CapturedLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(capturedLogs));
        var worker = CreateWorker(
            provider,
            new RetentionAutomationWorkerOptions
            {
                Enabled = true,
                MaxBatchesPerTenant = 5,
                MaxTenantsPerTick = 5
            },
            loggerFactory.CreateLogger<RetentionAutomationWorker>());

        var result = await worker.RunOnceAsync();

        Assert.Equal(1, result.FailedTenantCount);
        Assert.DoesNotContain(
            capturedLogs.Flatten(),
            value => SensitiveSentinels.Any(sentinel => value.Contains(sentinel, StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(capturedLogs.Flatten(), value => value.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
    }

    private static ServiceProvider CreateProvider(
        IRetentionAutomationTenantSource tenantSource,
        IRetentionDueBatchStore batchStore)
    {
        return new ServiceCollection()
            .AddScoped(_ => tenantSource)
            .AddScoped(_ => batchStore)
            .BuildServiceProvider();
    }

    private static RetentionAutomationWorker CreateWorker(
        ServiceProvider provider,
        RetentionAutomationWorkerOptions options,
        ILogger<RetentionAutomationWorker>? logger = null)
    {
        return new RetentionAutomationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<RetentionAutomationWorker>.Instance);
    }

    private sealed class RecordingRetentionAutomationTenantSource(
        IReadOnlyList<Guid> tenantIds) : IRetentionAutomationTenantSource
    {
        public int Calls { get; private set; }

        public int? MaxTenantsPerTick { get; private set; }

        public Task<IReadOnlyList<Guid>> ListEligibleTenantIdsAsync(
            DateTimeOffset asOf,
            int maxTenantsPerTick,
            CancellationToken cancellationToken)
        {
            Calls++;
            MaxTenantsPerTick = maxTenantsPerTick;
            return Task.FromResult<IReadOnlyList<Guid>>(tenantIds.Take(maxTenantsPerTick).ToArray());
        }
    }

    private sealed class RecordingRetentionDueBatchStore(
        IReadOnlySet<Guid>? failingTenantIds = null,
        string? throwMessage = null) : IRetentionDueBatchStore
    {
        private readonly List<RunCall> _calls = [];
        private readonly IReadOnlySet<Guid> _failingTenantIds = failingTenantIds ?? new HashSet<Guid>();

        public IReadOnlyList<RunCall> Calls => _calls;

        public Task<Result<RetentionDueBatchAutomationRunResponse>> RunDueBatchAutomationAsync(
            Guid tenantId,
            DateTimeOffset asOf,
            int maxBatches,
            CancellationToken cancellationToken)
        {
            _calls.Add(new RunCall(tenantId, maxBatches));

            if (throwMessage is not null || _failingTenantIds.Contains(tenantId))
            {
                throw new InvalidOperationException(throwMessage ?? "tenant failed");
            }

            return Task.FromResult(Result.Success(new RetentionDueBatchAutomationRunResponse(
                tenantId,
                asOf,
                maxBatches,
                SeriesScannedCount: 2,
                DueBatchCount: 1,
                ClaimedBatchCount: 1,
                CompletedBatchCount: 1,
                FailedBatchCount: 0,
                NoCandidateSeriesCount: 1,
                SkippedBatchCount: 0,
                Items: [])));
        }

        public Task<Result<RetentionDueBatchResponse>> PlanDueBatchAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            DateTimeOffset asOf,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<RetentionDueBatchDryRunResponse>> DryRunDueBatchAsync(
            Guid tenantId,
            Guid dueBatchId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<RetentionDueBatchResponse>> ClaimDueBatchAsync(
            Guid tenantId,
            Guid dueBatchId,
            DateTimeOffset processingStartedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<RetentionDueBatchResponse>> CompleteDueBatchAsync(
            Guid tenantId,
            Guid dueBatchId,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<RetentionDueBatchResponse>> FailDueBatchAsync(
            Guid tenantId,
            Guid dueBatchId,
            string failureCode,
            string? failureDetail,
            DateTimeOffset failedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<RetentionDueBatchExecutionResponse>> ExecuteDueBatchAsync(
            Guid tenantId,
            Guid dueBatchId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed record RunCall(Guid TenantId, int MaxBatches);

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
