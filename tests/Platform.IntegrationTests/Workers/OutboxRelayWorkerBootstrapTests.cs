using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Domain.Outbox;
using Platform.IntegrationTests.Support.Logging;
using Platform.Workers;
using Platform.Workers.Outbox;
using Platform.Workers.Operations;
using Platform.Workers.Reports;
using Platform.Workers.Retention;

namespace Platform.IntegrationTests.Workers;

public sealed class OutboxRelayWorkerBootstrapTests
{
    [Fact]
    public async Task WorkerHostBuilder_registers_outbox_relay_worker_services()
    {
        using var host = WorkerHostBuilder.Build(
        [
            "--ConnectionStrings:PlatformDb=Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used",
            "--OutboxRelay:Enabled=false"
        ]);

        Assert.Contains(host.Services.GetServices<IHostedService>(), service => service is OutboxRelayWorker);
        Assert.Contains(host.Services.GetServices<IHostedService>(), service => service is WorkerHeartbeatWorker);
        Assert.Contains(host.Services.GetServices<IHostedService>(), service => service is RetentionAutomationWorker);
        Assert.Contains(host.Services.GetServices<IHostedService>(), service => service is ReportPdfArtifactWorker);
        Assert.False(host.Services.GetRequiredService<IOptions<RetentionAutomationWorkerOptions>>().Value.Enabled);
        Assert.False(host.Services.GetRequiredService<IOptions<ReportPdfArtifactWorkerOptions>>().Value.Enabled);

        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OutboxRelay>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOutboxRelayTickProcessor>());

        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxEventDispatcher>();
        await dispatcher.DispatchAsync(CreateOutboxEvent("InvitationEmailQueued"));
        await Assert.ThrowsAsync<OutboxEventHandlerNotFoundException>(
            () => dispatcher.DispatchAsync(CreateOutboxEvent("UnknownEvent")));
    }

    [Fact]
    public void WorkerHostBuilder_builds_in_development_with_service_provider_validation()
    {
        var previousEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

            using var host = WorkerHostBuilder.Build(
            [
                "--ConnectionStrings:PlatformDb=Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used",
                "--OutboxRelay:Enabled=false"
            ]);

            Assert.Equal("Development", host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", previousEnvironment);
        }
    }

    [Theory]
    [InlineData("", "1", "WorkerName")]
    [InlineData("platform-workers", "0", "IntervalSeconds")]
    public void AddWorkerHeartbeat_rejects_invalid_options(
        string workerName,
        string intervalSeconds,
        string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkerHeartbeat:WorkerName"] = workerName,
                ["WorkerHeartbeat:IntervalSeconds"] = intervalSeconds
            })
            .Build();

        var services = new ServiceCollection();
        services.AddWorkerHeartbeat(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<WorkerHeartbeatWorkerOptions>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("0", "1", "1", "1", "InitialDelaySeconds")]
    [InlineData("1", "0", "1", "1", "IntervalSeconds")]
    [InlineData("1", "1", "0", "1", "MaxBatchesPerTenant")]
    [InlineData("1", "1", "101", "1", "MaxBatchesPerTenant")]
    [InlineData("1", "1", "1", "0", "MaxTenantsPerTick")]
    [InlineData("1", "1", "1", "501", "MaxTenantsPerTick")]
    public void AddRetentionAutomationWorker_rejects_invalid_options(
        string initialDelaySeconds,
        string intervalSeconds,
        string maxBatchesPerTenant,
        string maxTenantsPerTick,
        string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RetentionAutomation:InitialDelaySeconds"] = initialDelaySeconds,
                ["RetentionAutomation:IntervalSeconds"] = intervalSeconds,
                ["RetentionAutomation:MaxBatchesPerTenant"] = maxBatchesPerTenant,
                ["RetentionAutomation:MaxTenantsPerTick"] = maxTenantsPerTick
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRetentionAutomationWorker(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RetentionAutomationWorkerOptions>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("0", "1", "1", "1", "1", "InitialDelaySeconds")]
    [InlineData("1", "0", "1", "1", "1", "IntervalSeconds")]
    [InlineData("1", "1", "0", "1", "1", "RenderingTimeoutMinutes")]
    [InlineData("1", "1", "1", "0", "1", "MaxArtifactsPerTenant")]
    [InlineData("1", "1", "1", "101", "1", "MaxArtifactsPerTenant")]
    [InlineData("1", "1", "1", "1", "0", "MaxTenantsPerTick")]
    [InlineData("1", "1", "1", "1", "501", "MaxTenantsPerTick")]
    public void AddReportPdfArtifactWorker_rejects_invalid_options(
        string initialDelaySeconds,
        string intervalSeconds,
        string renderingTimeoutMinutes,
        string maxArtifactsPerTenant,
        string maxTenantsPerTick,
        string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReportPdfArtifacts:InitialDelaySeconds"] = initialDelaySeconds,
                ["ReportPdfArtifacts:IntervalSeconds"] = intervalSeconds,
                ["ReportPdfArtifacts:RenderingTimeoutMinutes"] = renderingTimeoutMinutes,
                ["ReportPdfArtifacts:MaxArtifactsPerTenant"] = maxArtifactsPerTenant,
                ["ReportPdfArtifacts:MaxTenantsPerTick"] = maxTenantsPerTick
            })
            .Build();

        var services = new ServiceCollection();
        services.AddReportPdfArtifactWorker(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ReportPdfArtifactWorkerOptions>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task ProcessOnceAsync_uses_configured_batch_size_when_enabled()
    {
        var processor = new RecordingTickProcessor(processedCount: 3);
        using var provider = CreateProcessorProvider(processor);
        var worker = CreateWorker(
            provider,
            new OutboxRelayWorkerOptions
            {
                Enabled = true,
                BatchSize = 25
            });

        var processed = await worker.ProcessOnceAsync();

        Assert.Equal(3, processed);
        Assert.Equal([25], processor.BatchSizes);
    }

    [Fact]
    public async Task ProcessOnceAsync_does_not_process_when_disabled()
    {
        var processor = new RecordingTickProcessor(processedCount: 3);
        using var provider = CreateProcessorProvider(processor);
        var worker = CreateWorker(
            provider,
            new OutboxRelayWorkerOptions
            {
                Enabled = false,
                BatchSize = 25
            });

        var processed = await worker.ProcessOnceAsync();

        Assert.Equal(0, processed);
        Assert.Empty(processor.BatchSizes);
    }

    [Fact]
    public async Task ProcessOnceAsync_catches_tick_processor_failures()
    {
        var processor = new ThrowingTickProcessor();
        using var provider = CreateProcessorProvider(processor);
        var worker = CreateWorker(
            provider,
            new OutboxRelayWorkerOptions
            {
                Enabled = true,
                BatchSize = 25
            });

        var processed = await worker.ProcessOnceAsync();

        Assert.Equal(0, processed);
        Assert.Equal(1, processor.Attempts);
    }

    [Fact]
    public async Task ProcessOnceAsync_does_not_log_raw_sensitive_exception_diagnostics()
    {
        var processor = new ThrowingTickProcessor(SensitiveLogAssert.JoinSentinels());
        using var provider = CreateProcessorProvider(processor);
        using var capturedLogs = new CapturedLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(capturedLogs));
        var worker = CreateWorker(
            provider,
            new OutboxRelayWorkerOptions
            {
                Enabled = true,
                BatchSize = 25
            },
            loggerFactory.CreateLogger<OutboxRelayWorker>());

        var processed = await worker.ProcessOnceAsync();

        Assert.Equal(0, processed);
        Assert.Equal(1, processor.Attempts);
        SensitiveLogAssert.DoesNotContain(capturedLogs, SensitiveLogAssert.DefaultSentinels);
        SensitiveLogAssert.Contains(capturedLogs, nameof(InvalidOperationException));
    }

    [Theory]
    [InlineData("0", "1", "BatchSize")]
    [InlineData("10", "0", "PollIntervalSeconds")]
    public void AddOutboxRelayWorker_rejects_non_positive_options(
        string batchSize,
        string pollIntervalSeconds,
        string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OutboxRelay:BatchSize"] = batchSize,
                ["OutboxRelay:PollIntervalSeconds"] = pollIntervalSeconds
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOutboxRelayWorker(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OutboxRelayWorkerOptions>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    private static ServiceProvider CreateProcessorProvider(IOutboxRelayTickProcessor processor)
    {
        return new ServiceCollection()
            .AddScoped(_ => processor)
            .BuildServiceProvider();
    }

    private static OutboxRelayWorker CreateWorker(
        ServiceProvider serviceProvider,
        OutboxRelayWorkerOptions options,
        ILogger<OutboxRelayWorker>? logger = null)
    {
        return new OutboxRelayWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<OutboxRelayWorker>.Instance);
    }

    private static OutboxEvent CreateOutboxEvent(string eventType)
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            eventType,
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["notification_id"] = Guid.NewGuid()
            }),
            null);
    }

    private sealed class RecordingTickProcessor(int processedCount) : IOutboxRelayTickProcessor
    {
        private readonly List<int> _batchSizes = [];

        public IReadOnlyList<int> BatchSizes => _batchSizes;

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            _batchSizes.Add(batchSize);
            return Task.FromResult(processedCount);
        }
    }

    private sealed class ThrowingTickProcessor(string message = "boom") : IOutboxRelayTickProcessor
    {
        public int Attempts { get; private set; }

        public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException(message);
        }
    }
}
