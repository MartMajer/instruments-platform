using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.Responses;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.ProductSurfaces;
using Platform.Infrastructure.Reports;
using Platform.Infrastructure.Responses;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ResponseCaptureStoreTests : IAsyncLifetime
{
    private const string ConsentGrant = "participate";
    private const string SensitiveRespondentExternalId = "sensitive-respondent-external-id";
    private const string SensitiveManagerExternalId = "sensitive-manager-external-id";
    private const string SensitivePeerExternalId = "sensitive-peer-external-id";
    private const string SensitiveOtherRespondentExternalId = "sensitive-other-respondent-external-id";
    private const string ValidDocumentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task GetIdentifiedQueueAsync_returns_only_target_assignments_for_queue_respondent_without_external_ids()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var fixture = await SeedIdentifiedQueueAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        var result = await store.GetIdentifiedQueueAsync(fixture.Token, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.CampaignId, result.Value.CampaignId);
        Assert.Equal(ResponseIdentityModes.Identified, result.Value.ResponseIdentityMode);
        Assert.Equal(fixture.RespondentSubjectId, result.Value.Respondent.Id);
        var assignmentIds = result.Value.Assignments.Select(assignment => assignment.AssignmentId).ToArray();
        Assert.Equal([fixture.ManagerAssignmentId, fixture.PeerAssignmentId], assignmentIds);
        Assert.All(result.Value.Assignments, assignment => Assert.NotNull(assignment.Target));
        Assert.DoesNotContain(fixture.OtherRespondentAssignmentId, assignmentIds);
        Assert.DoesNotContain(fixture.SelfAssignmentId, assignmentIds);

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain(SensitiveRespondentExternalId, serialized);
        Assert.DoesNotContain(SensitiveManagerExternalId, serialized);
        Assert.DoesNotContain(SensitivePeerExternalId, serialized);
        Assert.DoesNotContain(SensitiveOtherRespondentExternalId, serialized);
    }

    [DockerFact]
    public async Task GetIdentifiedQueueAsync_rejects_wrong_tenant_used_and_expired_queue_tokens()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var wrongTenantFixture = await SeedIdentifiedQueueAsync(options, QueueTokenState.WrongTenant);
        var usedFixture = await SeedIdentifiedQueueAsync(options, QueueTokenState.Used);
        var expiredFixture = await SeedIdentifiedQueueAsync(options, QueueTokenState.Expired);
        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        var wrongTenant = await store.GetIdentifiedQueueAsync(wrongTenantFixture.Token, CancellationToken.None);
        var used = await store.GetIdentifiedQueueAsync(usedFixture.Token, CancellationToken.None);
        var expired = await store.GetIdentifiedQueueAsync(expiredFixture.Token, CancellationToken.None);

        Assert.True(wrongTenant.IsFailure);
        Assert.True(used.IsFailure);
        Assert.True(expired.IsFailure);
    }

    [DockerFact]
    public async Task CreateIdentifiedQueueAssignmentSessionAsync_reuses_existing_session_and_blocks_wrong_respondent_assignment()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var fixture = await SeedIdentifiedQueueAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        var first = await store.CreateIdentifiedQueueAssignmentSessionAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            CreateAcceptedConsentRequest(fixture),
            CancellationToken.None);
        var second = await store.CreateIdentifiedQueueAssignmentSessionAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            CreateAcceptedConsentRequest(fixture),
            CancellationToken.None);
        var wrongRespondent = await store.CreateIdentifiedQueueAssignmentSessionAsync(
            fixture.Token,
            fixture.OtherRespondentAssignmentId,
            CreateAcceptedConsentRequest(fixture),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.True(wrongRespondent.IsFailure);
    }

    [DockerFact]
    public async Task Identified_queue_session_endpoints_reject_assignment_session_mismatch()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var fixture = await SeedIdentifiedQueueAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        var managerSession = await store.CreateIdentifiedQueueAssignmentSessionAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            CreateAcceptedConsentRequest(fixture),
            CancellationToken.None);
        var peerSession = await store.CreateIdentifiedQueueAssignmentSessionAsync(
            fixture.Token,
            fixture.PeerAssignmentId,
            CreateAcceptedConsentRequest(fixture),
            CancellationToken.None);

        Assert.True(managerSession.IsSuccess);
        Assert.True(peerSession.IsSuccess);

        var draft = await store.GetIdentifiedQueueSessionDraftAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            peerSession.Value.Id,
            CancellationToken.None);
        var save = await store.SaveIdentifiedQueueAnswersAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            peerSession.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(fixture.QuestionId, "4")
            ]),
            CancellationToken.None);
        var submit = await store.SubmitIdentifiedQueueSessionAsync(
            fixture.Token,
            fixture.ManagerAssignmentId,
            peerSession.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200),
            CancellationToken.None);

        Assert.True(draft.IsFailure);
        Assert.True(save.IsFailure);
        Assert.True(submit.IsFailure);
    }

    [DockerFact]
    public async Task UnsubscribeEmailInvitation_scopes_to_the_study_by_default_and_workspace_wide_when_requested()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);

        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Unsub pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var series = new CampaignSeries(Guid.NewGuid(), tenantId, "Unsubscribe study", CreateCodeSalt());
        var campaign = new Campaign(
            Guid.NewGuid(),
            tenantId,
            version.Id,
            "Unsubscribe campaign",
            ResponseIdentityModes.Anonymous,
            campaignSeriesId: series.Id,
            status: CampaignStatuses.Live,
            schedule: """{"kind":"one_shot"}""");

        var studyIssue = OpenLinkTokens.IssueInvitation(tenantId);
        var workspaceIssue = OpenLinkTokens.IssueInvitation(tenantId);
        var studyToken = new InvitationToken(
            Guid.NewGuid(), tenantId, campaign.Id, studyIssue.TokenHash,
            InvitationTokenChannels.Email, recipient: "scoped@example.test",
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));
        var workspaceToken = new InvitationToken(
            Guid.NewGuid(), tenantId, campaign.Id, workspaceIssue.TokenHash,
            InvitationTokenChannels.Email, recipient: "global@example.test",
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));

        await using (var seed = new ApplicationDbContext(options))
        {
            var scope = new TenantDbScope(seed);
            await using var transaction = await scope.BeginTransactionAsync(tenantId);
            seed.Tenants.Add(new Tenant(tenantId, $"unsub-{tenantId:N}"[..20], "Unsub tenant"));
            seed.SurveyTemplates.Add(template);
            seed.TemplateVersions.Add(version);
            seed.CampaignSeries.Add(series);
            seed.Campaigns.Add(campaign);
            seed.InvitationTokens.AddRange(studyToken, workspaceToken);
            await seed.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        // default: unsubscribe is scoped to the invitation's study
        var scoped = await store.UnsubscribeEmailInvitationAsync(studyIssue.RawToken, workspaceWide: false, CancellationToken.None);
        Assert.True(scoped.IsSuccess);
        Assert.Equal("study", scoped.Value.Scope);

        // explicit workspace-wide: global suppression (series = null)
        var global = await store.UnsubscribeEmailInvitationAsync(workspaceIssue.RawToken, workspaceWide: true, CancellationToken.None);
        Assert.True(global.IsSuccess);
        Assert.Equal("workspace", global.Value.Scope);

        await using var verify = new ApplicationDbContext(options);
        var verifyScope = new TenantDbScope(verify);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        var suppressions = await verify.EmailSuppressions
            .Where(suppression => suppression.TenantId == tenantId)
            .ToListAsync();
        await verifyTransaction.CommitAsync();

        var scopedRow = Assert.Single(suppressions, s => s.Recipient == "scoped@example.test");
        Assert.Equal(series.Id, scopedRow.CampaignSeriesId);
        Assert.Equal(EmailSuppression.RecipientUnsubscribedReason, scopedRow.Reason);

        var globalRow = Assert.Single(suppressions, s => s.Recipient == "global@example.test");
        Assert.Null(globalRow.CampaignSeriesId);
    }

    [DockerFact]
    public async Task Respondent_branding_is_token_scoped_and_never_leaks_across_tenants()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var a = await SeedIdentifiedQueueAsync(options);
        var b = await SeedIdentifiedQueueAsync(options);

        var root = Path.Combine(Path.GetTempPath(), "vs-branding-test", Guid.NewGuid().ToString("N"));
        var objectStore = new LocalExportArtifactObjectStore(
            Options.Create(new ExportArtifactObjectStoreOptions { RootPath = root }));

        byte[] logoA = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 1, 1, 1];
        byte[] logoB = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 2, 2, 2, 2];
        var keyA = $"tenant-branding/{a.TenantId:N}/logo-a.png";
        var keyB = $"tenant-branding/{b.TenantId:N}/logo-b.png";
        await objectStore.StoreAsync(keyA, logoA, CancellationToken.None);
        await objectStore.StoreAsync(keyB, logoB, CancellationToken.None);

        await using (var brandDb = new ApplicationDbContext(options))
        {
            var writeStore = new ProductSurfaceWriteStore(brandDb, new TenantDbScope(brandDb));
            await writeStore.UpdateTenantAppBrandingAsync(
                a.TenantId,
                Guid.NewGuid(),
                new UpdateTenantAppBrandingRequest("#2b5fd9", keyA, "image/png"),
                CancellationToken.None);
            await writeStore.UpdateTenantAppBrandingAsync(
                b.TenantId,
                Guid.NewGuid(),
                new UpdateTenantAppBrandingRequest("#0e7a5f", keyB, "image/png"),
                CancellationToken.None);
        }

        await using var db = new ApplicationDbContext(options);
        var store = new ResponseCaptureStore(db, new TenantDbScope(db), brandingAssetStore: objectStore);

        var brandedA = await store.GetIdentifiedQueueAsync(a.Token, CancellationToken.None);
        var brandedB = await store.GetIdentifiedQueueAsync(b.Token, CancellationToken.None);

        Assert.True(brandedA.IsSuccess);
        Assert.True(brandedB.IsSuccess);

        var dataUriA = $"data:image/png;base64,{Convert.ToBase64String(logoA)}";
        var dataUriB = $"data:image/png;base64,{Convert.ToBase64String(logoB)}";

        // Tenant A's token resolves only tenant A's accent and logo.
        Assert.NotNull(brandedA.Value.Branding);
        Assert.Equal("#2b5fd9", brandedA.Value.Branding!.AccentColorHex);
        Assert.Equal(dataUriA, brandedA.Value.Branding.LogoDataUri);
        Assert.NotEqual("#0e7a5f", brandedA.Value.Branding.AccentColorHex);
        Assert.NotEqual(dataUriB, brandedA.Value.Branding.LogoDataUri);

        // Tenant B's token resolves only tenant B's accent and logo.
        Assert.NotNull(brandedB.Value.Branding);
        Assert.Equal("#0e7a5f", brandedB.Value.Branding!.AccentColorHex);
        Assert.Equal(dataUriB, brandedB.Value.Branding.LogoDataUri);
        Assert.NotEqual("#2b5fd9", brandedB.Value.Branding.AccentColorHex);
        Assert.NotEqual(dataUriA, brandedB.Value.Branding.LogoDataUri);

        // Nothing in tenant A's payload carries tenant B's logo bytes.
        var serializedA = JsonSerializer.Serialize(brandedA.Value);
        Assert.DoesNotContain(Convert.ToBase64String(logoB), serializedA);
    }

    [DockerFact]
    public async Task Respondent_branding_is_null_when_the_tenant_has_set_none()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var fixture = await SeedIdentifiedQueueAsync(options);
        await using var db = new ApplicationDbContext(options);
        var store = CreateStore(db);

        var result = await store.GetIdentifiedQueueAsync(fixture.Token, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Branding);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    private static ResponseCaptureStore CreateStore(ApplicationDbContext db)
    {
        return new ResponseCaptureStore(db, new TenantDbScope(db));
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

    private static CreateOpenLinkSessionRequest CreateAcceptedConsentRequest(IdentifiedQueueFixture fixture)
    {
        return new CreateOpenLinkSessionRequest(
            Locale: "en",
            AcceptedConsentDocumentId: fixture.ConsentDocumentId,
            AcceptedGrants: [ConsentGrant]);
    }

    private static async Task<IdentifiedQueueFixture> SeedIdentifiedQueueAsync(
        DbContextOptions<ApplicationDbContext> options,
        QueueTokenState tokenState = QueueTokenState.Valid)
    {
        var tenantId = Guid.NewGuid();
        var tenantSlug = $"idq-{tenantId.ToString("N")[..8]}";
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Identified queue pulse");
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
            "I feel equipped to give feedback.",
            required: true,
            measurementLevel: MeasurementLevels.Ordinal);
        var scoringRule = CreateScoringRule(version.Id);
        var series = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            "Identified queue study",
            CreateCodeSalt());
        var campaign = new Campaign(
            Guid.NewGuid(),
            tenantId,
            version.Id,
            "Identified queue campaign",
            ResponseIdentityModes.Identified,
            campaignSeriesId: series.Id,
            status: CampaignStatuses.Live,
            schedule: """{"kind":"one_shot"}""");
        var publishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var consentDocument = new ConsentDocument(
            Guid.NewGuid(),
            tenantId,
            series.Id,
            "en",
            "1.0.0",
            "Consent",
            "Consent body",
            """["participate"]""",
            "[]",
            publishedAt);
        var snapshot = new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            series.Id,
            version.Id,
            scoringRule.Id,
            ResponseIdentityModes.Identified,
            "en",
            templateQuestionCount: 1,
            scoringRuleDocumentHash: scoringRule.DocumentHash,
            launchReadiness: "{}",
            launchedAt: DateTimeOffset.UtcNow.AddMinutes(-30),
            consentDocumentId: consentDocument.Id);
        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: SensitiveRespondentExternalId,
            email: "respondent@example.test",
            displayName: "Respondent One");
        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: SensitiveManagerExternalId,
            email: "manager@example.test",
            displayName: "Manager One");
        var peer = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: SensitivePeerExternalId,
            email: "peer@example.test",
            displayName: "Peer One");
        var otherRespondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: SensitiveOtherRespondentExternalId,
            email: "other@example.test",
            displayName: "Other Respondent");
        var managerAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "manager",
            respondent.Id,
            manager.Id);
        var peerAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "peer",
            respondent.Id,
            peer.Id);
        var otherRespondentAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "manager",
            otherRespondent.Id,
            manager.Id);
        var selfAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "self",
            respondent.Id);
        var issued = OpenLinkTokens.IssueIdentifiedQueue(tenantId);
        var invitationTokenTenantId = tokenState == QueueTokenState.WrongTenant
            ? Guid.NewGuid()
            : tenantId;
        var invitationToken = new InvitationToken(
            Guid.NewGuid(),
            invitationTokenTenantId,
            campaign.Id,
            issued.TokenHash,
            InvitationTokenChannels.IdentifiedQueue,
            expiresAt: tokenState == QueueTokenState.Expired
                ? DateTimeOffset.UtcNow.AddMinutes(-1)
                : DateTimeOffset.UtcNow.AddDays(7),
            respondentSubjectId: respondent.Id);
        if (tokenState == QueueTokenState.Used)
        {
            invitationToken.MarkUsed(DateTimeOffset.UtcNow);
        }

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, $"Tenant {tenantSlug}"));
        if (invitationTokenTenantId != tenantId)
        {
            var wrongTenantSlug = $"idq-{invitationTokenTenantId.ToString("N")[..8]}";
            db.Tenants.Add(new Tenant(invitationTokenTenantId, wrongTenantSlug, $"Tenant {wrongTenantSlug}"));
        }

        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.Add(question);
        db.ScoringRules.Add(scoringRule);
        db.CampaignSeries.Add(series);
        db.Campaigns.Add(campaign);
        db.ConsentDocuments.Add(consentDocument);
        db.CampaignLaunchSnapshots.Add(snapshot);
        db.Subjects.AddRange(respondent, manager, peer, otherRespondent);
        db.Assignments.AddRange(managerAssignment, peerAssignment, otherRespondentAssignment, selfAssignment);
        db.InvitationTokens.Add(invitationToken);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new IdentifiedQueueFixture(
            issued.RawToken,
            tenantId,
            campaign.Id,
            consentDocument.Id,
            respondent.Id,
            managerAssignment.Id,
            peerAssignment.Id,
            otherRespondentAssignment.Id,
            selfAssignment.Id,
            question.Id);
    }

    private static ScoringRule CreateScoringRule(Guid templateVersionId)
    {
        return ScoringRule.CreateDraft(
            Guid.NewGuid(),
            templateVersionId,
            "feedback.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidDocumentHash,
            """{"rule_id":"feedback.total","version":"1.0.0"}""",
            """{"scores":["total"]}""");
    }

    private static byte[] CreateCodeSalt()
    {
        return Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
    }

    private enum QueueTokenState
    {
        Valid,
        WrongTenant,
        Used,
        Expired
    }

    private sealed record IdentifiedQueueFixture(
        string Token,
        Guid TenantId,
        Guid CampaignId,
        Guid ConsentDocumentId,
        Guid RespondentSubjectId,
        Guid ManagerAssignmentId,
        Guid PeerAssignmentId,
        Guid OtherRespondentAssignmentId,
        Guid SelfAssignmentId,
        Guid QuestionId);
}
