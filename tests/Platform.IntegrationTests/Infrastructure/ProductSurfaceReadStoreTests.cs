using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Integrations;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.ProductSurfaces;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ProductSurfaceReadStoreTests : IAsyncLifetime
{
    private const string RuntimeUsername = "platform_app_runtime";
    private const string RuntimePassword = "platform_app_runtime";
    private const string SensitiveTokenHash = "sensitive-token-hash";
    private const string SensitiveIpHash = "ip-hash-value";
    private const string SensitiveUserAgentHash = "ua-hash-value";
    private const string SensitiveAnswerValue = "sensitive-answer";
    private const string SensitiveExportContent = "sensitive-export-content";
    private const string SensitiveCodebookContent = "sensitive-codebook-content";
    private const string SensitiveRecipient = "sensitive.recipient@example.test";
    private const string SensitiveProviderMessageId = "provider-message-sensitive";
    private const string SensitiveDeliveryError = "delivery-error-sensitive";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Sample_study_seeder_creates_finished_read_only_studies_idempotently()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            db.Tenants.Add(new Tenant(tenantId, "sample-seed", "Sample Seed"));
            db.UserAccounts.Add(new UserAccount(actorUserId, tenantId, "owner@example.test"));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var seeder = new SampleStudySeeder(
            db,
            tenantDbScope,
            new SubmittedResponseScoreMaterializer(db));

        var first = await seeder.EnsureAsync(tenantId, actorUserId, CancellationToken.None);
        var second = await seeder.EnsureAsync(tenantId, actorUserId, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(3, first.Value.CreatedSampleStudyCount);
        Assert.True(first.Value.CreatedCampaignSeriesIds.Count >= 3);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value.CreatedSampleStudyCount);

        var store = new ProductSurfaceReadStore(db, tenantDbScope);
        var overview = await store.GetWorkspaceOverviewAsync(
            tenantId,
            canManageSetup: true,
            canManageTeam: true,
            CancellationToken.None);

        Assert.Equal(3, overview.StudyCollections.SampleStudies.Count);
        Assert.Empty(overview.StudyCollections.OwnStudies);
        Assert.All(overview.StudyCollections.SampleStudies, sample =>
        {
            Assert.True(sample.IsSample);
            Assert.Equal(CampaignSeriesReadOnlyReasons.SampleStudy, sample.ReadOnlyReason);
            Assert.True(sample.CampaignCount >= 1);
            Assert.Equal(0, sample.LiveCampaignCount);
            Assert.True(sample.SubmittedResponseCount > 0);
        });

        await using var inspection = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.True(await db.Campaigns.CountAsync(campaign => campaign.TenantId == tenantId, CancellationToken.None) >= 8);
        Assert.True(await db.Subjects.CountAsync(subject => subject.TenantId == tenantId, CancellationToken.None) > 0);
        Assert.True(await db.SubjectGroups.CountAsync(group => group.TenantId == tenantId, CancellationToken.None) > 0);
        Assert.True(await db.ResponseSessions.CountAsync(session => session.TenantId == tenantId, CancellationToken.None) > 0);
        Assert.True(await db.Scores.CountAsync(score => score.TenantId == tenantId, CancellationToken.None) > 0);
        Assert.True(await db.ExportArtifacts.CountAsync(artifact => artifact.TenantId == tenantId, CancellationToken.None) >= 9);
        var responseExports = await db.ExportArtifacts
            .Where(artifact =>
                artifact.TenantId == tenantId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook)
            .ToListAsync(CancellationToken.None);
        Assert.Equal(3, responseExports.Count);
        Assert.All(responseExports, artifact =>
        {
            Assert.True(artifact.RowCount > 20);
            Assert.NotNull(artifact.Content);
            Assert.StartsWith(
                "study,wave,response_key,trajectory_key,submitted_at,question_code,question_text,answer_value,score_output_code,score_value",
                artifact.Content);
            Assert.Contains("q01", artifact.Content);
            Assert.DoesNotContain("score_total", artifact.Content);
            Assert.Contains("sample_response_rows", artifact.MetadataJson);
            Assert.Contains("question_code", artifact.CodebookJson);
            Assert.Contains("score_output_code", artifact.CodebookJson);
        });
        var matrixExports = await db.ExportArtifacts
            .Where(artifact =>
                artifact.TenantId == tenantId &&
                artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook)
            .ToListAsync(CancellationToken.None);
        Assert.Equal(3, matrixExports.Count);
        Assert.All(matrixExports, artifact =>
        {
            Assert.True(artifact.RowCount >= 8);
            Assert.NotNull(artifact.Content);
            Assert.StartsWith("result_scope,result_scope_label,campaign_series_id", artifact.Content);
            Assert.Contains("overall", artifact.Content);
            Assert.Contains("wave", artifact.Content);
            Assert.Contains("visible", artifact.Content);
            Assert.Contains("sample_results_matrix", artifact.MetadataJson);
            Assert.Contains("campaign_series_results_matrix_csv_codebook", artifact.CodebookJson);
        });
        Assert.Contains(matrixExports, artifact => artifact.Content?.Contains("\"group\"", StringComparison.Ordinal) == true);
        Assert.Contains(matrixExports, artifact => artifact.Content?.Contains("workload_manageability", StringComparison.Ordinal) == true);
        await inspection.CommitAsync();

        var groupSample = overview.StudyCollections.SampleStudies.Single(sample =>
            sample.Name == "Ergonomics risk and workstation fit");
        var reportsWorkspace = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            groupSample.Id,
            CancellationToken.None);

        Assert.True(reportsWorkspace.IsSuccess, reportsWorkspace.Error.ToString());
        Assert.NotNull(reportsWorkspace.Value.ResultsDashboard);
        var dashboard = reportsWorkspace.Value.ResultsDashboard!;
        Assert.True(dashboard.OutputBars.Count >= 4);
        Assert.True(dashboard.GroupBars.Count >= 12);
        Assert.Contains(dashboard.GroupBars, bar => bar.Disclosure == "visible" && bar.Value.HasValue);
        Assert.Contains(dashboard.GroupBars, bar => bar.Disclosure == "suppressed" && bar.Value is null);
        Assert.True(dashboard.WaveTrendPoints.Count >= 8);
    }

    [DockerFact]
    public async Task Workspace_overview_counts_only_current_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        var tenantATemplate = await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-overview");
        var tenantBTemplate = await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-overview");
        var tenantASeries = await SeedSeriesAsync(runtimeOptions, tenantA, "Tenant A series");
        await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B series");
        var tenantACampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantA,
            tenantATemplate.TemplateVersionId,
            tenantASeries.Id,
            "Tenant A campaign");

        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantA,
            tenantACampaign.Id,
            tenantATemplate.QuestionId);
        await SeedCampaignAsync(
            runtimeOptions,
            tenantB,
            tenantBTemplate.TemplateVersionId,
            campaignSeriesId: null,
            "Tenant B ungrouped campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetWorkspaceOverviewAsync(
            tenantA,
            canManageSetup: false,
            canManageTeam: false,
            CancellationToken.None);

        Assert.Equal(tenantA, result.TenantId);
        Assert.Equal(1, result.Totals.CampaignSeriesCount);
        Assert.Equal(1, result.Totals.CampaignCount);
        Assert.Equal(0, result.Totals.LiveCampaignCount);
        Assert.Equal(1, result.Totals.SubmittedResponseCount);
        Assert.Equal(0, result.Totals.ExportArtifactCount);
        var recent = Assert.Single(result.RecentSeries);
        Assert.Equal(tenantASeries.Id, recent.Id);
        Assert.Equal("Tenant A series", recent.Name);
        Assert.Equal("proof_only", recent.ReadinessStatus);
        Assert.Equal(CampaignSeriesStudyKinds.Own, recent.StudyKind);
        Assert.False(recent.IsSample);
        Assert.Null(recent.SampleScenario);
        Assert.Null(recent.ReadOnlyReason);
    }

    [DockerFact]
    public async Task Export_artifact_library_advertises_pdf_download_and_failed_pdf_retry_safely()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-export-library-actions");
        var otherTemplate = await SeedTenantShellAsync(runtimeOptions, otherTenantId, "tenant-export-library-actions-b");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Export library study");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Export library wave",
            CampaignStatuses.Closed);
        await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            "export-library-report.pdf",
            DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"));
        await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            "export-library-report-failed.pdf",
            DateTimeOffset.Parse("2026-05-06T12:01:00+00:00"),
            ExportArtifactStatuses.Failed,
            failedAt: DateTimeOffset.Parse("2026-05-06T12:01:01+00:00"),
            failureReasonCode: "report_pdf.render_failed");
        var otherSeries = await SeedSeriesAsync(runtimeOptions, otherTenantId, "Other tenant hidden");
        var otherCampaign = await SeedCampaignAsync(
            runtimeOptions,
            otherTenantId,
            otherTemplate.TemplateVersionId,
            otherSeries.Id,
            "Other tenant hidden wave");
        await SeedExportArtifactAsync(
            runtimeOptions,
            otherTenantId,
            otherCampaign.Id,
            otherSeries.Id,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            "other-tenant-hidden.pdf",
            DateTimeOffset.Parse("2026-05-06T12:02:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var managerResult = await store.ListExportArtifactsAsync(
            tenantId,
            canManageSetup: true,
            CancellationToken.None);

        Assert.Equal(tenantId, managerResult.TenantId);
        Assert.Equal(2, managerResult.Summary.TotalCount);
        Assert.Equal(1, managerResult.Summary.DownloadableCount);
        Assert.Equal(1, managerResult.Summary.FailedCount);
        Assert.Equal(0, managerResult.Summary.PendingCount);
        Assert.Equal(1, managerResult.Summary.RetryableCount);
        Assert.DoesNotContain(managerResult.Artifacts, artifact => artifact.FileName == "other-tenant-hidden.pdf");
        var succeededPdf = Assert.Single(
            managerResult.Artifacts,
            artifact => artifact.FileName == "export-library-report.pdf");
        Assert.True(succeededPdf.CanDownload);
        Assert.False(succeededPdf.CanRetry);
        var failedPdf = Assert.Single(
            managerResult.Artifacts,
            artifact => artifact.FileName == "export-library-report-failed.pdf");
        Assert.False(failedPdf.CanDownload);
        Assert.True(failedPdf.CanRetry);

        var serialized = JsonSerializer.Serialize(managerResult);
        Assert.DoesNotContain(SensitiveExportContent, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("storageKey", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebookJson", serialized, StringComparison.OrdinalIgnoreCase);

        var viewerResult = await store.ListExportArtifactsAsync(
            tenantId,
            canManageSetup: false,
            CancellationToken.None);

        Assert.Equal(0, viewerResult.Summary.RetryableCount);
        Assert.All(viewerResult.Artifacts, artifact => Assert.False(artifact.CanDownload));
        Assert.All(viewerResult.Artifacts, artifact => Assert.False(artifact.CanRetry));
    }

    [DockerFact]
    public async Task Workspace_overview_separates_sample_and_own_study_collections()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "tenant-home-collections");
        var sample = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            studyKind: CampaignSeriesStudyKinds.Sample,
            sampleScenario: CampaignSeriesSampleScenarios.MixedLifecycle);
        var own = await SeedSeriesAsync(runtimeOptions, tenantId, "Own study");
        var archived = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Archived starter sample",
            studyKind: CampaignSeriesStudyKinds.Sample,
            sampleScenario: CampaignSeriesSampleScenarios.Longitudinal);
        await ArchiveSeriesAsync(
            runtimeOptions,
            tenantId,
            archived.Id,
            actorUserId,
            DateTimeOffset.Parse("2026-05-12T10:30:00+00:00"),
            "Hidden from home");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetWorkspaceOverviewAsync(
            tenantId,
            canManageSetup: true,
            canManageTeam: false,
            CancellationToken.None);

        var sampleStudy = Assert.Single(result.StudyCollections.SampleStudies);
        Assert.Equal(sample.Id, sampleStudy.Id);
        Assert.Equal(CampaignSeriesStudyKinds.Sample, sampleStudy.StudyKind);
        Assert.True(sampleStudy.IsSample);
        Assert.Equal(CampaignSeriesSampleScenarios.MixedLifecycle, sampleStudy.SampleScenario);
        Assert.Equal(CampaignSeriesReadOnlyReasons.SampleStudy, sampleStudy.ReadOnlyReason);

        var ownStudy = Assert.Single(result.StudyCollections.OwnStudies);
        Assert.Equal(own.Id, ownStudy.Id);
        Assert.Equal(CampaignSeriesStudyKinds.Own, ownStudy.StudyKind);
        Assert.False(ownStudy.IsSample);
        Assert.Null(ownStudy.SampleScenario);
        Assert.Null(ownStudy.ReadOnlyReason);

        Assert.DoesNotContain(
            result.StudyCollections.SampleStudies.Concat(result.StudyCollections.OwnStudies),
            item => item.Id == archived.Id);
    }

    [DockerFact]
    public async Task Workspace_overview_command_center_prioritizes_current_tenant_actions()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var analystUserId = Guid.NewGuid();
        var unassignedUserId = Guid.NewGuid();
        var deletedUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        var template = await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-command-center");
        var tenantBTemplate = await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-command-center-b");
        await SeedTenantMemberRosterAsync(
            runtimeOptions,
            tenantA,
            ownerUserId,
            analystUserId,
            unassignedUserId,
            deletedUserId);
        await SeedExternalAuthIdentityAsync(runtimeOptions, tenantA, ownerUserId);
        var setupSeries = await SeedSeriesAsync(runtimeOptions, tenantA, "Setup blocker");
        var liveSeries = await SeedSeriesAsync(runtimeOptions, tenantA, "Live pulse");
        await SeedCampaignAsync(
            runtimeOptions,
            tenantA,
            template.TemplateVersionId,
            liveSeries.Id,
            "Live campaign",
            CampaignStatuses.Live);
        var reportSeries = await SeedSeriesAsync(runtimeOptions, tenantA, "Report ready");
        var reportCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantA,
            template.TemplateVersionId,
            reportSeries.Id,
            "Submitted campaign");
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantA,
            reportCampaign.Id,
            template.QuestionId);
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B hidden");
        await SeedCampaignAsync(
            runtimeOptions,
            tenantB,
            tenantBTemplate.TemplateVersionId,
            tenantBSeries.Id,
            "Tenant B live",
            CampaignStatuses.Live);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetWorkspaceOverviewAsync(
            tenantA,
            canManageSetup: true,
            canManageTeam: true,
            CancellationToken.None);

        var commands = result.CommandCenter.Items;
        Assert.Equal(
            commands.Select(item => item.Priority).OrderBy(priority => priority),
            commands.Select(item => item.Priority));
        Assert.Contains(
            commands,
            item =>
                item.Id == "directory.setup" &&
                item.State == "blocked" &&
                item.Surface == "directory" &&
                item.Route == "/app/directory" &&
                item.RequiredPermission == "setup.manage");
        Assert.Contains(
            commands,
            item =>
                item.Id == $"series.{setupSeries.Id:N}.setup" &&
                item.Title.Contains("Setup blocker", StringComparison.Ordinal) &&
                item.State == "blocked" &&
                item.Surface == "setup" &&
                item.Route == $"/app/campaign-series/{setupSeries.Id}/setup" &&
                item.CampaignSeriesId == setupSeries.Id);
        Assert.Contains(
            commands,
            item =>
                item.Id == $"series.{liveSeries.Id:N}.operations" &&
                item.State == "ready" &&
                item.Surface == "operations" &&
                item.Route == $"/app/campaign-series/{liveSeries.Id}/operations");
        Assert.Contains(
            commands,
            item =>
                item.Id == $"series.{reportSeries.Id:N}.reports" &&
                item.State == "ready" &&
                item.Surface == "reports" &&
                item.Route == $"/app/campaign-series/{reportSeries.Id}/reports");
        Assert.Contains(
            commands,
            item =>
                item.Id == $"series.{reportSeries.Id:N}.score_remediation" &&
                item.State == "blocked" &&
                item.Surface == "reports" &&
                item.Route == $"/app/campaign-series/{reportSeries.Id}/reports" &&
                item.RequiredPermission == "setup.manage");
        Assert.Contains(
            commands,
            item =>
                item.Id == "team.pending_provider_links" &&
                item.State == "pending" &&
                item.Surface == "team" &&
                item.Route == "/app/team" &&
                item.RequiredPermission == "team.manage");
        Assert.DoesNotContain(commands, item => item.Title.Contains("Tenant B", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task Campaign_series_list_returns_readiness_statuses()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-readiness");
        var noCampaigns = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "No campaigns",
            updatedAt: DateTimeOffset.Parse("2026-05-03T10:00:00+00:00"));
        var pending = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Pending series",
            updatedAt: DateTimeOffset.Parse("2026-05-02T10:00:00+00:00"));
        var proof = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Proof series",
            updatedAt: DateTimeOffset.Parse("2026-05-01T10:00:00+00:00"));
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var pendingCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            pending.Id,
            "Pending campaign");
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            pendingCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-04T09:00:00+00:00"));
        var proofCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            proof.Id,
            "Proof campaign");
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            proofCampaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-05T09:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(),
            CancellationToken.None);

        Assert.Equal(
            [proof.Id, pending.Id, noCampaigns.Id],
            result.Items.Select(item => item.Id).ToArray());
        Assert.Equal("not_configured", result.Items.Single(item => item.Id == noCampaigns.Id).ReadinessStatus);
        Assert.Equal("pending", result.Items.Single(item => item.Id == pending.Id).ReadinessStatus);
        Assert.Equal("proof_only", result.Items.Single(item => item.Id == proof.Id).ReadinessStatus);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-04T09:00:00+00:00"),
            result.Items.Single(item => item.Id == pending.Id).LatestLaunchAt);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-05T09:00:00+00:00"),
            result.Items.Single(item => item.Id == proof.Id).LatestSubmissionAt);
    }

    [DockerFact]
    public async Task Campaign_series_list_projects_sample_metadata()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "tenant-sample-metadata");
        var sample = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            studyKind: CampaignSeriesStudyKinds.Sample,
            sampleScenario: CampaignSeriesSampleScenarios.MixedLifecycle);
        var own = await SeedSeriesAsync(runtimeOptions, tenantId, "Own study");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(Sort: CampaignSeriesPortfolioSorts.NameAsc),
            CancellationToken.None);

        var sampleItem = result.Items.Single(item => item.Id == sample.Id);
        Assert.Equal(CampaignSeriesStudyKinds.Sample, sampleItem.StudyKind);
        Assert.True(sampleItem.IsSample);
        Assert.Equal(CampaignSeriesSampleScenarios.MixedLifecycle, sampleItem.SampleScenario);
        Assert.Equal(CampaignSeriesReadOnlyReasons.SampleStudy, sampleItem.ReadOnlyReason);

        var ownItem = result.Items.Single(item => item.Id == own.Id);
        Assert.Equal(CampaignSeriesStudyKinds.Own, ownItem.StudyKind);
        Assert.False(ownItem.IsSample);
        Assert.Null(ownItem.SampleScenario);
        Assert.Null(ownItem.ReadOnlyReason);
    }

    [DockerFact]
    public async Task Campaign_series_list_filters_by_search_status_and_sort()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-portfolio");
        await SeedSeriesAsync(runtimeOptions, tenantId, "Alpha setup");
        var pending = await SeedSeriesAsync(runtimeOptions, tenantId, "Beta live");
        var proof = await SeedSeriesAsync(runtimeOptions, tenantId, "Gamma proof");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var pendingCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            pending.Id,
            "Pending campaign");
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            pendingCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-04T09:00:00+00:00"));
        var proofCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            proof.Id,
            "Proof campaign");
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            proofCampaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-05T09:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(
                Search: "ga",
                Status: "proof_only",
                Sort: "name_asc"),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(proof.Id, item.Id);
        Assert.Equal("Gamma proof", item.Name);
        Assert.Equal("proof_only", item.ReadinessStatus);
    }

    [DockerFact]
    public async Task Subject_directory_includes_manager_report_counts_and_groups_for_current_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "subject-directory-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "subject-directory-b");
        var manager = await SeedSubjectAsync(runtimeOptions, tenantA, "Mira Manager", "mira@example.test", "mgr-001");
        var employee = await SeedSubjectAsync(
            runtimeOptions,
            tenantA,
            "Ana Analyst",
            "ana@example.test",
            "emp-001",
            """{"directory_import_stale":true,"directory_import_stale_at":"2026-06-11T10:15:00+00:00"}""");
        await SeedSubjectAsync(runtimeOptions, tenantA, "Ivan Intern", "ivan@example.test", "emp-002");
        await SeedSubjectAsync(runtimeOptions, tenantB, "Tenant B Person", "other@example.test", "emp-x");
        var group = await SeedSubjectGroupAsync(runtimeOptions, tenantA, SubjectGroupTypes.Department, "Research");
        await SeedSubjectMembershipAsync(runtimeOptions, tenantA, employee.Id, group.Id, SubjectGroupRoles.Member);
        await SeedSubjectRelationshipAsync(runtimeOptions, tenantA, manager.Id, employee.Id, SubjectRelationshipTypes.ManagerOf);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListSubjectsAsync(tenantA, CancellationToken.None);

        Assert.Equal(tenantA, result.TenantId);
        Assert.Equal(3, result.Summary.SubjectCount);
        Assert.Equal(1, result.Summary.GroupCount);
        Assert.Equal(1, result.Summary.ManagerRelationshipCount);
        Assert.DoesNotContain(result.Subjects, subject => subject.DisplayName == "Tenant B Person");
        var employeeRow = result.Subjects.Single(subject => subject.Id == employee.Id);
        Assert.Equal("Ana Analyst", employeeRow.DisplayName);
        Assert.Equal("ana@example.test", employeeRow.Email);
        Assert.Equal("emp-001", employeeRow.ExternalId);
        Assert.Equal(manager.Id, employeeRow.ManagerSubjectId);
        Assert.Equal("Mira Manager", employeeRow.ManagerDisplayName);
        Assert.Equal(0, employeeRow.DirectReportCount);
        Assert.True(employeeRow.DirectoryImportStale);
        Assert.Equal(
            DateTimeOffset.Parse("2026-06-11T10:15:00+00:00"),
            employeeRow.DirectoryImportStaleAt);
        var membership = Assert.Single(employeeRow.Groups);
        Assert.Equal(group.Id, membership.GroupId);
        Assert.Equal("Research", membership.GroupName);
        Assert.Equal(SubjectGroupTypes.Department, membership.GroupType);
        var managerRow = result.Subjects.Single(subject => subject.Id == manager.Id);
        Assert.Equal(1, managerRow.DirectReportCount);
        Assert.False(managerRow.DirectoryImportStale);
    }

    [DockerFact]
    public async Task Microsoft_graph_directory_connection_state_uses_current_tenant_connection()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "graph-connection-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "graph-connection-b");

        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(seedDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);
            seedDb.DirectoryConnections.Add(new DirectoryConnection(
                Guid.NewGuid(),
                tenantA,
                DirectoryConnectionProviders.MicrosoftGraph,
                "microsoft-tenant-a",
                "Contoso University",
                "contoso.example",
                """["User.Read.All","GroupMember.Read.All"]""",
                DirectoryConnectionStatuses.Active));
            await seedDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var connected = await store.GetMicrosoftGraphDirectoryConnectionStateAsync(tenantA, CancellationToken.None);
        var disconnected = await store.GetMicrosoftGraphDirectoryConnectionStateAsync(tenantB, CancellationToken.None);

        Assert.Equal(tenantA, connected.TenantId);
        Assert.True(connected.Connected);
        Assert.Equal("active", connected.Status);
        Assert.Equal("Contoso University", connected.DisplayName);
        Assert.Equal("contoso.example", connected.PrimaryDomain);
        Assert.Equal(["User.Read.All", "GroupMember.Read.All"], connected.GrantedScopes);
        Assert.Equal(tenantB, disconnected.TenantId);
        Assert.False(disconnected.Connected);
        Assert.Equal("disconnected", disconnected.Status);
        Assert.Empty(disconnected.GrantedScopes);
    }

    [DockerFact]
    public async Task Microsoft_graph_directory_import_rules_are_tenant_scoped_and_safe()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "graph-rules-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "graph-rules-b");
        var connectionAId = Guid.NewGuid();
        var ruleAId = Guid.NewGuid();

        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(seedDb);
            await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantA))
            {
                seedDb.DirectoryConnections.Add(new DirectoryConnection(
                    connectionAId,
                    tenantA,
                    DirectoryConnectionProviders.MicrosoftGraph,
                    "ms-tenant-a",
                    "Contoso University",
                    "contoso.example",
                    """["User.Read.All"]""",
                    DirectoryConnectionStatuses.Active));
                seedDb.DirectoryImportRules.Add(new DirectoryImportRule(
                    ruleAId,
                    tenantA,
                    connectionAId,
                    "All employees",
                    """{"source_kind":"msgraph","population":"all_users","mark_missing_subjects_stale":true}""",
                    """["external_id","email","manager_external_id"]""",
                    DirectoryImportStalePolicies.MarkStale,
                    DirectoryImportRuleStatuses.Active,
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:00:00+00:00")));
                var archived = new DirectoryImportRule(
                    Guid.NewGuid(),
                    tenantA,
                    connectionAId,
                    "Archived employees",
                    """{"source_kind":"msgraph","population":"all_users","mark_missing_subjects_stale":false}""",
                    """["external_id","email"]""",
                    DirectoryImportStalePolicies.None,
                    DirectoryImportRuleStatuses.Active,
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:05:00+00:00"));
                archived.Archive(DateTimeOffset.Parse("2026-06-12T12:06:00+00:00"));
                seedDb.DirectoryImportRules.Add(archived);
                await seedDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantB))
            {
                var connectionBId = Guid.NewGuid();
                seedDb.DirectoryConnections.Add(new DirectoryConnection(
                    connectionBId,
                    tenantB,
                    DirectoryConnectionProviders.MicrosoftGraph,
                    "ms-tenant-b",
                    "Fabrikam",
                    "fabrikam.example",
                    """["User.Read.All"]""",
                    DirectoryConnectionStatuses.Active));
                seedDb.DirectoryImportRules.Add(new DirectoryImportRule(
                    Guid.NewGuid(),
                    tenantB,
                    connectionBId,
                    "Other tenant employees",
                    """{"source_kind":"msgraph","population":"all_users","mark_missing_subjects_stale":true}""",
                    """["external_id","email"]""",
                    DirectoryImportStalePolicies.MarkStale,
                    DirectoryImportRuleStatuses.Active,
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:10:00+00:00")));
                await seedDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var response = await store.ListMicrosoftGraphDirectoryImportRulesAsync(tenantA, CancellationToken.None);

        Assert.Equal(tenantA, response.TenantId);
        var rule = Assert.Single(response.Rules);
        Assert.Equal(ruleAId, rule.Id);
        Assert.Equal(connectionAId, rule.DirectoryConnectionId);
        Assert.Equal("All employees", rule.Name);
        Assert.Equal(DirectoryImportRuleStatuses.Active, rule.Status);
        Assert.Equal(DirectoryImportStalePolicies.MarkStale, rule.StalePolicy);
        Assert.Equal(["external_id", "email", "manager_external_id"], rule.RetainedFields);
        Assert.Equal(DateTimeOffset.Parse("2026-06-12T12:00:00+00:00"), rule.CreatedAt);
    }

    [DockerFact]
    public async Task Microsoft_graph_directory_import_runs_history_is_tenant_scoped_and_safe()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "graph-runs-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "graph-runs-b");
        var connectionAId = Guid.NewGuid();
        var previewRunId = Guid.NewGuid();

        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(seedDb);
            await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantA))
            {
                seedDb.DirectoryConnections.Add(new DirectoryConnection(
                    connectionAId,
                    tenantA,
                    DirectoryConnectionProviders.MicrosoftGraph,
                    "ms-tenant-a",
                    "Contoso University",
                    "contoso.example",
                    """["User.Read.All"]""",
                    DirectoryConnectionStatuses.Active));
                var preview = new DirectoryImportRun(
                    previewRunId,
                    tenantA,
                    connectionAId,
                    DirectoryImportRunModes.Preview,
                    """{"source_kind":"msgraph","source_prefix_present":true}""",
                    retainedFields: """["external_id","email"]""",
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:00:00+00:00"));
                preview.Start(DateTimeOffset.Parse("2026-06-12T12:00:01+00:00"));
                preview.Succeed(
                    """{"row_count":4,"imported_row_count":3,"failed_row_count":1}""",
                    """["row_failed"]""",
                    """{"source_kind":"msgraph","completed":true}""",
                    DateTimeOffset.Parse("2026-06-12T12:00:02+00:00"));
                seedDb.DirectoryImportRuns.Add(preview);
                await seedDb.SaveChangesAsync();

                var apply = new DirectoryImportRun(
                    Guid.NewGuid(),
                    tenantA,
                    connectionAId,
                    DirectoryImportRunModes.Apply,
                    """{"source_kind":"msgraph","source_prefix_present":true}""",
                    retainedFields: """["external_id","email"]""",
                    previewRunId: previewRunId,
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:10:00+00:00"));
                apply.Start(DateTimeOffset.Parse("2026-06-12T12:10:01+00:00"));
                apply.Succeed(
                    """{"row_count":4,"imported_row_count":4,"failed_row_count":0}""",
                    "[]",
                    """{"source_kind":"msgraph","completed":true}""",
                    DateTimeOffset.Parse("2026-06-12T12:10:02+00:00"));
                seedDb.DirectoryImportRuns.Add(apply);
                await seedDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantB))
            {
                var connectionBId = Guid.NewGuid();
                seedDb.DirectoryConnections.Add(new DirectoryConnection(
                    connectionBId,
                    tenantB,
                    DirectoryConnectionProviders.MicrosoftGraph,
                    "ms-tenant-b",
                    "Other Tenant",
                    null,
                    "[]",
                    DirectoryConnectionStatuses.Active));
                var otherRun = new DirectoryImportRun(
                    Guid.NewGuid(),
                    tenantB,
                    connectionBId,
                    DirectoryImportRunModes.Preview,
                    """{"source_kind":"msgraph","source_prefix_present":true}""",
                    observedAt: DateTimeOffset.Parse("2026-06-12T12:20:00+00:00"));
                otherRun.Start(DateTimeOffset.Parse("2026-06-12T12:20:01+00:00"));
                otherRun.Succeed(
                    """{"row_count":99,"imported_row_count":99,"failed_row_count":0}""",
                    "[]",
                    """{"source_kind":"msgraph","completed":true}""",
                    DateTimeOffset.Parse("2026-06-12T12:20:02+00:00"));
                seedDb.DirectoryImportRuns.Add(otherRun);
                await seedDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListMicrosoftGraphDirectoryImportRunsAsync(tenantA, CancellationToken.None);

        Assert.Equal(tenantA, result.TenantId);
        Assert.Equal(2, result.Runs.Count);
        var latest = result.Runs[0];
        Assert.Equal(DirectoryImportRunModes.Apply, latest.Mode);
        Assert.Equal(DirectoryImportRunStatuses.Succeeded, latest.Status);
        Assert.Equal(previewRunId, latest.PreviewRunId);
        Assert.Equal(4, latest.RowCount);
        Assert.Equal(4, latest.ImportedRowCount);
        Assert.Equal(0, latest.FailedRowCount);
        var previewItem = result.Runs[1];
        Assert.Equal(1, previewItem.WarningCategoryCount);
        Assert.Equal(["row_failed"], previewItem.WarningCategories);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.DoesNotContain("ms-tenant-a", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ms-tenant-b", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ana@example.test", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user-001", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("source_prefix", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("99", json, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Subject_groups_list_counts_only_current_tenant_memberships()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "subject-groups-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "subject-groups-b");
        var subject = await SeedSubjectAsync(runtimeOptions, tenantA, "Ana Analyst", "ana@example.test", "emp-001");
        var tenantAGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantA, SubjectGroupTypes.Team, "Research Team");
        var tenantBGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantB, SubjectGroupTypes.Team, "Other Team");
        await SeedSubjectMembershipAsync(runtimeOptions, tenantA, subject.Id, tenantAGroup.Id, SubjectGroupRoles.Member);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListSubjectGroupsAsync(tenantA, CancellationToken.None);

        Assert.Equal(tenantA, result.TenantId);
        var group = Assert.Single(result.Groups);
        Assert.Equal(tenantAGroup.Id, group.Id);
        Assert.Equal("Research Team", group.Name);
        Assert.Equal(1, group.MemberCount);
        Assert.DoesNotContain(result.Groups, item => item.Id == tenantBGroup.Id);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_self_uses_active_campaign_audience_members()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-self");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview self series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview self campaign");
        var ana = await SeedSubjectAsync(runtimeOptions, tenantId, "Ana Analyst", "ana@example.test", "emp-001");
        var ivan = await SeedSubjectAsync(runtimeOptions, tenantId, "Ivan Intern", "ivan@example.test", "emp-002");
        await SeedSubjectAsync(runtimeOptions, tenantId, "Mira Manager", "mira@example.test", "mgr-001");
        await SeedAudienceAsync(runtimeOptions, tenantId, campaign.Id, ana.Id, ivan.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"self"}""", MaxRows: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("self", result.Value.RuleKind);
        Assert.Equal("self", result.Value.Role);
        Assert.Equal(2, result.Value.Summary.TargetCount);
        Assert.Equal(2, result.Value.Summary.RespondentCount);
        Assert.Equal(2, result.Value.Summary.AssignmentPairCount);
        Assert.False(result.Value.Summary.Truncated);
        Assert.Empty(result.Value.Warnings);
        Assert.Equal([ana.Id, ivan.Id], result.Value.Rows.Select(row => row.Target!.Id).ToArray());
        Assert.All(result.Value.Rows, row => Assert.Equal(row.Target!.Id, row.Respondent!.Id));
        Assert.DoesNotContain(result.Value.Rows, row => row.Target!.DisplayName == "Mira Manager");
    }

    [DockerFact]
    public async Task Respondent_rule_preview_self_falls_back_to_active_tenant_subjects_without_campaign_audience()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-self-fallback");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview self fallback series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview self fallback campaign");
        var ana = await SeedSubjectAsync(runtimeOptions, tenantId, "Ana Analyst", "ana@example.test", "emp-001");
        var ivan = await SeedSubjectAsync(runtimeOptions, tenantId, "Ivan Intern", "ivan@example.test", "emp-002");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"self"}""", MaxRows: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(2, result.Value.Summary.TargetCount);
        Assert.Equal(2, result.Value.Summary.RespondentCount);
        Assert.Equal([ana.Id, ivan.Id], result.Value.Rows.Select(row => row.Target!.Id).ToArray());
        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal("respondent_rule_preview.audience_missing", warning.Code);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_all_in_group_returns_active_current_tenant_members()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-group");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview group series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview group campaign");
        var ana = await SeedSubjectAsync(runtimeOptions, tenantId, "Ana Analyst", "ana@example.test", "emp-001");
        var ivan = await SeedSubjectAsync(runtimeOptions, tenantId, "Ivan Intern", "ivan@example.test", "emp-002");
        var group = await SeedSubjectGroupAsync(runtimeOptions, tenantId, SubjectGroupTypes.Team, "Research Team");
        await SeedSubjectMembershipAsync(runtimeOptions, tenantId, ana.Id, group.Id, SubjectGroupRoles.Member);
        await SeedSubjectMembershipAsync(runtimeOptions, tenantId, ivan.Id, group.Id, SubjectGroupRoles.Member);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest(
                """{"kind":"all_in_group"}""",
                GroupId: group.Id,
                MaxRows: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("all_in_group", result.Value.RuleKind);
        Assert.Equal("group_member", result.Value.Role);
        Assert.Equal(0, result.Value.Summary.TargetCount);
        Assert.Equal(2, result.Value.Summary.RespondentCount);
        Assert.Equal(2, result.Value.Summary.AssignmentPairCount);
        Assert.All(result.Value.Rows, row => Assert.Null(row.Target));
        Assert.Equal([ana.Id, ivan.Id], result.Value.Rows.Select(row => row.Respondent!.Id).ToArray());
    }

    [DockerFact]
    public async Task Respondent_rule_preview_all_in_group_requires_group_selector()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-group-required");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview group required series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview group required campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"all_in_group"}"""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("respondent_rule_preview.group_required", result.Error.Code);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_all_in_group_fails_closed_for_cross_tenant_group()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantA, "preview-group-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "preview-group-b");
        var series = await SeedSeriesAsync(runtimeOptions, tenantA, "Preview group series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantA,
            template.TemplateVersionId,
            series.Id,
            "Preview group campaign");
        var otherGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantB, SubjectGroupTypes.Team, "Other Team");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantA,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest(
                """{"kind":"all_in_group"}""",
                GroupId: otherGroup.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("subject_group.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_returns_not_found_for_cross_tenant_campaign()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "preview-campaign-a");
        var tenantBTemplate = await SeedTenantShellAsync(runtimeOptions, tenantB, "preview-campaign-b");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B series");
        var tenantBCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantB,
            tenantBTemplate.TemplateVersionId,
            tenantBSeries.Id,
            "Tenant B campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantA,
            tenantBSeries.Id,
            tenantBCampaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"self"}"""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_manager_and_reports_resolve_manager_of_relationships()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-manager");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview manager series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview manager campaign");
        var manager = await SeedSubjectAsync(runtimeOptions, tenantId, "Mira Manager", "mira@example.test", "mgr-001");
        var ana = await SeedSubjectAsync(runtimeOptions, tenantId, "Ana Analyst", "ana@example.test", "emp-001");
        var ivan = await SeedSubjectAsync(runtimeOptions, tenantId, "Ivan Intern", "ivan@example.test", "emp-002");
        await SeedSubjectRelationshipAsync(runtimeOptions, tenantId, manager.Id, ana.Id, SubjectRelationshipTypes.ManagerOf);
        await SeedSubjectRelationshipAsync(runtimeOptions, tenantId, manager.Id, ivan.Id, SubjectRelationshipTypes.ManagerOf);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var managerResult = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest(
                """{"kind":"manager_of_target"}""",
                TargetSubjectId: ana.Id),
            CancellationToken.None);
        var reportsResult = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest(
                """{"kind":"reports_of_target"}""",
                TargetSubjectId: manager.Id),
            CancellationToken.None);

        Assert.True(managerResult.IsSuccess, managerResult.Error.ToString());
        var managerRow = Assert.Single(managerResult.Value.Rows);
        Assert.Equal(ana.Id, managerRow.Target!.Id);
        Assert.Equal(manager.Id, managerRow.Respondent!.Id);
        Assert.Equal("manager", managerResult.Value.Role);

        Assert.True(reportsResult.IsSuccess, reportsResult.Error.ToString());
        Assert.Equal("direct_report", reportsResult.Value.Role);
        Assert.Equal([ana.Id, ivan.Id], reportsResult.Value.Rows.Select(row => row.Respondent!.Id).ToArray());
        Assert.All(reportsResult.Value.Rows, row => Assert.Equal(manager.Id, row.Target!.Id));
    }

    [DockerFact]
    public async Task Respondent_rule_preview_manager_of_target_requires_target_selector()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-target-required");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview target required series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview target required campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"manager_of_target"}"""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("respondent_rule_preview.target_required", result.Error.Code);
    }

    [DockerFact]
    public async Task Respondent_rule_preview_returns_validation_for_unsupported_kind()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "preview-unsupported");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Preview unsupported series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Preview unsupported campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.PreviewRespondentRuleAsync(
            tenantId,
            series.Id,
            campaign.Id,
            new RespondentRulePreviewRequest("""{"kind":"peers_of_target"}"""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("respondent_rule_preview.unsupported_kind", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_list_hides_archived_by_default_and_supports_visibility_filter()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var archivedAt = DateTimeOffset.Parse("2026-05-11T10:30:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-portfolio-archive");
        var active = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Active pulse",
            updatedAt: DateTimeOffset.Parse("2026-05-10T09:00:00+00:00"));
        var archived = await SeedSeriesAsync(runtimeOptions, tenantId, "Archived pulse");
        await ArchiveSeriesAsync(runtimeOptions, tenantId, archived.Id, actorUserId, archivedAt, "Out of rotation");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var defaultResult = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(),
            CancellationToken.None);
        var archivedResult = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(Visibility: CampaignSeriesPortfolioVisibilities.Archived),
            CancellationToken.None);
        var allResult = await store.ListCampaignSeriesAsync(
            tenantId,
            new CampaignSeriesPortfolioQuery(Visibility: CampaignSeriesPortfolioVisibilities.All),
            CancellationToken.None);

        var defaultItem = Assert.Single(defaultResult.Items);
        Assert.Equal(active.Id, defaultItem.Id);
        Assert.False(defaultItem.Archived);

        var archivedItem = Assert.Single(archivedResult.Items);
        Assert.Equal(archived.Id, archivedItem.Id);
        Assert.True(archivedItem.Archived);
        Assert.Equal(archivedAt, archivedItem.ArchivedAt);
        Assert.Equal(actorUserId, archivedItem.ArchivedByUserId);
        Assert.Equal("Out of rotation", archivedItem.ArchiveReason);

        Assert.Equal(
            [archived.Id, active.Id],
            allResult.Items.Select(item => item.Id).ToArray());
    }

    [DockerFact]
    public async Task Tenant_member_roster_returns_current_tenant_members_roles_and_permissions()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantAOwnerId = Guid.NewGuid();
        var tenantAAnalystId = Guid.NewGuid();
        var tenantAUnassignedUserId = Guid.NewGuid();
        var tenantADeletedUserId = Guid.NewGuid();
        var tenantBUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-members");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-members");
        var ownerRoleId = await SeedTenantMemberRosterAsync(
            runtimeOptions,
            tenantA,
            tenantAOwnerId,
            tenantAAnalystId,
            tenantAUnassignedUserId,
            tenantADeletedUserId);
        await SeedTenantBMemberAsync(runtimeOptions, tenantB, tenantBUserId);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListTenantMembersAsync(tenantA, CancellationToken.None);

        Assert.Equal(tenantA, result.TenantId);
        Assert.Equal([tenantAAnalystId, tenantAOwnerId], result.Members.Select(member => member.UserId).ToArray());
        Assert.DoesNotContain(result.Members, member => member.UserId == tenantAUnassignedUserId);
        Assert.DoesNotContain(result.Members, member => member.UserId == tenantADeletedUserId);
        Assert.DoesNotContain(result.Members, member => member.UserId == tenantBUserId);

        var owner = result.Members.Single(member => member.UserId == tenantAOwnerId);
        Assert.Equal("owner@example.test", owner.Email);
        Assert.Equal("en", owner.Locale);
        Assert.Equal(DateTimeOffset.Parse("2026-05-10T08:00:00+00:00"), owner.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-05-11T09:00:00+00:00"), owner.LastLoginAt);
        Assert.Equal(["export.read", "setup.manage"], owner.Permissions);
        var ownerRole = Assert.Single(owner.Roles);
        Assert.Equal(ownerRoleId, ownerRole.RoleId);
        Assert.Equal("tenant_owner", ownerRole.Code);
        Assert.Equal("Tenant Owner", ownerRole.Name);
        Assert.Equal(RoleAssignmentScopes.Tenant, ownerRole.ScopeType);
        Assert.Null(ownerRole.ScopeId);

        var analyst = result.Members.Single(member => member.UserId == tenantAAnalystId);
        Assert.Equal("analyst@example.test", analyst.Email);
        Assert.Equal(["export.read"], analyst.Permissions);
        Assert.Equal("analyst", Assert.Single(analyst.Roles).Code);
    }

    [DockerFact]
    public async Task Tenant_roles_returns_current_tenant_assignable_roles_with_sorted_permissions()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-roles");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-roles");
        var seeded = await SeedTenantRolesAsync(runtimeOptions, tenantA, tenantB);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListTenantRolesAsync(tenantA, CancellationToken.None);

        Assert.Equal([seeded.AnalystRoleId, seeded.OwnerRoleId], result.Roles.Select(role => role.RoleId).ToArray());
        var analyst = result.Roles.Single(role => role.Code == "analyst");
        Assert.Equal("Analyst", analyst.Name);
        Assert.Equal(["export.read"], analyst.Permissions);
        var owner = result.Roles.Single(role => role.Code == "tenant_owner");
        Assert.Equal("Tenant Owner", owner.Name);
        Assert.Equal(["export.read", "setup.manage", "team.manage"], owner.Permissions);
        Assert.DoesNotContain(result.Roles, role => role.Code == "foreign_owner");
    }

    [DockerFact]
    public async Task Tenant_member_roster_marks_provider_binding_status()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var analystUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-member-status");
        await SeedTenantMemberRosterAsync(
            runtimeOptions,
            tenantId,
            ownerUserId,
            analystUserId,
            Guid.NewGuid(),
            Guid.NewGuid());
        await SeedExternalAuthIdentityAsync(runtimeOptions, tenantId, ownerUserId);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.ListTenantMembersAsync(tenantId, CancellationToken.None);

        Assert.Equal("active", result.Members.Single(member => member.UserId == ownerUserId).IdentityStatus);
        Assert.Equal(
            "pending_provider_link",
            result.Members.Single(member => member.UserId == analystUserId).IdentityStatus);
    }

    [DockerFact]
    public async Task Campaign_series_hub_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantA, tenantBSeries.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_hub_exposes_archive_state_for_direct_inspection()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var archivedAt = DateTimeOffset.Parse("2026-05-11T10:30:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-hub-archive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Archived hub series");
        await ArchiveSeriesAsync(runtimeOptions, tenantId, series.Id, actorUserId, archivedAt, "Completed pilot");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantId, series.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.True(result.Value.Archived);
        Assert.Equal(archivedAt, result.Value.ArchivedAt);
        Assert.Equal(actorUserId, result.Value.ArchivedByUserId);
        Assert.Equal("Completed pilot", result.Value.ArchiveReason);
    }

    [DockerFact]
    public async Task Campaign_series_hub_returns_blocked_lifecycle_without_campaigns()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-hub-lifecycle-blocked");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Blocked lifecycle series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantId, series.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Collection(
            result.Value.Lifecycle,
            setup =>
            {
                Assert.Equal("setup", setup.Id);
                Assert.Equal("blocked", setup.Status);
                Assert.Equal("setup", setup.Route);
            },
            operations =>
            {
                Assert.Equal("operations", operations.Id);
                Assert.Equal("blocked", operations.Status);
                Assert.Equal("operations", operations.Route);
            },
            reports =>
            {
                Assert.Equal("reports", reports.Id);
                Assert.Equal("pending", reports.Status);
                Assert.Equal("reports", reports.Route);
            },
            waves =>
            {
                Assert.Equal("waves", waves.Id);
                Assert.Equal("not_available", waves.Status);
                Assert.Equal("waves", waves.Route);
            });
    }

    [DockerFact]
    public async Task Campaign_series_hub_returns_ready_lifecycle_for_collecting_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-hub-lifecycle-ready");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Ready lifecycle series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var reportCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reportable campaign",
            status: CampaignStatuses.Live);
        var longitudinalCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Longitudinal wave",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            reportCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            longitudinalCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-07T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            reportCampaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await SeedScoreAsync(runtimeOptions, tenantId, reportCampaign.Id, session.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, reportCampaign.Id, series.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantId, series.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("ready", result.Value.Lifecycle.Single(item => item.Id == "setup").Status);
        Assert.Equal("ready", result.Value.Lifecycle.Single(item => item.Id == "operations").Status);
        Assert.Equal("ready", result.Value.Lifecycle.Single(item => item.Id == "reports").Status);
        Assert.Equal("pending", result.Value.Lifecycle.Single(item => item.Id == "waves").Status);
    }

    [DockerFact]
    public async Task Campaign_series_hub_marks_waves_ready_with_complete_trajectories()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-hub-lifecycle-waves-ready");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Waves-ready lifecycle series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var baseline = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 1",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var comparison = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 2",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            baseline,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            comparison,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);
        var participantCode = await SeedParticipantCodeAsync(runtimeOptions, tenantId, series.Id);
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            baseline.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            participantCodeId: participantCode.Id);
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            comparison.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00"),
            participantCodeId: participantCode.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantId, series.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("ready", result.Value.Lifecycle.Single(item => item.Id == "waves").Status);
    }

    [DockerFact]
    public async Task Campaign_series_hub_excludes_sensitive_fields()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-hub-sensitive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sensitive series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive campaign",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            includeSensitiveValues: true);
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);

        var commandRecorder = new RecordingCommandInterceptor();
        await using var db = new ApplicationDbContext(CreateRuntimeOptions(commandRecorder));
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesHubAsync(tenantId, series.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("CodeSalt", serialized);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain("ParticipantCode", serialized);
        Assert.DoesNotContain(Convert.ToBase64String(CreateCodeSalt()), serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);
        Assert.Equal(1, result.Value.Totals.CampaignCount);
        Assert.Equal(1, result.Value.Totals.LiveCampaignCount);
        Assert.Equal(1, result.Value.Totals.SubmittedResponseCount);
        Assert.Equal(1, result.Value.Totals.ScoreCount);
        Assert.Equal(1, result.Value.Totals.ExportArtifactCount);
        Assert.Equal("proof_only", result.Value.Governance.ConsentStatus);
        Assert.Equal("proof_only", result.Value.Governance.RetentionStatus);
        Assert.Equal("proof_only", result.Value.Governance.DisclosureStatus);
        Assert.Equal("proof_only", result.Value.Governance.ScoringStatus);
        var campaignRow = Assert.Single(result.Value.Campaigns);
        Assert.Equal(campaign.Id, campaignRow.Id);
        Assert.Equal("Sensitive campaign", campaignRow.Name);
        Assert.Equal(1, campaignRow.SubmittedResponseCount);
        Assert.Equal(1, campaignRow.ScoreCount);
        Assert.Equal(1, campaignRow.ExportArtifactCount);

        var storeSql = string.Join('\n', commandRecorder.Commands);
        Assert.DoesNotContain("code_salt", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ip_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_agent_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebook_json", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code\"", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("code_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("argon2", storeSql, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Campaign_series_setup_workspace_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-setup-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-setup-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B setup series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesSetupWorkspaceAsync(
            tenantA,
            tenantBSeries.Id,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_setup_workspace_returns_missing_prerequisites_for_empty_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-setup-empty");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Empty setup series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesSetupWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal("Empty setup series", result.Value.Series.Name);
        Assert.Equal(0, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Null(result.Value.SelectedCampaign);
        Assert.Null(result.Value.Template);
        Assert.Null(result.Value.Scoring);
        Assert.Equal("not_configured", result.Value.Policies.Consent.Status);
        Assert.Equal("not_configured", result.Value.Policies.Retention.Status);
        Assert.Equal("not_configured", result.Value.Policies.Disclosure.Status);
        Assert.Equal("not_available", result.Value.Readiness.Status);
        Assert.False(result.Value.Readiness.Ready);
        Assert.Null(result.Value.Readiness.CampaignId);
        Assert.Empty(result.Value.Campaigns);
        Assert.Equal(6, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Equal(6, result.Value.MissingPrerequisites.Count);
        var missingCodes = result.Value.MissingPrerequisites.Select(item => item.Code).ToArray();
        Assert.Contains("campaign.missing", missingCodes);
        Assert.Contains("template.missing", missingCodes);
        Assert.Contains("scoring_rule.missing", missingCodes);
        Assert.Contains("consent_document.missing", missingCodes);
        Assert.Contains("retention_policy.missing", missingCodes);
        Assert.Contains("disclosure_policy.missing", missingCodes);
    }

    [DockerFact]
    public async Task Campaign_series_setup_workspace_returns_setup_state_for_configured_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-setup-configured");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Configured setup series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var policies = await SeedPoliciesAsync(
            runtimeOptions,
            tenantId,
            series.Id,
            DateTimeOffset.Parse("2026-05-06T08:00:00+00:00"));
        var draftCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Configured draft campaign");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesSetupWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal(1, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(0, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Empty(result.Value.MissingPrerequisites);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(draftCampaign.Id, result.Value.SelectedCampaign.Id);
        Assert.Equal("Configured draft campaign", result.Value.SelectedCampaign.Name);
        Assert.Equal(CampaignStatuses.Draft, result.Value.SelectedCampaign.Status);
        Assert.Equal(ResponseIdentityModes.Anonymous, result.Value.SelectedCampaign.ResponseIdentityMode);
        Assert.Equal("en", result.Value.SelectedCampaign.DefaultLocale);
        Assert.Equal(template.TemplateVersionId, result.Value.SelectedCampaign.TemplateVersionId);
        Assert.Null(result.Value.SelectedCampaign.LatestLaunchAt);

        Assert.NotNull(result.Value.Template);
        Assert.Equal(template.TemplateVersionId, result.Value.Template.TemplateVersionId);
        Assert.Equal("tenant-setup-configured pulse", result.Value.Template.TemplateName);
        Assert.Equal("1.0.0", result.Value.Template.Semver);
        Assert.Equal(TemplateVersionStatuses.Draft, result.Value.Template.Status);
        Assert.Equal("en", result.Value.Template.DefaultLocale);
        Assert.Null(result.Value.Template.InstrumentId);
        Assert.Equal(1, result.Value.Template.QuestionCount);

        Assert.NotNull(result.Value.Scoring);
        Assert.Equal(scoringRule.Id, result.Value.Scoring.Id);
        Assert.Equal("burnout.total", result.Value.Scoring.RuleKey);
        Assert.Equal("1.0.0", result.Value.Scoring.RuleVersion);
        Assert.Equal(ScoringRuleStatuses.Draft, result.Value.Scoring.Status);
        Assert.Equal("template_version", result.Value.Scoring.Source);

        Assert.Equal(policies.ConsentId, result.Value.Policies.Consent.Id);
        Assert.Equal("1.0.0", result.Value.Policies.Consent.Version);
        Assert.Equal("configured", result.Value.Policies.Consent.Status);
        Assert.Equal(
            [
                new CampaignSeriesSetupPolicyDetailResponse("Title", "Consent"),
                new CampaignSeriesSetupPolicyDetailResponse("Locale", "en"),
                new CampaignSeriesSetupPolicyDetailResponse("Required grants", "1"),
                new CampaignSeriesSetupPolicyDetailResponse("Optional grants", "0"),
                new CampaignSeriesSetupPolicyDetailResponse("Published", "2026-05-06")
            ],
            result.Value.Policies.Consent.Details);
        Assert.Equal(policies.RetentionId, result.Value.Policies.Retention.Id);
        Assert.Equal("1.0.0", result.Value.Policies.Retention.Version);
        Assert.Equal("configured", result.Value.Policies.Retention.Status);
        Assert.Equal(
            [
                new CampaignSeriesSetupPolicyDetailResponse("Retain for", "1 year"),
                new CampaignSeriesSetupPolicyDetailResponse("Starts from", "response submitted at"),
                new CampaignSeriesSetupPolicyDetailResponse("Action after retention", "anonymize"),
                new CampaignSeriesSetupPolicyDetailResponse("Next review", "2027-05-06")
            ],
            result.Value.Policies.Retention.Details);
        Assert.Equal(policies.DisclosureId, result.Value.Policies.Disclosure.Id);
        Assert.Equal("1.0.0", result.Value.Policies.Disclosure.Version);
        Assert.Equal("configured", result.Value.Policies.Disclosure.Status);
        Assert.Equal(
            [
                new CampaignSeriesSetupPolicyDetailResponse("Minimum group size", "5"),
                new CampaignSeriesSetupPolicyDetailResponse("Suppression", "hide cell"),
                new CampaignSeriesSetupPolicyDetailResponse("Applies to", "total")
            ],
            result.Value.Policies.Disclosure.Details);

        Assert.Equal(draftCampaign.Id, result.Value.Readiness.CampaignId);
        Assert.Equal("ready", result.Value.Readiness.Status);
        Assert.True(result.Value.Readiness.Ready);
        var campaign = Assert.Single(result.Value.Campaigns);
        Assert.Equal(draftCampaign.Id, campaign.Id);
    }

    [DockerFact]
    public async Task Campaign_series_setup_workspace_excludes_sensitive_fields()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-setup-sensitive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sensitive setup series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        await SeedPoliciesAsync(
            runtimeOptions,
            tenantId,
            series.Id,
            DateTimeOffset.Parse("2026-05-06T08:00:00+00:00"));
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive setup campaign",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"));
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            includeSensitiveValues: true);
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);

        var commandRecorder = new RecordingCommandInterceptor();
        await using var db = new ApplicationDbContext(CreateRuntimeOptions(commandRecorder));
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesSetupWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("CodeSalt", serialized);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain(Convert.ToBase64String(CreateCodeSalt()), serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);

        var storeSql = string.Join('\n', commandRecorder.Commands);
        Assert.DoesNotContain("code_salt", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ip_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_agent_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebook_json", storeSql, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-operations-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-operations-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B operations series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantA,
            tenantBSeries.Id,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_missing_prerequisites_for_empty_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-empty");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Empty operations series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal("Empty operations series", result.Value.Series.Name);
        Assert.Equal(0, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(0, result.Value.Summary.OpenLinkAssignmentCount);
        Assert.Equal(0, result.Value.Summary.QueuedInvitationCount);
        Assert.Equal(0, result.Value.Summary.SentInvitationCount);
        Assert.Equal(0, result.Value.Summary.FailedInvitationCount);
        Assert.Equal(0, result.Value.Summary.DeliveryAttemptCount);
        Assert.Equal(0, result.Value.Summary.SubmittedResponseCount);
        Assert.Equal(5, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Null(result.Value.SelectedCampaign);
        Assert.Empty(result.Value.Campaigns);
        var missingCodes = result.Value.MissingPrerequisites.Select(item => item.Code).ToArray();
        Assert.Contains("campaign.missing", missingCodes);
        Assert.Contains("launchable_campaign.missing", missingCodes);
        Assert.Contains("live_campaign.missing", missingCodes);
        Assert.Contains("public_entry.missing", missingCodes);
        Assert.Contains("invitations.missing", missingCodes);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_launch_and_delivery_state()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-configured");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Configured operations series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var draftCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Draft wave");
        var liveCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Live wave",
            status: CampaignStatuses.Live);
        var launchSnapshot = await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            liveCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            "queued@example.test",
            NotificationStatuses.Queued);
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            "sent@example.test",
            NotificationStatuses.Sent,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-06T11:00:00+00:00"));
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            "failed@example.test",
            NotificationStatuses.Failed,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal(2, result.Value.Summary.CampaignCount);
        Assert.Equal(1, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(1, result.Value.Summary.OpenLinkAssignmentCount);
        Assert.Equal(1, result.Value.Summary.QueuedInvitationCount);
        Assert.Equal(1, result.Value.Summary.SentInvitationCount);
        Assert.Equal(1, result.Value.Summary.FailedInvitationCount);
        Assert.Equal(2, result.Value.Summary.DeliveryAttemptCount);
        Assert.Equal(1, result.Value.Summary.SubmittedResponseCount);
        Assert.Equal(0, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Empty(result.Value.MissingPrerequisites);

        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(liveCampaign.Id, result.Value.SelectedCampaign.Id);
        Assert.Equal(CampaignStatuses.Live, result.Value.SelectedCampaign.Status);
        Assert.Equal(launchSnapshot.Id, result.Value.SelectedCampaign.LatestLaunchSnapshotId);
        Assert.Equal(DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"), result.Value.SelectedCampaign.LatestLaunchAt);
        Assert.NotNull(result.Value.SelectedCampaign.LaunchSnapshot);
        Assert.Equal(launchSnapshot.Id, result.Value.SelectedCampaign.LaunchSnapshot.Id);
        Assert.Equal(template.TemplateVersionId, result.Value.SelectedCampaign.LaunchSnapshot.TemplateVersionId);
        Assert.Equal(scoringRule.Id, result.Value.SelectedCampaign.LaunchSnapshot.ScoringRuleId);
        Assert.Equal(launchSnapshot.ConsentDocumentId, result.Value.SelectedCampaign.LaunchSnapshot.ConsentDocumentId);
        Assert.Equal(launchSnapshot.RetentionPolicyId, result.Value.SelectedCampaign.LaunchSnapshot.RetentionPolicyId);
        Assert.Equal(launchSnapshot.DisclosurePolicyId, result.Value.SelectedCampaign.LaunchSnapshot.DisclosurePolicyId);
        Assert.Equal(liveCampaign.ResponseIdentityMode, result.Value.SelectedCampaign.LaunchSnapshot.ResponseIdentityMode);
        Assert.Equal(liveCampaign.DefaultLocale, result.Value.SelectedCampaign.LaunchSnapshot.DefaultLocale);
        Assert.Equal(1, result.Value.SelectedCampaign.LaunchSnapshot.TemplateQuestionCount);
        Assert.NotNull(result.Value.SelectedCampaign.LaunchSnapshot.LaunchPacket);
        Assert.Equal(1, result.Value.SelectedCampaign.LaunchSnapshot.LaunchPacket.SchemaVersion);
        Assert.Contains("scoring", result.Value.SelectedCampaign.LaunchSnapshot.LaunchPacket.Sections);
        Assert.Equal("runtime_launch", result.Value.SelectedCampaign.LaunchSnapshot.LaunchPacket.Source);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            result.Value.SelectedCampaign.LaunchSnapshot.LaunchedAt);
        Assert.Equal(1, result.Value.SelectedCampaign.OpenLinkAssignmentCount);
        Assert.Equal(1, result.Value.SelectedCampaign.QueuedInvitationCount);
        Assert.Equal(1, result.Value.SelectedCampaign.SentInvitationCount);
        Assert.Equal(1, result.Value.SelectedCampaign.FailedInvitationCount);
        Assert.Equal(2, result.Value.SelectedCampaign.DeliveryAttemptCount);
        Assert.Equal(DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"), result.Value.SelectedCampaign.LatestDeliveryAttemptAt);

        Assert.Equal([liveCampaign.Id, draftCampaign.Id], result.Value.Campaigns.Select(campaign => campaign.Id).ToArray());
        var draft = result.Value.Campaigns.Single(campaign => campaign.Id == draftCampaign.Id);
        Assert.Equal(CampaignStatuses.Draft, draft.Status);
        Assert.Null(draft.LatestLaunchSnapshotId);
        Assert.Equal(0, draft.OpenLinkAssignmentCount);
        Assert.Equal(0, draft.DeliveryAttemptCount);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_reports_safe_group_coverage()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-group-coverage");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Coverage series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var liveCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Coverage wave",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.Identified);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            liveCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-06-01T09:00:00+00:00"),
            configured: true);

        var group = await SeedSubjectGroupAsync(runtimeOptions, tenantId, SubjectGroupTypes.Department, "ICU");
        var submittedMember = await SeedSubjectAsync(
            runtimeOptions, tenantId, "Submitted Member", "submitted@example.test", "cov-1");
        var invitedMember = await SeedSubjectAsync(
            runtimeOptions, tenantId, "Invited Member", "invited@example.test", "cov-2");
        await SeedSubjectMembershipAsync(
            runtimeOptions, tenantId, submittedMember.Id, group.Id, SubjectGroupRoles.Member);
        await SeedSubjectMembershipAsync(
            runtimeOptions, tenantId, invitedMember.Id, group.Id, SubjectGroupRoles.Member);

        await SeedSubmittedIdentifiedResponseAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            template.QuestionId,
            submittedMember.Id,
            DateTimeOffset.Parse("2026-06-02T10:00:00+00:00"));

        // invited but not yet submitted: assignment without a response session
        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var seedScope = new TenantDbScope(seedDb);
            await using var seedTransaction = await seedScope.BeginTransactionAsync(tenantId);
            seedDb.Assignments.Add(Assignment.CreateIdentified(
                Guid.NewGuid(),
                tenantId,
                liveCampaign.Id,
                "self",
                invitedMember.Id,
                targetSubjectId: invitedMember.Id));
            await seedDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        // a respondent without any group membership stays unattributed
        var soloSubject = await SeedSubjectAsync(
            runtimeOptions, tenantId, "Solo Member", "solo@example.test", "cov-3");
        await SeedSubmittedIdentifiedResponseAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            template.QuestionId,
            soloSubject.Id,
            DateTimeOffset.Parse("2026-06-02T11:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var coverage = result.Value.GroupCoverage;
        Assert.NotNull(coverage);
        Assert.Equal(DisclosurePolicy.MinimumKMin, coverage.KMin);

        var groupRow = Assert.Single(coverage.Groups);
        Assert.Equal(group.Id, groupRow.GroupId);
        Assert.Equal("ICU", groupRow.GroupName);
        Assert.Equal(2, groupRow.InvitedCount);
        Assert.Equal(1, groupRow.SubmittedCount);
        Assert.False(groupRow.MeetsThreshold);

        Assert.Equal(1, coverage.UnattributedInvitedCount);
        Assert.Equal(1, coverage.UnattributedSubmittedCount);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_collection_monitor_state()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-collection");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Collection operations series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Live collection wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);

        await SeedDraftResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            startedAt: DateTimeOffset.Parse("2026-05-06T10:05:00+00:00"));

        for (var index = 0; index < DisclosurePolicy.MinimumKMin; index++)
        {
            await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-06T10:10:00+00:00").AddMinutes(index));
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(6, result.Value.Summary.StartedResponseCount);
        Assert.Equal(1, result.Value.Summary.DraftResponseCount);
        Assert.Equal(5, result.Value.Summary.SubmittedResponseCount);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-06T10:14:00+00:00"),
            result.Value.Summary.LatestResponseSubmittedAt);
        Assert.Equal("has_submissions", result.Value.Summary.CollectionStatus);
        Assert.Equal("ready_for_aggregate_report", result.Value.Summary.ReportVisibilityStatus);
        Assert.Equal(
            "Enough submitted responses exist for aggregate report visibility.",
            result.Value.Summary.CollectionGuidance);

        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(6, result.Value.SelectedCampaign.StartedResponseCount);
        Assert.Equal(1, result.Value.SelectedCampaign.DraftResponseCount);
        Assert.Equal("has_submissions", result.Value.SelectedCampaign.CollectionStatus);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_complete_score_coverage()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-score-complete");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Complete score coverage series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Complete score coverage wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);

        for (var index = 0; index < 3; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                session.Id,
                scoringRule.Id,
                ranAt: DateTimeOffset.Parse("2026-05-13T11:00:00+00:00").AddMinutes(index));
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var scoreCoverage = Assert.IsType<CampaignSeriesScoreCoverageResponse>(result.Value.ScoreCoverage);
        Assert.Equal(3, scoreCoverage.SubmittedResponseCount);
        Assert.Equal(3, scoreCoverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(1, scoreCoverage.CampaignsWithScoringRuleCount);
        Assert.Equal(0, scoreCoverage.CampaignsWithoutScoringRuleCount);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-13T11:02:00+00:00"),
            scoreCoverage.LatestScoringActivityAt);
        Assert.Equal("complete", scoreCoverage.Status);

        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(scoringRule.Id, result.Value.SelectedCampaign.ScoringRuleId);
        Assert.Equal(3, result.Value.SelectedCampaign.ScoredSubmittedResponseCount);
        Assert.Equal(0, result.Value.SelectedCampaign.UnscoredSubmittedResponseCount);
        Assert.Equal(0, result.Value.SelectedCampaign.NotConfiguredSubmittedResponseCount);
        Assert.Equal("complete", result.Value.SelectedCampaign.ScoreCoverageStatus);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_partial_unscored_score_coverage()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-score-partial");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Partial score coverage series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Partial score coverage wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);

        for (var index = 0; index < 3; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00").AddMinutes(index));

            if (index < 2)
            {
                await SeedScoreAsync(
                    runtimeOptions,
                    tenantId,
                    campaign.Id,
                    session.Id,
                    scoringRule.Id,
                    ranAt: DateTimeOffset.Parse("2026-05-13T11:00:00+00:00").AddMinutes(index));
            }
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var scoreCoverage = Assert.IsType<CampaignSeriesScoreCoverageResponse>(result.Value.ScoreCoverage);
        Assert.Equal(3, scoreCoverage.SubmittedResponseCount);
        Assert.Equal(2, scoreCoverage.ScoredSubmittedResponseCount);
        Assert.Equal(1, scoreCoverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal("partial", scoreCoverage.Status);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(2, result.Value.SelectedCampaign.ScoredSubmittedResponseCount);
        Assert.Equal(1, result.Value.SelectedCampaign.UnscoredSubmittedResponseCount);
        Assert.Equal("partial", result.Value.SelectedCampaign.ScoreCoverageStatus);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_not_configured_score_coverage()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-score-not-configured");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Not configured score coverage series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Not configured score coverage wave",
            status: CampaignStatuses.Live);

        for (var index = 0; index < 2; index++)
        {
            await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00").AddMinutes(index));
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var scoreCoverage = Assert.IsType<CampaignSeriesScoreCoverageResponse>(result.Value.ScoreCoverage);
        Assert.Equal(2, scoreCoverage.SubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.UnscoredSubmittedResponseCount);
        Assert.Equal(2, scoreCoverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.CampaignsWithScoringRuleCount);
        Assert.Equal(1, scoreCoverage.CampaignsWithoutScoringRuleCount);
        Assert.Null(scoreCoverage.LatestScoringActivityAt);
        Assert.Equal("not_configured", scoreCoverage.Status);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Null(result.Value.SelectedCampaign.ScoringRuleId);
        Assert.Equal(0, result.Value.SelectedCampaign.ScoredSubmittedResponseCount);
        Assert.Equal(0, result.Value.SelectedCampaign.UnscoredSubmittedResponseCount);
        Assert.Equal(2, result.Value.SelectedCampaign.NotConfiguredSubmittedResponseCount);
        Assert.Equal("not_configured", result.Value.SelectedCampaign.ScoreCoverageStatus);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_returns_closed_campaign_metadata()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T14:30:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-closed");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Closed operations series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Closed wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await CloseCampaignAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            actorUserId,
            closedAt,
            "Collection complete");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(campaign.Id, result.Value.SelectedCampaign.Id);
        Assert.Equal(CampaignStatuses.Closed, result.Value.SelectedCampaign.Status);
        Assert.Equal(closedAt, result.Value.SelectedCampaign.ClosedAt);
        Assert.Equal(actorUserId, result.Value.SelectedCampaign.ClosedByUserId);
        Assert.Equal("Collection complete", result.Value.SelectedCampaign.CloseReason);
        Assert.Equal(1, result.Value.SelectedCampaign.SubmittedResponseCount);
        Assert.Equal("has_submissions", result.Value.SelectedCampaign.CollectionStatus);
    }

    [DockerFact]
    public async Task Campaign_series_operations_workspace_excludes_sensitive_fields()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-operations-sensitive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sensitive operations series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive operations campaign",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"));
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            includeSensitiveValues: true);
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            SensitiveRecipient,
            NotificationStatuses.Failed,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"));

        var commandRecorder = new RecordingCommandInterceptor();
        await using var db = new ApplicationDbContext(CreateRuntimeOptions(commandRecorder));
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesOperationsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("Recipient", serialized);
        Assert.DoesNotContain("ProviderMessage", serialized);
        Assert.DoesNotContain("Error", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain("ParticipantCode", serialized);
        Assert.DoesNotContain("CodeSalt", serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveRecipient, serialized);
        Assert.DoesNotContain(SensitiveProviderMessageId, serialized);
        Assert.DoesNotContain(SensitiveDeliveryError, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);

        var storeSql = string.Join('\n', commandRecorder.Commands);
        Assert.DoesNotContain("token_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("code_salt", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message_id", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ip_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_agent_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebook_json", storeSql, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-reports-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-reports-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B reports series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantA,
            tenantBSeries.Id,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_missing_prerequisites_for_empty_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-empty");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Empty reports series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal("Empty reports series", result.Value.Series.Name);
        Assert.Equal(0, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(0, result.Value.Summary.ReportableCampaignCount);
        Assert.Equal(0, result.Value.Summary.SubmittedResponseCount);
        Assert.Equal(0, result.Value.Summary.ScoreCount);
        Assert.Equal(0, result.Value.Summary.ExportArtifactCount);
        Assert.Equal(0, result.Value.Summary.VisibleScoreCount);
        Assert.Equal(0, result.Value.Summary.SuppressedScoreCount);
        Assert.Equal(5, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Null(result.Value.SelectedCampaign);
        Assert.Empty(result.Value.Campaigns);
        var missingCodes = result.Value.MissingPrerequisites.Select(item => item.Code).ToArray();
        Assert.Contains("campaign.missing", missingCodes);
        Assert.Contains("launched_campaign.missing", missingCodes);
        Assert.Contains("submitted_responses.missing", missingCodes);
        Assert.Contains("scores.missing", missingCodes);
        Assert.Contains("disclosure_policy.missing", missingCodes);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_report_and_export_state()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-configured");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Configured reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var draftCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Draft report wave");
        var liveCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Live report wave",
            status: CampaignStatuses.Live);
        var launchSnapshot = await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            liveCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        for (var index = 0; index < DisclosurePolicy.MinimumKMin; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                liveCampaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(runtimeOptions, tenantId, liveCampaign.Id, session.Id, scoringRule.Id);
        }

        var exportArtifact = await SeedExportArtifactAsync(runtimeOptions, tenantId, liveCampaign.Id, series.Id);
        var responseExportArtifact = await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            "responses.csv",
            DateTimeOffset.Parse("2026-05-06T12:10:00+00:00"));
        var reportHtmlArtifact = await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesReportHtml,
            "series-report.html",
            DateTimeOffset.Parse("2026-05-06T12:15:00+00:00"));
        var failedResponseExportArtifact = await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            liveCampaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            "responses-failed.csv",
            DateTimeOffset.Parse("2026-05-06T12:20:00+00:00"),
            status: ExportArtifactStatuses.Failed,
            failedAt: DateTimeOffset.Parse("2026-05-06T12:20:02+00:00"),
            failureReasonCode: "renderer.failed");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal(2, result.Value.Summary.CampaignCount);
        Assert.Equal(1, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(1, result.Value.Summary.ReportableCampaignCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.Summary.SubmittedResponseCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.Summary.ScoreCount);
        Assert.Equal(4, result.Value.Summary.ExportArtifactCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.Summary.VisibleScoreCount);
        Assert.Equal(0, result.Value.Summary.SuppressedScoreCount);
        Assert.Equal(0, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Empty(result.Value.MissingPrerequisites);

        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(liveCampaign.Id, result.Value.SelectedCampaign.Id);
        Assert.Equal(CampaignStatuses.Live, result.Value.SelectedCampaign.Status);
        Assert.Equal(launchSnapshot.Id, result.Value.SelectedCampaign.LatestLaunchSnapshotId);
        Assert.Equal(DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"), result.Value.SelectedCampaign.LatestLaunchAt);
        Assert.Equal(scoringRule.Id, result.Value.SelectedCampaign.ScoringRuleId);
        Assert.Equal(launchSnapshot.ConsentDocumentId, result.Value.SelectedCampaign.ConsentDocumentId);
        Assert.Equal(launchSnapshot.RetentionPolicyId, result.Value.SelectedCampaign.RetentionPolicyId);
        Assert.Equal(launchSnapshot.DisclosurePolicyId, result.Value.SelectedCampaign.DisclosurePolicyId);
        Assert.NotNull(result.Value.SelectedCampaign.LaunchPacket);
        Assert.Equal(1, result.Value.SelectedCampaign.LaunchPacket.SchemaVersion);
        Assert.Contains("scoring", result.Value.SelectedCampaign.LaunchPacket.Sections);
        Assert.Equal("runtime_launch", result.Value.SelectedCampaign.LaunchPacket.Source);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.SelectedCampaign.SubmittedResponseCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.SelectedCampaign.ScoreCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.SelectedCampaign.VisibleScoreCount);
        Assert.Equal(0, result.Value.SelectedCampaign.SuppressedScoreCount);
        Assert.Equal("visible", result.Value.SelectedCampaign.DisclosureState);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.SelectedCampaign.DisclosureKMin);
        Assert.Equal("proof_only", result.Value.SelectedCampaign.ReportStatus);
        Assert.Equal("not_validated_interpretation", result.Value.SelectedCampaign.InterpretationStatus);
        Assert.Equal(exportArtifact.Id, result.Value.SelectedCampaign.LatestExportArtifactId);
        Assert.Equal("proof.csv", result.Value.SelectedCampaign.LatestExportArtifactFileName);
        Assert.Equal(ExportArtifactStatuses.Succeeded, result.Value.SelectedCampaign.LatestExportArtifactStatus);
        Assert.Equal(exportArtifact.CreatedAt, result.Value.SelectedCampaign.LatestExportArtifactCreatedAt);
        Assert.Equal(exportArtifact.CompletedAt, result.Value.SelectedCampaign.LatestExportArtifactCompletedAt);
        Assert.Null(result.Value.SelectedCampaign.LatestExportArtifactStartedAt);
        Assert.Null(result.Value.SelectedCampaign.LatestExportArtifactFailedAt);
        Assert.Null(result.Value.SelectedCampaign.LatestExportArtifactExpiresAt);
        Assert.Null(result.Value.SelectedCampaign.LatestExportArtifactDeletedAt);
        Assert.Null(result.Value.SelectedCampaign.LatestExportArtifactFailureReasonCode);
        Assert.True(result.Value.SelectedCampaign.LatestExportArtifactCanDownload);

        Assert.Equal(4, result.Value.ExportArtifacts.Count);
        var registryFailedArtifact = result.Value.ExportArtifacts[0];
        Assert.Equal(failedResponseExportArtifact.Id, registryFailedArtifact.Id);
        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, registryFailedArtifact.TargetKind);
        Assert.Equal(series.Id, registryFailedArtifact.TargetId);
        Assert.Equal("Configured reports series", registryFailedArtifact.TargetLabel);
        Assert.Null(registryFailedArtifact.CampaignId);
        Assert.Null(registryFailedArtifact.CampaignName);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesResponseCsvCodebook, registryFailedArtifact.ArtifactType);
        Assert.Equal(ExportArtifactStatuses.Failed, registryFailedArtifact.Status);
        Assert.Equal(ExportArtifactFormats.CsvCodebook, registryFailedArtifact.Format);
        Assert.Equal("responses-failed.csv", registryFailedArtifact.FileName);
        Assert.Equal(0, registryFailedArtifact.RowCount);
        Assert.Equal(0, registryFailedArtifact.ByteSize);
        Assert.Null(registryFailedArtifact.ChecksumSha256);
        Assert.Equal(failedResponseExportArtifact.CreatedAt, registryFailedArtifact.CreatedAt);
        Assert.Null(registryFailedArtifact.CompletedAt);
        Assert.Null(registryFailedArtifact.StartedAt);
        Assert.Equal(failedResponseExportArtifact.FailedAt, registryFailedArtifact.FailedAt);
        Assert.Null(registryFailedArtifact.ExpiresAt);
        Assert.Null(registryFailedArtifact.DeletedAt);
        Assert.Equal("renderer.failed", registryFailedArtifact.FailureReasonCode);
        Assert.False(registryFailedArtifact.CanDownload);

        var registryHtmlArtifact = result.Value.ExportArtifacts[1];
        Assert.Equal(reportHtmlArtifact.Id, registryHtmlArtifact.Id);
        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, registryHtmlArtifact.TargetKind);
        Assert.Equal(series.Id, registryHtmlArtifact.TargetId);
        Assert.Equal("Configured reports series", registryHtmlArtifact.TargetLabel);
        Assert.Null(registryHtmlArtifact.CampaignId);
        Assert.Null(registryHtmlArtifact.CampaignName);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportHtml, registryHtmlArtifact.ArtifactType);
        Assert.Equal(ExportArtifactStatuses.Succeeded, registryHtmlArtifact.Status);
        Assert.Equal(ExportArtifactFormats.Html, registryHtmlArtifact.Format);
        Assert.Equal("series-report.html", registryHtmlArtifact.FileName);
        Assert.Equal(1, registryHtmlArtifact.RowCount);
        Assert.Equal(reportHtmlArtifact.ByteSize, registryHtmlArtifact.ByteSize);
        Assert.Equal(reportHtmlArtifact.ChecksumSha256, registryHtmlArtifact.ChecksumSha256);
        Assert.Equal(reportHtmlArtifact.CreatedAt, registryHtmlArtifact.CreatedAt);
        Assert.Equal(reportHtmlArtifact.CompletedAt, registryHtmlArtifact.CompletedAt);
        Assert.Null(registryHtmlArtifact.StartedAt);
        Assert.Null(registryHtmlArtifact.FailedAt);
        Assert.Null(registryHtmlArtifact.ExpiresAt);
        Assert.Null(registryHtmlArtifact.DeletedAt);
        Assert.Null(registryHtmlArtifact.FailureReasonCode);
        Assert.True(registryHtmlArtifact.CanDownload);

        var registryResponseArtifact = result.Value.ExportArtifacts[2];
        Assert.Equal(responseExportArtifact.Id, registryResponseArtifact.Id);
        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, registryResponseArtifact.TargetKind);
        Assert.Equal(series.Id, registryResponseArtifact.TargetId);
        Assert.Equal("Configured reports series", registryResponseArtifact.TargetLabel);
        Assert.Null(registryResponseArtifact.CampaignId);
        Assert.Null(registryResponseArtifact.CampaignName);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesResponseCsvCodebook, registryResponseArtifact.ArtifactType);
        Assert.Equal(ExportArtifactStatuses.Succeeded, registryResponseArtifact.Status);
        Assert.Equal(ExportArtifactFormats.CsvCodebook, registryResponseArtifact.Format);
        Assert.Equal("responses.csv", registryResponseArtifact.FileName);
        Assert.Equal(1, registryResponseArtifact.RowCount);
        Assert.Equal(responseExportArtifact.ByteSize, registryResponseArtifact.ByteSize);
        Assert.Equal(responseExportArtifact.ChecksumSha256, registryResponseArtifact.ChecksumSha256);
        Assert.Equal(responseExportArtifact.CreatedAt, registryResponseArtifact.CreatedAt);
        Assert.Equal(responseExportArtifact.CompletedAt, registryResponseArtifact.CompletedAt);
        Assert.Null(registryResponseArtifact.StartedAt);
        Assert.Null(registryResponseArtifact.FailedAt);
        Assert.Null(registryResponseArtifact.ExpiresAt);
        Assert.Null(registryResponseArtifact.DeletedAt);
        Assert.Null(registryResponseArtifact.FailureReasonCode);
        Assert.True(registryResponseArtifact.CanDownload);

        var registryArtifact = result.Value.ExportArtifacts[3];
        Assert.Equal(exportArtifact.Id, registryArtifact.Id);
        Assert.Equal(ExportArtifactTargetKinds.Campaign, registryArtifact.TargetKind);
        Assert.Equal(liveCampaign.Id, registryArtifact.TargetId);
        Assert.Equal("Live report wave", registryArtifact.TargetLabel);
        Assert.Equal(liveCampaign.Id, registryArtifact.CampaignId);
        Assert.Equal("Live report wave", registryArtifact.CampaignName);
        Assert.Equal(ExportArtifactTypes.ReportProofCsvCodebook, registryArtifact.ArtifactType);
        Assert.Equal(ExportArtifactStatuses.Succeeded, registryArtifact.Status);
        Assert.Equal(ExportArtifactFormats.CsvCodebook, registryArtifact.Format);
        Assert.Equal("proof.csv", registryArtifact.FileName);
        Assert.Equal(1, registryArtifact.RowCount);
        Assert.Equal(exportArtifact.ByteSize, registryArtifact.ByteSize);
        Assert.Equal(exportArtifact.ChecksumSha256, registryArtifact.ChecksumSha256);
        Assert.Equal(exportArtifact.CreatedAt, registryArtifact.CreatedAt);
        Assert.Equal(exportArtifact.CompletedAt, registryArtifact.CompletedAt);
        Assert.Null(registryArtifact.StartedAt);
        Assert.Null(registryArtifact.FailedAt);
        Assert.Null(registryArtifact.ExpiresAt);
        Assert.Null(registryArtifact.DeletedAt);
        Assert.Null(registryArtifact.FailureReasonCode);
        Assert.True(registryArtifact.CanDownload);

        Assert.Equal([liveCampaign.Id, draftCampaign.Id], result.Value.Campaigns.Select(campaign => campaign.Id).ToArray());
        var draft = result.Value.Campaigns.Single(campaign => campaign.Id == draftCampaign.Id);
        Assert.Equal(CampaignStatuses.Draft, draft.Status);
        Assert.Equal("not_reportable", draft.DataFinality);
        Assert.Equal("not_available", draft.DisclosureState);
        Assert.Equal("blocked", draft.ReportStatus);
        Assert.Null(draft.LatestExportArtifactId);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_score_coverage_signal()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-score-coverage");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports score coverage series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports score coverage wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);

        for (var index = 0; index < 2; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                session.Id,
                scoringRule.Id,
                ranAt: DateTimeOffset.Parse("2026-05-13T11:00:00+00:00").AddMinutes(index));
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var scoreCoverage = Assert.IsType<CampaignSeriesScoreCoverageResponse>(result.Value.ScoreCoverage);
        Assert.Equal(2, scoreCoverage.SubmittedResponseCount);
        Assert.Equal(2, scoreCoverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, scoreCoverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-13T11:01:00+00:00"),
            scoreCoverage.LatestScoringActivityAt);
        Assert.Equal("complete", scoreCoverage.Status);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_closed_wave_finality_metadata()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-07T16:00:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-closed-finality");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Closed finality reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Closed report wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-07T09:00:00+00:00"),
            configured: true);
        for (var index = 0; index < DisclosurePolicy.MinimumKMin; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-07T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        }

        await CloseCampaignAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            actorUserId,
            closedAt,
            "Collection window complete");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(1, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(1, result.Value.Summary.ReportableCampaignCount);
        Assert.Equal(0, result.Value.Summary.PreliminaryLiveReportCount);
        Assert.Equal(1, result.Value.Summary.ClosedWaveReportCount);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(CampaignStatuses.Closed, result.Value.SelectedCampaign.Status);
        Assert.Equal(closedAt, result.Value.SelectedCampaign.ClosedAt);
        Assert.Equal(actorUserId, result.Value.SelectedCampaign.ClosedByUserId);
        Assert.Equal("Collection window complete", result.Value.SelectedCampaign.CloseReason);
        Assert.Equal("closed_wave", result.Value.SelectedCampaign.DataFinality);
        Assert.Equal("proof_only", result.Value.SelectedCampaign.ReportStatus);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_returns_all_series_exports_for_results_workflow()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-many-exports");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Many exports series");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Many exports wave",
            status: CampaignStatuses.Live);

        var firstCreatedAt = DateTimeOffset.Parse("2026-05-06T12:00:00+00:00");
        for (var index = 0; index < 12; index++)
        {
            await SeedExportArtifactAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                series.Id,
                ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
                $"responses-{index:00}.csv",
                firstCreatedAt.AddMinutes(index));
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(12, result.Value.Summary.ExportArtifactCount);
        Assert.Equal(12, result.Value.ExportArtifacts.Count);
        Assert.Equal("responses-11.csv", result.Value.ExportArtifacts[0].FileName);
        Assert.Equal("responses-00.csv", result.Value.ExportArtifacts[^1].FileName);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_marks_low_n_scores_suppressed()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-suppressed");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Suppressed reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Suppressed report wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(1, result.Value.Summary.ReportableCampaignCount);
        Assert.Equal(1, result.Value.Summary.ScoreCount);
        Assert.Equal(0, result.Value.Summary.VisibleScoreCount);
        Assert.Equal(1, result.Value.Summary.SuppressedScoreCount);
        Assert.Equal(1, result.Value.Summary.MissingPrerequisiteCount);
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal("suppressed", result.Value.SelectedCampaign.DisclosureState);
        Assert.Equal("proof_only", result.Value.SelectedCampaign.ReportStatus);
        Assert.Equal(0, result.Value.SelectedCampaign.VisibleScoreCount);
        Assert.Equal(1, result.Value.SelectedCampaign.SuppressedScoreCount);
        Assert.Equal("export_artifact.missing", Assert.Single(result.Value.MissingPrerequisites).Code);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_suppresses_sparse_score_outputs()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-sparse-output");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sparse output reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sparse output wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);

        for (var index = 0; index < 5; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00").AddMinutes(index));

            if (index == 0)
            {
                await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
            }
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(5, result.Value.SelectedCampaign.SubmittedResponseCount);
        Assert.Equal(1, result.Value.SelectedCampaign.ScoreCount);
        Assert.Equal(0, result.Value.SelectedCampaign.VisibleScoreCount);
        Assert.Equal(1, result.Value.SelectedCampaign.SuppressedScoreCount);
        Assert.Equal("suppressed", result.Value.SelectedCampaign.DisclosureState);

        var output = Assert.Single(result.Value.ResultsAnalytics!.ScoreOutputs);
        Assert.Equal("suppressed", output.Disclosure);
        Assert.Null(output.SubmittedResponseCount);
        Assert.Null(output.ScoreCount);
        Assert.Null(output.Mean);
        Assert.Equal("insufficient_responses", output.SuppressionReason);
        Assert.NotNull(result.Value.ResultsDashboard);
        var dashboardBar = Assert.Single(result.Value.ResultsDashboard!.OutputBars);
        Assert.Equal("suppressed", dashboardBar.Disclosure);
        Assert.Null(dashboardBar.Value);
        Assert.Null(dashboardBar.Count);
        Assert.Equal("insufficient_responses", dashboardBar.SuppressionReason);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_deduplicates_group_memberships_for_results_matrix()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-group-dedup");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Group dedup reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Group dedup wave",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.Identified);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var primaryGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantId, SubjectGroupTypes.Team, "Research");
        var duplicateNameGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantId, SubjectGroupTypes.Team, "Research");

        for (var index = 0; index < 5; index++)
        {
            var subject = await SeedSubjectAsync(
                runtimeOptions,
                tenantId,
                $"Researcher {index}",
                $"researcher-{index}@example.test",
                $"researcher-{index:00}");
            await SeedSubjectMembershipAsync(runtimeOptions, tenantId, subject.Id, primaryGroup.Id, SubjectGroupRoles.Member);
            if (index == 0)
            {
                await SeedSubjectMembershipAsync(
                    runtimeOptions,
                    tenantId,
                    subject.Id,
                    duplicateNameGroup.Id,
                    SubjectGroupRoles.Member);
            }

            var session = await SeedSubmittedIdentifiedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                subject.Id,
                submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var groupRow = Assert.Single(result.Value.ResultsAnalytics!.GroupRows);
        Assert.Equal("visible", groupRow.Disclosure);
        Assert.Equal(5, groupRow.ScoreCount);
        Assert.Equal(5, groupRow.SubmittedResponseCount);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_ignores_unsubmitted_scores()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-draft-score");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Draft score reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Draft score wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var draftSession = await SeedDraftResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, draftSession.Id, scoringRule.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.NotNull(result.Value.SelectedCampaign);
        Assert.Equal(0, result.Value.SelectedCampaign.SubmittedResponseCount);
        Assert.Equal(0, result.Value.SelectedCampaign.ScoreCount);
        Assert.Equal(0, result.Value.Summary.ScoreCount);
        Assert.Equal("pending", result.Value.SelectedCampaign.DisclosureState);
        Assert.Equal("blocked", result.Value.SelectedCampaign.ReportStatus);
        Assert.Empty(result.Value.ResultsAnalytics!.ScoreOutputs);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_ignores_scores_with_mismatched_assignment_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-mismatched-score");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Mismatched score reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var sourceCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Source wave",
            status: CampaignStatuses.Live);
        var mismatchedCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Mismatched wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            mismatchedCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:05:00+00:00"),
            configured: true);
        var sourceSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"));
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign.Id,
            sourceSession.Id,
            scoringRule.Id);
        await using (var ownerDb = new ApplicationDbContext(migratorOptions))
        {
            await ownerDb.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE invitation_token
                SET campaign_id = {mismatchedCampaign.Id}
                WHERE id = (
                    SELECT invite_token_id
                    FROM assignment
                    WHERE id = {sourceSession.AssignmentId}
                      AND tenant_id = {tenantId}
                )
                  AND tenant_id = {tenantId};

                UPDATE assignment
                SET campaign_id = {mismatchedCampaign.Id}
                WHERE id = {sourceSession.AssignmentId}
                  AND tenant_id = {tenantId}
                """);
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(0, result.Value.Summary.ScoreCount);
        Assert.All(result.Value.Campaigns, campaign => Assert.Equal(0, campaign.ScoreCount));
        Assert.Empty(result.Value.ResultsAnalytics!.ScoreOutputs);
    }

    [DockerFact]
    public async Task Campaign_series_reports_workspace_excludes_sensitive_fields()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-sensitive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sensitive reports series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive reports campaign",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            includeSensitiveValues: true);
        await SeedScoreAsync(runtimeOptions, tenantId, campaign.Id, session.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);
        await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            "report.pdf",
            DateTimeOffset.Parse("2026-05-06T12:30:00+00:00"));
        await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            "report-failed.pdf",
            DateTimeOffset.Parse("2026-05-06T12:31:00+00:00"),
            ExportArtifactStatuses.Failed,
            failedAt: DateTimeOffset.Parse("2026-05-06T12:31:01+00:00"),
            failureReasonCode: "report_pdf.render_failed");
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            SensitiveRecipient,
            NotificationStatuses.Failed,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"));

        var commandRecorder = new RecordingCommandInterceptor();
        await using var db = new ApplicationDbContext(CreateRuntimeOptions(commandRecorder));
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Contains(
            result.Value.ExportArtifacts,
            artifact =>
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                artifact.Format == ExportArtifactFormats.Pdf &&
                artifact.CanDownload);
        Assert.Contains(
            result.Value.ExportArtifacts,
            artifact =>
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                artifact.Format == ExportArtifactFormats.Pdf &&
                artifact.Status == ExportArtifactStatuses.Failed &&
                !artifact.CanDownload);
        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("Recipient", serialized);
        Assert.DoesNotContain("ProviderMessage", serialized);
        Assert.DoesNotContain("Error", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain("Content", serialized);
        Assert.DoesNotContain("Codebook", serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveRecipient, serialized);
        Assert.DoesNotContain(SensitiveProviderMessageId, serialized);
        Assert.DoesNotContain(SensitiveDeliveryError, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);

        var storeSql = string.Join('\n', commandRecorder.Commands);
        Assert.DoesNotContain("token_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message_id", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ip_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_agent_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FROM answer", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JOIN answer", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebook_json", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("scoring_rule_document_hash", storeSql, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_is_tenant_scoped_and_returns_not_found_for_wrong_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-reports-widget-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-reports-widget-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B reports widget series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantA,
            tenantBSeries.Id,
            canManageSetup: true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_returns_safe_ordered_widgets_from_reports_workspace_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-safe");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget manifest series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports widget manifest wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);
        for (var index = 0; index < 5; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-15T10:00:00+00:00").AddMinutes(index),
                includeSensitiveValues: index == 0);
            await SeedScoreAsync(
                runtimeOptions,
                tenantId,
                campaign.Id,
                session.Id,
                scoringRule.Id,
                ranAt: DateTimeOffset.Parse("2026-05-15T11:00:00+00:00").AddMinutes(index));
        }
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            SensitiveRecipient,
            NotificationStatuses.Failed,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-15T12:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: true,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.CampaignSeriesId);
        Assert.Equal("reports", result.Value.Surface);
        Assert.Equal("reports-widget-manifest/v1", result.Value.SurfaceVersion);
        Assert.Equal("dashboard-grid/v1", result.Value.Layout.Kind);
        Assert.Equal(
            [
                "results-dashboard",
                "report-readiness-summary",
                "score-coverage-summary",
                "selected-campaign-report-state",
                "export-artifact-registry",
                "visual-analytics-entry",
                "finality-provenance-summary"
            ],
            result.Value.Widgets.Select(widget => widget.Id).ToArray());
        Assert.Equal(
            [
                "results-dashboard/v1",
                "report-readiness-summary/v1",
                "score-coverage-summary/v1",
                "selected-campaign-report-state/v1",
                "export-artifact-registry/v1",
                "visual-analytics-entry/v1",
                "finality-provenance-summary/v1"
            ],
            result.Value.Widgets.Select(widget => widget.Kind).ToArray());
        var dashboardWidget = result.Value.Widgets.Single(widget => widget.Id == "results-dashboard");
        Assert.Equal("ready", dashboardWidget.State);
        var dashboardData = Assert.IsType<ResultsDashboardWidgetDataResponse>(dashboardWidget.Data);
        Assert.Equal(campaign.Id, dashboardData.Dashboard.SelectedCampaignId);
        var outputBar = Assert.Single(dashboardData.Dashboard.OutputBars);
        Assert.Equal("visible", outputBar.Disclosure);
        Assert.Equal(4.2m, outputBar.Value);
        Assert.Equal(5, outputBar.Count);
        Assert.DoesNotContain(
            dashboardData.Dashboard.OutputBars,
            bar => bar.Disclosure != "visible" && (bar.Value.HasValue || bar.Count.HasValue));
        Assert.Contains(
            result.Value.Widgets.SelectMany(widget => widget.Actions),
            action => action.Id == "create-aggregate-export");
        var readinessWidget = result.Value.Widgets.Single(widget => widget.Id == "report-readiness-summary");
        var readinessData = Assert.IsType<ReportReadinessWidgetDataResponse>(readinessWidget.Data);
        Assert.Equal(1, readinessData.CampaignCount);
        Assert.Equal(0, readinessData.MissingPrerequisiteCount);
        var scoreCoverageWidget = result.Value.Widgets.Single(widget => widget.Id == "score-coverage-summary");
        var scoreCoverageData = Assert.IsType<ScoreCoverageWidgetDataResponse>(scoreCoverageWidget.Data);
        Assert.Equal(5, scoreCoverageData.SubmittedResponseCount);
        Assert.Equal("complete", scoreCoverageData.Status);
        var selectedCampaignWidget = result.Value.Widgets.Single(widget => widget.Id == "selected-campaign-report-state");
        var selectedCampaignData = Assert.IsType<SelectedCampaignReportStateWidgetDataResponse>(
            selectedCampaignWidget.Data);
        Assert.Equal(campaign.Id, selectedCampaignData.CampaignId);
        Assert.Equal("proof_only", selectedCampaignData.ReportStatus);
        Assert.Equal($"/campaigns/{campaign.Id}/report-proof", selectedCampaignWidget.DataSource?.Href);
        var exportWidget = result.Value.Widgets.Single(widget => widget.Id == "export-artifact-registry");
        var exportData = Assert.IsType<ExportArtifactRegistryWidgetDataResponse>(exportWidget.Data);
        Assert.Equal(1, exportData.ExportArtifactCount);
        Assert.Single(exportData.Artifacts);
        var exportAction = Assert.Single(exportWidget.Actions, action => action.Id == "create-aggregate-export");
        Assert.True(exportAction.Enabled);
        Assert.Null(exportAction.DisabledReason);
        var visualAnalyticsWidget = result.Value.Widgets.Single(widget => widget.Id == "visual-analytics-entry");
        var visualAnalyticsData = Assert.IsType<VisualAnalyticsEntryWidgetDataResponse>(visualAnalyticsWidget.Data);
        Assert.Equal(campaign.Id, visualAnalyticsData.SelectedCampaignId);
        Assert.Equal($"/campaigns/{campaign.Id}/report-proof", visualAnalyticsWidget.DataSource?.Href);
        var finalityWidget = result.Value.Widgets.Single(widget => widget.Id == "finality-provenance-summary");
        var finalityData = Assert.IsType<FinalityProvenanceWidgetDataResponse>(finalityWidget.Data);
        Assert.Equal(campaign.Id, finalityData.SelectedCampaignId);
        Assert.Equal("preliminary_live", finalityData.SelectedDataFinality);

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("Recipient", serialized);
        Assert.DoesNotContain("ProviderMessage", serialized);
        Assert.DoesNotContain("Error", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain("Content", serialized);
        Assert.DoesNotContain("Codebook", serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveRecipient, serialized);
        Assert.DoesNotContain(SensitiveProviderMessageId, serialized);
        Assert.DoesNotContain(SensitiveDeliveryError, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_hides_setup_management_actions_when_not_allowed()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-actions");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget actions series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports widget actions wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-15T10:00:00+00:00"));
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            session.Id,
            scoringRule.Id,
            ranAt: DateTimeOffset.Parse("2026-05-15T11:00:00+00:00"));
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: false,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.All(result.Value.Widgets, widget =>
            Assert.DoesNotContain(widget.Actions, action => action.Id == "create-aggregate-export"));
        var selectedCampaignWidget = result.Value.Widgets.Single(widget => widget.Id == "selected-campaign-report-state");
        var selectedCampaignData = Assert.IsType<SelectedCampaignReportStateWidgetDataResponse>(
            selectedCampaignWidget.Data);
        Assert.False(selectedCampaignData.LatestExportArtifactCanDownload);
        var exportWidget = result.Value.Widgets.Single(widget => widget.Id == "export-artifact-registry");
        var exportData = Assert.IsType<ExportArtifactRegistryWidgetDataResponse>(exportWidget.Data);
        var exportArtifact = Assert.Single(exportData.Artifacts);
        Assert.False(exportArtifact.CanDownload);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_allows_series_response_export_download_only_for_setup_managers()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-response-export");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget response export series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports widget response export wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-15T10:00:00+00:00"));
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            session.Id,
            scoringRule.Id,
            ranAt: DateTimeOffset.Parse("2026-05-15T11:00:00+00:00"));
        await SeedExportArtifactAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            "responses.csv",
            DateTimeOffset.Parse("2026-05-15T12:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var managerResult = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: true,
            CancellationToken.None);
        var viewerResult = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: false,
            CancellationToken.None);

        Assert.True(managerResult.IsSuccess, managerResult.Error.ToString());
        Assert.True(viewerResult.IsSuccess, viewerResult.Error.ToString());
        var managerExportData = Assert.IsType<ExportArtifactRegistryWidgetDataResponse>(
            managerResult.Value.Widgets.Single(widget => widget.Id == "export-artifact-registry").Data);
        var viewerExportData = Assert.IsType<ExportArtifactRegistryWidgetDataResponse>(
            viewerResult.Value.Widgets.Single(widget => widget.Id == "export-artifact-registry").Data);
        var managerArtifact = Assert.Single(
            managerExportData.Artifacts,
            artifact => artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook);
        var viewerArtifact = Assert.Single(
            viewerExportData.Artifacts,
            artifact => artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook);

        Assert.Null(managerArtifact.DataFinality);
        Assert.True(managerArtifact.CanDownload);
        Assert.False(viewerArtifact.CanDownload);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_does_not_advertise_report_proof_for_blocked_selected_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-blocked");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget blocked series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports widget blocked wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: true,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var selectedCampaignWidget = result.Value.Widgets.Single(widget => widget.Id == "selected-campaign-report-state");
        var visualAnalyticsWidget = result.Value.Widgets.Single(widget => widget.Id == "visual-analytics-entry");

        Assert.Equal("blocked", selectedCampaignWidget.State);
        Assert.NotNull(selectedCampaignWidget.Message);
        Assert.Null(selectedCampaignWidget.DataSource);
        Assert.Equal("blocked", visualAnalyticsWidget.State);
        Assert.NotNull(visualAnalyticsWidget.Message);
        Assert.Null(visualAnalyticsWidget.DataSource);
        Assert.All(result.Value.Widgets, widget =>
        {
            Assert.DoesNotContain("report-proof", widget.DataSource?.Href ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain(widget.Actions, action => action.Id == "create-aggregate-export" && action.Enabled);
        });
    }

    [DockerFact]
    public async Task Tenant_settings_returns_report_branding_preview_from_tenant_profile()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            db.Tenants.Add(new Tenant(tenantId, "acme-reports", "Acme Reports"));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var store = new ProductSurfaceReadStore(db, tenantDbScope);
        var settings = await store.GetTenantSettingsAsync(tenantId, CancellationToken.None);

        Assert.True(settings.IsSuccess, settings.Error.ToString());
        Assert.Equal("Acme Reports", settings.Value.ReportBranding.OrganizationLabel);
        Assert.Equal("Campaign series report", settings.Value.ReportBranding.ReportTitle);
        Assert.Equal("tenant_profile", settings.Value.ReportBranding.BrandingSource);
        Assert.Equal("none", settings.Value.ReportBranding.LogoMode);
        Assert.Equal("#2563eb", settings.Value.ReportBranding.AccentColorHex);
        Assert.Equal("standard", settings.Value.ReportBranding.LayoutVariant);
        Assert.Contains("logo_upload", settings.Value.ReportBranding.DeferredCustomizations);
        Assert.Contains("custom_fonts", settings.Value.ReportBranding.DeferredCustomizations);
    }

    [DockerFact]
    public async Task Tenant_settings_returns_report_branding_preview_from_mutable_tenant_settings()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var tenant = new Tenant(tenantId, "acme-safety", "Acme Safety");
            tenant.UpdateReportBranding(
                "Acme OSH Consulting",
                "Monthly workplace risk report",
                "#0F766E",
                "compact",
                DateTimeOffset.Parse("2026-06-13T12:00:00+00:00"));
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var store = new ProductSurfaceReadStore(db, tenantDbScope);
        var settings = await store.GetTenantSettingsAsync(tenantId, CancellationToken.None);

        Assert.True(settings.IsSuccess, settings.Error.ToString());
        Assert.Equal("Acme OSH Consulting", settings.Value.ReportBranding.OrganizationLabel);
        Assert.Equal("Monthly workplace risk report", settings.Value.ReportBranding.ReportTitle);
        Assert.Equal("tenant_settings", settings.Value.ReportBranding.BrandingSource);
        Assert.Equal("#0f766e", settings.Value.ReportBranding.AccentColorHex);
        Assert.Equal("compact", settings.Value.ReportBranding.LayoutVariant);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_blocks_cancelled_proof_only_campaign_without_report_capabilities()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-cancelled");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget cancelled series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Reports widget cancelled wave",
            status: CampaignStatuses.Cancelled);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);
        var session = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-15T10:00:00+00:00"));
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            session.Id,
            scoringRule.Id,
            ranAt: DateTimeOffset.Parse("2026-05-15T11:00:00+00:00"));
        await SeedExportArtifactAsync(runtimeOptions, tenantId, campaign.Id, series.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: true,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var selectedCampaignWidget = result.Value.Widgets.Single(widget => widget.Id == "selected-campaign-report-state");
        var selectedCampaignData = Assert.IsType<SelectedCampaignReportStateWidgetDataResponse>(
            selectedCampaignWidget.Data);
        var visualAnalyticsWidget = result.Value.Widgets.Single(widget => widget.Id == "visual-analytics-entry");
        var exportWidget = result.Value.Widgets.Single(widget => widget.Id == "export-artifact-registry");
        var exportData = Assert.IsType<ExportArtifactRegistryWidgetDataResponse>(exportWidget.Data);

        Assert.Equal(CampaignStatuses.Cancelled, selectedCampaignData.Status);
        Assert.Equal("proof_only", selectedCampaignData.ReportStatus);
        Assert.Equal("not_reportable", selectedCampaignData.DataFinality);
        Assert.False(selectedCampaignData.LatestExportArtifactCanDownload);
        Assert.Equal("blocked", selectedCampaignWidget.State);
        Assert.NotNull(selectedCampaignWidget.Message);
        Assert.Null(selectedCampaignWidget.DataSource);
        Assert.Equal("blocked", visualAnalyticsWidget.State);
        Assert.NotNull(visualAnalyticsWidget.Message);
        Assert.Null(visualAnalyticsWidget.DataSource);
        Assert.DoesNotContain(exportWidget.Actions, action => action.Id == "create-aggregate-export" && action.Enabled);
        Assert.False(Assert.Single(exportData.Artifacts).CanDownload);
        var readinessWidget = result.Value.Widgets.Single(widget => widget.Id == "report-readiness-summary");
        var readinessData = Assert.IsType<ReportReadinessWidgetDataResponse>(readinessWidget.Data);
        var finalityWidget = result.Value.Widgets.Single(widget => widget.Id == "finality-provenance-summary");
        var finalityData = Assert.IsType<FinalityProvenanceWidgetDataResponse>(finalityWidget.Data);

        Assert.Equal(0, readinessData.ReportableCampaignCount);
        Assert.Equal("empty", finalityWidget.State);
        Assert.Equal("not_reportable", finalityData.SelectedDataFinality);
    }

    [DockerFact]
    public async Task Reports_widget_manifest_selects_closed_reportable_campaign_over_cancelled_proof_shaped_campaign()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-reports-widget-mixed-cancelled");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Reports widget mixed cancelled series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var closedCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Closed reportable wave",
            status: CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            closedCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-14T09:00:00+00:00"),
            configured: true);
        for (var index = 0; index < DisclosurePolicy.MinimumKMin; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                closedCampaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-14T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(runtimeOptions, tenantId, closedCampaign.Id, session.Id, scoringRule.Id);
        }

        await CloseCampaignAsync(
            runtimeOptions,
            tenantId,
            closedCampaign.Id,
            actorUserId,
            DateTimeOffset.Parse("2026-05-14T17:00:00+00:00"),
            "Collection complete");

        var cancelledCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Cancelled proof-shaped wave",
            status: CampaignStatuses.Cancelled);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            cancelledCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-15T09:00:00+00:00"),
            configured: true);
        for (var index = 0; index < DisclosurePolicy.MinimumKMin + 1; index++)
        {
            var session = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                cancelledCampaign.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-15T10:00:00+00:00").AddMinutes(index));
            await SeedScoreAsync(runtimeOptions, tenantId, cancelledCampaign.Id, session.Id, scoringRule.Id);
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesReportsWidgetManifestAsync(
            tenantId,
            series.Id,
            canManageSetup: true,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var selectedCampaignWidget = result.Value.Widgets.Single(widget => widget.Id == "selected-campaign-report-state");
        var selectedCampaignData = Assert.IsType<SelectedCampaignReportStateWidgetDataResponse>(
            selectedCampaignWidget.Data);
        var visualAnalyticsWidget = result.Value.Widgets.Single(widget => widget.Id == "visual-analytics-entry");
        var readinessData = Assert.IsType<ReportReadinessWidgetDataResponse>(
            result.Value.Widgets.Single(widget => widget.Id == "report-readiness-summary").Data);

        Assert.Equal(closedCampaign.Id, selectedCampaignData.CampaignId);
        Assert.Equal(CampaignStatuses.Closed, selectedCampaignData.Status);
        Assert.Equal("closed_wave", selectedCampaignData.DataFinality);
        Assert.Equal($"/campaigns/{closedCampaign.Id}/report-proof", selectedCampaignWidget.DataSource?.Href);
        Assert.Equal($"/campaigns/{closedCampaign.Id}/report-proof", visualAnalyticsWidget.DataSource?.Href);
        Assert.Equal(1, readinessData.ReportableCampaignCount);
    }

    [DockerFact]
    public async Task Campaign_series_waves_workspace_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantA, "tenant-a-waves-cross-series");
        await SeedTenantShellAsync(runtimeOptions, tenantB, "tenant-b-waves-cross-series");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B waves series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesWavesWorkspaceAsync(
            tenantA,
            tenantBSeries.Id,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_waves_workspace_returns_missing_prerequisites_for_empty_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-waves-empty");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Empty waves series");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesWavesWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal("Empty waves series", result.Value.Series.Name);
        Assert.Equal(0, result.Value.Summary.CampaignCount);
        Assert.Equal(0, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(0, result.Value.Summary.LongitudinalWaveCount);
        Assert.Equal(0, result.Value.Summary.SubmittedWaveCount);
        Assert.Equal(0, result.Value.Summary.LinkedTrajectoryCount);
        Assert.Equal(0, result.Value.Summary.CompleteTrajectoryCount);
        Assert.Equal(0, result.Value.Summary.ComparableScoreCount);
        Assert.Equal(0, result.Value.Summary.VisibleComparisonCount);
        Assert.Equal(0, result.Value.Summary.SuppressedComparisonCount);
        Assert.Equal(0, result.Value.Summary.BlockedComparisonCount);
        Assert.Equal(5, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Null(result.Value.SelectedBaselineWave);
        Assert.Null(result.Value.SelectedComparisonWave);
        Assert.Equal("blocked", result.Value.Comparison.Status);
        Assert.Equal("not_available", result.Value.Comparison.DisclosureState);
        Assert.Equal("not_available", result.Value.Comparison.CompatibilityState);
        Assert.Empty(result.Value.Waves);
        var missingCodes = result.Value.MissingPrerequisites.Select(item => item.Code).ToArray();
        Assert.Contains("campaign.missing", missingCodes);
        Assert.Contains("longitudinal_waves.missing", missingCodes);
        Assert.Contains("two_waves.missing", missingCodes);
        Assert.Contains("linked_trajectories.missing", missingCodes);
        Assert.Contains("scores.missing", missingCodes);
    }

    [DockerFact]
    public async Task Campaign_series_waves_workspace_returns_two_wave_state()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-20T15:00:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-waves-configured");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Configured waves series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var baseline = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 1",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var comparison = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 2",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var baselineSnapshot = await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            baseline,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        var comparisonSnapshot = await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            comparison,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);

        for (var index = 0; index < DisclosurePolicy.MinimumKMin + 1; index++)
        {
            var participantCode = await SeedParticipantCodeAsync(
                runtimeOptions,
                tenantId,
                series.Id,
                seedOffset: index);
            var baselineSession = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                baseline.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00").AddMinutes(index),
                participantCodeId: participantCode.Id);
            await SeedScoreAsync(runtimeOptions, tenantId, baseline.Id, baselineSession.Id, scoringRule.Id);
            var comparisonSession = await SeedSubmittedResponseAsync(
                runtimeOptions,
                tenantId,
                comparison.Id,
                template.QuestionId,
                submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00").AddMinutes(index),
                participantCodeId: participantCode.Id);
            await SeedScoreAsync(runtimeOptions, tenantId, comparison.Id, comparisonSession.Id, scoringRule.Id);
        }

        await CloseCampaignAsync(
            runtimeOptions,
            tenantId,
            baseline.Id,
            actorUserId,
            closedAt,
            "Baseline wave complete");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesWavesWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Series.Id);
        Assert.Equal(2, result.Value.Summary.CampaignCount);
        Assert.Equal(1, result.Value.Summary.LiveCampaignCount);
        Assert.Equal(2, result.Value.Summary.LongitudinalWaveCount);
        Assert.Equal(1, result.Value.Summary.PreliminaryLiveWaveCount);
        Assert.Equal(1, result.Value.Summary.ClosedWaveCount);
        Assert.Equal(2, result.Value.Summary.SubmittedWaveCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.Summary.LinkedTrajectoryCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.Summary.CompleteTrajectoryCount);
        Assert.Equal(1, result.Value.Summary.ComparableScoreCount);
        Assert.Equal(1, result.Value.Summary.VisibleComparisonCount);
        Assert.Equal(0, result.Value.Summary.SuppressedComparisonCount);
        Assert.Equal(0, result.Value.Summary.BlockedComparisonCount);
        Assert.Equal(0, result.Value.Summary.MissingPrerequisiteCount);
        Assert.Empty(result.Value.MissingPrerequisites);
        Assert.NotNull(result.Value.SelectedBaselineWave);
        Assert.Equal(baseline.Id, result.Value.SelectedBaselineWave.Id);
        Assert.Equal(CampaignStatuses.Closed, result.Value.SelectedBaselineWave.Status);
        Assert.Equal(closedAt, result.Value.SelectedBaselineWave.ClosedAt);
        Assert.Equal(actorUserId, result.Value.SelectedBaselineWave.ClosedByUserId);
        Assert.Equal("Baseline wave complete", result.Value.SelectedBaselineWave.CloseReason);
        Assert.Equal("closed_wave", result.Value.SelectedBaselineWave.DataFinality);
        Assert.Equal(baselineSnapshot.Id, result.Value.SelectedBaselineWave.LatestLaunchSnapshotId);
        Assert.NotNull(result.Value.SelectedBaselineWave.LaunchPacket);
        Assert.Equal(1, result.Value.SelectedBaselineWave.LaunchPacket.SchemaVersion);
        Assert.Contains("scoring", result.Value.SelectedBaselineWave.LaunchPacket.Sections);
        Assert.Equal("runtime_launch", result.Value.SelectedBaselineWave.LaunchPacket.Source);
        Assert.Equal(scoringRule.Id, result.Value.SelectedBaselineWave.ScoringRuleId);
        Assert.Equal(scoringRule.RuleKey, result.Value.SelectedBaselineWave.ScoringRuleKey);
        Assert.Equal(scoringRule.RuleVersion, result.Value.SelectedBaselineWave.ScoringRuleVersion);
        Assert.Equal(baselineSnapshot.DisclosurePolicyId, result.Value.SelectedBaselineWave.DisclosurePolicyId);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.SelectedBaselineWave.DisclosureKMin);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.SelectedBaselineWave.SubmittedResponseCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.SelectedBaselineWave.ScoreCount);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.SelectedBaselineWave.LinkedTrajectoryCount);
        Assert.Equal("wave", result.Value.SelectedBaselineWave.WaveState);
        Assert.NotNull(result.Value.SelectedComparisonWave);
        Assert.Equal(comparison.Id, result.Value.SelectedComparisonWave.Id);
        Assert.Equal(CampaignStatuses.Live, result.Value.SelectedComparisonWave.Status);
        Assert.Null(result.Value.SelectedComparisonWave.ClosedAt);
        Assert.Equal("preliminary_live", result.Value.SelectedComparisonWave.DataFinality);
        Assert.Equal(comparisonSnapshot.Id, result.Value.SelectedComparisonWave.LatestLaunchSnapshotId);
        Assert.NotNull(result.Value.SelectedComparisonWave.LaunchPacket);
        Assert.Equal(1, result.Value.SelectedComparisonWave.LaunchPacket.SchemaVersion);
        Assert.Equal("runtime_launch", result.Value.SelectedComparisonWave.LaunchPacket.Source);
        Assert.Equal("proof_only", result.Value.Comparison.Status);
        Assert.Equal("visible", result.Value.Comparison.DisclosureState);
        Assert.Equal("compatible", result.Value.Comparison.CompatibilityState);
        Assert.Equal("not_validated_interpretation", result.Value.Comparison.InterpretationStatus);
        Assert.Equal(DisclosurePolicy.MinimumKMin, result.Value.Comparison.DisclosureKMin);
        Assert.Equal(DisclosurePolicy.MinimumKMin + 1, result.Value.Comparison.LinkedPairCount);
        Assert.Equal(1, result.Value.Comparison.VisibleScoreCount);
        Assert.Equal(0, result.Value.Comparison.SuppressedScoreCount);
        Assert.Equal(0, result.Value.Comparison.BlockedScoreCount);
        Assert.Equal([baseline.Id, comparison.Id], result.Value.Waves.Select(wave => wave.Id).ToArray());
    }

    [DockerFact]
    public async Task Campaign_series_waves_workspace_marks_low_n_comparisons_suppressed()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-waves-suppressed");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Suppressed waves series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var baseline = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 1",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var comparison = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Wave 2",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            baseline,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            comparison,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);
        var participantCode = await SeedParticipantCodeAsync(runtimeOptions, tenantId, series.Id);
        var baselineSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            baseline.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            participantCodeId: participantCode.Id);
        await SeedScoreAsync(runtimeOptions, tenantId, baseline.Id, baselineSession.Id, scoringRule.Id);
        var comparisonSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            comparison.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00"),
            participantCodeId: participantCode.Id);
        await SeedScoreAsync(runtimeOptions, tenantId, comparison.Id, comparisonSession.Id, scoringRule.Id);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesWavesWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(1, result.Value.Summary.ComparableScoreCount);
        Assert.Equal(0, result.Value.Summary.VisibleComparisonCount);
        Assert.Equal(1, result.Value.Summary.SuppressedComparisonCount);
        Assert.Equal(0, result.Value.Summary.BlockedComparisonCount);
        Assert.Equal("proof_only", result.Value.Comparison.Status);
        Assert.Equal("suppressed", result.Value.Comparison.DisclosureState);
        Assert.Equal("compatible", result.Value.Comparison.CompatibilityState);
        Assert.Equal(1, result.Value.Comparison.LinkedPairCount);
        Assert.Equal(0, result.Value.Comparison.VisibleScoreCount);
        Assert.Equal(1, result.Value.Comparison.SuppressedScoreCount);
        Assert.Equal(0, result.Value.Comparison.BlockedScoreCount);
    }

    [DockerFact]
    public async Task Campaign_series_waves_workspace_excludes_sensitive_fields()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var template = await SeedTenantShellAsync(runtimeOptions, tenantId, "tenant-waves-sensitive");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Sensitive waves series");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var baseline = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive wave 1",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var comparison = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Sensitive wave 2",
            status: CampaignStatuses.Live,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            baseline,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00"),
            configured: true);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            comparison,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-13T09:00:00+00:00"),
            configured: true);
        var participantCode = await SeedParticipantCodeAsync(runtimeOptions, tenantId, series.Id);
        var baselineSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            baseline.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-06T10:00:00+00:00"),
            includeSensitiveValues: true,
            participantCodeId: participantCode.Id);
        await SeedScoreAsync(runtimeOptions, tenantId, baseline.Id, baselineSession.Id, scoringRule.Id);
        var comparisonSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            comparison.Id,
            template.QuestionId,
            submittedAt: DateTimeOffset.Parse("2026-05-13T10:00:00+00:00"),
            includeSensitiveValues: true,
            participantCodeId: participantCode.Id);
        await SeedScoreAsync(runtimeOptions, tenantId, comparison.Id, comparisonSession.Id, scoringRule.Id);
        await SeedExportArtifactAsync(runtimeOptions, tenantId, baseline.Id, series.Id);
        await SeedEmailInvitationAsync(
            runtimeOptions,
            tenantId,
            baseline.Id,
            SensitiveRecipient,
            NotificationStatuses.Failed,
            attemptCreatedAt: DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"));

        var commandRecorder = new RecordingCommandInterceptor();
        await using var db = new ApplicationDbContext(CreateRuntimeOptions(commandRecorder));
        var store = new ProductSurfaceReadStore(db, new TenantDbScope(db));

        var result = await store.GetCampaignSeriesWavesWorkspaceAsync(
            tenantId,
            series.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("ParticipantCode", serialized);
        Assert.DoesNotContain("CodeSalt", serialized);
        Assert.DoesNotContain("Token", serialized);
        Assert.DoesNotContain("Hash", serialized);
        Assert.DoesNotContain("Recipient", serialized);
        Assert.DoesNotContain("ProviderMessage", serialized);
        Assert.DoesNotContain("Error", serialized);
        Assert.DoesNotContain("IpHash", serialized);
        Assert.DoesNotContain("UserAgentHash", serialized);
        Assert.DoesNotContain("Answer", serialized);
        Assert.DoesNotContain("Content", serialized);
        Assert.DoesNotContain("Codebook", serialized);
        Assert.DoesNotContain("DocumentHash", serialized);
        Assert.DoesNotContain(SensitiveTokenHash, serialized);
        Assert.DoesNotContain(SensitiveRecipient, serialized);
        Assert.DoesNotContain(SensitiveProviderMessageId, serialized);
        Assert.DoesNotContain(SensitiveDeliveryError, serialized);
        Assert.DoesNotContain(SensitiveIpHash, serialized);
        Assert.DoesNotContain(SensitiveUserAgentHash, serialized);
        Assert.DoesNotContain(SensitiveAnswerValue, serialized);
        Assert.DoesNotContain(SensitiveExportContent, serialized);
        Assert.DoesNotContain(SensitiveCodebookContent, serialized);

        var storeSql = string.Join('\n', commandRecorder.Commands);
        Assert.DoesNotContain("code_salt", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message_id", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ip_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_agent_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("content", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebook_json", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("scoring_rule_document_hash", storeSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("document_hash", storeSql, StringComparison.OrdinalIgnoreCase);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> migratorOptions)
    {
        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);
    }

    private DbContextOptions<ApplicationDbContext> CreateMigratorOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private DbContextOptions<ApplicationDbContext> CreateRuntimeOptions(params IInterceptor[] interceptors)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static async Task CreateRuntimeRoleAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            $$"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_roles
                    WHERE rolname = '{{RuntimeUsername}}'
                ) THEN
                    CREATE ROLE {{RuntimeUsername}} LOGIN PASSWORD '{{RuntimePassword}}';
                END IF;
            END
            $$;

            GRANT USAGE ON SCHEMA public TO {{RuntimeUsername}};
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
                tenant,
                user_account,
                external_auth_identity,
                auth_session,
                role,
                permission,
                role_permission,
                role_assignment,
                subject,
                subject_group,
                subject_membership,
                subject_relationship,
                directory_connection,
                directory_connection_consent_request,
                directory_import_rule,
                directory_import_run,
                instrument,
                instrument_subscale,
                instrument_item,
                instrument_norm,
                translation,
                survey_template,
                template_version,
                scoring_rule,
                score_run,
                score,
                export_artifact,
                campaign_series,
                campaign,
                campaign_launch_snapshot,
                consent_document,
                retention_policy,
                disclosure_policy,
                consent_record,
                audience,
                audience_member,
                respondent_rule,
                assignment,
                invitation_token,
                notification,
                notification_delivery_attempt,
                notification_delivery_event,
                email_suppression,
                email_template,
                participant_code,
                response_session,
                answer,
                section,
                scale,
                question,
                choice_option
            TO {{RuntimeUsername}};

            GRANT SELECT, INSERT ON TABLE
                audit_event,
                outbox_event
            TO {{RuntimeUsername}};
            """);
    }

    private static async Task<TenantRolesFixture> SeedTenantRolesAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantA,
        Guid tenantB)
    {
        var ownerRoleId = Guid.NewGuid();
        var analystRoleId = Guid.NewGuid();
        var foreignRoleId = Guid.NewGuid();
        var setupPermissionId = Guid.NewGuid();
        var teamPermissionId = Guid.NewGuid();
        var exportPermissionId = Guid.NewGuid();

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);
        db.Roles.AddRange(
            new Role(ownerRoleId, tenantA, "tenant_owner", "Tenant Owner"),
            new Role(analystRoleId, tenantA, "analyst", "Analyst"));
        db.Permissions.AddRange(
            new Permission(setupPermissionId, "setup.manage"),
            new Permission(teamPermissionId, "team.manage"),
            new Permission(exportPermissionId, "export.read"));
        db.RolePermissions.AddRange(
            new RolePermission(ownerRoleId, teamPermissionId),
            new RolePermission(ownerRoleId, exportPermissionId),
            new RolePermission(ownerRoleId, setupPermissionId),
            new RolePermission(analystRoleId, exportPermissionId));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await using var foreignTransaction = await tenantDbScope.BeginTransactionAsync(tenantB);
        db.Roles.Add(new Role(foreignRoleId, tenantB, "foreign_owner", "Foreign Owner"));
        db.RolePermissions.Add(new RolePermission(foreignRoleId, teamPermissionId));
        await db.SaveChangesAsync();
        await foreignTransaction.CommitAsync();

        return new TenantRolesFixture(ownerRoleId, analystRoleId);
    }

    private static async Task<TemplateFixture> SeedTenantShellAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug)
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, $"{tenantSlug} pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "core", "Core");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "agreement",
            ScaleTypes.Likert,
            1,
            5,
            1,
            naAllowed: false,
            """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "q01",
            QuestionTypes.Likert,
            scale.Id,
            "I feel depleted after work.",
            required: true,
            measurementLevel: MeasurementLevels.Ordinal);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, $"Tenant {tenantSlug}"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.Add(question);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new TemplateFixture(version.Id, question.Id);
    }

    private static async Task<CampaignSeries> SeedSeriesAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string name,
        DateTimeOffset? updatedAt = null,
        string studyKind = CampaignSeriesStudyKinds.Own,
        string? sampleScenario = null)
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            name,
            CreateCodeSalt(),
            studyKind: studyKind,
            sampleScenario: sampleScenario);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.CampaignSeries.Add(series);
        await db.SaveChangesAsync();

        if (updatedAt.HasValue)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE campaign_series SET updated_at = {updatedAt.Value} WHERE id = {series.Id}");
        }

        await transaction.CommitAsync();

        return series;
    }

    private static async Task<Guid> SeedTenantMemberRosterAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid ownerUserId,
        Guid analystUserId,
        Guid unassignedUserId,
        Guid deletedUserId)
    {
        var ownerRoleId = Guid.NewGuid();
        var analystRoleId = Guid.NewGuid();
        var setupPermissionId = Guid.NewGuid();
        var exportPermissionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-10T08:00:00+00:00");
        var grantedAt = DateTimeOffset.Parse("2026-05-10T08:30:00+00:00");
        var lastLoginAt = DateTimeOffset.Parse("2026-05-11T09:00:00+00:00");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        db.UserAccounts.AddRange(
            new UserAccount(ownerUserId, tenantId, "owner@example.test"),
            new UserAccount(analystUserId, tenantId, "analyst@example.test"),
            new UserAccount(unassignedUserId, tenantId, "unassigned@example.test"),
            new UserAccount(deletedUserId, tenantId, "deleted@example.test"));
        db.Roles.AddRange(
            new Role(ownerRoleId, tenantId, "tenant_owner", "Tenant Owner"),
            new Role(analystRoleId, tenantId, "analyst", "Analyst"));
        db.Permissions.AddRange(
            new Permission(setupPermissionId, "setup.manage"),
            new Permission(exportPermissionId, "export.read"));
        db.RolePermissions.AddRange(
            new RolePermission(ownerRoleId, setupPermissionId),
            new RolePermission(ownerRoleId, exportPermissionId),
            new RolePermission(analystRoleId, exportPermissionId));
        db.RoleAssignments.AddRange(
            new RoleAssignment(
                Guid.NewGuid(),
                tenantId,
                ownerUserId,
                ownerRoleId,
                RoleAssignmentScopes.Tenant,
                grantedBy: ownerUserId),
            new RoleAssignment(
                Guid.NewGuid(),
                tenantId,
                analystUserId,
                analystRoleId,
                RoleAssignmentScopes.Tenant,
                grantedBy: ownerUserId),
            new RoleAssignment(
                Guid.NewGuid(),
                tenantId,
                deletedUserId,
                analystRoleId,
                RoleAssignmentScopes.Tenant,
                grantedBy: ownerUserId));
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE user_account SET created_at = {createdAt}, updated_at = {createdAt}, last_login_at = {lastLoginAt} WHERE id = {ownerUserId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE user_account SET created_at = {createdAt}, updated_at = {createdAt} WHERE id = {analystUserId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE user_account SET deleted_at = {createdAt.AddDays(1)} WHERE id = {deletedUserId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE role_assignment SET granted_at = {grantedAt} WHERE tenant_id = {tenantId}");
        await transaction.CommitAsync();

        return ownerRoleId;
    }

    private static async Task SeedExternalAuthIdentityAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
            Guid.NewGuid(),
            tenantId,
            userId,
            "auth0",
            $"hash-{userId:N}",
            "owner@example.test",
            DateTimeOffset.Parse("2026-05-11T08:00:00+00:00")));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedTenantBMemberAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId)
    {
        var roleId = Guid.NewGuid();

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.UserAccounts.Add(new UserAccount(userId, tenantId, "tenant-b@example.test"));
        db.Roles.Add(new Role(roleId, tenantId, "viewer", "Viewer"));
        db.RoleAssignments.Add(new RoleAssignment(
            Guid.NewGuid(),
            tenantId,
            userId,
            roleId,
            RoleAssignmentScopes.Tenant,
            grantedBy: userId));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task ArchiveSeriesAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        DateTimeOffset archivedAt,
        string reason)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var series = await db.CampaignSeries.SingleAsync(item => item.Id == campaignSeriesId);
        series.Archive(reason, actorUserId, archivedAt);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<Campaign> SeedCampaignAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid templateVersionId,
        Guid? campaignSeriesId,
        string name,
        string status = CampaignStatuses.Draft,
        string responseIdentityMode = ResponseIdentityModes.Anonymous)
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            tenantId,
            templateVersionId,
            name,
            responseIdentityMode,
            campaignSeriesId: campaignSeriesId,
            status: status,
            schedule: """{"kind":"one_shot"}""");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return campaign;
    }

    private static async Task<ScoringRule> SeedScoringRuleAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid templateVersionId)
    {
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            templateVersionId,
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            """{"rule_id":"burnout.total","version":"1.0.0"}""",
            """{"scores":["total"]}""");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ScoringRules.Add(rule);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return rule;
    }

    private static async Task<CampaignLaunchSnapshot> SeedLaunchSnapshotAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Campaign campaign,
        Guid templateVersionId,
        Guid scoringRuleId,
        DateTimeOffset launchedAt,
        bool configured = false)
    {
        var policyVersion = $"1.0.{launchedAt.UtcDateTime:yyyyMMddHHmmss}";
        var consentDocument = configured
            ? new ConsentDocument(
                Guid.NewGuid(),
                tenantId,
                campaign.CampaignSeriesId!.Value,
                "en",
                policyVersion,
                "Consent",
                "Consent body",
                """["participate"]""",
                "[]",
                launchedAt.AddDays(-1))
            : null;
        var retentionPolicy = configured
            ? new RetentionPolicy(
                Guid.NewGuid(),
                tenantId,
                campaign.CampaignSeriesId!.Value,
                policyVersion,
                retainForYears: 1,
                RetentionPolicy.ResponseSubmittedAt,
                RetentionPolicy.Anonymize,
                DateOnly.FromDateTime(launchedAt.UtcDateTime.Date.AddYears(1)),
                "{}",
                launchedAt.AddDays(-1))
            : null;
        var disclosurePolicy = configured
            ? new DisclosurePolicy(
                Guid.NewGuid(),
                tenantId,
                campaign.CampaignSeriesId!.Value,
                policyVersion,
                DisclosurePolicy.MinimumKMin,
                DisclosurePolicy.HideCell,
                """["total"]""",
                launchedAt.AddDays(-1))
            : null;
        var launchPacket = JsonSerializer.Serialize(new
        {
            schema_version = 1,
            template = new { status = "configured" },
            instrument = new { status = "configured" },
            scoring = new { status = "configured" },
            policies = new
            {
                consent = consentDocument is null ? "not_configured" : "configured",
                retention = retentionPolicy is null ? "not_configured" : "configured",
                disclosure = disclosurePolicy is null ? "not_configured" : "configured"
            },
            identity = new { response_identity_mode = campaign.ResponseIdentityMode },
            respondent_rules = new
            {
                materialization = "not_requested",
                materialized_assignment_count = 0
            },
            launch_readiness = new { status = "ready" },
            provenance = new
            {
                source = "runtime_launch",
                campaign_id = campaign.Id,
                campaign_series_id = campaign.CampaignSeriesId,
                launched_at = launchedAt
            }
        });
        var snapshot = new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            campaign.CampaignSeriesId,
            templateVersionId,
            scoringRuleId,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            templateQuestionCount: 1,
            scoringRuleDocumentHash: "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            launchReadiness: "{}",
            launchedAt,
            consentDocumentId: consentDocument?.Id,
            retentionPolicyId: retentionPolicy?.Id,
            disclosurePolicyId: disclosurePolicy?.Id,
            launchPacket: launchPacket);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        if (consentDocument is not null)
        {
            db.ConsentDocuments.Add(consentDocument);
        }

        if (retentionPolicy is not null)
        {
            db.RetentionPolicies.Add(retentionPolicy);
        }

        if (disclosurePolicy is not null)
        {
            db.DisclosurePolicies.Add(disclosurePolicy);
        }

        db.CampaignLaunchSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return snapshot;
    }

    private static async Task<PolicyFixture> SeedPoliciesAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset createdAt)
    {
        var consentDocument = new ConsentDocument(
            Guid.NewGuid(),
            tenantId,
            campaignSeriesId,
            "en",
            "1.0.0",
            "Consent",
            "Consent body",
            """["participate"]""",
            "[]",
            createdAt);
        var retentionPolicy = new RetentionPolicy(
            Guid.NewGuid(),
            tenantId,
            campaignSeriesId,
            "1.0.0",
            retainForYears: 1,
            RetentionPolicy.ResponseSubmittedAt,
            RetentionPolicy.Anonymize,
            DateOnly.FromDateTime(createdAt.UtcDateTime.Date.AddYears(1)),
            "{}",
            createdAt);
        var disclosurePolicy = new DisclosurePolicy(
            Guid.NewGuid(),
            tenantId,
            campaignSeriesId,
            "1.0.0",
            DisclosurePolicy.MinimumKMin,
            DisclosurePolicy.HideCell,
            """["total"]""",
            createdAt);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ConsentDocuments.Add(consentDocument);
        db.RetentionPolicies.Add(retentionPolicy);
        db.DisclosurePolicies.Add(disclosurePolicy);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PolicyFixture(
            consentDocument.Id,
            retentionPolicy.Id,
            disclosurePolicy.Id);
    }

    private static async Task<ResponseSession> SeedSubmittedResponseAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid questionId,
        DateTimeOffset? submittedAt = null,
        bool includeSensitiveValues = false,
        Guid? participantCodeId = null)
    {
        var effectiveSubmittedAt = submittedAt ?? DateTimeOffset.UtcNow;
        var token = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            $"{SensitiveTokenHash}-{Guid.NewGuid():N}",
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "proof_respondent",
            token.Id);
        var session = new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            assignment.Id,
            "en",
            participantCodeId,
            startedAt: effectiveSubmittedAt.AddMinutes(-5),
            ipHash: includeSensitiveValues ? SensitiveIpHash : null,
            userAgentHash: includeSensitiveValues ? SensitiveUserAgentHash : null);
        session.Submit(effectiveSubmittedAt, timeTakenMs: 1200);
        var answer = new Answer(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            questionId,
            $"\"{SensitiveAnswerValue}\"");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.InvitationTokens.Add(token);
        db.Assignments.Add(assignment);
        db.ResponseSessions.Add(session);
        db.Answers.Add(answer);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return session;
    }

    private static async Task<ResponseSession> SeedSubmittedIdentifiedResponseAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid questionId,
        Guid respondentSubjectId,
        DateTimeOffset submittedAt)
    {
        var assignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "self",
            respondentSubjectId,
            targetSubjectId: respondentSubjectId);
        var session = new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            assignment.Id,
            "en",
            participantCodeId: null,
            startedAt: submittedAt.AddMinutes(-5),
            ipHash: null,
            userAgentHash: null);
        session.Submit(submittedAt, timeTakenMs: 1200);
        var answer = new Answer(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            questionId,
            $"\"{SensitiveAnswerValue}\"");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Assignments.Add(assignment);
        db.ResponseSessions.Add(session);
        db.Answers.Add(answer);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return session;
    }

    private static async Task CloseCampaignAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid actorUserId,
        DateTimeOffset closedAt,
        string reason)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, actorUserId);
        var campaign = await db.Campaigns.SingleAsync(entity => entity.Id == campaignId);
        campaign.Close(reason, actorUserId, closedAt);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<ResponseSession> SeedDraftResponseAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        DateTimeOffset startedAt)
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            $"{SensitiveTokenHash}-{Guid.NewGuid():N}",
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "proof_respondent",
            token.Id);
        var session = new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            assignment.Id,
            "en",
            participantCodeId: null,
            startedAt: startedAt,
            ipHash: null,
            userAgentHash: null);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.InvitationTokens.Add(token);
        db.Assignments.Add(assignment);
        db.ResponseSessions.Add(session);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return session;
    }

    private static async Task<ParticipantCode> SeedParticipantCodeAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignSeriesId,
        int seedOffset = 0)
    {
        var hash = new byte[ParticipantCode.MinimumArgon2OutputBytes];
        hash[0] = (byte)(seedOffset % 256);
        hash[1] = (byte)((seedOffset + 1) % 256);
        var participantCode = new ParticipantCode(
            Guid.NewGuid(),
            tenantId,
            campaignSeriesId,
            hash,
            ParticipantCode.MinimumArgon2MemoryKiB,
            ParticipantCode.MinimumArgon2Iterations,
            ParticipantCode.MinimumArgon2Parallelism,
            ParticipantCode.MinimumArgon2OutputBytes,
            DateTimeOffset.Parse("2026-05-06T09:00:00+00:00").AddMinutes(seedOffset));

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ParticipantCodes.Add(participantCode);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return participantCode;
    }

    private static async Task SeedScoreAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid responseSessionId,
        Guid scoringRuleId,
        DateTimeOffset? ranAt = null)
    {
        var run = new ScoreRun(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            responseSessionId,
            scoringRuleId,
            ranAt: ranAt);
        var score = new Score(
            Guid.NewGuid(),
            tenantId,
            run.Id,
            campaignId,
            responseSessionId,
            "total",
            4.2m,
            1,
            computedAt: ranAt);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ScoreRuns.Add(run);
        db.Scores.Add(score);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<ExportArtifact> SeedExportArtifactAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid campaignSeriesId,
        string artifactType = ExportArtifactTypes.ReportProofCsvCodebook,
        string fileName = "proof.csv",
        DateTimeOffset? createdAtOverride = null,
        string status = ExportArtifactStatuses.Succeeded,
        DateTimeOffset? failedAt = null,
        string? failureReasonCode = null)
    {
        var createdAt = createdAtOverride ?? DateTimeOffset.Parse("2026-05-06T12:00:00+00:00");
        var succeeded = status == ExportArtifactStatuses.Succeeded;
        var targetKind = artifactType is ExportArtifactTypes.CampaignSeriesResponseCsvCodebook
            or ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook
            or ExportArtifactTypes.CampaignSeriesReportHtml
            or ExportArtifactTypes.CampaignSeriesReportPdf
            ? ExportArtifactTargetKinds.CampaignSeries
            : ExportArtifactTargetKinds.Campaign;
        var format = artifactType switch
        {
            ExportArtifactTypes.CampaignSeriesReportHtml => ExportArtifactFormats.Html,
            ExportArtifactTypes.CampaignSeriesReportPdf => ExportArtifactFormats.Pdf,
            _ => ExportArtifactFormats.CsvCodebook
        };
        var contentType = artifactType switch
        {
            ExportArtifactTypes.CampaignSeriesReportHtml => "text/html; charset=utf-8",
            ExportArtifactTypes.CampaignSeriesReportPdf => "application/pdf",
            _ => "text/csv"
        };
        var isExternalObject = artifactType == ExportArtifactTypes.CampaignSeriesReportPdf;
        var artifact = new ExportArtifact(
            Guid.NewGuid(),
            tenantId,
            targetKind,
            targetKind == ExportArtifactTargetKinds.CampaignSeries ? null : campaignId,
            campaignSeriesId,
            artifactType,
            status,
            format,
            fileName,
            contentType,
            rowCount: succeeded ? 1 : 0,
            byteSize: succeeded ? 10 : 0,
            checksumSha256: succeeded ? "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789" : null,
            metadataJson: "{}",
            content: succeeded && !isExternalObject ? SensitiveExportContent : null,
            codebookJson: $$"""{"label":"{{SensitiveCodebookContent}}"}""",
            createdAt: createdAt,
            completedAt: succeeded ? createdAt.AddSeconds(1) : null,
            failedAt: status == ExportArtifactStatuses.Failed ? failedAt ?? createdAt.AddSeconds(1) : null,
            failureReasonCode: status == ExportArtifactStatuses.Failed ? failureReasonCode ?? "export.failed" : null,
            storageKind: isExternalObject
                ? ExportArtifactStorageKinds.ExternalObject
                : ExportArtifactStorageKinds.InlineText,
            storageKey: isExternalObject && succeeded
                ? $"tenants/{tenantId:N}/campaign-series/{campaignSeriesId:N}/reports/{Guid.NewGuid():N}.pdf"
                : null);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.ExportArtifacts.Add(artifact);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return artifact;
    }

    private static async Task<Notification> SeedEmailInvitationAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        string recipient,
        string status,
        DateTimeOffset? attemptCreatedAt = null)
    {
        var effectiveAt = attemptCreatedAt ?? DateTimeOffset.UtcNow;
        var token = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            $"{SensitiveTokenHash}-{Guid.NewGuid():N}",
            InvitationTokenChannels.Email,
            recipient);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "invited_respondent",
            token.Id);
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            assignment.Id,
            recipient,
            effectiveAt.AddMinutes(-5));

        NotificationDeliveryAttempt? deliveryAttempt = null;
        if (status == NotificationStatuses.Sent)
        {
            notification.MarkSent(effectiveAt);
            deliveryAttempt = NotificationDeliveryAttempt.CreateSent(
                Guid.NewGuid(),
                tenantId,
                notification.Id,
                "local-dev",
                recipient,
                SensitiveProviderMessageId,
                effectiveAt);
        }
        else if (status == NotificationStatuses.Failed)
        {
            notification.MarkFailed(SensitiveDeliveryError, effectiveAt);
            deliveryAttempt = NotificationDeliveryAttempt.CreateFailed(
                Guid.NewGuid(),
                tenantId,
                notification.Id,
                "local-dev",
                recipient,
                SensitiveDeliveryError,
                effectiveAt);
        }
        else if (status != NotificationStatuses.Queued)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.");
        }

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.InvitationTokens.Add(token);
        db.Assignments.Add(assignment);
        db.Notifications.Add(notification);
        if (deliveryAttempt is not null)
        {
            db.NotificationDeliveryAttempts.Add(deliveryAttempt);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return notification;
    }

    private static byte[] CreateCodeSalt()
    {
        var salt = new byte[32];
        salt[0] = 42;

        return salt;
    }

    private static async Task<Subject> SeedSubjectAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string displayName,
        string email,
        string externalId,
        string attributes = """{"source":"test"}""")
    {
        var subject = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: externalId,
            email: email,
            displayName: displayName,
            attributes: attributes);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return subject;
    }

    private static async Task SeedTenantAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, $"Tenant {tenantSlug}"));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<SubjectGroup> SeedSubjectGroupAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string type,
        string name)
    {
        var group = new SubjectGroup(Guid.NewGuid(), tenantId, type, name);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SubjectGroups.Add(group);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return group;
    }

    private static async Task SeedSubjectMembershipAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid subjectId,
        Guid groupId,
        string roleInGroup)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SubjectMemberships.Add(new SubjectMembership(subjectId, groupId, roleInGroup));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedSubjectRelationshipAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid subjectId,
        Guid relatedSubjectId,
        string relationshipType)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SubjectRelationships.Add(new SubjectRelationship(
            Guid.NewGuid(),
            tenantId,
            subjectId,
            relatedSubjectId,
            relationshipType));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedAudienceAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        params Guid[] subjectIds)
    {
        var audience = new Audience(Guid.NewGuid(), campaignId);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Audiences.Add(audience);
        db.AudienceMembers.AddRange(subjectIds.Select(subjectId => new AudienceMember(audience.Id, subjectId)));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private sealed record TemplateFixture(Guid TemplateVersionId, Guid QuestionId);

    private sealed record TenantRolesFixture(Guid OwnerRoleId, Guid AnalystRoleId);

    private sealed record PolicyFixture(Guid ConsentId, Guid RetentionId, Guid DisclosureId);

    private sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<string> _commands = [];

        public IReadOnlyList<string> Commands => _commands;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _commands.Add(command.CommandText);

            return ValueTask.FromResult(result);
        }
    }
}
