using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Operations;
using Platform.Application.Features.System.GetHealth;
using Platform.Domain.Outbox;
using Platform.Domain.Tenancy;
using Platform.Infrastructure;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Health;
using Platform.Infrastructure.Operations;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class OutboxDeadLetterHealthCheckTests
{
    [Fact]
    public void Infrastructure_registration_includes_outbox_dead_letter_health_check()
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
                service.ImplementationType == typeof(OutboxDeadLetterHealthCheck));
        Assert.Contains(
            services,
            service =>
                service.ServiceType == typeof(IOutboxOperationalSnapshotStore) &&
                service.ImplementationType == typeof(OutboxOperationalSnapshotStore));
    }

    [Fact]
    public async Task Check_returns_unready_when_snapshot_store_fails()
    {
        var check = new OutboxDeadLetterHealthCheck(new ThrowingSnapshotStore());

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("outbox_dead_letters", check.Name);
        Assert.Equal("outbox_dead_letters", result.Name);
        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Check_returns_unready_when_snapshot_has_dead_letters()
    {
        var check = new OutboxDeadLetterHealthCheck(new FixedSnapshotStore(DeadLetterCount: 1));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Check_returns_ok_when_snapshot_has_no_dead_letters()
    {
        var check = new OutboxDeadLetterHealthCheck(new FixedSnapshotStore(DeadLetterCount: 0));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    private sealed class FixedSnapshotStore(int DeadLetterCount) : IOutboxOperationalSnapshotStore
    {
        public Task<OutboxOperationalSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new OutboxOperationalSnapshotResponse(
                DueCount: 0,
                ScheduledRetryCount: 0,
                DeadLetterCount,
                OldestDueCreatedAt: null));
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

public sealed class OutboxDeadLetterHealthCheckDockerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Check_reports_unready_when_unpublished_dead_letter_exists()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var tenantId = await SeedTenantAsync(db, "dead-letter-tenant");
        var outboxEvent = CreateOutboxEvent(tenantId);
        var failedAt = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            outboxEvent.MarkFailed("DISPATCH_FAILED", failedAt.AddMinutes(attempt));
        }

        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync();

        var snapshotStore = new OutboxOperationalSnapshotStore(db);
        var result = await new OutboxDeadLetterHealthCheck(snapshotStore).CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [DockerFact]
    public async Task Check_reports_ok_when_unpublished_outbox_events_are_not_dead_lettered()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        await using var db = new ApplicationDbContext(options);
        var tenantId = await SeedTenantAsync(db, "pending-tenant");

        db.OutboxEvents.Add(CreateOutboxEvent(tenantId));
        await db.SaveChangesAsync();

        var snapshotStore = new OutboxOperationalSnapshotStore(db);
        var result = await new OutboxDeadLetterHealthCheck(snapshotStore).CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
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

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private static async Task<Guid> SeedTenantAsync(ApplicationDbContext db, string slug)
    {
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant(tenantId, slug, $"Tenant {slug}"));
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
}
