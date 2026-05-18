using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure;
using Platform.Infrastructure.Health;
using Platform.Infrastructure.Operations;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class OutboxDueBacklogHealthCheckTests
{
    [Fact]
    public void Infrastructure_registration_includes_outbox_due_backlog_health_check()
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
                service.ImplementationType == typeof(OutboxDueBacklogHealthCheck));
    }

    [Fact]
    public void Infrastructure_registration_rejects_invalid_due_backlog_threshold()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app",
                ["OutboxOperations:DueBacklogUnreadyAfterMinutes"] = "0"
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OutboxOperationalReadinessOptions>>().Value);

        Assert.Contains("DueBacklogUnreadyAfterMinutes", exception.Message);
    }

    [Fact]
    public async Task Check_returns_ok_when_there_is_no_due_backlog()
    {
        var check = CreateCheck(new FixedSnapshotStore(OldestDueCreatedAt: null));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("outbox_due_backlog", check.Name);
        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Check_returns_ok_when_due_backlog_is_recent()
    {
        var check = CreateCheck(new FixedSnapshotStore(DateTimeOffset.UtcNow.AddMinutes(-2)));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Check_returns_unready_when_due_backlog_exceeds_threshold()
    {
        var check = CreateCheck(new FixedSnapshotStore(DateTimeOffset.UtcNow.AddMinutes(-30)));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Check_returns_unready_when_snapshot_store_fails()
    {
        var check = CreateCheck(new ThrowingSnapshotStore());

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    private static OutboxDueBacklogHealthCheck CreateCheck(IOutboxOperationalSnapshotStore snapshotStore)
    {
        return new OutboxDueBacklogHealthCheck(
            snapshotStore,
            Options.Create(new OutboxOperationalReadinessOptions
            {
                DueBacklogUnreadyAfterMinutes = 15
            }));
    }

    private sealed class FixedSnapshotStore(DateTimeOffset? OldestDueCreatedAt) : IOutboxOperationalSnapshotStore
    {
        public Task<OutboxOperationalSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new OutboxOperationalSnapshotResponse(
                DueCount: OldestDueCreatedAt is null ? 0 : 1,
                ScheduledRetryCount: 0,
                DeadLetterCount: 0,
                OldestDueCreatedAt));
        }
    }

    private sealed class ThrowingSnapshotStore : IOutboxOperationalSnapshotStore
    {
        public Task<OutboxOperationalSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("snapshot unavailable");
        }
    }
}
