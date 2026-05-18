using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.ProductSurfaces;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ProductSurfaceWriteStoreTests : IAsyncLifetime
{
    private const string RuntimeUsername = "platform_app_runtime";
    private const string RuntimePassword = "platform_app_runtime";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Rename_campaign_series_updates_name_under_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "rename-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Original pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RenameCampaignSeriesAsync(
            tenantId,
            series.Id,
            new RenameCampaignSeriesRequest("  Renamed pulse  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Id);
        Assert.Equal("Renamed pulse", result.Value.Name);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verificationDb.CampaignSeries.SingleAsync(item => item.Id == series.Id);
        Assert.Equal("Renamed pulse", persisted.Name);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rename_campaign_series_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "tenant-b");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RenameCampaignSeriesAsync(
            tenantA,
            tenantBSeries.Id,
            new RenameCampaignSeriesRequest("Cross tenant rename"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Rename_campaign_series_returns_validation_for_invalid_name()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "invalid-name-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Original pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RenameCampaignSeriesAsync(
            tenantId,
            series.Id,
            new RenameCampaignSeriesRequest("   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.invalid", result.Error.Code);
    }

    [DockerFact]
    public async Task Rename_campaign_series_returns_conflict_for_sample_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "sample-rename-tenant");
        var series = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.MixedLifecycle);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RenameCampaignSeriesAsync(
            tenantId,
            series.Id,
            new RenameCampaignSeriesRequest("Renamed sample"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.sample_read_only", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [DockerFact]
    public async Task Duplicate_campaign_series_copies_sample_setup_skeleton_without_operational_data()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-16T09:00:00+00:00");
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "duplicate-tenant");
        var sourceSeries = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.MixedLifecycle);
        var policies = await SeedPoliciesAsync(runtimeOptions, tenantId, sourceSeries.Id, createdAt);
        var template = await SeedScoringTemplateAsync(runtimeOptions, tenantId, "duplicate-template");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var sourceCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            sourceSeries.Id,
            "Baseline wave",
            CampaignStatuses.Live);
        var snapshot = await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign,
            template.TemplateVersionId,
            scoringRule.Id,
            createdAt.AddHours(1));
        var sourceSession = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign.Id,
            template.QuestionId,
            createdAt.AddHours(2),
            "4");
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            sourceCampaign.Id,
            sourceSession.Id,
            scoringRule.Id,
            createdAt.AddHours(3));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.DuplicateCampaignSeriesAsync(
            tenantId,
            sourceSeries.Id,
            actorUserId,
            new DuplicateCampaignSeriesRequest("  Copy of Starter sample  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.NotEqual(sourceSeries.Id, result.Value.Id);
        Assert.Equal("Copy of Starter sample", result.Value.Name);
        Assert.Equal(CampaignSeriesStudyKinds.Own, result.Value.StudyKind);
        Assert.False(result.Value.IsSample);
        Assert.Equal(sourceSeries.Id, result.Value.SourceCampaignSeriesId);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var copiedSeries = await verificationDb.CampaignSeries.SingleAsync(series => series.Id == result.Value.Id);
        Assert.Equal(tenantId, copiedSeries.TenantId);
        Assert.Equal("Copy of Starter sample", copiedSeries.Name);
        Assert.Equal(CampaignSeriesStudyKinds.Own, copiedSeries.StudyKind);
        Assert.False(copiedSeries.IsSample);
        Assert.Null(copiedSeries.SampleScenario);
        Assert.False(copiedSeries.Archived);
        Assert.Equal(32, copiedSeries.CodeSalt.Length);
        Assert.NotEqual(Convert.ToHexString(sourceSeries.CodeSalt), Convert.ToHexString(copiedSeries.CodeSalt));

        var copiedConsent = await verificationDb.ConsentDocuments.SingleAsync(document =>
            document.CampaignSeriesId == copiedSeries.Id);
        var sourceConsent = await verificationDb.ConsentDocuments.SingleAsync(document => document.Id == policies.ConsentDocumentId);
        Assert.NotEqual(sourceConsent.Id, copiedConsent.Id);
        Assert.Equal(sourceConsent.Locale, copiedConsent.Locale);
        Assert.Equal(sourceConsent.Version, copiedConsent.Version);
        Assert.Equal(sourceConsent.Title, copiedConsent.Title);
        Assert.Equal(sourceConsent.BodyMarkdown, copiedConsent.BodyMarkdown);
        Assert.Equal(sourceConsent.RequiredGrants, copiedConsent.RequiredGrants);
        Assert.Equal(sourceConsent.OptionalGrants, copiedConsent.OptionalGrants);

        var copiedRetention = await verificationDb.RetentionPolicies.SingleAsync(policy =>
            policy.CampaignSeriesId == copiedSeries.Id);
        var sourceRetention = await verificationDb.RetentionPolicies.SingleAsync(policy => policy.Id == policies.RetentionPolicyId);
        Assert.NotEqual(sourceRetention.Id, copiedRetention.Id);
        Assert.Equal(sourceRetention.Version, copiedRetention.Version);
        Assert.Equal(sourceRetention.RetainForYears, copiedRetention.RetainForYears);
        Assert.Equal(sourceRetention.RetentionStartEvent, copiedRetention.RetentionStartEvent);
        Assert.Equal(sourceRetention.ActionAfter, copiedRetention.ActionAfter);
        Assert.Equal(sourceRetention.PublicationLimits, copiedRetention.PublicationLimits);

        var copiedDisclosure = await verificationDb.DisclosurePolicies.SingleAsync(policy =>
            policy.CampaignSeriesId == copiedSeries.Id);
        var sourceDisclosure = await verificationDb.DisclosurePolicies.SingleAsync(policy => policy.Id == policies.DisclosurePolicyId);
        Assert.NotEqual(sourceDisclosure.Id, copiedDisclosure.Id);
        Assert.Equal(sourceDisclosure.Version, copiedDisclosure.Version);
        Assert.Equal(sourceDisclosure.KMin, copiedDisclosure.KMin);
        Assert.Equal(sourceDisclosure.SuppressionStrategy, copiedDisclosure.SuppressionStrategy);
        Assert.Equal(sourceDisclosure.AppliesToDimensions, copiedDisclosure.AppliesToDimensions);

        var copiedCampaign = await verificationDb.Campaigns.SingleAsync(campaign =>
            campaign.CampaignSeriesId == copiedSeries.Id);
        Assert.NotEqual(sourceCampaign.Id, copiedCampaign.Id);
        Assert.Equal(CampaignStatuses.Draft, copiedCampaign.Status);
        Assert.Equal(sourceCampaign.TemplateVersionId, copiedCampaign.TemplateVersionId);
        Assert.Equal(sourceCampaign.Name, copiedCampaign.Name);
        Assert.Equal(sourceCampaign.ResponseIdentityMode, copiedCampaign.ResponseIdentityMode);
        Assert.Equal(sourceCampaign.Schedule, copiedCampaign.Schedule);
        Assert.Equal(sourceCampaign.DefaultLocale, copiedCampaign.DefaultLocale);
        Assert.Null(copiedCampaign.StartAt);
        Assert.Null(copiedCampaign.ClosedAt);

        Assert.Equal(1, await verificationDb.CampaignLaunchSnapshots.CountAsync(item => item.Id == snapshot.Id));
        Assert.Equal(0, await verificationDb.CampaignLaunchSnapshots.CountAsync(item =>
            item.CampaignSeriesId == copiedSeries.Id));
        Assert.Equal(0, await verificationDb.Assignments.CountAsync(assignment =>
            assignment.CampaignId == copiedCampaign.Id));
        Assert.Equal(0, await verificationDb.InvitationTokens.CountAsync(token =>
            token.CampaignId == copiedCampaign.Id));
        Assert.Equal(0, await verificationDb.ResponseSessions.CountAsync(session =>
            verificationDb.Assignments.Any(assignment =>
                assignment.Id == session.AssignmentId &&
                assignment.CampaignId == copiedCampaign.Id)));
        Assert.Equal(0, await verificationDb.Answers.CountAsync(answer =>
            verificationDb.ResponseSessions.Any(session =>
                session.Id == answer.SessionId &&
                verificationDb.Assignments.Any(assignment =>
                    assignment.Id == session.AssignmentId &&
                    assignment.CampaignId == copiedCampaign.Id))));
        Assert.Equal(0, await verificationDb.ScoreRuns.CountAsync(run => run.CampaignId == copiedCampaign.Id));
        Assert.Equal(0, await verificationDb.Scores.CountAsync(score => score.CampaignId == copiedCampaign.Id));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Duplicate_campaign_series_returns_not_found_for_cross_tenant_source()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "duplicate-tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "duplicate-tenant-b");
        var tenantBSeries = await SeedSeriesAsync(
            runtimeOptions,
            tenantB,
            "Tenant B sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.Setup);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.DuplicateCampaignSeriesAsync(
            tenantA,
            tenantBSeries.Id,
            Guid.NewGuid(),
            new DuplicateCampaignSeriesRequest("Copy"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [DockerFact]
    public async Task Duplicate_campaign_series_returns_conflict_for_own_source()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "duplicate-own-tenant");
        var ownSeries = await SeedSeriesAsync(runtimeOptions, tenantId, "Own study");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.DuplicateCampaignSeriesAsync(
            tenantId,
            ownSeries.Id,
            Guid.NewGuid(),
            new DuplicateCampaignSeriesRequest("Copy"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_sample", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [DockerFact]
    public async Task Duplicate_campaign_series_returns_validation_for_invalid_name()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "duplicate-invalid-name-tenant");
        var sourceSeries = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.Setup);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.DuplicateCampaignSeriesAsync(
            tenantId,
            sourceSeries.Id,
            Guid.NewGuid(),
            new DuplicateCampaignSeriesRequest("   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [DockerFact]
    public async Task Archive_campaign_series_sets_metadata_under_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "archive-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Archive pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.ArchiveCampaignSeriesAsync(
            tenantId,
            series.Id,
            actorUserId,
            new ArchiveCampaignSeriesRequest("  Out of rotation  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Id);
        Assert.True(result.Value.Archived);
        Assert.NotNull(result.Value.ArchivedAt);
        Assert.Equal(actorUserId, result.Value.ArchivedByUserId);
        Assert.Equal("Out of rotation", result.Value.ArchiveReason);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verificationDb.CampaignSeries.SingleAsync(item => item.Id == series.Id);
        Assert.True(persisted.Archived);
        Assert.Equal(actorUserId, persisted.ArchivedByUserId);
        Assert.Equal("Out of rotation", persisted.ArchiveReason);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Restore_campaign_series_clears_archive_metadata_under_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "restore-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Restore pulse");

        await using (var archiveDb = new ApplicationDbContext(runtimeOptions))
        {
            var archiveStore = new ProductSurfaceWriteStore(archiveDb, new TenantDbScope(archiveDb));
            var archiveResult = await archiveStore.ArchiveCampaignSeriesAsync(
                tenantId,
                series.Id,
                actorUserId,
                new ArchiveCampaignSeriesRequest("Temporary pause"),
                CancellationToken.None);
            Assert.True(archiveResult.IsSuccess, archiveResult.Error.ToString());
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RestoreCampaignSeriesAsync(
            tenantId,
            series.Id,
            actorUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.Id);
        Assert.False(result.Value.Archived);
        Assert.Null(result.Value.ArchivedAt);
        Assert.Null(result.Value.ArchivedByUserId);
        Assert.Null(result.Value.ArchiveReason);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verificationDb.CampaignSeries.SingleAsync(item => item.Id == series.Id);
        Assert.False(persisted.Archived);
        Assert.Null(persisted.ArchivedAt);
        Assert.Null(persisted.ArchivedByUserId);
        Assert.Null(persisted.ArchiveReason);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Archive_campaign_series_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "archive-tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "archive-tenant-b");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B archive pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.ArchiveCampaignSeriesAsync(
            tenantA,
            tenantBSeries.Id,
            Guid.NewGuid(),
            new ArchiveCampaignSeriesRequest("Wrong tenant"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Archive_campaign_series_returns_conflict_for_sample_series()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "sample-archive-tenant");
        var series = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.MixedLifecycle);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.ArchiveCampaignSeriesAsync(
            tenantId,
            series.Id,
            actorUserId,
            new ArchiveCampaignSeriesRequest("Wrong"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.sample_read_only", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [DockerFact]
    public async Task Restore_campaign_series_returns_conflict_for_sample_series()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "sample-restore-tenant");
        var series = await SeedSeriesAsync(
            runtimeOptions,
            tenantId,
            "Starter sample",
            CampaignSeriesStudyKinds.Sample,
            CampaignSeriesSampleScenarios.MixedLifecycle);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RestoreCampaignSeriesAsync(
            tenantId,
            series.Id,
            actorUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.sample_read_only", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [DockerFact]
    public async Task Close_campaign_sets_metadata_under_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "close-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Close pulse");
        var templateVersionId = await SeedTemplateVersionAsync(runtimeOptions, tenantId, "close-tenant");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            templateVersionId,
            series.Id,
            "Close wave",
            CampaignStatuses.Live);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CloseCampaignAsync(
            tenantId,
            series.Id,
            campaign.Id,
            actorUserId,
            new CloseCampaignRequest("  Collection complete  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(campaign.Id, result.Value.Id);
        Assert.Equal(CampaignStatuses.Closed, result.Value.Status);
        Assert.NotNull(result.Value.ClosedAt);
        Assert.Equal(actorUserId, result.Value.ClosedByUserId);
        Assert.Equal("Collection complete", result.Value.CloseReason);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verificationDb.Campaigns.SingleAsync(item => item.Id == campaign.Id);
        Assert.Equal(CampaignStatuses.Closed, persisted.Status);
        Assert.Equal(actorUserId, persisted.ClosedByUserId);
        Assert.Equal("Collection complete", persisted.CloseReason);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Close_campaign_returns_conflict_for_non_live_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "close-conflict-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Close conflict pulse");
        var templateVersionId = await SeedTemplateVersionAsync(runtimeOptions, tenantId, "close-conflict-tenant");
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            templateVersionId,
            series.Id,
            "Draft wave",
            CampaignStatuses.Draft);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CloseCampaignAsync(
            tenantId,
            series.Id,
            campaign.Id,
            Guid.NewGuid(),
            new CloseCampaignRequest("Too early"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign.not_closeable", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [DockerFact]
    public async Task Close_campaign_returns_not_found_for_cross_tenant_campaign()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "close-tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "close-tenant-b");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B close pulse");
        var templateVersionId = await SeedTemplateVersionAsync(runtimeOptions, tenantB, "close-tenant-b");
        var tenantBCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantB,
            templateVersionId,
            tenantBSeries.Id,
            "Tenant B wave",
            CampaignStatuses.Live);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CloseCampaignAsync(
            tenantA,
            tenantBSeries.Id,
            tenantBCampaign.Id,
            Guid.NewGuid(),
            new CloseCampaignRequest("Wrong tenant"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Remediate_campaign_series_scores_materializes_unscored_submitted_sessions()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "score-remediation-tenant");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Score remediation pulse");
        var scoredTemplate = await SeedScoringTemplateAsync(runtimeOptions, tenantId, "score-remediation");
        var unconfiguredTemplate = await SeedScoringTemplateAsync(runtimeOptions, tenantId, "unconfigured");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, scoredTemplate.TemplateVersionId);
        var scoredCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            scoredTemplate.TemplateVersionId,
            series.Id,
            "Scored wave",
            CampaignStatuses.Live);
        var unconfiguredCampaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            unconfiguredTemplate.TemplateVersionId,
            series.Id,
            "Unconfigured wave",
            CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            scoredCampaign,
            scoredTemplate.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-11T12:00:00+00:00"));
        var alreadyScored = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            scoredCampaign.Id,
            scoredTemplate.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:00:00+00:00"),
            "4");
        var missingOne = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            scoredCampaign.Id,
            scoredTemplate.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:05:00+00:00"),
            "5");
        var missingTwo = await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            scoredCampaign.Id,
            scoredTemplate.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:10:00+00:00"),
            "3");
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            unconfiguredCampaign.Id,
            unconfiguredTemplate.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:15:00+00:00"),
            "2");
        await SeedScoreAsync(
            runtimeOptions,
            tenantId,
            scoredCampaign.Id,
            alreadyScored.Id,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-11T14:00:00+00:00"));

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RemediateCampaignSeriesScoresAsync(
            tenantId,
            series.Id,
            actorUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(series.Id, result.Value.CampaignSeriesId);
        Assert.Equal(4, result.Value.SubmittedResponseCount);
        Assert.Equal(3, result.Value.EligibleSubmittedResponseCount);
        Assert.Equal(1, result.Value.AlreadyScoredSubmittedResponseCount);
        Assert.Equal(2, result.Value.RemediatedSubmittedResponseCount);
        Assert.Equal(1, result.Value.SkippedNotConfiguredSubmittedResponseCount);
        Assert.Equal(0, result.Value.FailedSubmittedResponseCount);
        Assert.NotNull(result.Value.LatestScoringActivityAt);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var scoreRuns = await verificationDb.ScoreRuns
            .Where(run => run.CampaignId == scoredCampaign.Id && run.Status == ScoreRunStatuses.Success)
            .ToListAsync();
        Assert.Equal(3, scoreRuns.Count);
        Assert.Contains(scoreRuns, run => run.ResponseSessionId == alreadyScored.Id);
        Assert.Contains(scoreRuns, run => run.ResponseSessionId == missingOne.Id);
        Assert.Contains(scoreRuns, run => run.ResponseSessionId == missingTwo.Id);
        Assert.Equal(3, await verificationDb.Scores.CountAsync(score => score.CampaignId == scoredCampaign.Id));
        Assert.Equal(0, await verificationDb.ScoreRuns.CountAsync(run => run.CampaignId == unconfiguredCampaign.Id));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Remediate_campaign_series_scores_is_idempotent()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "score-remediation-idempotent");
        var series = await SeedSeriesAsync(runtimeOptions, tenantId, "Score remediation idempotent pulse");
        var template = await SeedScoringTemplateAsync(runtimeOptions, tenantId, "score-remediation-idempotent");
        var scoringRule = await SeedScoringRuleAsync(runtimeOptions, tenantId, template.TemplateVersionId);
        var campaign = await SeedCampaignAsync(
            runtimeOptions,
            tenantId,
            template.TemplateVersionId,
            series.Id,
            "Scored wave",
            CampaignStatuses.Live);
        await SeedLaunchSnapshotAsync(
            runtimeOptions,
            tenantId,
            campaign,
            template.TemplateVersionId,
            scoringRule.Id,
            DateTimeOffset.Parse("2026-05-11T12:00:00+00:00"));
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:00:00+00:00"),
            "4");
        await SeedSubmittedResponseAsync(
            runtimeOptions,
            tenantId,
            campaign.Id,
            template.QuestionId,
            DateTimeOffset.Parse("2026-05-11T13:05:00+00:00"),
            "5");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var firstResult = await store.RemediateCampaignSeriesScoresAsync(
            tenantId,
            series.Id,
            actorUserId,
            CancellationToken.None);
        var secondResult = await store.RemediateCampaignSeriesScoresAsync(
            tenantId,
            series.Id,
            actorUserId,
            CancellationToken.None);

        Assert.True(firstResult.IsSuccess, firstResult.Error.ToString());
        Assert.Equal(2, firstResult.Value.RemediatedSubmittedResponseCount);
        Assert.True(secondResult.IsSuccess, secondResult.Error.ToString());
        Assert.Equal(2, secondResult.Value.SubmittedResponseCount);
        Assert.Equal(2, secondResult.Value.EligibleSubmittedResponseCount);
        Assert.Equal(2, secondResult.Value.AlreadyScoredSubmittedResponseCount);
        Assert.Equal(0, secondResult.Value.RemediatedSubmittedResponseCount);
        Assert.Equal(0, secondResult.Value.SkippedNotConfiguredSubmittedResponseCount);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(2, await verificationDb.ScoreRuns.CountAsync(run => run.CampaignId == campaign.Id));
        Assert.Equal(2, await verificationDb.Scores.CountAsync(score => score.CampaignId == campaign.Id));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Remediate_campaign_series_scores_returns_not_found_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "score-remediation-tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "score-remediation-tenant-b");
        var tenantBSeries = await SeedSeriesAsync(runtimeOptions, tenantB, "Tenant B pulse");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.RemediateCampaignSeriesScoresAsync(
            tenantA,
            tenantBSeries.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Create_tenant_member_normalizes_email_and_creates_user_and_tenant_role_assignment()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "team-create-member");
        await SeedUserAccountAsync(runtimeOptions, tenantId, actorUserId, "actor@example.test");
        var analystRoleId = await SeedTenantRoleAsync(runtimeOptions, tenantId, "analyst", "Analyst");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CreateTenantMemberAsync(
            tenantId,
            actorUserId,
            new CreateTenantMemberRequest("  NEW.Member@Example.TEST  ", "analyst", "hr"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("new.member@example.test", result.Value.Member.Email);
        Assert.Equal("hr", result.Value.Member.Locale);
        Assert.Equal("pending_provider_link", result.Value.Member.IdentityStatus);
        Assert.Equal("analyst", Assert.Single(result.Value.Member.Roles).Code);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var user = await verificationDb.UserAccounts.SingleAsync(user => user.Email == "new.member@example.test");
        Assert.Equal("hr", user.Locale);
        var assignment = await verificationDb.RoleAssignments.SingleAsync(assignment =>
            assignment.UserId == user.Id &&
            assignment.ScopeType == RoleAssignmentScopes.Tenant);
        Assert.Equal(analystRoleId, assignment.RoleId);
        Assert.Equal(actorUserId, assignment.GrantedBy);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Create_tenant_member_is_idempotent_for_same_email_and_role()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "team-create-idempotent");
        await SeedUserAccountAsync(runtimeOptions, tenantId, actorUserId, "actor@example.test");
        await SeedTenantRoleAsync(runtimeOptions, tenantId, "viewer", "Viewer");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var first = await store.CreateTenantMemberAsync(
            tenantId,
            actorUserId,
            new CreateTenantMemberRequest("Viewer@Example.TEST", "viewer"),
            CancellationToken.None);
        var second = await store.CreateTenantMemberAsync(
            tenantId,
            actorUserId,
            new CreateTenantMemberRequest(" viewer@example.test ", "viewer"),
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal(first.Value.Member.UserId, second.Value.Member.UserId);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var user = await verificationDb.UserAccounts.SingleAsync(user => user.Email == "viewer@example.test");
        Assert.Equal(1, await verificationDb.RoleAssignments.CountAsync(assignment =>
            assignment.UserId == user.Id &&
            assignment.ScopeType == RoleAssignmentScopes.Tenant));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Create_tenant_member_returns_validation_for_unknown_role_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "team-create-unknown-role");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CreateTenantMemberAsync(
            tenantId,
            Guid.NewGuid(),
            new CreateTenantMemberRequest("new.member@example.test", "analyst"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("tenant_role.unknown", result.Error.Code);
    }

    [DockerFact]
    public async Task Change_tenant_member_role_replaces_tenant_role_preserves_resource_assignments_and_revokes_sessions()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "team-change-role");
        await SeedUserAccountAsync(runtimeOptions, tenantId, actorUserId, "actor@example.test");
        var oldRoleId = await SeedTenantRoleAsync(runtimeOptions, tenantId, "viewer", "Viewer");
        var newRoleId = await SeedTenantRoleAsync(runtimeOptions, tenantId, "analyst", "Analyst");
        var externalIdentityId = await SeedTenantMemberWithRoleAsync(
            runtimeOptions,
            tenantId,
            targetUserId,
            "target@example.test",
            oldRoleId,
            actorUserId,
            withExternalIdentity: true);
        await SeedResourceRoleAssignmentAsync(runtimeOptions, tenantId, targetUserId, oldRoleId, resourceId);
        var activeSessionId = await SeedAuthSessionAsync(runtimeOptions, tenantId, targetUserId, externalIdentityId);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.ChangeTenantMemberRoleAsync(
            tenantId,
            targetUserId,
            actorUserId,
            new ChangeTenantMemberRoleRequest("analyst"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(targetUserId, result.Value.Member.UserId);
        Assert.Equal("analyst", Assert.Single(result.Value.Member.Roles).Code);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var tenantAssignment = await verificationDb.RoleAssignments.SingleAsync(assignment =>
            assignment.UserId == targetUserId &&
            assignment.ScopeType == RoleAssignmentScopes.Tenant);
        Assert.Equal(newRoleId, tenantAssignment.RoleId);
        Assert.Equal(actorUserId, tenantAssignment.GrantedBy);
        var resourceAssignment = await verificationDb.RoleAssignments.SingleAsync(assignment =>
            assignment.UserId == targetUserId &&
            assignment.ScopeType == RoleAssignmentScopes.CampaignSeries);
        Assert.Equal(oldRoleId, resourceAssignment.RoleId);
        Assert.Equal(resourceId, resourceAssignment.ScopeId);
        var session = await verificationDb.AuthSessions.SingleAsync(session => session.Id == activeSessionId);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("role_changed", session.RevokedReason);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Change_tenant_member_role_rejects_self_role_change()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "team-change-self");
        var roleId = await SeedTenantRoleAsync(runtimeOptions, tenantId, "viewer", "Viewer");
        await SeedTenantMemberWithRoleAsync(
            runtimeOptions,
            tenantId,
            actorUserId,
            "self@example.test",
            roleId,
            actorUserId);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.ChangeTenantMemberRoleAsync(
            tenantId,
            actorUserId,
            actorUserId,
            new ChangeTenantMemberRoleRequest("viewer"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("tenant_member.self_role_change", result.Error.Code);
    }

    [DockerFact]
    public async Task Change_tenant_member_role_fails_closed_for_wrong_tenant_user_and_role()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantAUserId = Guid.NewGuid();
        var tenantBUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "team-change-tenant-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "team-change-tenant-b");
        var tenantAViewerRoleId = await SeedTenantRoleAsync(runtimeOptions, tenantA, "viewer", "Viewer");
        var tenantBAnalystRoleId = await SeedTenantRoleAsync(runtimeOptions, tenantB, "analyst", "Analyst");
        await SeedTenantMemberWithRoleAsync(
            runtimeOptions,
            tenantA,
            tenantAUserId,
            "a@example.test",
            tenantAViewerRoleId,
            tenantAUserId);
        await SeedTenantMemberWithRoleAsync(
            runtimeOptions,
            tenantB,
            tenantBUserId,
            "b@example.test",
            tenantBAnalystRoleId,
            tenantBUserId);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var wrongTenantUser = await store.ChangeTenantMemberRoleAsync(
            tenantA,
            tenantBUserId,
            tenantAUserId,
            new ChangeTenantMemberRoleRequest("viewer"),
            CancellationToken.None);
        var wrongTenantRole = await store.ChangeTenantMemberRoleAsync(
            tenantA,
            tenantAUserId,
            tenantBUserId,
            new ChangeTenantMemberRoleRequest("analyst"),
            CancellationToken.None);

        Assert.True(wrongTenantUser.IsFailure);
        Assert.Equal("tenant_member.not_found", wrongTenantUser.Error.Code);
        Assert.True(wrongTenantRole.IsFailure);
        Assert.Equal("tenant_role.unknown", wrongTenantRole.Error.Code);
    }

    [DockerFact]
    public async Task Create_subject_persists_directory_subject_under_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "directory-create-subject");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CreateSubjectAsync(
            tenantId,
            actorUserId,
            new CreateSubjectRequest(
                "  Ana Analyst  ",
                "  ana@example.test  ",
                "  emp-001  ",
                "hr-HR",
                """{"title":"Analyst"}"""),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("Ana Analyst", result.Value.DisplayName);
        Assert.Equal("ana@example.test", result.Value.Email);
        Assert.Equal("emp-001", result.Value.ExternalId);
        Assert.Equal("hr-hr", result.Value.Locale);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verificationDb.Subjects.SingleAsync(subject => subject.Id == result.Value.Id);
        Assert.Equal(tenantId, persisted.TenantId);
        Assert.Equal("Ana Analyst", persisted.DisplayName);
        using var attributes = JsonDocument.Parse(persisted.Attributes);
        Assert.Equal("Analyst", attributes.RootElement.GetProperty("title").GetString());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Create_subject_returns_validation_for_non_object_attributes()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "directory-invalid-subject");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.CreateSubjectAsync(
            tenantId,
            actorUserId,
            new CreateSubjectRequest("Ana", "ana@example.test", "emp-001", "en", """["not-object"]"""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("subject.attributes_invalid", result.Error.Code);
    }

    [DockerFact]
    public async Task Add_subject_group_member_rejects_cross_tenant_subject()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "directory-member-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "directory-member-b");
        var tenantAGroup = await SeedSubjectGroupAsync(runtimeOptions, tenantA, SubjectGroupTypes.Department, "Research");
        var tenantBSubject = await SeedSubjectAsync(runtimeOptions, tenantB, "Other Person", "other@example.test", "emp-b");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var result = await store.AddSubjectGroupMemberAsync(
            tenantA,
            tenantAGroup.Id,
            actorUserId,
            new AddSubjectGroupMemberRequest(tenantBSubject.Id, SubjectGroupRoles.Member),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("subject.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Set_subject_manager_replaces_and_clears_active_manager()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "directory-manager");
        var employee = await SeedSubjectAsync(runtimeOptions, tenantId, "Ana Analyst", "ana@example.test", "emp-001");
        var oldManager = await SeedSubjectAsync(runtimeOptions, tenantId, "Mira Manager", "mira@example.test", "mgr-001");
        var newManager = await SeedSubjectAsync(runtimeOptions, tenantId, "Iva Lead", "iva@example.test", "mgr-002");
        var oldRelationship = await SeedSubjectRelationshipAsync(
            runtimeOptions,
            tenantId,
            oldManager.Id,
            employee.Id,
            SubjectRelationshipTypes.ManagerOf);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new ProductSurfaceWriteStore(db, new TenantDbScope(db));

        var setResult = await store.SetSubjectManagerAsync(
            tenantId,
            employee.Id,
            actorUserId,
            new SetSubjectManagerRequest(newManager.Id, new DateOnly(2026, 5, 1)),
            CancellationToken.None);

        Assert.True(setResult.IsSuccess, setResult.Error.ToString());
        Assert.Equal(newManager.Id, setResult.Value.ManagerSubjectId);
        Assert.Equal("Iva Lead", setResult.Value.ManagerDisplayName);

        var clearResult = await store.SetSubjectManagerAsync(
            tenantId,
            employee.Id,
            actorUserId,
            new SetSubjectManagerRequest(null, new DateOnly(2026, 5, 2)),
            CancellationToken.None);

        Assert.True(clearResult.IsSuccess, clearResult.Error.ToString());
        Assert.Null(clearResult.Value.ManagerSubjectId);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var relationships = await verificationDb.SubjectRelationships
            .Where(relationship => relationship.RelatedSubjectId == employee.Id)
            .OrderBy(relationship => relationship.SubjectId)
            .ToListAsync();
        Assert.Equal(2, relationships.Count);
        Assert.All(relationships, relationship => Assert.NotNull(relationship.ValidTo));
        Assert.Contains(relationships, relationship => relationship.Id == oldRelationship.Id);
        Assert.Contains(relationships, relationship => relationship.SubjectId == newManager.Id);
        await transaction.CommitAsync();
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

    private DbContextOptions<ApplicationDbContext> CreateRuntimeOptions()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
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
                campaign_series,
                survey_template,
                template_version,
                section,
                scale,
                question,
                campaign,
                campaign_launch_snapshot,
                consent_document,
                retention_policy,
                disclosure_policy,
                scoring_rule,
                invitation_token,
                assignment,
                response_session,
                answer,
                score_run,
                score
            TO {{RuntimeUsername}};
            """);
    }

    private static async Task SeedUserAccountAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId,
        string email)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.UserAccounts.Add(new UserAccount(userId, tenantId, email));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<Guid> SeedTenantRoleAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string code,
        string name)
    {
        var roleId = Guid.NewGuid();

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Roles.Add(new Role(roleId, tenantId, code, name));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return roleId;
    }

    private static async Task<Guid> SeedTenantMemberWithRoleAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId,
        string email,
        Guid roleId,
        Guid grantedBy,
        bool withExternalIdentity = false)
    {
        var externalIdentityId = Guid.NewGuid();

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.UserAccounts.Add(new UserAccount(userId, tenantId, email));
        db.RoleAssignments.Add(new RoleAssignment(
            Guid.NewGuid(),
            tenantId,
            userId,
            roleId,
            RoleAssignmentScopes.Tenant,
            grantedBy: grantedBy));
        if (withExternalIdentity)
        {
            db.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
                externalIdentityId,
                tenantId,
                userId,
                "auth0",
                $"hash-{userId:N}",
                email,
                DateTimeOffset.Parse("2026-05-11T08:00:00+00:00")));
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return externalIdentityId;
    }

    private static async Task SeedResourceRoleAssignmentAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId,
        Guid roleId,
        Guid resourceId)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.RoleAssignments.Add(new RoleAssignment(
            Guid.NewGuid(),
            tenantId,
            userId,
            roleId,
            RoleAssignmentScopes.CampaignSeries,
            resourceId,
            grantedBy: userId));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<Guid> SeedAuthSessionAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId,
        Guid externalIdentityId)
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.AuthSessions.Add(new AuthSession(
            sessionId,
            tenantId,
            userId,
            externalIdentityId,
            createdAt,
            createdAt.AddHours(1)));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return sessionId;
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

    private static async Task<CampaignSeries> SeedSeriesAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string name,
        string studyKind = CampaignSeriesStudyKinds.Own,
        string? sampleScenario = null)
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            name,
            new byte[32],
            studyKind: studyKind,
            sampleScenario: sampleScenario);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.CampaignSeries.Add(series);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return series;
    }

    private static async Task<Guid> SeedTemplateVersionAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug)
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, $"{tenantSlug} pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return version.Id;
    }

    private static async Task<TemplateFixture> SeedScoringTemplateAsync(
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
            minValue: 1,
            maxValue: 5,
            step: 1,
            naAllowed: false,
            anchors: """[{"value":1,"label":"Low"},{"value":5,"label":"High"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "q01",
            QuestionTypes.Likert,
            scale.Id,
            "I can complete my work effectively.",
            required: true,
            measurementLevel: MeasurementLevels.Ordinal);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.Add(question);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new TemplateFixture(version.Id, question.Id);
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
            """
            {
              "operations": [
                { "op": "mean", "items": ["q01"], "output": "total" }
              ]
            }
            """,
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
        DateTimeOffset launchedAt)
    {
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
            launchedAt);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.CampaignLaunchSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return snapshot;
    }

    private static async Task<Campaign> SeedCampaignAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid templateVersionId,
        Guid campaignSeriesId,
        string name,
        string status)
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            tenantId,
            templateVersionId,
            name,
            ResponseIdentityModes.Anonymous,
            campaignSeriesId: campaignSeriesId,
            status: status,
            startAt: status == CampaignStatuses.Live
                ? DateTimeOffset.Parse("2026-05-11T12:00:00+00:00")
                : null);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return campaign;
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
        DateTimeOffset submittedAt,
        string valueJson)
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            $"token-{Guid.NewGuid():N}",
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
            startedAt: submittedAt.AddMinutes(-5));
        session.Submit(submittedAt, timeTakenMs: 1200);
        var answer = new Answer(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            questionId,
            valueJson);

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

    private static async Task SeedScoreAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid campaignId,
        Guid responseSessionId,
        Guid scoringRuleId,
        DateTimeOffset ranAt)
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

    private static async Task<Subject> SeedSubjectAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string displayName,
        string email,
        string externalId)
    {
        var subject = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: externalId,
            email: email,
            displayName: displayName,
            attributes: """{"source":"test"}""");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return subject;
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

    private static async Task<SubjectRelationship> SeedSubjectRelationshipAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid subjectId,
        Guid relatedSubjectId,
        string relationshipType)
    {
        var relationship = new SubjectRelationship(
            Guid.NewGuid(),
            tenantId,
            subjectId,
            relatedSubjectId,
            relationshipType);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.SubjectRelationships.Add(relationship);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return relationship;
    }

    private sealed record TemplateFixture(Guid TemplateVersionId, Guid QuestionId);

    private sealed record PolicyFixture(
        Guid ConsentDocumentId,
        Guid RetentionPolicyId,
        Guid DisclosurePolicyId);
}
