using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;
using Platform.Workers.Operations;

namespace Platform.UnitTests.Workers;

public sealed class WorkerHeartbeatWorkerTests
{
    private static readonly string[] SensitiveSentinels =
    [
        "Host=db.example;Username=platform_app;Password=super-secret",
        "smtp-provider-token-secret",
        "tenant-raw-id",
        "worker-host-name-sensitive",
        "C:\\sensitive\\machine\\path"
    ];

    [Fact]
    public async Task RecordOnceAsync_does_not_call_store_when_disabled()
    {
        var store = new RecordingWorkerHeartbeatStore();
        using var provider = CreateProvider(store);
        var worker = CreateWorker(provider, new WorkerHeartbeatWorkerOptions { Enabled = false });

        var recorded = await worker.RecordOnceAsync();

        Assert.False(recorded);
        Assert.Empty(store.Requests);
    }

    [Fact]
    public async Task RecordOnceAsync_records_safe_worker_name_and_generated_instance_id_when_enabled()
    {
        var store = new RecordingWorkerHeartbeatStore();
        using var provider = CreateProvider(store);
        var worker = CreateWorker(
            provider,
            new WorkerHeartbeatWorkerOptions
            {
                Enabled = true,
                WorkerName = "platform-workers"
            });

        var recorded = await worker.RecordOnceAsync();

        Assert.True(recorded);
        var request = Assert.Single(store.Requests);
        Assert.Equal("platform-workers", request.WorkerName);
        Assert.NotEmpty(request.InstanceId);
        Assert.DoesNotContain(Environment.MachineName, request.InstanceId, StringComparison.OrdinalIgnoreCase);
        Assert.True(request.ObservedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RecordOnceAsync_logs_exception_type_without_sensitive_exception_message()
    {
        var store = new RecordingWorkerHeartbeatStore(
            throwMessage: string.Join(" | ", SensitiveSentinels));
        using var provider = CreateProvider(store);
        using var capturedLogs = new CapturedLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(capturedLogs));
        var worker = CreateWorker(
            provider,
            new WorkerHeartbeatWorkerOptions
            {
                Enabled = true,
                WorkerName = "platform-workers"
            },
            loggerFactory.CreateLogger<WorkerHeartbeatWorker>());

        var recorded = await worker.RecordOnceAsync();

        Assert.False(recorded);
        Assert.DoesNotContain(
            capturedLogs.Flatten(),
            value => SensitiveSentinels.Any(sentinel => value.Contains(sentinel, StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(capturedLogs.Flatten(), value => value.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
    }

    private static ServiceProvider CreateProvider(IWorkerHeartbeatStore store)
    {
        return new ServiceCollection()
            .AddScoped(_ => store)
            .BuildServiceProvider();
    }

    private static WorkerHeartbeatWorker CreateWorker(
        ServiceProvider provider,
        WorkerHeartbeatWorkerOptions options,
        ILogger<WorkerHeartbeatWorker>? logger = null)
    {
        return new WorkerHeartbeatWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<WorkerHeartbeatWorker>.Instance);
    }

    private sealed class RecordingWorkerHeartbeatStore(string? throwMessage = null) : IWorkerHeartbeatStore
    {
        private readonly List<WorkerHeartbeatRecordRequest> _requests = [];

        public IReadOnlyList<WorkerHeartbeatRecordRequest> Requests => _requests;

        public Task RecordHeartbeatAsync(
            WorkerHeartbeatRecordRequest request,
            CancellationToken cancellationToken)
        {
            if (throwMessage is not null)
            {
                throw new InvalidOperationException(throwMessage);
            }

            _requests.Add(request);
            return Task.CompletedTask;
        }

        public Task<WorkerHeartbeatSnapshotResponse?> GetLatestHeartbeatAsync(
            string workerName,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

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
