using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Platform.Application.Auditing;
using Platform.Application.Features.Operations;
using Platform.Application.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Interceptors;
using Platform.Infrastructure.Operations;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class WorkerHeartbeatStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task RecordHeartbeatAsync_upserts_existing_worker_instance()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = new WorkerHeartbeatStore(db);
        var firstSeen = DateTimeOffset.UtcNow.AddMinutes(-5);
        var secondSeen = DateTimeOffset.UtcNow;

        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("platform-workers", "instance-1", firstSeen),
            CancellationToken.None);
        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("platform-workers", "instance-1", secondSeen),
            CancellationToken.None);

        var heartbeat = Assert.Single(await db.WorkerHeartbeats.ToListAsync());
        Assert.Equal("platform-workers", heartbeat.WorkerName);
        Assert.Equal("instance-1", heartbeat.InstanceId);
        AssertClose(firstSeen, heartbeat.StartedAt);
        AssertClose(secondSeen, heartbeat.LastSeenAt);
    }

    [DockerFact]
    public async Task RecordHeartbeatAsync_does_not_require_tenant_context_or_write_audit_event()
    {
        var options = CreateOptions(new AuditSaveChangesInterceptor(new CurrentTenant(), new CurrentAuditContext()));
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = new WorkerHeartbeatStore(db);

        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("platform-workers", "instance-1", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(1, await db.WorkerHeartbeats.CountAsync());
        Assert.Equal(0, await db.AuditEvents.CountAsync());
    }

    [DockerFact]
    public async Task GetLatestHeartbeatAsync_returns_latest_heartbeat_for_worker_name()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = new WorkerHeartbeatStore(db);
        var olderSeen = DateTimeOffset.UtcNow.AddMinutes(-10);
        var latestSeen = DateTimeOffset.UtcNow;

        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("platform-workers", "older-instance", olderSeen),
            CancellationToken.None);
        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("platform-workers", "latest-instance", latestSeen),
            CancellationToken.None);
        await store.RecordHeartbeatAsync(
            new WorkerHeartbeatRecordRequest("other-worker", "other-instance", latestSeen.AddMinutes(5)),
            CancellationToken.None);

        var heartbeat = await store.GetLatestHeartbeatAsync("platform-workers", CancellationToken.None);

        Assert.NotNull(heartbeat);
        Assert.Equal("platform-workers", heartbeat.WorkerName);
        Assert.Equal("latest-instance", heartbeat.InstanceId);
        AssertClose(latestSeen, heartbeat.LastSeenAt);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString());

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static void AssertClose(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.True(
            (expected - actual).Duration() < TimeSpan.FromMilliseconds(1),
            $"Expected {actual:o} to be within 1 ms of {expected:o}.");
    }
}
