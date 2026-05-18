using Microsoft.EntityFrameworkCore;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Retention;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class RetentionAutomationTenantSourceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("platform_tests")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task RetentionAutomationTenantSource_returns_active_policy_and_incomplete_due_batch_tenants_only()
    {
        var options = CreateMigratorOptions();
        await PrepareDatabaseAsync(options);
        var asOf = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        var activePolicyTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var futurePolicyTenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var retiredPolicyTenantId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var plannedBatchTenantId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var processingBatchTenantId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        var completedBatchTenantId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        var failedBatchTenantId = Guid.Parse("00000000-0000-0000-0000-000000000007");
        var emptyTenantId = Guid.Parse("00000000-0000-0000-0000-000000000008");

        await using (var db = new ApplicationDbContext(options))
        {
            SeedTenantSeriesAndPolicies(db, activePolicyTenantId, asOf.AddDays(-1), retiredAt: null);
            SeedTenantSeriesAndPolicies(db, futurePolicyTenantId, asOf.AddDays(1), retiredAt: null);
            SeedTenantSeriesAndPolicies(db, retiredPolicyTenantId, asOf.AddDays(-10), retiredAt: asOf.AddDays(-1));
            SeedTenantSeriesAndPolicies(db, plannedBatchTenantId, asOf.AddDays(-10), retiredAt: asOf.AddDays(-1), dueBatchStatus: RetentionDueBatchStatuses.Planned);
            SeedTenantSeriesAndPolicies(db, processingBatchTenantId, asOf.AddDays(-10), retiredAt: asOf.AddDays(-1), dueBatchStatus: RetentionDueBatchStatuses.Processing);
            SeedTenantSeriesAndPolicies(db, completedBatchTenantId, asOf.AddDays(-10), retiredAt: asOf.AddDays(-1), dueBatchStatus: RetentionDueBatchStatuses.Completed);
            SeedTenantSeriesAndPolicies(db, failedBatchTenantId, asOf.AddDays(-10), retiredAt: asOf.AddDays(-1), dueBatchStatus: RetentionDueBatchStatuses.Failed);
            db.Tenants.Add(new Tenant(emptyTenantId, "qa06-empty", "QA06 Empty"));
            await db.SaveChangesAsync();
        }

        await using (var db = new ApplicationDbContext(options))
        {
            var source = new RetentionAutomationTenantSource(db);

            var tenantIds = await source.ListEligibleTenantIdsAsync(asOf, maxTenantsPerTick: 20, CancellationToken.None);

            Assert.Equal(
                [activePolicyTenantId, plannedBatchTenantId, processingBatchTenantId],
                tenantIds);
            Assert.DoesNotContain(futurePolicyTenantId, tenantIds);
            Assert.DoesNotContain(retiredPolicyTenantId, tenantIds);
            Assert.DoesNotContain(completedBatchTenantId, tenantIds);
            Assert.DoesNotContain(failedBatchTenantId, tenantIds);
            Assert.DoesNotContain(emptyTenantId, tenantIds);
        }
    }

    [DockerFact]
    public async Task RetentionAutomationTenantSource_honors_max_tenants_per_tick()
    {
        var options = CreateMigratorOptions();
        await PrepareDatabaseAsync(options);
        var asOf = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
        var firstTenantId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var secondTenantId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var thirdTenantId = Guid.Parse("00000000-0000-0000-0000-000000000103");

        await using (var db = new ApplicationDbContext(options))
        {
            SeedTenantSeriesAndPolicies(db, thirdTenantId, asOf.AddDays(-1), retiredAt: null);
            SeedTenantSeriesAndPolicies(db, firstTenantId, asOf.AddDays(-1), retiredAt: null);
            SeedTenantSeriesAndPolicies(db, secondTenantId, asOf.AddDays(-1), retiredAt: null);
            await db.SaveChangesAsync();
        }

        await using (var db = new ApplicationDbContext(options))
        {
            var source = new RetentionAutomationTenantSource(db);

            var tenantIds = await source.ListEligibleTenantIdsAsync(asOf, maxTenantsPerTick: 2, CancellationToken.None);

            Assert.Equal([firstTenantId, secondTenantId], tenantIds);
        }
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }

    private DbContextOptions<ApplicationDbContext> CreateMigratorOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private static void SeedTenantSeriesAndPolicies(
        ApplicationDbContext db,
        Guid tenantId,
        DateTimeOffset policyCreatedAt,
        DateTimeOffset? retiredAt,
        string? dueBatchStatus = null)
    {
        var seriesId = Guid.Parse($"10000000-0000-0000-0000-{tenantId.ToString("N")[20..]}");
        var policyId = Guid.Parse($"20000000-0000-0000-0000-{tenantId.ToString("N")[20..]}");
        var asOf = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        db.Tenants.Add(new Tenant(tenantId, $"qa06-{tenantId.ToString("N")[^8..]}", $"QA06 {tenantId:N}"));
        db.CampaignSeries.Add(new CampaignSeries(
            seriesId,
            tenantId,
            $"QA06 Series {tenantId:N}",
            Enumerable.Repeat((byte)tenantId.ToByteArray()[0], 32).ToArray()));
        db.RetentionPolicies.Add(new RetentionPolicy(
            policyId,
            tenantId,
            seriesId,
            "v1",
            retainForYears: 1,
            RetentionPolicy.ResponseSubmittedAt,
            RetentionPolicy.Anonymize,
            DateOnly.FromDateTime(asOf.DateTime.AddDays(30)),
            "{}",
            policyCreatedAt,
            retiredAt));

        if (dueBatchStatus is null)
        {
            return;
        }

        var batch = RetentionDueBatch.Plan(
            Guid.Parse($"30000000-0000-0000-0000-{tenantId.ToString("N")[20..]}"),
            tenantId,
            seriesId,
            policyId,
            RetentionPolicy.ResponseSubmittedAt,
            RetentionPolicy.Anonymize,
            asOf,
            asOf.AddDays(-1),
            consentRecordCount: 0,
            responseSessionCount: 1,
            answerCount: 0,
            scoreRunCount: 0,
            scoreCount: 0,
            derivedArtifactCount: 0,
            $"qa06:{tenantId:N}:{dueBatchStatus}",
            asOf.AddDays(-1));

        if (dueBatchStatus == RetentionDueBatchStatuses.Processing)
        {
            batch.Claim(asOf.AddMinutes(-10));
        }
        else if (dueBatchStatus == RetentionDueBatchStatuses.Completed)
        {
            batch.Claim(asOf.AddMinutes(-10));
            batch.Complete(asOf.AddMinutes(-5));
        }
        else if (dueBatchStatus == RetentionDueBatchStatuses.Failed)
        {
            batch.Fail("qa06.failed", "QA06 synthetic failure.", asOf.AddMinutes(-5));
        }

        db.RetentionDueBatches.Add(batch);
    }
}
