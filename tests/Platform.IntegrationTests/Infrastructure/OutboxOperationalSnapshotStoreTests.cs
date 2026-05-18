using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Platform.Domain.Outbox;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Operations;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class OutboxOperationalSnapshotStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task GetSnapshotAsync_returns_zero_counts_when_outbox_is_empty()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = new OutboxOperationalSnapshotStore(db);

        var snapshot = await store.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(0, snapshot.DueCount);
        Assert.Equal(0, snapshot.ScheduledRetryCount);
        Assert.Equal(0, snapshot.DeadLetterCount);
        Assert.Null(snapshot.OldestDueCreatedAt);
    }

    [DockerFact]
    public async Task GetSnapshotAsync_counts_due_scheduled_retry_and_dead_letter_events()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var tenantId = await SeedTenantAsync(db);
        var dueEvent = CreateOutboxEvent(tenantId);
        var scheduledRetryEvent = CreateOutboxEvent(tenantId);
        var deadLetterEvent = CreateOutboxEvent(tenantId);

        scheduledRetryEvent.MarkFailed("DISPATCH_FAILED", DateTimeOffset.UtcNow.AddMinutes(10));

        var failedAt = DateTimeOffset.UtcNow;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            deadLetterEvent.MarkFailed("DISPATCH_FAILED", failedAt.AddMinutes(attempt));
        }

        db.OutboxEvents.AddRange(dueEvent, scheduledRetryEvent, deadLetterEvent);
        await db.SaveChangesAsync();

        var snapshot = await new OutboxOperationalSnapshotStore(db).GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, snapshot.DueCount);
        Assert.Equal(1, snapshot.ScheduledRetryCount);
        Assert.Equal(1, snapshot.DeadLetterCount);
        Assert.NotNull(snapshot.OldestDueCreatedAt);
    }

    [DockerFact]
    public async Task GetSnapshotAsync_reuses_snapshot_queries_within_same_store_instance()
    {
        var interceptor = new CountingCommandInterceptor();
        var options = CreateOptions(interceptor);
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var tenantId = await SeedTenantAsync(db);
        db.OutboxEvents.Add(CreateOutboxEvent(tenantId));
        await db.SaveChangesAsync();
        interceptor.Reset();
        var store = new OutboxOperationalSnapshotStore(db);

        await store.GetSnapshotAsync(CancellationToken.None);
        await store.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(4, interceptor.ReaderCommandCount);
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

    private static async Task<Guid> SeedTenantAsync(ApplicationDbContext db)
    {
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant(tenantId, $"snapshot-{tenantId:N}", "Snapshot tenant"));
        await db.SaveChangesAsync();

        return tenantId;
    }

    private static OutboxEvent CreateOutboxEvent(Guid tenantId)
    {
        return OutboxEvent.Create(
            tenantId,
            Guid.NewGuid(),
            "test_aggregate",
            "TestEvent",
            JsonDocument.Parse("{}"),
            correlationId: null);
    }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommandCount++;

            return ValueTask.FromResult(result);
        }
    }
}
