using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure;
using Platform.Infrastructure.Health;
using Platform.Infrastructure.Operations;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class WorkerHeartbeatHealthCheckTests
{
    [Fact]
    public void Infrastructure_registration_includes_worker_heartbeat_health_check()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app"
            })
            .Build();

        var services = new ServiceCollection()
            .AddPlatformInfrastructure(configuration);

        Assert.Contains(
            services,
            service =>
                service.ServiceType == typeof(IPlatformHealthCheck) &&
                service.ImplementationType == typeof(WorkerHeartbeatHealthCheck));
    }

    [Theory]
    [InlineData("", "60", "ExpectedWorkerName")]
    [InlineData("platform-workers", "0", "StaleAfterSeconds")]
    public void Infrastructure_registration_rejects_invalid_worker_heartbeat_readiness_options(
        string expectedWorkerName,
        string staleAfterSeconds,
        string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app",
                ["WorkerHeartbeatReadiness:ExpectedWorkerName"] = expectedWorkerName,
                ["WorkerHeartbeatReadiness:StaleAfterSeconds"] = staleAfterSeconds
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<WorkerHeartbeatReadinessOptions>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task Check_returns_ok_without_querying_store_when_disabled()
    {
        var store = new RecordingWorkerHeartbeatStore();
        var check = CreateCheck(
            store,
            new WorkerHeartbeatReadinessOptions { Enabled = false });

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("worker_heartbeat", check.Name);
        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
        Assert.Equal(0, store.QueryCount);
    }

    [Fact]
    public async Task Check_returns_unready_when_enabled_and_heartbeat_is_missing()
    {
        var check = CreateCheck(
            new RecordingWorkerHeartbeatStore(),
            new WorkerHeartbeatReadinessOptions { Enabled = true });

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Check_returns_unready_when_enabled_and_heartbeat_is_stale()
    {
        var check = CreateCheck(
            new RecordingWorkerHeartbeatStore(DateTimeOffset.UtcNow.AddMinutes(-5)),
            new WorkerHeartbeatReadinessOptions
            {
                Enabled = true,
                StaleAfterSeconds = 60
            });

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Check_returns_ok_when_enabled_and_heartbeat_is_recent()
    {
        var check = CreateCheck(
            new RecordingWorkerHeartbeatStore(DateTimeOffset.UtcNow.AddSeconds(-5)),
            new WorkerHeartbeatReadinessOptions
            {
                Enabled = true,
                StaleAfterSeconds = 60
            });

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Check_returns_unready_when_store_fails()
    {
        var check = CreateCheck(
            new RecordingWorkerHeartbeatStore(throwOnQuery: true),
            new WorkerHeartbeatReadinessOptions { Enabled = true });

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    private static WorkerHeartbeatHealthCheck CreateCheck(
        IWorkerHeartbeatStore store,
        WorkerHeartbeatReadinessOptions options)
    {
        return new WorkerHeartbeatHealthCheck(store, Options.Create(options));
    }

    private sealed class RecordingWorkerHeartbeatStore(
        DateTimeOffset? lastSeenAt = null,
        bool throwOnQuery = false) : IWorkerHeartbeatStore
    {
        public int QueryCount { get; private set; }

        public Task RecordHeartbeatAsync(
            WorkerHeartbeatRecordRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<WorkerHeartbeatSnapshotResponse?> GetLatestHeartbeatAsync(
            string workerName,
            CancellationToken cancellationToken)
        {
            QueryCount++;
            if (throwOnQuery)
            {
                throw new InvalidOperationException("heartbeat query failed");
            }

            return Task.FromResult(lastSeenAt is null
                ? null
                : new WorkerHeartbeatSnapshotResponse(workerName, "instance-1", lastSeenAt.Value));
        }
    }
}
