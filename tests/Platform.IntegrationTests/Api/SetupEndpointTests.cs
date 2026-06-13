using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.Setup;
using Platform.Domain.Campaigns;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class SetupEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Private_import_endpoint_creates_tenant_private_instrument()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/instruments/private-imports",
            tenantId,
            new CreatePrivateInstrumentImportRequest(
                "custom-olbi",
                "1.0.0",
                "Custom OLBI",
                "psychometric",
                "Tenant attested source",
                "attested_by_tenant",
                "tenant_provided"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<InstrumentSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("custom-olbi", payload.Code);
        Assert.Equal("attested_by_tenant", payload.RightsStatus);
    }

    [Fact]
    public async Task Private_import_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/instruments/private-imports",
            tenantId,
            new CreatePrivateInstrumentImportRequest(
                "custom-olbi",
                "1.0.0",
                "Custom OLBI",
                "psychometric",
                "Tenant attested source",
                "attested_by_tenant",
                "tenant_provided"),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Private_import_endpoint_maps_duplicate_code_version_to_conflict()
    {
        var tenantId = Guid.NewGuid();
        var duplicate = Result.Failure<InstrumentSummaryResponse>(
            Error.Conflict(
                "instrument.duplicate_code_version",
                "An instrument with this code and version already exists for this tenant."));
        using var client = CreateClient(new FakeSetupWorkflowStore(createInstrumentResult: duplicate));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/instruments/private-imports",
            tenantId,
            new CreatePrivateInstrumentImportRequest(
                "custom-pulse",
                "1.0.0",
                "Custom Pulse",
                "psychometric",
                "Tenant attested source",
                "attested_by_tenant",
                "tenant_provided"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("instrument.duplicate_code_version", payload.Title);
    }

    [Fact]
    public async Task Instruments_endpoint_lists_visible_instruments()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(HttpMethod.Get, "/instruments", tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<InstrumentSummaryResponse[]>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload);
    }

    [Fact]
    public async Task Instruments_endpoint_allows_tenant_member_without_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/instruments",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Template_version_endpoint_creates_template_graph()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/template-versions",
            tenantId,
            SampleTemplateVersionRequest());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Private burnout pulse", payload.TemplateName);
        Assert.Single(payload.Sections);
        Assert.Single(payload.Scales);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public async Task Template_version_detail_endpoint_returns_template_graph()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(templateVersionId));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/template-versions/{templateVersionId}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal(templateVersionId, payload.TemplateVersionId);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public async Task Template_version_history_endpoint_returns_template_versions()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(templateVersionId));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/template-versions/{templateVersionId}/versions",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionListResponse>();
        Assert.NotNull(payload);
        Assert.Equal(templateVersionId, payload.AnchorTemplateVersionId);
        Assert.Equal(2, payload.Versions.Count);
        Assert.Contains(payload.Versions, version => version.Status == "published");
        Assert.Contains(payload.Versions, version => version.Status == "draft");
    }

    [Fact]
    public async Task Template_version_publish_endpoint_publishes_template_graph()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(templateVersionId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/template-versions/{templateVersionId}/publish",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal(templateVersionId, payload.TemplateVersionId);
        Assert.Equal("published", payload.Status);
    }

    [Fact]
    public async Task Template_version_draft_endpoint_creates_draft_from_published_template_graph()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(templateVersionId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/template-versions/{templateVersionId}/drafts",
            tenantId,
            new CreateTemplateVersionDraftRequest("1.1.0"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionDetailResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(templateVersionId, payload.TemplateVersionId);
        Assert.Equal("1.1.0", payload.Semver);
        Assert.Equal("draft", payload.Status);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public async Task Template_version_draft_content_endpoint_updates_draft_template_graph()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(templateVersionId));
        var template = SampleTemplateVersionRequest();
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/template-versions/{templateVersionId}/draft-content",
            tenantId,
            new UpdateTemplateVersionDraftContentRequest(
                template.Sections,
                template.Scales,
                template.Questions));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TemplateVersionDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal(templateVersionId, payload.TemplateVersionId);
        Assert.Equal("draft", payload.Status);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public async Task Template_version_draft_content_endpoint_maps_draft_scoring_guard_to_conflict()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        var staleGuard = Result.Failure<TemplateVersionDetailResponse>(
            Error.Conflict(
                "template_version.draft_scoring_exists",
                "Draft questionnaire content cannot be changed while draft scoring rules exist."));
        using var client = CreateClient(new FakeSetupWorkflowStore(
            templateVersionId,
            updateDraftContentResult: staleGuard));
        var template = SampleTemplateVersionRequest();
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/template-versions/{templateVersionId}/draft-content",
            tenantId,
            new UpdateTemplateVersionDraftContentRequest(
                template.Sections,
                template.Scales,
                template.Questions));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("template_version.draft_scoring_exists", problem.Title);
    }

    [Fact]
    public async Task Scoring_rule_endpoint_creates_draft_rule()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/scoring-rules",
            tenantId,
            new CreateScoringRuleRequest(
                Guid.NewGuid(),
                "tenant-burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                ValidGraphDocument,
                """{"scores":["total"]}"""));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SetupIdResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Id);
    }

    [Fact]
    public async Task Template_version_draft_scoring_retire_endpoint_binds_request_and_actor()
    {
        var tenantId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/template-versions/{templateVersionId}/draft-scoring/retire",
            tenantId,
            body: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RetireTemplateVersionDraftScoringResponse>();
        Assert.NotNull(payload);
        Assert.Equal(templateVersionId, payload.TemplateVersionId);
        Assert.Equal(1, payload.RetiredScoringRuleCount);
        Assert.Equal(tenantId, store.RetireDraftScoringTenantId);
        Assert.Equal(templateVersionId, store.RetireDraftScoringTemplateVersionId);
    }

    [Fact]
    public async Task Scoring_rule_endpoint_maps_validation_failure_to_bad_request()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/scoring-rules",
            tenantId,
            new CreateScoringRuleRequest(
                Guid.NewGuid(),
                "tenant-burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                ValidGraphDocument,
                """{"scores":["other"]}"""));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("score.rule_produces_mismatch", payload.Title);
        Assert.Equal(0, store.CreateScoringRuleCallCount);
    }

    [Fact]
    public async Task Campaign_series_endpoint_creates_series()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/campaign-series",
            tenantId,
            new CreateCampaignSeriesRequest("Private study"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_series_setup_template_endpoint_binds_request_and_actor()
    {
        var tenantId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/campaign-series/{campaignSeriesId}/setup-template",
            tenantId,
            new SelectCampaignSeriesSetupTemplateRequest(templateVersionId));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SelectCampaignSeriesSetupTemplateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignSeriesId, payload.CampaignSeriesId);
        Assert.Equal(templateVersionId, payload.TemplateVersionId);
        Assert.Equal(tenantId, store.SelectSetupTemplateTenantId);
        Assert.Equal(campaignSeriesId, store.SelectSetupTemplateCampaignSeriesId);
        Assert.Equal(templateVersionId, store.SelectSetupTemplateRequest?.TemplateVersionId);
    }

    [Fact]
    public async Task Campaign_series_two_wave_proof_endpoint_returns_counts()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var proof = new CampaignSeriesTwoWaveProofResponse(
            seriesId,
            "ready",
            ExpectedWaveCount: 2,
            LaunchedWaveCount: 2,
            SubmittedWaveCount: 2,
            LinkedTrajectoryCount: 1,
            CompleteTrajectoryCount: 1,
            Waves:
            [
                new TwoWaveProofWaveResponse(
                    Guid.NewGuid(),
                    "Wave 1",
                    "live",
                    "anonymous_longitudinal",
                    SubmittedResponseCount: 1),
                new TwoWaveProofWaveResponse(
                    Guid.NewGuid(),
                    "Wave 2",
                    "live",
                    "anonymous_longitudinal",
                    SubmittedResponseCount: 1)
            ]);
        var proofStore = new FakeCampaignSeriesProofStore(seriesId, Result.Success(proof));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            campaignSeriesProofStore: proofStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/two-wave-proof",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignSeriesTwoWaveProofResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ready", payload.ProofStatus);
        Assert.Equal(2, payload.LaunchedWaveCount);
        Assert.Equal(1, payload.CompleteTrajectoryCount);
        Assert.Equal(tenantId, proofStore.TenantId);
        Assert.Equal(seriesId, proofStore.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_endpoint_creates_draft_with_identity_mode()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore());
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/campaigns",
            tenantId,
            new CreateCampaignRequest(
                Guid.NewGuid(),
                "Wave 1",
                "anonymous_longitudinal",
                CampaignSeriesId: Guid.NewGuid()));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignDraftResponse>();
        Assert.NotNull(payload);
        Assert.Equal("draft", payload.Status);
        Assert.Equal("anonymous_longitudinal", payload.ResponseIdentityMode);
    }

    [Fact]
    public async Task Launch_readiness_endpoint_returns_structured_issues()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/launch-readiness",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LaunchReadinessResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Contains(
            payload.Issues,
            issue => issue.Code == "template.no_questions");
        Assert.DoesNotContain(
            payload.Issues,
            issue => issue.Code == "identity.participant_codes_not_implemented");
    }

    [Fact]
    public async Task Launch_campaign_endpoint_returns_snapshot_response()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/launch",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LaunchCampaignResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal("live", payload.Status);
        Assert.NotEqual(Guid.Empty, payload.LaunchSnapshotId);
        Assert.NotEqual(Guid.Empty, payload.RetentionPolicyId);
        Assert.NotEqual(Guid.Empty, payload.DisclosurePolicyId);
    }

    [Fact]
    public async Task Campaign_respondent_rules_endpoint_saves_ordered_rules()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore(campaignId: campaignId);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/campaigns/{campaignId}/respondent-rules",
            tenantId,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"all_in_group","role":"group_member","group_id":"{{groupId:D}}"}""")
            ]));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignRespondentRuleListResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal([1, 2], payload.Rules.Select(rule => rule.Ordinal).ToArray());
        Assert.Equal(tenantId, store.UpdateRespondentRulesTenantId);
        Assert.Equal(campaignId, store.UpdateRespondentRulesCampaignId);
        Assert.Equal(2, store.UpdateRespondentRulesRequest?.Rules.Count);
    }

    [Fact]
    public async Task Campaign_respondent_rules_endpoint_requires_setup_manage_permission_for_save()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore(campaignId: campaignId);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/campaigns/{campaignId}/respondent-rules",
            tenantId,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self"}""")
            ]),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(store.UpdateRespondentRulesRequest);
    }

    [Fact]
    public async Task Campaign_respondent_rules_endpoint_lists_saved_rules()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore(campaignId: campaignId);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/respondent-rules",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignRespondentRuleListResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        var rule = Assert.Single(payload.Rules);
        Assert.Equal("self", rule.RuleKind);
        Assert.Equal("self", rule.Role);
        Assert.Equal(1, rule.AssignmentPairCount);
        Assert.Equal(tenantId, store.ListRespondentRulesTenantId);
        Assert.Equal(campaignId, store.ListRespondentRulesCampaignId);
    }

    [Fact]
    public async Task Campaign_assignments_endpoint_returns_safe_roster()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var store = new FakeSetupWorkflowStore(campaignId: campaignId);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/assignments",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignAssignmentListResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        var assignment = Assert.Single(payload.Assignments);
        Assert.Equal("self", assignment.Role);
        Assert.False(assignment.Anonymous);
        Assert.NotNull(assignment.Target);
        Assert.NotNull(assignment.Respondent);
        Assert.Equal(tenantId, store.ListAssignmentsTenantId);
        Assert.Equal(campaignId, store.ListAssignmentsCampaignId);

        var serialized = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Campaign_open_link_endpoint_returns_raw_token_and_respondent_path()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/open-link",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignOpenLinkResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.StartsWith("opn_", payload.Token, StringComparison.Ordinal);
        Assert.Equal($"/r/{payload.Token}", payload.RespondentPath);
    }

    [Fact]
    public async Task Campaign_identified_entry_endpoint_returns_raw_token_subject_and_respondent_path()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/identified-entry",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignIdentifiedEntryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.NotEqual(Guid.Empty, payload.AssignmentId);
        Assert.NotEqual(Guid.Empty, payload.SubjectId);
        Assert.StartsWith("idn_", payload.Token, StringComparison.Ordinal);
        Assert.Equal($"/r/{payload.Token}", payload.RespondentPath);
    }

    [Fact]
    public async Task Campaign_invitation_batch_endpoint_returns_raw_invite_paths()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/invitation-batches",
            tenantId,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com"),
                new InvitationRecipientRequest("bo@example.com")
            ]));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignInvitationBatchResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal(2, payload.CreatedInvitationCount);
        Assert.All(payload.Invitations, invitation =>
        {
            Assert.StartsWith("inv_", invitation.Token, StringComparison.Ordinal);
            Assert.Equal($"/r/{invitation.Token}", invitation.RespondentPath);
            Assert.Equal("queued", invitation.Status);
        });
    }

    [Fact]
    public async Task Campaign_notification_deliveries_endpoint_processes_queued_notifications()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(new ProcessCampaignEmailDeliveriesResponse(
                campaignId,
                RequestedBatchSize: 5,
                ProcessedCount: 1,
                SentCount: 1,
                FailedCount: 0,
                Deliveries:
                [
                    new NotificationDeliveryProofResponse(
                        notificationId,
                        "ada@example.com",
                        "sent",
                        "local-dev",
                        "local-dev-message",
                        "/r/inv_example",
                        Error: null)
                ])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(campaignId: campaignId),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/notification-deliveries/process",
            tenantId,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 5));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProcessCampaignEmailDeliveriesResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal(1, payload.SentCount);
        Assert.Equal("/r/inv_example", Assert.Single(payload.Deliveries).RespondentPath);
        Assert.Equal(tenantId, deliveryStore.TenantId);
        Assert.Equal(campaignId, deliveryStore.CampaignId);
        Assert.Equal(5, deliveryStore.Request!.BatchSize);
    }

    [Fact]
    public async Task Campaign_notification_deliveries_requeue_failed_endpoint_requeues_failed_notifications()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(campaignId)),
            Result.Success(new RequeueFailedCampaignEmailDeliveriesResponse(
                campaignId,
                RequestedBatchSize: 7,
                RequeuedCount: 2)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(campaignId: campaignId),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/notification-deliveries/requeue-failed",
            tenantId,
            new RequeueFailedCampaignEmailDeliveriesRequest(
                BatchSize: 7,
                ConfirmedAnotherEmailAppropriate: true));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RequeueFailedCampaignEmailDeliveriesResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal(7, payload.RequestedBatchSize);
        Assert.Equal(2, payload.RequeuedCount);
        Assert.Equal(tenantId, deliveryStore.RequeueTenantId);
        Assert.Equal(campaignId, deliveryStore.RequeueCampaignId);
        Assert.Equal(7, deliveryStore.RequeueRequest!.BatchSize);
    }

    [Fact]
    public async Task Campaign_notification_deliveries_requeue_failed_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(campaignId)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(campaignId: campaignId),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/notification-deliveries/requeue-failed",
            tenantId,
            new RequeueFailedCampaignEmailDeliveriesRequest(BatchSize: 5),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(deliveryStore.RequeueRequest);
    }

    [Fact]
    public async Task Campaign_notification_deliveries_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(new ProcessCampaignEmailDeliveriesResponse(
                campaignId,
                RequestedBatchSize: 5,
                ProcessedCount: 0,
                SentCount: 0,
                FailedCount: 0,
                Deliveries: [])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(campaignId: campaignId),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/notification-deliveries/process",
            tenantId,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 5),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(deliveryStore.Request);
    }

    [Fact]
    public async Task Campaign_notification_deliveries_endpoint_maps_store_failure_to_bad_request()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Failure<ProcessCampaignEmailDeliveriesResponse>(
                Error.Validation("notification_delivery.invalid", "Delivery request was invalid.")));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(campaignId: campaignId),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/notification-deliveries/process",
            tenantId,
            new ProcessCampaignEmailDeliveriesRequest());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("notification_delivery.invalid", payload.Title);
    }

    [Fact]
    public async Task Provider_delivery_webhook_returns_no_content_without_internal_ids()
    {
        var tenantId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        const string webhookSecret = "test-provider-webhook-secret-32-chars";
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(Guid.NewGuid())),
            providerEventResponse: Result.Success(new RecordProviderDeliveryEventResponse(
                notificationId,
                deliveryAttemptId,
                NotificationDeliveryEventTypes.Delivered,
                "sent",
                SuppressionCreated: false,
                DuplicateEvent: false)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            deliveryStore,
            configuration: new Dictionary<string, string?>
            {
                ["EmailDelivery:ProviderWebhookSecret"] = webhookSecret
            });
        var body = $$"""
            {
              "deliveryAttemptKey": "campaign-email:{{tenantId:N}}:pdk_test_delivery_key",
              "eventType": "delivered",
              "occurredAt": "2026-05-21T20:15:00Z",
              "providerEventId": "evt_test_delivery"
            }
            """;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/webhook");
        request.Headers.Add("X-Platform-Webhook-Timestamp", timestamp);
        request.Headers.Add(
            "X-Platform-Webhook-Signature",
            "sha256=" + ComputeProviderWebhookSignature(webhookSecret, timestamp, body));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, responseBody);
        Assert.Equal(tenantId, deliveryStore.ProviderEventTenantId);
        Assert.NotNull(deliveryStore.ProviderEventRequest);
        Assert.Equal(NotificationDeliveryEventTypes.Delivered, deliveryStore.ProviderEventRequest!.EventType);
        Assert.DoesNotContain(notificationId.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(deliveryAttemptId.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_delivery_events_endpoint_returns_privacy_minimized_rows()
    {
        var tenantId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(Guid.NewGuid())),
            providerEventsResponse: Result.Success(new ListProviderDeliveryEventsResponse(
                RequestedLimit: 12,
                Events:
                [
                    new ProviderDeliveryEventResponse(
                        "smtp",
                        NotificationDeliveryEventTypes.Delivered,
                        DateTimeOffset.Parse("2026-05-21T20:15:00Z"),
                        DateTimeOffset.Parse("2026-05-21T20:15:30Z"),
                        "sent",
                        "sent",
                        HasProviderEventId: true,
                        HasProviderMessageId: true)
                ])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/notification-deliveries/provider-events?limit=12",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<ListProviderDeliveryEventsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(12, payload.RequestedLimit);
        var deliveryEvent = Assert.Single(payload.Events);
        Assert.Equal(NotificationDeliveryEventTypes.Delivered, deliveryEvent.EventType);
        Assert.True(deliveryEvent.HasProviderEventId);
        Assert.True(deliveryEvent.HasProviderMessageId);
        Assert.Equal(tenantId, deliveryStore.ProviderEventsTenantId);
        Assert.Equal(12, deliveryStore.ProviderEventsLimit);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(notificationId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(deliveryAttemptId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notificationId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deliveryAttemptId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_reason", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Campaign_email_delivery_repair_readiness_endpoint_returns_safe_counts()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(campaignId)),
            repairReadinessResponse: Result.Success(new CampaignEmailDeliveryRepairReadinessResponse(
                StalePreparedAttemptCount: 1,
                AmbiguousFailedNotificationCount: 1,
                RetryableFailedNotificationCount: 2,
                SuppressedFailedNotificationCount: 3,
                ProviderEventCount: 4,
                LatestProviderEventAt: DateTimeOffset.Parse("2026-05-21T20:15:30Z"),
                CanRetryFailed: true,
                HasRepairWork: true,
                Issues:
                [
                    new CampaignEmailDeliveryRepairReadinessIssueResponse(
                        "notification_delivery_repair.ambiguous_failures",
                        "warning",
                        "Some failed invitation emails have ambiguous provider handoff state.")
                ])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            deliveryStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/notification-deliveries/repair-readiness",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<CampaignEmailDeliveryRepairReadinessResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.StalePreparedAttemptCount);
        Assert.Equal(2, payload.RetryableFailedNotificationCount);
        Assert.True(payload.CanRetryFailed);
        Assert.Equal(tenantId, deliveryStore.RepairReadinessTenantId);
        Assert.Equal(campaignId, deliveryStore.RepairReadinessCampaignId);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notificationId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deliveryAttemptId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerEventId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerMessageId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_rejects_unsupported_signature_version()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn));
        var body = $$"""
            {
              "Type": "Notification",
              "MessageId": "sns-message-1",
              "TopicArn": "{{topicArn}}",
              "Message": "{}",
              "SignatureVersion": "1",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("aws_ses_webhook.signature_version_unsupported", payload.Title);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_rejects_unsafe_signing_cert_url()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn));
        var body = $$"""
            {
              "Type": "Notification",
              "MessageId": "sns-message-1",
              "TopicArn": "{{topicArn}}",
              "Message": "{}",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "http://localhost/SimpleNotificationService-test.pem"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("aws_ses_webhook.signing_cert_url_invalid", payload.Title);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_records_verified_delivery_event_without_internal_ids()
    {
        var tenantId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        var signatureVerifier = new FakeAwsSnsSignatureVerifier(IsValid: true);
        var deliveryStore = new FakeNotificationDeliveryStore(
            Result.Success(CreateEmptyProcessResponse(Guid.NewGuid())),
            providerEventResponse: Result.Success(new RecordProviderDeliveryEventResponse(
                notificationId,
                deliveryAttemptId,
                NotificationDeliveryEventTypes.Delivered,
                "sent",
                SuppressionCreated: false,
                DuplicateEvent: false)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            deliveryStore,
            configuration: AwsSesWebhookConfiguration(topicArn),
            awsSnsSignatureVerifier: signatureVerifier);
        var body = $$$"""
            {
              "Type": "Notification",
              "MessageId": "sns-message-1",
              "TopicArn": "{{{topicArn}}}",
              "Message": "{\"notificationType\":\"Delivery\",\"mail\":{\"timestamp\":\"2026-05-21T20:14:00Z\",\"messageId\":\"ses-message-1\",\"headers\":[{\"name\":\"X-Platform-Delivery-Key\",\"value\":\"campaign-email:{{{tenantId:N}}}:pdk_test_delivery_key\"}]},\"delivery\":{\"timestamp\":\"2026-05-21T20:15:00Z\",\"recipients\":[\"ada@example.test\"]}}",
              "Timestamp": "2026-05-21T20:15:30Z",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, responseBody);
        Assert.NotNull(signatureVerifier.Request);
        Assert.Equal(topicArn, signatureVerifier.Request!.TopicArn);
        Assert.Equal("Notification", signatureVerifier.Request.Type);
        Assert.Equal(tenantId, deliveryStore.ProviderEventTenantId);
        Assert.NotNull(deliveryStore.ProviderEventRequest);
        Assert.Equal(NotificationDeliveryEventTypes.Delivered, deliveryStore.ProviderEventRequest!.EventType);
        Assert.Equal("sns-message-1", deliveryStore.ProviderEventRequest.ProviderEventId);
        Assert.Equal("ses-message-1", deliveryStore.ProviderEventRequest.ProviderMessageId);
        Assert.Equal(DateTimeOffset.Parse("2026-05-21T20:15:00Z"), deliveryStore.ProviderEventRequest.OccurredAt);
        Assert.DoesNotContain(notificationId.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(deliveryAttemptId.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_rejects_message_without_platform_delivery_key()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        var signatureVerifier = new FakeAwsSnsSignatureVerifier(IsValid: true);
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn),
            awsSnsSignatureVerifier: signatureVerifier);
        var body = $$$"""
            {
              "Type": "Notification",
              "MessageId": "sns-message-1",
              "TopicArn": "{{{topicArn}}}",
              "Message": "{\"notificationType\":\"Delivery\",\"mail\":{\"timestamp\":\"2026-05-21T20:14:00Z\",\"messageId\":\"ses-message-1\",\"headers\":[]},\"delivery\":{\"timestamp\":\"2026-05-21T20:15:00Z\",\"recipients\":[\"ada@example.test\"]}}",
              "Timestamp": "2026-05-21T20:15:30Z",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("aws_ses_webhook.message_invalid", payload.Title);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_rejects_invalid_sns_signature()
    {
        var tenantId = Guid.NewGuid();
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        var signatureVerifier = new FakeAwsSnsSignatureVerifier(IsValid: false);
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn),
            awsSnsSignatureVerifier: signatureVerifier);
        var body = $$$"""
            {
              "Type": "Notification",
              "MessageId": "sns-message-1",
              "TopicArn": "{{{topicArn}}}",
              "Message": "{\"notificationType\":\"Delivery\",\"mail\":{\"timestamp\":\"2026-05-21T20:14:00Z\",\"messageId\":\"ses-message-1\",\"headers\":[{\"name\":\"X-Platform-Delivery-Key\",\"value\":\"campaign-email:{{{tenantId:N}}}:pdk_test_delivery_key\"}]},\"delivery\":{\"timestamp\":\"2026-05-21T20:15:00Z\",\"recipients\":[\"ada@example.test\"]}}",
              "Timestamp": "2026-05-21T20:15:30Z",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("aws_ses_webhook.signature_invalid", payload.Title);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_confirms_valid_subscription_confirmation()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        const string subscribeUrl = "https://sns.eu-central-1.amazonaws.com/?Action=ConfirmSubscription&TopicArn=arn%3Aaws%3Asns%3Aeu-central-1%3A123456789012%3Ases-events&Token=test-token";
        var signatureVerifier = new FakeAwsSnsSignatureVerifier(IsValid: true);
        var subscriptionConfirmer = new FakeAwsSnsSubscriptionConfirmer(IsConfirmed: true);
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn),
            awsSnsSignatureVerifier: signatureVerifier,
            awsSnsSubscriptionConfirmer: subscriptionConfirmer);
        var body = $$"""
            {
              "Type": "SubscriptionConfirmation",
              "MessageId": "sns-message-1",
              "TopicArn": "{{topicArn}}",
              "Message": "Confirm this subscription.",
              "Timestamp": "2026-05-21T20:15:30Z",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem",
              "SubscribeURL": "{{subscribeUrl}}",
              "Token": "test-token"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, responseBody);
        Assert.Null(signatureVerifier.Request);
        Assert.Equal(subscribeUrl, subscriptionConfirmer.SubscribeUrl);
    }

    [Fact]
    public async Task Aws_ses_sns_webhook_rejects_unsafe_subscription_confirmation_url()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            configuration: AwsSesWebhookConfiguration(topicArn));
        var body = $$"""
            {
              "Type": "SubscriptionConfirmation",
              "MessageId": "sns-message-1",
              "TopicArn": "{{topicArn}}",
              "Message": "Confirm this subscription.",
              "Timestamp": "2026-05-21T20:15:30Z",
              "SignatureVersion": "2",
              "Signature": "test-signature",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-test.pem",
              "SubscribeURL": "http://localhost/?Action=ConfirmSubscription&Token=test-token",
              "Token": "test-token"
            }
            """;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/notification-deliveries/provider-events/aws-ses-sns");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("aws_ses_webhook.subscription_confirmation_invalid", payload.Title);
    }

    [Fact]
    public async Task Operational_notifications_endpoint_returns_safe_pointer_rows()
    {
        var tenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(
                RequestedLimit: 10,
                Notifications:
                [
                    new OperationalNotificationResponse(
                        Guid.NewGuid(),
                        "report_pdf_artifact_terminal",
                        "warning",
                        "unread",
                        exportArtifactId,
                        "ReportPdfArtifactTerminalStateReached",
                        DateTimeOffset.Parse("2026-05-18T20:30:00+00:00"),
                        CampaignSeriesId: campaignSeriesId,
                        ArtifactStatus: "failed",
                        SourceStatus: "failed",
                        FailureReasonCode: "export_artifact.object_store_unavailable")
                ])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/operational-notifications?limit=10",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<ListOperationalNotificationsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(10, payload.RequestedLimit);
        var notification = Assert.Single(payload.Notifications);
        Assert.Equal(exportArtifactId, notification.SourceAggregateId);
        Assert.Equal(campaignSeriesId, notification.CampaignSeriesId);
        Assert.Equal("failed", notification.ArtifactStatus);
        Assert.Equal("failed", notification.SourceStatus);
        Assert.Contains("\"sourceStatus\":\"failed\"", body, StringComparison.Ordinal);
        Assert.Equal("export_artifact.object_store_unavailable", notification.FailureReasonCode);
        Assert.Equal(tenantId, operationalNotificationStore.TenantId);
        Assert.Equal(10, operationalNotificationStore.Limit);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Amz", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operational_notifications_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/operational-notifications",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, operationalNotificationStore.Limit);
    }

    [Fact]
    public async Task Operational_notifications_summary_endpoint_returns_unread_counts_without_internals()
    {
        var tenantId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])),
            summaryResponse: Result.Success(new OperationalNotificationSummaryResponse(
                UnreadCount: 2,
                InfoUnreadCount: 1,
                WarningUnreadCount: 1,
                LatestUnreadAt: DateTimeOffset.Parse("2026-05-18T21:15:00+00:00"))));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/operational-notifications/summary",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<OperationalNotificationSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.UnreadCount);
        Assert.Equal(1, payload.InfoUnreadCount);
        Assert.Equal(1, payload.WarningUnreadCount);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T21:15:00+00:00"), payload.LatestUnreadAt);
        Assert.Equal(tenantId, operationalNotificationStore.SummaryTenantId);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Amz", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operational_notifications_summary_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])),
            summaryResponse: Result.Success(new OperationalNotificationSummaryResponse(
                0,
                0,
                0,
                LatestUnreadAt: null)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/operational-notifications/summary",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(Guid.Empty, operationalNotificationStore.SummaryTenantId);
    }

    [Fact]
    public async Task Operational_notification_mark_read_endpoint_returns_read_notification()
    {
        var tenantId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var readAt = DateTimeOffset.Parse("2026-05-18T21:05:00+00:00");
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])),
            markReadResponse: Result.Success(new OperationalNotificationResponse(
                notificationId,
                "report_pdf_artifact_terminal",
                "info",
                "read",
                Guid.NewGuid(),
                "ReportPdfArtifactTerminalStateReached",
                DateTimeOffset.Parse("2026-05-18T20:30:00+00:00"),
                ReadAt: readAt,
                UpdatedAt: readAt)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/operational-notifications/{notificationId}/mark-read",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<OperationalNotificationResponse>();
        Assert.NotNull(payload);
        Assert.Equal(notificationId, payload.Id);
        Assert.Equal("read", payload.Status);
        Assert.Equal(readAt, payload.ReadAt);
        Assert.Equal(readAt, payload.UpdatedAt);
        Assert.Equal(tenantId, operationalNotificationStore.MarkReadTenantId);
        Assert.Equal(notificationId, operationalNotificationStore.MarkReadNotificationId);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Amz", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operational_notification_mark_read_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/operational-notifications/{notificationId}/mark-read",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(Guid.Empty, operationalNotificationStore.MarkReadNotificationId);
    }

    [Fact]
    public async Task Operational_notification_mark_all_read_endpoint_returns_safe_count()
    {
        var tenantId = Guid.NewGuid();
        var readAt = DateTimeOffset.Parse("2026-05-18T21:35:00+00:00");
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])),
            markAllReadResponse: Result.Success(new MarkAllOperationalNotificationsReadResponse(
                MarkedReadCount: 3,
                ReadAt: readAt)));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/operational-notifications/mark-all-read",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<MarkAllOperationalNotificationsReadResponse>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.MarkedReadCount);
        Assert.Equal(readAt, payload.ReadAt);
        Assert.Equal(tenantId, operationalNotificationStore.MarkAllReadTenantId);
        Assert.DoesNotContain(tenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Amz", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sourceAggregateId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operational_notification_mark_all_read_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var operationalNotificationStore = new FakeOperationalNotificationStore(
            Result.Success(new ListOperationalNotificationsResponse(25, [])));
        using var client = CreateClient(
            new FakeSetupWorkflowStore(),
            operationalNotificationStore: operationalNotificationStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/operational-notifications/mark-all-read",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(Guid.Empty, operationalNotificationStore.MarkAllReadTenantId);
    }

    [Fact]
    public async Task Campaign_invitation_batch_endpoint_maps_validation_failure_to_bad_request()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeSetupWorkflowStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/invitation-batches",
            tenantId,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("not-an-email")
            ]));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("validation.failed", payload.Title);
    }

    [Fact]
    public async Task Campaign_invitation_batch_endpoint_maps_identity_mode_block_to_bad_request()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var blocked = Result.Failure<CampaignInvitationBatchResponse>(
            Error.Validation(
                "invitation_batch.identity_mode_not_supported",
                "Email invitation batches support anonymous campaigns only."));
        using var client = CreateClient(new FakeSetupWorkflowStore(
            campaignId: campaignId,
            invitationBatchResult: blocked));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/invitation-batches",
            tenantId,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("invitation_batch.identity_mode_not_supported", payload.Title);
    }

    private static ProcessCampaignEmailDeliveriesResponse CreateEmptyProcessResponse(Guid campaignId)
    {
        return new ProcessCampaignEmailDeliveriesResponse(
            campaignId,
            RequestedBatchSize: 25,
            ProcessedCount: 0,
            SentCount: 0,
            FailedCount: 0,
            Deliveries: []);
    }


    private HttpClient CreateClient(
        ISetupWorkflowStore store,
        INotificationDeliveryStore? notificationDeliveryStore = null,
        ICampaignSeriesProofStore? campaignSeriesProofStore = null,
        IOperationalNotificationStore? operationalNotificationStore = null,
        IAwsSnsSignatureVerifier? awsSnsSignatureVerifier = null,
        IAwsSnsSubscriptionConfirmer? awsSnsSubscriptionConfirmer = null,
        IReadOnlyDictionary<string, string?>? configuration = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            if (configuration is not null)
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(configuration);
                });
            }

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                services.AddSingleton(store);
                if (notificationDeliveryStore is not null)
                {
                    services.AddSingleton(notificationDeliveryStore);
                }

                if (campaignSeriesProofStore is not null)
                {
                    services.AddSingleton(campaignSeriesProofStore);
                }

                if (operationalNotificationStore is not null)
                {
                    services.AddSingleton(operationalNotificationStore);
                }

                if (awsSnsSignatureVerifier is not null)
                {
                    services.AddSingleton(awsSnsSignatureVerifier);
                }

                if (awsSnsSubscriptionConfirmer is not null)
                {
                    services.AddSingleton(awsSnsSubscriptionConfirmer);
                }
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        object? body = null,
        string? permissions = "setup.manage")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        if (permissions is not null)
        {
            request.Headers.Add(TestAuthHandler.PermissionsHeader, permissions);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static IReadOnlyDictionary<string, string?> AwsSesWebhookConfiguration(string topicArn)
    {
        return new Dictionary<string, string?>
        {
            ["EmailDelivery:Provider"] = EmailDeliveryProviderNames.Smtp,
            ["EmailDelivery:ManagedProviderName"] = "aws-ses",
            ["EmailDelivery:AwsSes:SnsTopicArn"] = topicArn
        };
    }

    private static string ComputeProviderWebhookSignature(
        string webhookSecret,
        string timestamp,
        string body)
    {
        var signedPayload = $"{timestamp}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    private sealed class FakeAwsSnsSignatureVerifier(bool IsValid) : IAwsSnsSignatureVerifier
    {
        public AwsSnsSignatureVerificationRequest? Request { get; private set; }

        public Task<bool> VerifyAsync(
            AwsSnsSignatureVerificationRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(IsValid);
        }
    }

    private sealed class FakeAwsSnsSubscriptionConfirmer(bool IsConfirmed) : IAwsSnsSubscriptionConfirmer
    {
        public string? SubscribeUrl { get; private set; }

        public Task<bool> ConfirmAsync(
            string subscribeUrl,
            CancellationToken cancellationToken)
        {
            SubscribeUrl = subscribeUrl;
            return Task.FromResult(IsConfirmed);
        }
    }

    private static CreateTemplateVersionRequest SampleTemplateVersionRequest()
    {
        return new CreateTemplateVersionRequest(
            "Private burnout pulse",
            "1.0.0",
            "en",
            InstrumentId: null,
            Sections:
            [
                new CreateTemplateSectionRequest(1, "core", "Core")
            ],
            Scales:
            [
                new CreateQuestionScaleRequest(
                    "agreement",
                    "likert",
                    1,
                    5,
                    1,
                    NaAllowed: false,
                    """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""")
            ],
            Questions:
            [
                new CreateTemplateQuestionRequest(
                    1,
                    "q01",
                    "likert",
                    "I feel depleted after work.",
                    SectionCode: "core",
                    ScaleCode: "agreement",
                    MeasurementLevel: "ordinal")
            ]);
    }

    private const string ValidGraphDocument = """
        {
          "schema_version": "1.0.0",
          "engine_min_version": "1.0.0",
          "rule_id": "tenant-burnout.total",
          "rule_version": "1.0.0",
          "scale_defaults": {
            "agreement": { "min": 1, "max": 5 }
          },
          "inputs": [
            { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
          ],
          "nodes": [
            { "id": "core_answers", "op": "select_answers", "input": "core_items" },
            {
              "id": "scored_answers",
              "op": "reverse_code",
              "input": "core_answers",
              "scale": "agreement",
              "reverse_flag_source": "explicit_list",
              "explicit_reverse_items": ["q03"]
            },
            { "id": "total", "op": "mean", "input": "scored_answers" }
          ],
          "outputs": [
            { "code": "total", "node": "total" }
          ],
          "missing_data": {
            "defaults": { "strategy": "require_all" }
          }
        }
        """;

    private sealed class FakeSetupWorkflowStore : ISetupWorkflowStore
    {
        private readonly Guid _instrumentId = Guid.NewGuid();
        private readonly Guid _templateVersionId;
        private readonly Guid _campaignId;
        private readonly Result<InstrumentSummaryResponse>? _createInstrumentResult;
        private readonly Result<CampaignOpenLinkResponse>? _openLinkResult;
        private readonly Result<CampaignIdentifiedEntryResponse>? _identifiedEntryResult;
        private readonly Result<CampaignIdentifiedQueueAccessResponse>? _identifiedQueueAccessResult;
        private readonly Result<CampaignInvitationBatchResponse>? _invitationBatchResult;
        private readonly Result<TemplateVersionDetailResponse>? _updateDraftContentResult;

        public FakeSetupWorkflowStore(
            Guid? templateVersionId = null,
            Guid? campaignId = null,
            Result<InstrumentSummaryResponse>? createInstrumentResult = null,
            Result<CampaignOpenLinkResponse>? openLinkResult = null,
            Result<CampaignIdentifiedEntryResponse>? identifiedEntryResult = null,
            Result<CampaignIdentifiedQueueAccessResponse>? identifiedQueueAccessResult = null,
            Result<CampaignInvitationBatchResponse>? invitationBatchResult = null,
            Result<TemplateVersionDetailResponse>? updateDraftContentResult = null)
        {
            _templateVersionId = templateVersionId ?? Guid.NewGuid();
            _campaignId = campaignId ?? Guid.NewGuid();
            _createInstrumentResult = createInstrumentResult;
            _openLinkResult = openLinkResult;
            _identifiedEntryResult = identifiedEntryResult;
            _identifiedQueueAccessResult = identifiedQueueAccessResult;
            _invitationBatchResult = invitationBatchResult;
            _updateDraftContentResult = updateDraftContentResult;
        }

        public Task<Result<InstrumentSummaryResponse>> CreatePrivateInstrumentImportAsync(
            Guid tenantId,
            CreatePrivateInstrumentImportRequest request,
            CancellationToken cancellationToken)
        {
            if (_createInstrumentResult.HasValue)
            {
                return Task.FromResult(_createInstrumentResult.Value);
            }

            return Task.FromResult(Result.Success(new InstrumentSummaryResponse(
                _instrumentId,
                request.Code,
                request.Version,
                request.FullName,
                request.RightsStatus,
                request.ValidityLabel,
                CanStartNewCampaign: true)));
        }

        public Task<IReadOnlyList<InstrumentSummaryResponse>> ListInstrumentsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<InstrumentSummaryResponse> instruments =
            [
                new(
                    _instrumentId,
                    "custom-olbi",
                    "1.0.0",
                    "Custom OLBI",
                    "attested_by_tenant",
                    "tenant_provided",
                    true)
            ];

            return Task.FromResult(instruments);
        }

        public Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionAsync(
            Guid tenantId,
            Guid? actorId,
            CreateTemplateVersionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(CreateTemplateVersionResponse(
                request.TemplateName,
                _templateVersionId)));
        }

        public Task<Result<TemplateVersionDetailResponse>> GetTemplateVersionAsync(
            Guid tenantId,
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(CreateTemplateVersionResponse(
                "Private burnout pulse",
                templateVersionId)));
        }

        public Task<Result<TemplateVersionListResponse>> ListTemplateVersionsAsync(
            Guid tenantId,
            Guid anchorTemplateVersionId,
            CancellationToken cancellationToken)
        {
            var templateId = Guid.NewGuid();

            return Task.FromResult(Result.Success(new TemplateVersionListResponse(
                templateId,
                anchorTemplateVersionId,
                [
                    new TemplateVersionSummaryResponse(
                        anchorTemplateVersionId,
                        "1.0.0",
                        "published",
                        IsLocked: true,
                        IsGlobal: false,
                        DateTimeOffset.UtcNow.AddMinutes(-5),
                        DateTimeOffset.UtcNow.AddMinutes(-4),
                        PublishedBy: null),
                    new TemplateVersionSummaryResponse(
                        Guid.NewGuid(),
                        "1.1.0",
                        "draft",
                        IsLocked: false,
                        IsGlobal: false,
                        DateTimeOffset.UtcNow,
                        PublishedAt: null,
                        PublishedBy: null)
                ])));
        }

        public Task<Result<TemplateVersionDetailResponse>> PublishTemplateVersionAsync(
            Guid tenantId,
            Guid? actorId,
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(CreateTemplateVersionResponse(
                "Private burnout pulse",
                templateVersionId,
                "published")));
        }

        public Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionDraftAsync(
            Guid tenantId,
            Guid? actorId,
            Guid sourceTemplateVersionId,
            CreateTemplateVersionDraftRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(CreateTemplateVersionResponse(
                "Private burnout pulse",
                Guid.NewGuid(),
                "draft",
                request.Semver)));
        }

        public Task<Result<TemplateVersionDetailResponse>> UpdateTemplateVersionDraftContentAsync(
            Guid tenantId,
            Guid? actorId,
            Guid templateVersionId,
            UpdateTemplateVersionDraftContentRequest request,
            CancellationToken cancellationToken)
        {
            if (_updateDraftContentResult.HasValue)
            {
                return Task.FromResult(_updateDraftContentResult.Value);
            }

            return Task.FromResult(Result.Success(CreateTemplateVersionResponse(
                "Private burnout pulse",
                templateVersionId,
                "draft")));
        }

        public Task<Result<SetupIdResponse>> CreateScoringRuleAsync(
            Guid tenantId,
            CreateScoringRuleRequest request,
            CancellationToken cancellationToken)
        {
            CreateScoringRuleCallCount++;

            return Task.FromResult(Result.Success(new SetupIdResponse(Guid.NewGuid())));
        }

        public int CreateScoringRuleCallCount { get; private set; }

        public Guid? RetireDraftScoringTenantId { get; private set; }

        public Guid? RetireDraftScoringTemplateVersionId { get; private set; }

        public Task<Result<RetireTemplateVersionDraftScoringResponse>> RetireTemplateVersionDraftScoringAsync(
            Guid tenantId,
            Guid? actorId,
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            RetireDraftScoringTenantId = tenantId;
            RetireDraftScoringTemplateVersionId = templateVersionId;

            return Task.FromResult(Result.Success(new RetireTemplateVersionDraftScoringResponse(
                templateVersionId,
                1)));
        }

        public Guid? ListRespondentRulesTenantId { get; private set; }

        public Guid? ListRespondentRulesCampaignId { get; private set; }

        public Guid? UpdateRespondentRulesTenantId { get; private set; }

        public Guid? UpdateRespondentRulesCampaignId { get; private set; }

        public UpdateCampaignRespondentRulesRequest? UpdateRespondentRulesRequest { get; private set; }

        public Guid? ListAssignmentsTenantId { get; private set; }

        public Guid? ListAssignmentsCampaignId { get; private set; }

        public Task<Result<SetupIdResponse>> CreateCampaignSeriesAsync(
            Guid tenantId,
            CreateCampaignSeriesRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new SetupIdResponse(Guid.NewGuid())));
        }

        public Guid? SelectSetupTemplateTenantId { get; private set; }

        public Guid? SelectSetupTemplateCampaignSeriesId { get; private set; }

        public SelectCampaignSeriesSetupTemplateRequest? SelectSetupTemplateRequest { get; private set; }

        public Task<Result<SelectCampaignSeriesSetupTemplateResponse>> SelectCampaignSeriesSetupTemplateAsync(
            Guid tenantId,
            Guid? actorId,
            Guid campaignSeriesId,
            SelectCampaignSeriesSetupTemplateRequest request,
            CancellationToken cancellationToken)
        {
            SelectSetupTemplateTenantId = tenantId;
            SelectSetupTemplateCampaignSeriesId = campaignSeriesId;
            SelectSetupTemplateRequest = request;

            return Task.FromResult(Result.Success(new SelectCampaignSeriesSetupTemplateResponse(
                campaignSeriesId,
                request.TemplateVersionId)));
        }

        public Task<Result<CampaignDraftResponse>> CreateCampaignAsync(
            Guid tenantId,
            Guid? actorId,
            CreateCampaignRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new CampaignDraftResponse(
                Guid.NewGuid(),
                request.CampaignSeriesId,
                request.TemplateVersionId,
                request.Name,
                "draft",
                request.ResponseIdentityMode)));
        }

        public Task<Result<LaunchReadinessResponse>> GetLaunchReadinessAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new LaunchReadinessResponse(
                campaignId,
                Ready: false,
                Issues:
                [
                    new LaunchReadinessIssueResponse(
                        "template.no_questions",
                        "blocker",
                        "Template version must contain at least one question before launch.")
                ])));
        }

        public Task<Result<CampaignRespondentRuleListResponse>> ListCampaignRespondentRulesAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            ListRespondentRulesTenantId = tenantId;
            ListRespondentRulesCampaignId = campaignId;

            return Task.FromResult(Result.Success(new CampaignRespondentRuleListResponse(
                campaignId,
                [
                    new CampaignRespondentRuleResponse(
                        Guid.NewGuid(),
                        1,
                        """{"kind":"self","role":"self"}""",
                        "self",
                        "self",
                        TargetSubjectId: null,
                        GroupId: null,
                        AssignmentPairCount: 1,
                        Issues: [])
                ])));
        }

        public Task<Result<CampaignRespondentRuleListResponse>> UpdateCampaignRespondentRulesAsync(
            Guid tenantId,
            Guid campaignId,
            UpdateCampaignRespondentRulesRequest request,
            CancellationToken cancellationToken)
        {
            UpdateRespondentRulesTenantId = tenantId;
            UpdateRespondentRulesCampaignId = campaignId;
            UpdateRespondentRulesRequest = request;

            return Task.FromResult(Result.Success(new CampaignRespondentRuleListResponse(
                campaignId,
                request.Rules
                    .Select((rule, index) => new CampaignRespondentRuleResponse(
                        Guid.NewGuid(),
                        index + 1,
                        rule.Rule,
                        index == 0 ? "self" : "all_in_group",
                        index == 0 ? "self" : "group_member",
                        TargetSubjectId: null,
                        GroupId: null,
                        AssignmentPairCount: 1,
                        Issues: []))
                    .ToArray())));
        }

        public Task<Result<CampaignAssignmentListResponse>> ListCampaignAssignmentsAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            ListAssignmentsTenantId = tenantId;
            ListAssignmentsCampaignId = campaignId;

            var targetSubjectId = Guid.NewGuid();
            var respondentSubjectId = Guid.NewGuid();

            return Task.FromResult(Result.Success(new CampaignAssignmentListResponse(
                campaignId,
                AssignmentCount: 1,
                Assignments:
                [
                    new CampaignAssignmentResponse(
                        Guid.NewGuid(),
                        "self",
                        "pending",
                        Anonymous: false,
                        targetSubjectId,
                        new CampaignAssignmentSubjectResponse(
                            targetSubjectId,
                            "Ana Example",
                            "Ana Example",
                            "ana@example.invalid",
                            "ANA-001"),
                        respondentSubjectId,
                        new CampaignAssignmentSubjectResponse(
                            respondentSubjectId,
                            "Ana Example",
                            "Ana Example",
                            "ana@example.invalid",
                            "ANA-001"),
                        DueAt: null,
                        CreatedAt: DateTimeOffset.Parse("2026-05-15T12:00:00+00:00"))
                ])));
        }

        public Task<Result<LaunchCampaignResponse>> LaunchCampaignAsync(
            Guid tenantId,
            Guid? actorId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new LaunchCampaignResponse(
                campaignId,
                Status: "live",
                LaunchSnapshotId: Guid.NewGuid(),
                TemplateVersionId: _templateVersionId,
                ScoringRuleId: Guid.NewGuid(),
                RetentionPolicyId: Guid.NewGuid(),
                DisclosurePolicyId: Guid.NewGuid(),
                ResponseIdentityMode: "anonymous",
                DefaultLocale: "en",
                LaunchedAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"))));
        }

        public Task<Result<CampaignOpenLinkResponse>> CreateCampaignOpenLinkAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            if (_openLinkResult.HasValue)
            {
                return Task.FromResult(_openLinkResult.Value);
            }

            var token = $"opn_{tenantId:N}_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";

            return Task.FromResult(Result.Success(new CampaignOpenLinkResponse(
                campaignId,
                Guid.NewGuid(),
                token,
                $"/r/{token}")));
        }

        public Task<Result<CampaignOpenLinkResponse>> ReplaceCampaignOpenLinkAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            var token = $"opn_{tenantId:N}_replacementabcdefghijklmnopqrstuvwxyzABCDEFGHI";

            return Task.FromResult(Result.Success(new CampaignOpenLinkResponse(
                campaignId,
                Guid.NewGuid(),
                token,
                $"/r/{token}")));
        }

        public Task<Result<CampaignIdentifiedEntryResponse>> CreateCampaignIdentifiedEntryAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            if (_identifiedEntryResult.HasValue)
            {
                return Task.FromResult(_identifiedEntryResult.Value);
            }

            var token = $"idn_{tenantId:N}_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";

            return Task.FromResult(Result.Success(new CampaignIdentifiedEntryResponse(
                campaignId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                token,
                $"/r/{token}")));
        }

        public Task<Result<CampaignIdentifiedQueueAccessResponse>> CreateCampaignIdentifiedQueueAccessAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            if (_identifiedQueueAccessResult.HasValue)
            {
                return Task.FromResult(_identifiedQueueAccessResult.Value);
            }

            var token = $"idq_{tenantId:N}_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";

            return Task.FromResult(Result.Success(new CampaignIdentifiedQueueAccessResponse(
                campaignId,
                RespondentCount: 1,
                AssignmentCount: 2,
                CreatedAccessCount: 1,
                ExistingAccessCount: 0,
                [
                    new CampaignIdentifiedQueueAccessLinkResponse(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        AssignmentCount: 2,
                        token,
                        $"/r/{token}",
                        "created")
                ])));
        }

        public Task<Result<CampaignInvitationBatchResponse>> CreateCampaignInvitationBatchAsync(
            Guid tenantId,
            Guid campaignId,
            CreateCampaignInvitationBatchRequest request,
            CancellationToken cancellationToken)
        {
            if (_invitationBatchResult.HasValue)
            {
                return Task.FromResult(_invitationBatchResult.Value);
            }

            var invitations = request.Recipients
                .Select((recipient, index) =>
                {
                    var token = $"inv_{tenantId:N}_abcdefghijklmnopqrstuvwxyz{index:D2}ABCDEFGHIJKLMNO";

                    return new CampaignInvitationResponse(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        recipient.Email.Trim().ToLowerInvariant(),
                        token,
                        $"/r/{token}",
                        "queued");
                })
                .ToArray();

            return Task.FromResult(Result.Success(new CampaignInvitationBatchResponse(
                campaignId,
                request.Recipients.Count,
                invitations.Length,
                invitations)));
        }

        private static TemplateVersionDetailResponse CreateTemplateVersionResponse(
            string templateName,
            Guid templateVersionId,
            string status = "draft",
            string semver = "1.0.0")
        {
            var sectionId = Guid.NewGuid();
            var scaleId = Guid.NewGuid();

            return new TemplateVersionDetailResponse(
                TemplateId: Guid.NewGuid(),
                TemplateVersionId: templateVersionId,
                TemplateName: templateName,
                Semver: semver,
                Status: status,
                DefaultLocale: "en",
                InstrumentId: null,
                Sections:
                [
                    new TemplateSectionResponse(sectionId, 1, "core", "Core")
                ],
                Scales:
                [
                    new QuestionScaleResponse(
                        scaleId,
                        "agreement",
                        "likert",
                        1,
                        5,
                        1,
                        NaAllowed: false,
                        """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""")
                ],
                Questions:
                [
                    new TemplateQuestionResponse(
                        Id: Guid.NewGuid(),
                        SectionId: sectionId,
                        Ordinal: 1,
                        Code: "q01",
                        Type: "likert",
                        ScaleId: scaleId,
                        TextDefault: "I feel depleted after work.",
                        DescriptionDefault: null,
                        Required: true,
                        ReverseCoded: false,
                        MeasurementLevel: "ordinal",
                        Weight: 1m,
                        VariableLabel: "q01",
                        Payload: "{}",
                        MissingCodes: "[]")
                ]);
        }
    }

    private sealed class FakeNotificationDeliveryStore(
        Result<ProcessCampaignEmailDeliveriesResponse> response,
        Result<RequeueFailedCampaignEmailDeliveriesResponse>? requeueResponse = null,
        Result<RecordProviderDeliveryEventResponse>? providerEventResponse = null,
        Result<ListProviderDeliveryEventsResponse>? providerEventsResponse = null,
        Result<CampaignEmailDeliveryRepairReadinessResponse>? repairReadinessResponse = null) : INotificationDeliveryStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignId { get; private set; }

        public ProcessCampaignEmailDeliveriesRequest? Request { get; private set; }

        public Guid RequeueTenantId { get; private set; }

        public Guid RequeueCampaignId { get; private set; }

        public RequeueFailedCampaignEmailDeliveriesRequest? RequeueRequest { get; private set; }

        public Guid ProviderEventTenantId { get; private set; }

        public RecordProviderDeliveryEventRequest? ProviderEventRequest { get; private set; }

        public Guid ProviderEventsTenantId { get; private set; }

        public int ProviderEventsLimit { get; private set; }

        public Guid RepairReadinessTenantId { get; private set; }

        public Guid RepairReadinessCampaignId { get; private set; }

        public Task<Result<ProcessCampaignEmailDeliveriesResponse>> ProcessCampaignEmailDeliveriesAsync(
            Guid tenantId,
            Guid campaignId,
            ProcessCampaignEmailDeliveriesRequest request,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignId = campaignId;
            Request = request;

            return Task.FromResult(response);
        }

        public Task<Result<RequeueFailedCampaignEmailDeliveriesResponse>> RequeueFailedCampaignEmailDeliveriesAsync(
            Guid tenantId,
            Guid campaignId,
            RequeueFailedCampaignEmailDeliveriesRequest request,
            CancellationToken cancellationToken)
        {
            RequeueTenantId = tenantId;
            RequeueCampaignId = campaignId;
            RequeueRequest = request;

              return Task.FromResult(requeueResponse ?? Result.Success(new RequeueFailedCampaignEmailDeliveriesResponse(
                  campaignId,
                  request.BatchSize,
                  RequeuedCount: 0)));
          }

        public Task<Result<CampaignEmailDeliveryRepairReadinessResponse>> GetCampaignEmailDeliveryRepairReadinessAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            RepairReadinessTenantId = tenantId;
            RepairReadinessCampaignId = campaignId;

            return Task.FromResult(repairReadinessResponse ?? Result.Success(new CampaignEmailDeliveryRepairReadinessResponse(
                StalePreparedAttemptCount: 0,
                AmbiguousFailedNotificationCount: 0,
                RetryableFailedNotificationCount: 0,
                SuppressedFailedNotificationCount: 0,
                ProviderEventCount: 0,
                LatestProviderEventAt: null,
                CanRetryFailed: false,
                HasRepairWork: false,
                Issues: [])));
        }

        public Task<Result<ListEmailSuppressionsResponse>> ListEmailSuppressionsAsync(
            Guid tenantId,
            int limit,
            bool includeReleased,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ListEmailSuppressionsResponse(
                limit,
                ActiveCount: 0,
                ReleasedCount: 0,
                Suppressions: [])));
        }

        public Task<Result<EmailSuppressionResponse>> AddEmailSuppressionAsync(
            Guid tenantId,
            AddEmailSuppressionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new EmailSuppressionResponse(
                Guid.NewGuid(),
                request.Recipient,
                request.Reason ?? "manual",
                "manual",
                request.Note,
                DateTimeOffset.UtcNow,
                ReleasedAt: null,
                ReleaseReason: null,
                Active: true)));
        }

        public Task<Result<EmailSuppressionResponse>> ReleaseEmailSuppressionAsync(
            Guid tenantId,
            Guid suppressionId,
            ReleaseEmailSuppressionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new EmailSuppressionResponse(
                suppressionId,
                "released@example.test",
                "manual",
                "manual",
                null,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow,
                request.Reason,
                Active: false)));
        }

        public Task<Result<RecordProviderDeliveryEventResponse>> RecordProviderDeliveryEventAsync(
            Guid tenantId,
            RecordProviderDeliveryEventRequest request,
            CancellationToken cancellationToken)
        {
            ProviderEventTenantId = tenantId;
            ProviderEventRequest = request;

            return Task.FromResult(providerEventResponse ?? Result.Success(new RecordProviderDeliveryEventResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                request.EventType,
                "sent",
                SuppressionCreated: false,
                DuplicateEvent: false)));
        }

        public Task<Result<ListProviderDeliveryEventsResponse>> ListProviderDeliveryEventsAsync(
            Guid tenantId,
            int limit,
            CancellationToken cancellationToken)
        {
            ProviderEventsTenantId = tenantId;
            ProviderEventsLimit = limit;

            return Task.FromResult(providerEventsResponse ?? Result.Success(new ListProviderDeliveryEventsResponse(
                limit,
                Events: [])));
        }
      }

    private sealed class FakeOperationalNotificationStore(
        Result<ListOperationalNotificationsResponse> response,
        Result<OperationalNotificationResponse>? markReadResponse = null,
        Result<OperationalNotificationSummaryResponse>? summaryResponse = null,
        Result<MarkAllOperationalNotificationsReadResponse>? markAllReadResponse = null) : IOperationalNotificationStore
    {
        public Guid TenantId { get; private set; }

        public int Limit { get; private set; }

        public Guid SummaryTenantId { get; private set; }

        public Guid MarkReadTenantId { get; private set; }

        public Guid MarkReadNotificationId { get; private set; }

        public Guid MarkAllReadTenantId { get; private set; }

        public Task<Result<OperationalNotificationResponse>> RecordReportPdfArtifactTerminalStateAsync(
            Guid tenantId,
            Guid exportArtifactId,
            Guid campaignSeriesId,
            string status,
            string? failureReasonCode,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<ListOperationalNotificationsResponse>> ListOperationalNotificationsAsync(
            Guid tenantId,
            int limit,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            Limit = limit;

            return Task.FromResult(response);
        }

        public Task<Result<OperationalNotificationSummaryResponse>> GetOperationalNotificationSummaryAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            SummaryTenantId = tenantId;

            return Task.FromResult(
                summaryResponse ??
                Result.Success(new OperationalNotificationSummaryResponse(
                    0,
                    0,
                    0,
                    LatestUnreadAt: null)));
        }

        public Task<Result<OperationalNotificationResponse>> MarkOperationalNotificationReadAsync(
            Guid tenantId,
            Guid notificationId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken)
        {
            MarkReadTenantId = tenantId;
            MarkReadNotificationId = notificationId;

            return Task.FromResult(
                markReadResponse ??
                Result.Failure<OperationalNotificationResponse>(
                    Error.NotFound(
                        "operational_notification.not_found",
                        "Operational notification was not found.")));
        }

        public Task<Result<MarkAllOperationalNotificationsReadResponse>> MarkAllOperationalNotificationsReadAsync(
            Guid tenantId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken)
        {
            MarkAllReadTenantId = tenantId;

            return Task.FromResult(
                markAllReadResponse ??
                Result.Success(new MarkAllOperationalNotificationsReadResponse(
                    MarkedReadCount: 0,
                    ReadAt: readAt)));
        }
    }

    private sealed class FakeCampaignSeriesProofStore(
        Guid expectedCampaignSeriesId,
        Result<CampaignSeriesTwoWaveProofResponse> result) : ICampaignSeriesProofStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignSeriesId { get; private set; }

        public Task<Result<CampaignSeriesTwoWaveProofResponse>> GetTwoWaveProofAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCampaignSeriesId, campaignSeriesId);
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(result);
        }
    }
}
