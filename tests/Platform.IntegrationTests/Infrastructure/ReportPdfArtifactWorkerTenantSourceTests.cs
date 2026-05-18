using Microsoft.EntityFrameworkCore;
using Platform.Domain.Campaigns;
using Platform.Domain.Reports;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Reports;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ReportPdfArtifactWorkerTenantSourceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("platform_tests")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task ReportPdfArtifactWorkerTenantSource_returns_tenants_with_queued_report_pdf_artifacts_only()
    {
        var options = CreateMigratorOptions();
        await PrepareDatabaseAsync(options);
        var firstTenantId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var secondTenantId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var thirdTenantId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var noQueueTenantId = Guid.Parse("00000000-0000-0000-0000-000000000104");

        await using (var db = new ApplicationDbContext(options))
        {
            SeedTenantAndArtifact(db, secondTenantId, ExportArtifactStatuses.Rendering, ExportArtifactTypes.CampaignSeriesReportPdf, startedAt: DateTimeOffset.UtcNow.AddHours(-2));
            SeedTenantAndArtifact(db, firstTenantId, ExportArtifactStatuses.Queued, ExportArtifactTypes.CampaignSeriesReportPdf);
            SeedTenantAndArtifact(db, thirdTenantId, ExportArtifactStatuses.Rendering, ExportArtifactTypes.CampaignSeriesReportPdf, startedAt: DateTimeOffset.UtcNow.AddMinutes(-2));
            SeedTenantAndArtifact(db, noQueueTenantId, ExportArtifactStatuses.Queued, ExportArtifactTypes.CampaignSeriesReportHtml);
            await db.SaveChangesAsync();
        }

        await using (var db = new ApplicationDbContext(options))
        {
            var source = new ReportPdfArtifactWorkerTenantSource(db);

            var tenantIds = await source.ListTenantIdsWithQueuedReportPdfArtifactsAsync(
                maxTenantsPerTick: 1,
                staleRenderingBefore: DateTimeOffset.UtcNow.AddMinutes(-30),
                CancellationToken.None);

            Assert.Equal([firstTenantId], tenantIds);
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

    private static void SeedTenantAndArtifact(
        ApplicationDbContext db,
        Guid tenantId,
        string status,
        string artifactType,
        DateTimeOffset? startedAt = null)
    {
        var seriesId = Guid.Parse($"10000000-0000-0000-0000-{tenantId.ToString("N")[20..]}");
        var artifactId = Guid.Parse($"20000000-0000-0000-0000-{tenantId.ToString("N")[20..]}");
        var createdAt = new DateTimeOffset(2026, 5, 18, 20, 0, 0, TimeSpan.Zero);

        db.Tenants.Add(new Tenant(tenantId, $"pdf-worker-{tenantId.ToString("N")[^8..]}", $"PDF Worker {tenantId:N}"));
        db.CampaignSeries.Add(new CampaignSeries(
            seriesId,
            tenantId,
            $"PDF Worker Series {tenantId:N}",
            Enumerable.Repeat((byte)tenantId.ToByteArray()[0], 32).ToArray()));

        var succeeded = status == ExportArtifactStatuses.Succeeded;
        db.ExportArtifacts.Add(new ExportArtifact(
            artifactId,
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: seriesId,
            artifactType,
            status,
            artifactType == ExportArtifactTypes.CampaignSeriesReportPdf
                ? ExportArtifactFormats.Pdf
                : ExportArtifactFormats.Html,
            $"campaign-series-{seriesId}-report.pdf",
            artifactType == ExportArtifactTypes.CampaignSeriesReportPdf
                ? "application/pdf"
                : "text/html; charset=utf-8",
            rowCount: succeeded ? 1 : 0,
            byteSize: succeeded ? 4 : 0,
            checksumSha256: succeeded ? "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" : null,
            metadataJson: """{"artifactType":"campaign_series_report_pdf"}""",
            content: succeeded && artifactType == ExportArtifactTypes.CampaignSeriesReportHtml
                ? "<!doctype html>"
                : null,
            codebookJson: "{}",
            createdAt,
            completedAt: succeeded ? createdAt : null,
            startedAt: startedAt,
            storageKind: artifactType == ExportArtifactTypes.CampaignSeriesReportPdf
                ? ExportArtifactStorageKinds.ExternalObject
                : ExportArtifactStorageKinds.InlineText,
            storageKey: succeeded ? $"tenants/{tenantId:N}/campaign-series/{seriesId:N}/reports/{artifactId:N}.pdf" : null));
    }
}
