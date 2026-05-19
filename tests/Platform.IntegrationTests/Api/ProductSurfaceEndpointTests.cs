using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Campaigns;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class ProductSurfaceEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Workspace_overview_endpoint_returns_store_projection()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var ownSeriesId = Guid.NewGuid();
        var commandItemId = "series-setup-blocker";
        var sampleStudy = new CampaignSeriesListItemResponse(
            seriesId,
            "Pulse 2026",
            DateTimeOffset.Parse("2026-05-01T08:00:00+00:00"),
            DateTimeOffset.Parse("2026-05-02T09:00:00+00:00"),
            CampaignCount: 2,
            LiveCampaignCount: 1,
            SubmittedResponseCount: 14,
            LatestLaunchAt: DateTimeOffset.Parse("2026-05-02T10:00:00+00:00"),
            LatestSubmissionAt: DateTimeOffset.Parse("2026-05-03T11:00:00+00:00"),
            ReadinessStatus: "proof_only",
            StudyKind: "sample",
            IsSample: true,
            SampleScenario: "mixed_lifecycle",
            ReadOnlyReason: "sample_study");
        var ownStudy = new CampaignSeriesListItemResponse(
            ownSeriesId,
            "Team baseline",
            DateTimeOffset.Parse("2026-05-04T08:00:00+00:00"),
            DateTimeOffset.Parse("2026-05-05T09:00:00+00:00"),
            CampaignCount: 1,
            LiveCampaignCount: 0,
            SubmittedResponseCount: 0,
            LatestLaunchAt: null,
            LatestSubmissionAt: null,
            ReadinessStatus: "not_configured",
            StudyKind: "own",
            IsSample: false,
            SampleScenario: null,
            ReadOnlyReason: null);
        var response = new WorkspaceOverviewResponse(
            tenantId,
            new WorkspaceOverviewTotalsResponse(
                CampaignSeriesCount: 3,
                CampaignCount: 7,
                LiveCampaignCount: 2,
                SubmittedResponseCount: 42,
                ExportArtifactCount: 5),
            [
                sampleStudy
            ],
            new WorkspaceCommandCenterResponse(
            [
                new WorkspaceCommandCenterItemResponse(
                    commandItemId,
                    "Finish setup for Pulse 2026",
                    "Consent, retention, disclosure, and scoring setup still need attention.",
                    "blocked",
                    "setup",
                    $"/app/campaign-series/{seriesId}/setup",
                    "Open setup",
                    Priority: 20,
                    CampaignSeriesId: seriesId,
                    CampaignId: null,
                    RequiredPermission: "setup.manage")
            ]),
            new WorkspaceStudyCollectionsResponse([sampleStudy], [ownStudy]));
        var store = new FakeProductSurfaceReadStore(workspaceOverview: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/workspace-overview",
            tenantId,
            permissions: "setup.manage team.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var json = await httpResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"studyCollections\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sampleStudies\"", json, StringComparison.Ordinal);
        Assert.Contains("\"ownStudies\"", json, StringComparison.Ordinal);
        Assert.Contains("\"studyKind\":\"sample\"", json, StringComparison.Ordinal);
        Assert.Contains("\"isSample\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"sampleScenario\":\"mixed_lifecycle\"", json, StringComparison.Ordinal);
        Assert.Contains("\"readOnlyReason\":\"sample_study\"", json, StringComparison.Ordinal);
        var payload = JsonSerializer.Deserialize<WorkspaceOverviewResponse>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal(3, payload.Totals.CampaignSeriesCount);
        Assert.Equal(7, payload.Totals.CampaignCount);
        Assert.Equal(2, payload.Totals.LiveCampaignCount);
        Assert.Equal(42, payload.Totals.SubmittedResponseCount);
        Assert.Equal(5, payload.Totals.ExportArtifactCount);
        var item = Assert.Single(payload.RecentSeries);
        Assert.Equal(seriesId, item.Id);
        Assert.Equal("Pulse 2026", item.Name);
        Assert.Equal("proof_only", item.ReadinessStatus);
        Assert.Equal("sample", item.StudyKind);
        Assert.True(item.IsSample);
        Assert.Equal("mixed_lifecycle", item.SampleScenario);
        Assert.Equal("sample_study", item.ReadOnlyReason);
        var sample = Assert.Single(payload.StudyCollections.SampleStudies);
        Assert.Equal(seriesId, sample.Id);
        Assert.Equal("sample", sample.StudyKind);
        Assert.True(sample.IsSample);
        Assert.Equal("mixed_lifecycle", sample.SampleScenario);
        Assert.Equal("sample_study", sample.ReadOnlyReason);
        var own = Assert.Single(payload.StudyCollections.OwnStudies);
        Assert.Equal(ownSeriesId, own.Id);
        Assert.Equal("own", own.StudyKind);
        Assert.False(own.IsSample);
        Assert.Null(own.SampleScenario);
        Assert.Null(own.ReadOnlyReason);
        var command = Assert.Single(payload.CommandCenter.Items);
        Assert.Equal(commandItemId, command.Id);
        Assert.Equal("Finish setup for Pulse 2026", command.Title);
        Assert.Equal("blocked", command.State);
        Assert.Equal("setup", command.Surface);
        Assert.Equal($"/app/campaign-series/{seriesId}/setup", command.Route);
        Assert.Equal("Open setup", command.ActionLabel);
        Assert.Equal(20, command.Priority);
        Assert.Equal(seriesId, command.CampaignSeriesId);
        Assert.Null(command.CampaignId);
        Assert.Equal("setup.manage", command.RequiredPermission);
        Assert.Equal(tenantId, store.TenantId);
        Assert.True(store.CanManageSetup);
        Assert.True(store.CanManageTeam);
    }

    [Fact]
    public async Task Campaign_series_list_endpoint_returns_items()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var response = new CampaignSeriesListResponse(
        [
            new CampaignSeriesListItemResponse(
                seriesId,
                "Quarterly engagement",
                DateTimeOffset.Parse("2026-04-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-04-03T09:00:00+00:00"),
                CampaignCount: 4,
                LiveCampaignCount: 2,
                SubmittedResponseCount: 99,
                LatestLaunchAt: DateTimeOffset.Parse("2026-04-04T10:00:00+00:00"),
                LatestSubmissionAt: DateTimeOffset.Parse("2026-04-05T11:00:00+00:00"),
                ReadinessStatus: "pending",
                StudyKind: "own",
                IsSample: false,
                SampleScenario: null,
                ReadOnlyReason: null)
        ]);
        var store = new FakeProductSurfaceReadStore(campaignSeriesList: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, "/campaign-series", tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesListResponse>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload.Items);
        Assert.Equal(seriesId, item.Id);
        Assert.Equal("Quarterly engagement", item.Name);
        Assert.Equal("pending", item.ReadinessStatus);
        Assert.Equal("own", item.StudyKind);
        Assert.False(item.IsSample);
        Assert.Null(item.SampleScenario);
        Assert.Null(item.ReadOnlyReason);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Campaign_series_list_endpoint_allows_tenant_member_without_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var response = new CampaignSeriesListResponse([]);
        var store = new FakeProductSurfaceReadStore(campaignSeriesList: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/campaign-series",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Tenant_members_endpoint_returns_roster()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var response = new TenantMemberRosterResponse(
            tenantId,
            [
                new TenantMemberResponse(
                    userId,
                    "researcher@example.test",
                    "en",
                    DateTimeOffset.Parse("2026-05-10T08:00:00+00:00"),
                    DateTimeOffset.Parse("2026-05-11T09:00:00+00:00"),
                    [
                        new TenantMemberRoleResponse(
                            roleId,
                            "researcher",
                            "Researcher",
                            "tenant",
                            null,
                            DateTimeOffset.Parse("2026-05-10T08:30:00+00:00"))
                    ],
                    ["setup.manage"],
                    IdentityStatus: "active")
            ]);
        var store = new FakeProductSurfaceReadStore(tenantMemberRoster: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, "/tenant-members", tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<TenantMemberRosterResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.TenantId);
        var member = Assert.Single(payload.Members);
        Assert.Equal(userId, member.UserId);
        Assert.Equal("researcher@example.test", member.Email);
        Assert.Equal("en", member.Locale);
        Assert.Equal(DateTimeOffset.Parse("2026-05-11T09:00:00+00:00"), member.LastLoginAt);
        Assert.Equal(["setup.manage"], member.Permissions);
        Assert.Equal("active", member.IdentityStatus);
        var role = Assert.Single(member.Roles);
        Assert.Equal(roleId, role.RoleId);
        Assert.Equal("researcher", role.Code);
        Assert.Equal("Researcher", role.Name);
        Assert.Equal("tenant", role.ScopeType);
        Assert.Null(role.ScopeId);
        Assert.Equal(DateTimeOffset.Parse("2026-05-10T08:30:00+00:00"), role.GrantedAt);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Tenant_members_endpoint_allows_tenant_member_without_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var response = new TenantMemberRosterResponse(tenantId, []);
        var store = new FakeProductSurfaceReadStore(tenantMemberRoster: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/tenant-members",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Tenant_roles_endpoint_allows_tenant_member_and_returns_assignable_roles()
    {
        var tenantId = Guid.NewGuid();
        var ownerRoleId = Guid.NewGuid();
        var analystRoleId = Guid.NewGuid();
        var response = new TenantRoleListResponse(
        [
            new TenantRoleResponse(ownerRoleId, "tenant_owner", "Tenant owner", ["setup.manage", "team.manage"]),
            new TenantRoleResponse(analystRoleId, "analyst", "Analyst", [])
        ]);
        var store = new FakeProductSurfaceReadStore(tenantRoleList: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/tenant-roles",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<TenantRoleListResponse>();
        Assert.NotNull(payload);
        Assert.Collection(
            payload.Roles,
            owner =>
            {
                Assert.Equal(ownerRoleId, owner.RoleId);
                Assert.Equal("tenant_owner", owner.Code);
        Assert.Equal(["setup.manage", "team.manage"], owner.Permissions);
            },
            analyst =>
            {
                Assert.Equal(analystRoleId, analyst.RoleId);
                Assert.Equal("analyst", analyst.Code);
                Assert.Empty(analyst.Permissions);
            });
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Tenant_settings_endpoint_returns_tenant_profile_and_counts()
    {
        var tenantId = Guid.NewGuid();
        var response = new TenantSettingsWorkspaceResponse(
            new TenantSettingsProfileResponse(
                tenantId,
                "algebra-research",
                "Algebra Research",
                "eu",
                "hr",
                "active",
                DateTimeOffset.Parse("2026-05-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-11T09:00:00+00:00")),
            new TenantSettingsWorkspaceCountsResponse(
                CampaignSeriesCount: 4,
                CampaignCount: 7,
                LiveCampaignCount: 2,
                SubmittedResponseCount: 128,
                SubjectCount: 42,
                SubjectGroupCount: 6,
                TenantMemberCount: 5,
                TenantRoleCount: 3,
                ExportArtifactCount: 9),
            [
                new TenantSettingsManagementLinkResponse(
                    "campaign-series",
                    "Campaign series",
                    "Create and select tenant campaign series.",
                    "/app/campaign-series"),
                new TenantSettingsManagementLinkResponse(
                    "team",
                    "Team",
                    "Review tenant members and app-owned roles.",
                    "/app/team"),
                new TenantSettingsManagementLinkResponse(
                    "directory",
                    "Directory",
                    "Review subjects, groups, and hierarchy.",
                    "/app/directory")
            ]);
        var store = new FakeProductSurfaceReadStore(tenantSettingsResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/tenant-settings",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<TenantSettingsWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.Profile.TenantId);
        Assert.Equal("algebra-research", payload.Profile.Slug);
        Assert.Equal("Algebra Research", payload.Profile.Name);
        Assert.Equal("eu", payload.Profile.Region);
        Assert.Equal("hr", payload.Profile.DefaultLocale);
        Assert.Equal("active", payload.Profile.Status);
        Assert.Equal(4, payload.Counts.CampaignSeriesCount);
        Assert.Equal(7, payload.Counts.CampaignCount);
        Assert.Equal(2, payload.Counts.LiveCampaignCount);
        Assert.Equal(128, payload.Counts.SubmittedResponseCount);
        Assert.Equal(42, payload.Counts.SubjectCount);
        Assert.Equal(6, payload.Counts.SubjectGroupCount);
        Assert.Equal(5, payload.Counts.TenantMemberCount);
        Assert.Equal(3, payload.Counts.TenantRoleCount);
        Assert.Equal(9, payload.Counts.ExportArtifactCount);
        Assert.Collection(
            payload.ManagementLinks,
            link => Assert.Equal("/app/campaign-series", link.Route),
            link => Assert.Equal("/app/team", link.Route),
            link => Assert.Equal("/app/directory", link.Route));
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Export_artifact_library_endpoint_returns_safe_inventory()
    {
        var tenantId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-16T08:00:00+00:00");
        var response = new ExportArtifactLibraryResponse(
            tenantId,
            new ExportArtifactLibrarySummaryResponse(
                TotalCount: 2,
                DownloadableCount: 1,
                FailedCount: 1,
                PendingCount: 0),
            [
                new CampaignSeriesReportsExportArtifactResponse(
                    artifactId,
                    "campaign",
                    campaignId,
                    "Baseline wave",
                    campaignId,
                    "Baseline wave",
                    "report_proof_csv_codebook",
                    "succeeded",
                    "csv_codebook",
                    "baseline-report.csv",
                    12,
                    2048,
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    createdAt,
                    createdAt.AddSeconds(3),
                    createdAt,
                    null,
                    null,
                    null,
                    null,
                    true,
                    "closed",
                    createdAt.AddHours(1),
                    "closed_wave")
            ]);
        var store = new FakeProductSurfaceReadStore(exportArtifactLibrary: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/export-artifacts",
            tenantId,
            permissions: "setup.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ExportArtifactLibraryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal(2, payload.Summary.TotalCount);
        Assert.Equal(1, payload.Summary.DownloadableCount);
        Assert.Equal(1, payload.Summary.FailedCount);
        Assert.Equal(0, payload.Summary.PendingCount);
        Assert.Equal(0, payload.Summary.RetryableCount);
        var artifact = Assert.Single(payload.Artifacts);
        Assert.Equal(artifactId, artifact.Id);
        Assert.Equal(campaignId, artifact.TargetId);
        Assert.Equal("Baseline wave", artifact.TargetLabel);
        Assert.Equal("baseline-report.csv", artifact.FileName);
        Assert.Equal("succeeded", artifact.Status);
        Assert.Equal(2048, artifact.ByteSize);
        Assert.True(artifact.CanDownload);
        Assert.False(artifact.CanRetry);
        var serialized = await httpResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("csvContent", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codebookJson", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(tenantId, store.TenantId);
        Assert.True(store.CanManageSetup);
    }

    [Fact]
    public async Task Subjects_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeProductSurfaceReadStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/subjects",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(Guid.Empty, store.TenantId);
    }

    [Fact]
    public async Task Subjects_endpoint_returns_directory_projection()
    {
        var tenantId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var response = new SubjectDirectoryResponse(
            tenantId,
            new SubjectDirectorySummaryResponse(
                SubjectCount: 2,
                GroupCount: 1,
                ManagerRelationshipCount: 1),
            [
                new SubjectDirectoryItemResponse(
                    subjectId,
                    "Ana Analyst",
                    "ana@example.test",
                    "emp-001",
                    "en",
                    """{"title":"Analyst"}""",
                    managerId,
                    "Mira Manager",
                    DirectReportCount: 0,
                    [
                        new SubjectGroupMembershipResponse(
                            groupId,
                            "department",
                            "Research",
                            "member",
                            new DateOnly(2026, 5, 1),
                            null)
                    ])
            ]);
        var store = new FakeProductSurfaceReadStore(subjectDirectory: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, "/subjects", tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<SubjectDirectoryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal(2, payload.Summary.SubjectCount);
        var subject = Assert.Single(payload.Subjects);
        Assert.Equal(subjectId, subject.Id);
        Assert.Equal("Ana Analyst", subject.DisplayName);
        Assert.Equal(managerId, subject.ManagerSubjectId);
        var membership = Assert.Single(subject.Groups);
        Assert.Equal(groupId, membership.GroupId);
        Assert.Equal("Research", membership.GroupName);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Create_subject_endpoint_maps_validation_failure_to_400()
    {
        var tenantId = Guid.NewGuid();
        var result = Result.Failure<SubjectDirectoryItemResponse>(
            Error.Validation("subject.attributes_invalid", "Subject attributes must be a JSON object."));
        var writeStore = new FakeProductSurfaceWriteStore(createSubjectResult: result);
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Post, "/subjects", tenantId);
        request.Content = JsonContent.Create(new CreateSubjectRequest(
            "Ana Analyst",
            "ana@example.test",
            "emp-001",
            "en",
            """["not-object"]"""));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("subject.attributes_invalid", payload.Title);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal("emp-001", writeStore.CreateSubjectRequest?.ExternalId);
    }

    [Fact]
    public async Task Import_subject_directory_csv_endpoint_binds_request_and_requires_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var importResponse = new SubjectDirectoryCsvImportResponse(
            tenantId,
            RowCount: 1,
            ImportedRowCount: 1,
            CreatedSubjectCount: 1,
            UpdatedSubjectCount: 0,
            CreatedGroupCount: 1,
            AddedMembershipCount: 1,
            SkippedMembershipCount: 0,
            [
                new SubjectDirectoryCsvImportRowResponse(
                    RowNumber: 2,
                    Status: "imported",
                    ExternalId: "emp-001",
                    Email: "ana@example.test",
                    DisplayName: "Ana Analyst",
                    GroupType: "department",
                    GroupName: "Research",
                    Action: "created_subject,created_group,added_membership",
                    Issues: [])
            ]);
        var writeStore = new FakeProductSurfaceWriteStore(importSubjectDirectoryCsvResult: Result.Success(importResponse));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Post, "/subjects/imports/csv", tenantId);
        request.Content = JsonContent.Create(new SubjectDirectoryCsvImportRequest(
            """
            external_id,email,display_name,group_type,group_name
            emp-001,ana@example.test,Ana Analyst,department,Research
            """));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<SubjectDirectoryCsvImportResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.ImportedRowCount);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Contains("emp-001", writeStore.ImportSubjectDirectoryCsvRequest?.CsvContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Subject_groups_endpoint_returns_groups_with_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var response = new SubjectGroupListResponse(
            tenantId,
            [
                new SubjectGroupResponse(
                    groupId,
                    "department",
                    "Research",
                    ParentGroupId: null,
                    "{}",
                    MemberCount: 3)
            ]);
        var store = new FakeProductSurfaceReadStore(subjectGroupList: response);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, "/subject-groups", tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<SubjectGroupListResponse>();
        Assert.NotNull(payload);
        var group = Assert.Single(payload.Groups);
        Assert.Equal(groupId, group.Id);
        Assert.Equal("Research", group.Name);
        Assert.Equal(3, group.MemberCount);
        Assert.Equal(tenantId, store.TenantId);
    }

    [Fact]
    public async Task Add_subject_group_member_endpoint_binds_group_and_request()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var result = Result.Success(new SubjectGroupMembershipResponse(
            groupId,
            "department",
            "Research",
            "member",
            new DateOnly(2026, 5, 1),
            null));
        var writeStore = new FakeProductSurfaceWriteStore(addSubjectGroupMemberResult: result);
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Post, $"/subject-groups/{groupId}/members", tenantId);
        request.Content = JsonContent.Create(new AddSubjectGroupMemberRequest(
            subjectId,
            "member",
            new DateOnly(2026, 5, 1),
            null));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(groupId, writeStore.SubjectGroupId);
        Assert.Equal(subjectId, writeStore.AddSubjectGroupMemberRequest?.SubjectId);
    }

    [Fact]
    public async Task Set_subject_manager_endpoint_binds_subject_and_manager()
    {
        var tenantId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var result = Result.Success(new SubjectDirectoryItemResponse(
            subjectId,
            "Ana Analyst",
            "ana@example.test",
            "emp-001",
            "en",
            "{}",
            managerId,
            "Mira Manager",
            DirectReportCount: 0,
            []));
        var writeStore = new FakeProductSurfaceWriteStore(setSubjectManagerResult: result);
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Put, $"/subjects/{subjectId}/manager", tenantId);
        request.Content = JsonContent.Create(new SetSubjectManagerRequest(
            managerId,
            new DateOnly(2026, 5, 1)));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(subjectId, writeStore.SubjectId);
        Assert.Equal(managerId, writeStore.SetSubjectManagerRequest?.ManagerSubjectId);
    }

    [Fact]
    public async Task Respondent_rule_preview_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var store = new FakeProductSurfaceReadStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/campaigns/{campaignId}/respondent-rule-preview",
            tenantId,
            permissions: null);
        request.Content = JsonContent.Create(new RespondentRulePreviewRequest("""{"kind":"self"}"""));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(Guid.Empty, store.TenantId);
    }

    [Fact]
    public async Task Respondent_rule_preview_endpoint_binds_series_campaign_and_request()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var targetSubjectId = Guid.NewGuid();
        var respondentSubjectId = Guid.NewGuid();
        var response = new RespondentRulePreviewResponse(
            seriesId,
            campaignId,
            "manager_of_target",
            "manager",
            new RespondentRulePreviewSummaryResponse(
                TargetCount: 1,
                RespondentCount: 1,
                AssignmentPairCount: 1,
                SkippedCount: 0,
                WarningCount: 0,
                Truncated: false),
            [
                new RespondentRulePreviewRowResponse(
                    Ordinal: 1,
                    RuleKind: "manager_of_target",
                    Role: "manager",
                    Target: new RespondentRulePreviewSubjectResponse(
                        targetSubjectId,
                        "Ana Analyst",
                        "Ana Analyst",
                        "ana@example.test",
                        "emp-001"),
                    Respondent: new RespondentRulePreviewSubjectResponse(
                        respondentSubjectId,
                        "Mira Manager",
                        "Mira Manager",
                        "mira@example.test",
                        "mgr-001"))
            ],
            []);
        var store = new FakeProductSurfaceReadStore(
            respondentRulePreviewResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/campaigns/{campaignId}/respondent-rule-preview",
            tenantId);
        request.Content = JsonContent.Create(new RespondentRulePreviewRequest(
            """{"kind":"manager_of_target","role":"manager"}""",
            TargetSubjectId: targetSubjectId,
            GroupId: null,
            MaxRows: 25));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<RespondentRulePreviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal("manager_of_target", payload.RuleKind);
        Assert.Equal(1, payload.Summary.AssignmentPairCount);
        var row = Assert.Single(payload.Rows);
        Assert.Equal(targetSubjectId, row.Target?.Id);
        Assert.Equal(respondentSubjectId, row.Respondent?.Id);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
        Assert.Equal(campaignId, store.CampaignId);
        Assert.Equal(targetSubjectId, store.RespondentRulePreviewRequest?.TargetSubjectId);
        Assert.Equal(25, store.RespondentRulePreviewRequest?.MaxRows);
    }

    [Fact]
    public async Task Respondent_rule_preview_endpoint_maps_validation_failure_to_400()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var result = Result.Failure<RespondentRulePreviewResponse>(
            Error.Validation("respondent_rule_preview.unsupported_kind", "Respondent rule kind is not supported for preview."));
        var store = new FakeProductSurfaceReadStore(respondentRulePreviewResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/campaigns/{campaignId}/respondent-rule-preview",
            tenantId);
        request.Content = JsonContent.Create(new RespondentRulePreviewRequest("""{"kind":"peers_of_target"}"""));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("respondent_rule_preview.unsupported_kind", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
        Assert.Equal(campaignId, store.CampaignId);
    }

    [Fact]
    public async Task Respondent_rule_preview_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var result = Result.Failure<RespondentRulePreviewResponse>(
            Error.NotFound("campaign.not_found", "Campaign was not found."));
        var store = new FakeProductSurfaceReadStore(respondentRulePreviewResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/campaigns/{campaignId}/respondent-rule-preview",
            tenantId);
        request.Content = JsonContent.Create(new RespondentRulePreviewRequest("""{"kind":"self"}"""));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
        Assert.Equal(campaignId, store.CampaignId);
    }

    [Fact]
    public async Task Create_tenant_member_endpoint_requires_team_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/tenant-members",
            tenantId,
            permissions: "setup.manage");
        request.Content = JsonContent.Create(new CreateTenantMemberRequest(
            "new.member@example.test",
            "analyst"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Create_tenant_member_endpoint_maps_validation_failure_to_400()
    {
        var tenantId = Guid.NewGuid();
        var result = Result.Failure<TenantMemberMutationResponse>(
            Error.Validation("tenant_member.email_invalid", "Enter a valid email address."));
        var writeStore = new FakeProductSurfaceWriteStore(createTenantMemberResult: result);
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/tenant-members",
            tenantId,
            permissions: "team.manage");
        request.Content = JsonContent.Create(new CreateTenantMemberRequest(
            "not an email",
            "analyst"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("tenant_member.email_invalid", payload.Title);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal("not an email", writeStore.CreateTenantMemberRequest?.Email);
    }

    [Fact]
    public async Task Change_tenant_member_role_endpoint_requires_team_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/tenant-members/{targetUserId}/tenant-role",
            tenantId,
            permissions: "setup.manage");
        request.Content = JsonContent.Create(new ChangeTenantMemberRoleRequest("viewer"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Change_tenant_member_role_endpoint_maps_self_role_change_rejection_to_409()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var result = Result.Failure<TenantMemberMutationResponse>(
            Error.Conflict("tenant_member.self_role_change", "You cannot change your own tenant role."));
        var writeStore = new FakeProductSurfaceWriteStore(changeTenantMemberRoleResult: result);
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Put,
            $"/tenant-members/{actorUserId}/tenant-role",
            tenantId,
            actorUserId,
            permissions: "team.manage");
        request.Content = JsonContent.Create(new ChangeTenantMemberRoleRequest("viewer"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("tenant_member.self_role_change", payload.Title);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
        Assert.Equal(actorUserId, writeStore.TargetUserId);
        Assert.Equal("viewer", writeStore.ChangeTenantMemberRoleRequest?.RoleCode);
    }

    [Fact]
    public async Task Campaign_series_list_endpoint_binds_portfolio_query()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeProductSurfaceReadStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/campaign-series?search=gamma&status=proof_only&sort=name_asc&visibility=archived",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(store.PortfolioQuery);
        Assert.Equal("gamma", store.PortfolioQuery.Search);
        Assert.Equal("proof_only", store.PortfolioQuery.Status);
        Assert.Equal("name_asc", store.PortfolioQuery.Sort);
        Assert.Equal("archived", store.PortfolioQuery.Visibility);
    }

    [Fact]
    public async Task Campaign_series_hub_endpoint_returns_selected_series()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var response = new CampaignSeriesHubResponse(
            seriesId,
            "Leadership pulse",
            DateTimeOffset.Parse("2026-03-01T08:00:00+00:00"),
            DateTimeOffset.Parse("2026-03-02T09:00:00+00:00"),
            new CampaignSeriesHubTotalsResponse(
                CampaignCount: 2,
                LiveCampaignCount: 1,
                SubmittedResponseCount: 31,
                ScoreCount: 28,
                ExportArtifactCount: 3),
            new CampaignSeriesGovernanceSummaryResponse(
                ConsentStatus: "proof_only",
                RetentionStatus: "proof_only",
                DisclosureStatus: "pending",
                ScoringStatus: "proof_only"),
            [
                new CampaignSeriesLifecycleItemResponse(
                    "setup",
                    "Setup",
                    "ready",
                    "Governance prerequisites are configured for this series.",
                    "setup",
                    "Review setup")
            ],
            [
                new CampaignSeriesHubCampaignResponse(
                    campaignId,
                    "Wave 1",
                    "live",
                    "anonymous",
                    "en",
                    StartAt: DateTimeOffset.Parse("2026-03-05T08:00:00+00:00"),
                    EndAt: DateTimeOffset.Parse("2026-03-15T18:00:00+00:00"),
                    LatestLaunchAt: DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                    SubmittedResponseCount: 31,
                    ScoreCount: 28,
                    ExportArtifactCount: 3)
            ],
            StudyKind: "sample",
            IsSample: true,
            SampleScenario: "completed",
            ReadOnlyReason: "sample_study");
        var store = new FakeProductSurfaceReadStore(campaignSeriesHubResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, $"/campaign-series/{seriesId}", tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesHubResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Id);
        Assert.Equal("sample", payload.StudyKind);
        Assert.True(payload.IsSample);
        Assert.Equal("completed", payload.SampleScenario);
        Assert.Equal("sample_study", payload.ReadOnlyReason);
        Assert.Equal(2, payload.Totals.CampaignCount);
        Assert.Equal(1, payload.Totals.LiveCampaignCount);
        Assert.Equal(31, payload.Totals.SubmittedResponseCount);
        Assert.Equal(28, payload.Totals.ScoreCount);
        Assert.Equal(3, payload.Totals.ExportArtifactCount);
        Assert.Equal("proof_only", payload.Governance.ConsentStatus);
        Assert.Equal("proof_only", payload.Governance.RetentionStatus);
        Assert.Equal("pending", payload.Governance.DisclosureStatus);
        Assert.Equal("proof_only", payload.Governance.ScoringStatus);
        var lifecycle = Assert.Single(payload.Lifecycle);
        Assert.Equal("setup", lifecycle.Id);
        Assert.Equal("Setup", lifecycle.Label);
        Assert.Equal("ready", lifecycle.Status);
        Assert.Equal("Review setup", lifecycle.ActionLabel);
        Assert.Equal("setup", lifecycle.Route);
        var campaign = Assert.Single(payload.Campaigns);
        Assert.Equal(campaignId, campaign.Id);
        Assert.Equal("Wave 1", campaign.Name);
        Assert.Equal("live", campaign.Status);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_hub_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesHubResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesHubResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(HttpMethod.Get, $"/campaign-series/{seriesId}", tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Rename_campaign_series_endpoint_returns_updated_name()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var renamedAt = DateTimeOffset.Parse("2026-05-09T12:30:00+00:00");
        var response = new CampaignSeriesRenameResponse(seriesId, "Renamed pulse", renamedAt);
        var writeStore = new FakeProductSurfaceWriteStore(Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Patch, $"/campaign-series/{seriesId}", tenantId);
        request.Content = JsonContent.Create(new RenameCampaignSeriesRequest("Renamed pulse"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesRenameResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Id);
        Assert.Equal("Renamed pulse", payload.Name);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(seriesId, writeStore.CampaignSeriesId);
        Assert.Equal("Renamed pulse", writeStore.Request?.Name);
    }

    [Fact]
    public async Task Rename_campaign_series_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Patch,
            $"/campaign-series/{seriesId}",
            tenantId,
            permissions: null);
        request.Content = JsonContent.Create(new RenameCampaignSeriesRequest("Renamed pulse"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Rename_campaign_series_endpoint_rejects_blank_name()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(HttpMethod.Patch, $"/campaign-series/{seriesId}", tenantId);
        request.Content = JsonContent.Create(new RenameCampaignSeriesRequest("   "));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Duplicate_campaign_series_endpoint_returns_new_own_study()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var sourceSeriesId = Guid.NewGuid();
        var copySeriesId = Guid.NewGuid();
        var response = new CampaignSeriesDuplicateResponse(
            copySeriesId,
            "Copy of Starter sample",
            CampaignSeriesStudyKinds.Own,
            IsSample: false,
            sourceSeriesId);
        var writeStore = new FakeProductSurfaceWriteStore(duplicateResult: Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{sourceSeriesId}/duplicate",
            tenantId,
            actorUserId);
        request.Content = JsonContent.Create(new DuplicateCampaignSeriesRequest("Copy of Starter sample"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesDuplicateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(copySeriesId, payload.Id);
        Assert.Equal("Copy of Starter sample", payload.Name);
        Assert.Equal(CampaignSeriesStudyKinds.Own, payload.StudyKind);
        Assert.False(payload.IsSample);
        Assert.Equal(sourceSeriesId, payload.SourceCampaignSeriesId);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(sourceSeriesId, writeStore.CampaignSeriesId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
        Assert.Equal("Copy of Starter sample", writeStore.DuplicateRequest?.Name);
    }

    [Fact]
    public async Task Duplicate_campaign_series_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var sourceSeriesId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{sourceSeriesId}/duplicate",
            tenantId,
            permissions: null);
        request.Content = JsonContent.Create(new DuplicateCampaignSeriesRequest("Copy"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Duplicate_campaign_series_endpoint_rejects_blank_name()
    {
        var tenantId = Guid.NewGuid();
        var sourceSeriesId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore();
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{sourceSeriesId}/duplicate",
            tenantId);
        request.Content = JsonContent.Create(new DuplicateCampaignSeriesRequest("   "));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        Assert.Equal(0, writeStore.CallCount);
    }

    [Fact]
    public async Task Duplicate_campaign_series_endpoint_returns_problem_for_store_errors()
    {
        var tenantId = Guid.NewGuid();
        var sourceSeriesId = Guid.NewGuid();
        var writeStore = new FakeProductSurfaceWriteStore(
            duplicateResult: Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.Conflict("campaign_series.not_sample", "Only sample studies can be duplicated.")));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{sourceSeriesId}/duplicate",
            tenantId);
        request.Content = JsonContent.Create(new DuplicateCampaignSeriesRequest("Copy"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_sample", payload.Title);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(sourceSeriesId, writeStore.CampaignSeriesId);
    }

    [Fact]
    public async Task Archive_campaign_series_endpoint_returns_archive_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var archivedAt = DateTimeOffset.Parse("2026-05-11T13:15:00+00:00");
        var response = new CampaignSeriesArchiveStateResponse(
            seriesId,
            Archived: true,
            UpdatedAt: archivedAt,
            ArchivedAt: archivedAt,
            ArchivedByUserId: actorUserId,
            ArchiveReason: "Completed pilot");
        var writeStore = new FakeProductSurfaceWriteStore(archiveResult: Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/archive",
            tenantId,
            actorUserId);
        request.Content = JsonContent.Create(new ArchiveCampaignSeriesRequest("Completed pilot"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesArchiveStateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Id);
        Assert.True(payload.Archived);
        Assert.Equal(archivedAt, payload.ArchivedAt);
        Assert.Equal(actorUserId, payload.ArchivedByUserId);
        Assert.Equal("Completed pilot", payload.ArchiveReason);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(seriesId, writeStore.CampaignSeriesId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
        Assert.Equal("Completed pilot", writeStore.ArchiveRequest?.Reason);
    }

    [Fact]
    public async Task Restore_campaign_series_endpoint_returns_active_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var restoredAt = DateTimeOffset.Parse("2026-05-11T13:30:00+00:00");
        var response = new CampaignSeriesArchiveStateResponse(
            seriesId,
            Archived: false,
            UpdatedAt: restoredAt);
        var writeStore = new FakeProductSurfaceWriteStore(restoreResult: Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/restore",
            tenantId,
            actorUserId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesArchiveStateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Id);
        Assert.False(payload.Archived);
        Assert.Null(payload.ArchivedAt);
        Assert.Null(payload.ArchivedByUserId);
        Assert.Null(payload.ArchiveReason);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(seriesId, writeStore.CampaignSeriesId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
    }

    [Fact]
    public async Task Close_campaign_endpoint_returns_close_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T14:30:00+00:00");
        var response = new CampaignCloseStateResponse(
            campaignId,
            CampaignStatuses.Closed,
            closedAt,
            closedAt,
            actorUserId,
            "Collection complete");
        var writeStore = new FakeProductSurfaceWriteStore(closeResult: Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/campaigns/{campaignId}/close",
            tenantId,
            actorUserId);
        request.Content = JsonContent.Create(new CloseCampaignRequest("Collection complete"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignCloseStateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.Id);
        Assert.Equal(CampaignStatuses.Closed, payload.Status);
        Assert.Equal(closedAt, payload.ClosedAt);
        Assert.Equal(actorUserId, payload.ClosedByUserId);
        Assert.Equal("Collection complete", payload.CloseReason);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(seriesId, writeStore.CampaignSeriesId);
        Assert.Equal(campaignId, writeStore.CampaignId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
        Assert.Equal("Collection complete", writeStore.CloseRequest?.Reason);
    }

    [Fact]
    public async Task Remediate_campaign_series_scores_endpoint_returns_aggregate_counts()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var latestScoringActivityAt = DateTimeOffset.Parse("2026-05-11T15:30:00+00:00");
        var response = new CampaignSeriesScoreRemediationResponse(
            seriesId,
            SubmittedResponseCount: 4,
            EligibleSubmittedResponseCount: 3,
            AlreadyScoredSubmittedResponseCount: 1,
            RemediatedSubmittedResponseCount: 2,
            SkippedNotConfiguredSubmittedResponseCount: 1,
            FailedSubmittedResponseCount: 0,
            LatestScoringActivityAt: latestScoringActivityAt);
        var writeStore = new FakeProductSurfaceWriteStore(scoreRemediationResult: Result.Success(response));
        using var client = CreateClient(new FakeProductSurfaceReadStore(), writeStore);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/score-remediation",
            tenantId,
            actorUserId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesScoreRemediationResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.Equal(4, payload.SubmittedResponseCount);
        Assert.Equal(3, payload.EligibleSubmittedResponseCount);
        Assert.Equal(1, payload.AlreadyScoredSubmittedResponseCount);
        Assert.Equal(2, payload.RemediatedSubmittedResponseCount);
        Assert.Equal(1, payload.SkippedNotConfiguredSubmittedResponseCount);
        Assert.Equal(0, payload.FailedSubmittedResponseCount);
        Assert.Equal(latestScoringActivityAt, payload.LatestScoringActivityAt);
        Assert.Equal(tenantId, writeStore.TenantId);
        Assert.Equal(seriesId, writeStore.CampaignSeriesId);
        Assert.Equal(actorUserId, writeStore.ActorUserId);
    }

    [Fact]
    public async Task Campaign_series_setup_workspace_endpoint_returns_selected_setup_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        var scoringRuleId = Guid.NewGuid();
        var response = new CampaignSeriesSetupWorkspaceResponse(
            new CampaignSeriesSetupSeriesResponse(
                seriesId,
                "Leadership setup",
                DateTimeOffset.Parse("2026-03-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-03-02T09:00:00+00:00"),
                StudyKind: "sample",
                IsSample: true,
                SampleScenario: "setup",
                ReadOnlyReason: "sample_study"),
            new CampaignSeriesSetupSummaryResponse(
                CampaignCount: 1,
                LiveCampaignCount: 0,
                MissingPrerequisiteCount: 1),
            new CampaignSeriesSetupCampaignResponse(
                campaignId,
                "Wave 1 draft",
                "draft",
                "anonymous",
                "en",
                templateVersionId,
                LatestLaunchAt: null),
            new CampaignSeriesSetupTemplateResponse(
                Guid.NewGuid(),
                templateVersionId,
                "Leadership template",
                "1.0.0",
                "draft",
                "en",
                InstrumentId: null,
                QuestionCount: 3),
            new CampaignSeriesSetupScoringResponse(
                scoringRuleId,
                "leadership.total",
                "1.0.0",
                "draft",
                "template_version"),
            new CampaignSeriesSetupPolicySummaryResponse(
                new CampaignSeriesSetupPolicyResponse(Guid.NewGuid(), "1.0.0", "configured"),
                new CampaignSeriesSetupPolicyResponse(Guid.NewGuid(), "1.0.0", "configured"),
                new CampaignSeriesSetupPolicyResponse(null, null, "not_configured")),
            new CampaignSeriesSetupReadinessResponse(
                campaignId,
                "blocked",
                Ready: false),
            [
                new CampaignSeriesSetupMissingPrerequisiteResponse(
                    "disclosure_policy.missing",
                    "Disclosure policy",
                    "Configure a disclosure policy before launch.",
                    "warning")
            ],
            [
                new CampaignSeriesSetupCampaignResponse(
                    campaignId,
                    "Wave 1 draft",
                    "draft",
                    "anonymous",
                    "en",
                    templateVersionId,
                    LatestLaunchAt: null)
            ]);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesSetupWorkspaceResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/setup-workspace",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesSetupWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Series.Id);
        Assert.Equal("Leadership setup", payload.Series.Name);
        Assert.Equal("sample", payload.Series.StudyKind);
        Assert.True(payload.Series.IsSample);
        Assert.Equal("setup", payload.Series.SampleScenario);
        Assert.Equal("sample_study", payload.Series.ReadOnlyReason);
        Assert.Equal(1, payload.Summary.CampaignCount);
        Assert.Equal(1, payload.Summary.MissingPrerequisiteCount);
        Assert.NotNull(payload.SelectedCampaign);
        Assert.Equal(campaignId, payload.SelectedCampaign.Id);
        Assert.NotNull(payload.Template);
        Assert.Equal(templateVersionId, payload.Template.TemplateVersionId);
        Assert.NotNull(payload.Scoring);
        Assert.Equal(scoringRuleId, payload.Scoring.Id);
        Assert.Equal("configured", payload.Policies.Consent.Status);
        Assert.Equal("not_configured", payload.Policies.Disclosure.Status);
        Assert.Equal("blocked", payload.Readiness.Status);
        Assert.Equal("disclosure_policy.missing", Assert.Single(payload.MissingPrerequisites).Code);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_setup_workspace_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesSetupWorkspaceResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesSetupWorkspaceResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/setup-workspace",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_operations_workspace_endpoint_returns_selected_operations_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var launchSnapshotId = Guid.NewGuid();
        var response = new CampaignSeriesOperationsWorkspaceResponse(
            new CampaignSeriesOperationsSeriesResponse(
                seriesId,
                "Leadership operations",
                DateTimeOffset.Parse("2026-03-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-03-02T09:00:00+00:00")),
            new CampaignSeriesOperationsSummaryResponse(
                CampaignCount: 1,
                LiveCampaignCount: 1,
                OpenLinkAssignmentCount: 1,
                QueuedInvitationCount: 2,
                SentInvitationCount: 1,
                FailedInvitationCount: 1,
                DeliveryAttemptCount: 3,
                SubmittedResponseCount: 14,
                StartedResponseCount: 18,
                DraftResponseCount: 4,
                LatestResponseStartedAt: DateTimeOffset.Parse("2026-03-05T09:15:00+00:00"),
                LatestResponseSubmittedAt: DateTimeOffset.Parse("2026-03-05T09:45:00+00:00"),
                CollectionStatus: "has_submissions",
                ReportVisibilityStatus: "ready_for_aggregate_report",
                CollectionGuidance: "Enough submitted responses exist for aggregate report visibility.",
                MissingPrerequisiteCount: 1),
            new CampaignSeriesOperationsCampaignResponse(
                campaignId,
                "Wave 1 live",
                "live",
                "anonymous",
                "en",
                launchSnapshotId,
                DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                SubmittedResponseCount: 14,
                StartedResponseCount: 18,
                DraftResponseCount: 4,
                LatestResponseStartedAt: DateTimeOffset.Parse("2026-03-05T09:15:00+00:00"),
                LatestResponseSubmittedAt: DateTimeOffset.Parse("2026-03-05T09:45:00+00:00"),
                CollectionStatus: "has_submissions",
                ReportVisibilityStatus: "ready_for_aggregate_report",
                CollectionGuidance: "Enough submitted responses exist for aggregate report visibility.",
                OpenLinkAssignmentCount: 1,
                QueuedInvitationCount: 2,
                SentInvitationCount: 1,
                FailedInvitationCount: 1,
                DeliveryAttemptCount: 3,
                LatestDeliveryAttemptAt: DateTimeOffset.Parse("2026-03-05T09:00:00+00:00")),
            [
                new CampaignSeriesOperationsMissingPrerequisiteResponse(
                    "queued_invitations.missing",
                    "Queued invitations",
                    "Queue invitations before running local delivery.",
                    "advisory")
            ],
            [
                new CampaignSeriesOperationsCampaignResponse(
                    campaignId,
                    "Wave 1 live",
                    "live",
                    "anonymous",
                    "en",
                    launchSnapshotId,
                    DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                    SubmittedResponseCount: 14,
                    StartedResponseCount: 18,
                    DraftResponseCount: 4,
                    LatestResponseStartedAt: DateTimeOffset.Parse("2026-03-05T09:15:00+00:00"),
                    LatestResponseSubmittedAt: DateTimeOffset.Parse("2026-03-05T09:45:00+00:00"),
                    CollectionStatus: "has_submissions",
                    ReportVisibilityStatus: "ready_for_aggregate_report",
                    CollectionGuidance: "Enough submitted responses exist for aggregate report visibility.",
                    OpenLinkAssignmentCount: 1,
                    QueuedInvitationCount: 2,
                    SentInvitationCount: 1,
                    FailedInvitationCount: 1,
                    DeliveryAttemptCount: 3,
                    LatestDeliveryAttemptAt: DateTimeOffset.Parse("2026-03-05T09:00:00+00:00"))
            ]);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesOperationsWorkspaceResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/operations-workspace",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesOperationsWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Series.Id);
        Assert.Equal("Leadership operations", payload.Series.Name);
        Assert.Equal(1, payload.Summary.CampaignCount);
        Assert.Equal(1, payload.Summary.LiveCampaignCount);
        Assert.Equal(1, payload.Summary.OpenLinkAssignmentCount);
        Assert.Equal(2, payload.Summary.QueuedInvitationCount);
        Assert.Equal(1, payload.Summary.SentInvitationCount);
        Assert.Equal(1, payload.Summary.FailedInvitationCount);
        Assert.Equal(3, payload.Summary.DeliveryAttemptCount);
        Assert.Equal(14, payload.Summary.SubmittedResponseCount);
        Assert.Equal(18, payload.Summary.StartedResponseCount);
        Assert.Equal(4, payload.Summary.DraftResponseCount);
        Assert.Equal("has_submissions", payload.Summary.CollectionStatus);
        Assert.Equal("ready_for_aggregate_report", payload.Summary.ReportVisibilityStatus);
        Assert.Equal(
            "Enough submitted responses exist for aggregate report visibility.",
            payload.Summary.CollectionGuidance);
        Assert.Equal(1, payload.Summary.MissingPrerequisiteCount);
        Assert.NotNull(payload.SelectedCampaign);
        Assert.Equal(campaignId, payload.SelectedCampaign.Id);
        Assert.Equal(launchSnapshotId, payload.SelectedCampaign.LatestLaunchSnapshotId);
        Assert.Equal(18, payload.SelectedCampaign.StartedResponseCount);
        Assert.Equal(4, payload.SelectedCampaign.DraftResponseCount);
        Assert.Equal("has_submissions", payload.SelectedCampaign.CollectionStatus);
        Assert.Equal("queued_invitations.missing", Assert.Single(payload.MissingPrerequisites).Code);
        var campaign = Assert.Single(payload.Campaigns);
        Assert.Equal(campaignId, campaign.Id);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_operations_workspace_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesOperationsWorkspaceResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesOperationsWorkspaceResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/operations-workspace",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_reports_workspace_endpoint_returns_selected_report_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var launchSnapshotId = Guid.NewGuid();
        var scoringRuleId = Guid.NewGuid();
        var disclosurePolicyId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var response = new CampaignSeriesReportsWorkspaceResponse(
            new CampaignSeriesReportsSeriesResponse(
                seriesId,
                "Leadership reports",
                DateTimeOffset.Parse("2026-03-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-03-02T09:00:00+00:00")),
            new CampaignSeriesReportsSummaryResponse(
                CampaignCount: 1,
                LiveCampaignCount: 1,
                ReportableCampaignCount: 1,
                SubmittedResponseCount: 14,
                ScoreCount: 12,
                ExportArtifactCount: 1,
                VisibleScoreCount: 12,
                SuppressedScoreCount: 0,
                MissingPrerequisiteCount: 1),
            new CampaignSeriesReportsCampaignResponse(
                campaignId,
                "Wave 1 live",
                "live",
                "anonymous",
                "en",
                launchSnapshotId,
                DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                scoringRuleId,
                ConsentDocumentId: null,
                RetentionPolicyId: null,
                disclosurePolicyId,
                SubmittedResponseCount: 14,
                ScoreCount: 12,
                ExportArtifactCount: 1,
                VisibleScoreCount: 12,
                SuppressedScoreCount: 0,
                DisclosureState: "visible",
                DisclosureKMin: 5,
                ReportStatus: "proof_only",
                InterpretationStatus: "not_validated_interpretation",
                artifactId,
                "campaign-report-proof.csv",
                "succeeded",
                DateTimeOffset.Parse("2026-03-05T09:00:00+00:00"),
                DateTimeOffset.Parse("2026-03-05T09:00:05+00:00"),
                LatestExportArtifactStartedAt: null,
                LatestExportArtifactFailedAt: null,
                LatestExportArtifactExpiresAt: null,
                LatestExportArtifactDeletedAt: null,
                LatestExportArtifactFailureReasonCode: null,
                LatestExportArtifactCanDownload: true),
            [
                new CampaignSeriesReportsMissingPrerequisiteResponse(
                    "export_artifact.missing",
                    "Export artifact",
                    "Create an aggregate export artifact after report proof.",
                    "advisory")
            ],
            [
                new CampaignSeriesReportsExportArtifactResponse(
                    artifactId,
                    "campaign",
                    campaignId,
                    "Wave 1 live",
                    campaignId,
                    "Wave 1 live",
                    "report_proof_csv_codebook",
                    "succeeded",
                    "csv_codebook",
                    "campaign-report-proof.csv",
                    12,
                    512,
                    "checksum-sha256",
                    DateTimeOffset.Parse("2026-03-05T09:00:00+00:00"),
                    DateTimeOffset.Parse("2026-03-05T09:00:05+00:00"),
                    StartedAt: null,
                    FailedAt: null,
                    ExpiresAt: null,
                    DeletedAt: null,
                    FailureReasonCode: null,
                    CanDownload: true)
            ],
            [
                new CampaignSeriesReportsCampaignResponse(
                    campaignId,
                    "Wave 1 live",
                    "live",
                    "anonymous",
                    "en",
                    launchSnapshotId,
                    DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                    scoringRuleId,
                    ConsentDocumentId: null,
                    RetentionPolicyId: null,
                    disclosurePolicyId,
                    SubmittedResponseCount: 14,
                    ScoreCount: 12,
                    ExportArtifactCount: 1,
                    VisibleScoreCount: 12,
                    SuppressedScoreCount: 0,
                    DisclosureState: "visible",
                    DisclosureKMin: 5,
                    ReportStatus: "proof_only",
                    InterpretationStatus: "not_validated_interpretation",
                    artifactId,
                    "campaign-report-proof.csv",
                    "succeeded",
                    DateTimeOffset.Parse("2026-03-05T09:00:00+00:00"),
                    DateTimeOffset.Parse("2026-03-05T09:00:05+00:00"),
                    LatestExportArtifactStartedAt: null,
                    LatestExportArtifactFailedAt: null,
                    LatestExportArtifactExpiresAt: null,
                    LatestExportArtifactDeletedAt: null,
                    LatestExportArtifactFailureReasonCode: null,
                    LatestExportArtifactCanDownload: true)
            ]);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesReportsWorkspaceResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/reports-workspace",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesReportsWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Series.Id);
        Assert.Equal("Leadership reports", payload.Series.Name);
        Assert.Equal(1, payload.Summary.CampaignCount);
        Assert.Equal(1, payload.Summary.LiveCampaignCount);
        Assert.Equal(1, payload.Summary.ReportableCampaignCount);
        Assert.Equal(14, payload.Summary.SubmittedResponseCount);
        Assert.Equal(12, payload.Summary.ScoreCount);
        Assert.Equal(1, payload.Summary.ExportArtifactCount);
        Assert.Equal(12, payload.Summary.VisibleScoreCount);
        Assert.Equal(0, payload.Summary.SuppressedScoreCount);
        Assert.Equal(1, payload.Summary.MissingPrerequisiteCount);
        Assert.NotNull(payload.SelectedCampaign);
        Assert.Equal(campaignId, payload.SelectedCampaign.Id);
        Assert.Equal(launchSnapshotId, payload.SelectedCampaign.LatestLaunchSnapshotId);
        Assert.Equal(scoringRuleId, payload.SelectedCampaign.ScoringRuleId);
        Assert.Equal(disclosurePolicyId, payload.SelectedCampaign.DisclosurePolicyId);
        Assert.Equal("visible", payload.SelectedCampaign.DisclosureState);
        Assert.Equal("proof_only", payload.SelectedCampaign.ReportStatus);
        Assert.Equal(1, payload.SelectedCampaign.ExportArtifactCount);
        Assert.Equal(artifactId, payload.SelectedCampaign.LatestExportArtifactId);
        Assert.Equal("export_artifact.missing", Assert.Single(payload.MissingPrerequisites).Code);
        var artifact = Assert.Single(payload.ExportArtifacts);
        Assert.Equal(artifactId, artifact.Id);
        Assert.Equal(campaignId, artifact.CampaignId);
        Assert.Equal("Wave 1 live", artifact.CampaignName);
        Assert.Equal("campaign-report-proof.csv", artifact.FileName);
        Assert.Equal("report_proof_csv_codebook", artifact.ArtifactType);
        Assert.Equal("csv_codebook", artifact.Format);
        Assert.Equal("succeeded", artifact.Status);
        Assert.Equal(12, artifact.RowCount);
        Assert.Equal(512, artifact.ByteSize);
        Assert.Equal("checksum-sha256", artifact.ChecksumSha256);
        Assert.True(artifact.CanDownload);
        var campaign = Assert.Single(payload.Campaigns);
        Assert.Equal(campaignId, campaign.Id);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_reports_workspace_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesReportsWorkspaceResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesReportsWorkspaceResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/reports-workspace",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Reports_widget_manifest_endpoint_returns_store_projection()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var response = new CampaignSeriesReportsWidgetManifestResponse(
            seriesId,
            "reports",
            "reports-widget-manifest/v1",
            new ReportWidgetLayoutResponse("dashboard-grid/v1", "standard"),
            [
                new ReportWidgetResponse(
                    "score-coverage",
                    "score-coverage-summary/v1",
                    "Score coverage",
                    "half",
                    "ready",
                    Message: null,
                    Data: new ScoreCoverageWidgetDataResponse(
                        SubmittedResponseCount: 14,
                        ScoredSubmittedResponseCount: 14,
                        UnscoredSubmittedResponseCount: 0,
                        NotConfiguredSubmittedResponseCount: 0,
                        CampaignsWithScoringRuleCount: 1,
                        CampaignsWithoutScoringRuleCount: 0,
                        LatestScoringActivityAt: DateTimeOffset.Parse("2026-05-15T11:00:00+00:00"),
                        Status: "complete",
                        Guidance: "All submitted responses have successful scoring activity."),
                    DataSource: null,
                    Actions: [])
            ]);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesReportsWidgetManifestResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/reports-widget-manifest",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesReportsWidgetManifestResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.Equal("reports", payload.Surface);
        Assert.Equal("reports-widget-manifest/v1", payload.SurfaceVersion);
        Assert.Equal("dashboard-grid/v1", payload.Layout.Kind);
        var widget = Assert.Single(payload.Widgets);
        Assert.Equal("score-coverage", widget.Id);
        Assert.Equal("score-coverage-summary/v1", widget.Kind);
        var data = Assert.IsType<JsonElement>(widget.Data);
        Assert.Equal(14, data.GetProperty("submittedResponseCount").GetInt32());
        Assert.Equal("complete", data.GetProperty("status").GetString());
        Assert.True(store.CanManageSetup);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Reports_widget_manifest_endpoint_passes_missing_setup_manage_to_store()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var response = new CampaignSeriesReportsWidgetManifestResponse(
            seriesId,
            "reports",
            "reports-widget-manifest/v1",
            new ReportWidgetLayoutResponse("dashboard-grid/v1", "standard"),
            []);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesReportsWidgetManifestResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/reports-widget-manifest",
            tenantId,
            permissions: null);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.False(store.CanManageSetup);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Reports_widget_manifest_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesReportsWidgetManifestResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesReportsWidgetManifestResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/reports-widget-manifest",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_waves_workspace_endpoint_returns_selected_waves_state()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var baselineWaveId = Guid.NewGuid();
        var comparisonWaveId = Guid.NewGuid();
        var baselineSnapshotId = Guid.NewGuid();
        var comparisonSnapshotId = Guid.NewGuid();
        var scoringRuleId = Guid.NewGuid();
        var disclosurePolicyId = Guid.NewGuid();
        var response = new CampaignSeriesWavesWorkspaceResponse(
            new CampaignSeriesWavesSeriesResponse(
                seriesId,
                "Leadership waves",
                DateTimeOffset.Parse("2026-03-01T08:00:00+00:00"),
                DateTimeOffset.Parse("2026-03-02T09:00:00+00:00")),
            new CampaignSeriesWavesSummaryResponse(
                CampaignCount: 2,
                LiveCampaignCount: 2,
                LongitudinalWaveCount: 2,
                SubmittedWaveCount: 2,
                LinkedTrajectoryCount: 8,
                CompleteTrajectoryCount: 6,
                ComparableScoreCount: 2,
                VisibleComparisonCount: 2,
                SuppressedComparisonCount: 0,
                BlockedComparisonCount: 0,
                MissingPrerequisiteCount: 1),
            new CampaignSeriesWavesWaveResponse(
                baselineWaveId,
                "Baseline wave",
                "live",
                "anonymous_longitudinal",
                "en",
                baselineSnapshotId,
                DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                scoringRuleId,
                "burnout.total",
                "1.0.0",
                disclosurePolicyId,
                DisclosureKMin: 5,
                SubmittedResponseCount: 8,
                ScoreCount: 8,
                LinkedTrajectoryCount: 8,
                WaveState: "wave"),
            new CampaignSeriesWavesWaveResponse(
                comparisonWaveId,
                "Follow-up wave",
                "live",
                "anonymous_longitudinal",
                "en",
                comparisonSnapshotId,
                DateTimeOffset.Parse("2026-03-12T08:30:00+00:00"),
                scoringRuleId,
                "burnout.total",
                "1.0.0",
                disclosurePolicyId,
                DisclosureKMin: 5,
                SubmittedResponseCount: 8,
                ScoreCount: 8,
                LinkedTrajectoryCount: 8,
                WaveState: "wave"),
            new CampaignSeriesWavesComparisonResponse(
                Status: "proof_only",
                DisclosureState: "visible",
                CompatibilityState: "compatible",
                InterpretationStatus: "not_validated_interpretation",
                DisclosureKMin: 5,
                LinkedPairCount: 6,
                VisibleScoreCount: 2,
                SuppressedScoreCount: 0,
                BlockedScoreCount: 0),
            [
                new CampaignSeriesWavesMissingPrerequisiteResponse(
                    "trajectory_export.deferred",
                    "Trajectory export",
                    "Trajectory exports are deferred for this proof surface.",
                    "advisory")
            ],
            [
                new CampaignSeriesWavesWaveResponse(
                    baselineWaveId,
                    "Baseline wave",
                    "live",
                    "anonymous_longitudinal",
                    "en",
                    baselineSnapshotId,
                    DateTimeOffset.Parse("2026-03-05T08:30:00+00:00"),
                    scoringRuleId,
                    "burnout.total",
                    "1.0.0",
                    disclosurePolicyId,
                    DisclosureKMin: 5,
                    SubmittedResponseCount: 8,
                    ScoreCount: 8,
                    LinkedTrajectoryCount: 8,
                    WaveState: "wave"),
                new CampaignSeriesWavesWaveResponse(
                    comparisonWaveId,
                    "Follow-up wave",
                    "live",
                    "anonymous_longitudinal",
                    "en",
                    comparisonSnapshotId,
                    DateTimeOffset.Parse("2026-03-12T08:30:00+00:00"),
                    scoringRuleId,
                    "burnout.total",
                    "1.0.0",
                    disclosurePolicyId,
                    DisclosureKMin: 5,
                    SubmittedResponseCount: 8,
                    ScoreCount: 8,
                    LinkedTrajectoryCount: 8,
                    WaveState: "wave")
            ]);
        var store = new FakeProductSurfaceReadStore(
            campaignSeriesWavesWorkspaceResult: Result.Success(response));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/waves-workspace",
            tenantId);

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<CampaignSeriesWavesWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(seriesId, payload.Series.Id);
        Assert.Equal(2, payload.Summary.LongitudinalWaveCount);
        Assert.Equal(6, payload.Summary.CompleteTrajectoryCount);
        Assert.Equal(2, payload.Summary.VisibleComparisonCount);
        Assert.NotNull(payload.SelectedBaselineWave);
        Assert.Equal(baselineWaveId, payload.SelectedBaselineWave.Id);
        Assert.NotNull(payload.SelectedComparisonWave);
        Assert.Equal(comparisonWaveId, payload.SelectedComparisonWave.Id);
        Assert.Equal("visible", payload.Comparison.DisclosureState);
        Assert.Equal("compatible", payload.Comparison.CompatibilityState);
        Assert.Equal("trajectory_export.deferred", Assert.Single(payload.MissingPrerequisites).Code);
        Assert.Equal([baselineWaveId, comparisonWaveId], payload.Waves.Select(wave => wave.Id).ToArray());
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    [Fact]
    public async Task Campaign_series_waves_workspace_endpoint_maps_not_found_to_404()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var result = Result.Failure<CampaignSeriesWavesWorkspaceResponse>(
            Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        var store = new FakeProductSurfaceReadStore(campaignSeriesWavesWorkspaceResult: result);
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/waves-workspace",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series.not_found", payload.Title);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(seriesId, store.CampaignSeriesId);
    }

    private HttpClient CreateClient(
        IProductSurfaceReadStore store,
        IProductSurfaceWriteStore? writeStore = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
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

                services.AddScoped<IProductSurfaceReadStore>(_ => store);
                if (writeStore is not null)
                {
                    services.AddScoped<IProductSurfaceWriteStore>(_ => writeStore);
                }
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        Guid? userId = null,
        string? permissions = "setup.manage")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, (userId ?? Guid.NewGuid()).ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        if (permissions is not null)
        {
            request.Headers.Add(TestAuthHandler.PermissionsHeader, permissions);
        }

        return request;
    }

    private sealed class FakeProductSurfaceReadStore(
        WorkspaceOverviewResponse? workspaceOverview = null,
        Result<TenantSettingsWorkspaceResponse>? tenantSettingsResult = null,
        CampaignSeriesListResponse? campaignSeriesList = null,
        ExportArtifactLibraryResponse? exportArtifactLibrary = null,
        TenantMemberRosterResponse? tenantMemberRoster = null,
        TenantRoleListResponse? tenantRoleList = null,
        SubjectDirectoryResponse? subjectDirectory = null,
        SubjectGroupListResponse? subjectGroupList = null,
        Result<CampaignSeriesHubResponse>? campaignSeriesHubResult = null,
        Result<CampaignSeriesSetupWorkspaceResponse>? campaignSeriesSetupWorkspaceResult = null,
        Result<CampaignSeriesOperationsWorkspaceResponse>? campaignSeriesOperationsWorkspaceResult = null,
        Result<CampaignSeriesReportsWorkspaceResponse>? campaignSeriesReportsWorkspaceResult = null,
        Result<CampaignSeriesReportsWidgetManifestResponse>? campaignSeriesReportsWidgetManifestResult = null,
        Result<CampaignSeriesWavesWorkspaceResponse>? campaignSeriesWavesWorkspaceResult = null,
        Result<RespondentRulePreviewResponse>? respondentRulePreviewResult = null)
        : IProductSurfaceReadStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignSeriesId { get; private set; }

        public Guid CampaignId { get; private set; }

        public bool CanManageSetup { get; private set; }

        public bool CanManageTeam { get; private set; }

        public CampaignSeriesPortfolioQuery? PortfolioQuery { get; private set; }

        public RespondentRulePreviewRequest? RespondentRulePreviewRequest { get; private set; }

        public Task<WorkspaceOverviewResponse> GetWorkspaceOverviewAsync(
            Guid tenantId,
            bool canManageSetup,
            bool canManageTeam,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CanManageSetup = canManageSetup;
            CanManageTeam = canManageTeam;

            return Task.FromResult(workspaceOverview ?? new WorkspaceOverviewResponse(
                tenantId,
                new WorkspaceOverviewTotalsResponse(0, 0, 0, 0, 0),
                [],
                new WorkspaceCommandCenterResponse([]),
                new WorkspaceStudyCollectionsResponse([], [])));
        }

        public Task<Result<TenantSettingsWorkspaceResponse>> GetTenantSettingsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;

            return Task.FromResult(tenantSettingsResult ?? Result.Success(
                new TenantSettingsWorkspaceResponse(
                    new TenantSettingsProfileResponse(
                        tenantId,
                        "tenant",
                        "Tenant",
                        "eu",
                        "en",
                        "active",
                        DateTimeOffset.Parse("2026-05-01T08:00:00+00:00"),
                        DateTimeOffset.Parse("2026-05-01T08:00:00+00:00")),
                    new TenantSettingsWorkspaceCountsResponse(0, 0, 0, 0, 0, 0, 0, 0, 0),
                    [])));
        }

        public Task<CampaignSeriesListResponse> ListCampaignSeriesAsync(
            Guid tenantId,
            CampaignSeriesPortfolioQuery query,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            PortfolioQuery = query;

            return Task.FromResult(campaignSeriesList ?? new CampaignSeriesListResponse([]));
        }

        public Task<ExportArtifactLibraryResponse> ListExportArtifactsAsync(
            Guid tenantId,
            bool canManageSetup,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CanManageSetup = canManageSetup;

            return Task.FromResult(exportArtifactLibrary ?? new ExportArtifactLibraryResponse(
                tenantId,
                new ExportArtifactLibrarySummaryResponse(0, 0, 0, 0),
                []));
        }

        public Task<TenantMemberRosterResponse> ListTenantMembersAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;

            return Task.FromResult(tenantMemberRoster ?? new TenantMemberRosterResponse(tenantId, []));
        }

        public Task<TenantRoleListResponse> ListTenantRolesAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;

            return Task.FromResult(tenantRoleList ?? new TenantRoleListResponse([]));
        }

        public Task<SubjectDirectoryResponse> ListSubjectsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;

            return Task.FromResult(subjectDirectory ?? new SubjectDirectoryResponse(
                tenantId,
                new SubjectDirectorySummaryResponse(0, 0, 0),
                []));
        }

        public Task<SubjectGroupListResponse> ListSubjectGroupsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;

            return Task.FromResult(subjectGroupList ?? new SubjectGroupListResponse(tenantId, []));
        }

        public Task<Result<RespondentRulePreviewResponse>> PreviewRespondentRuleAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid campaignId,
            RespondentRulePreviewRequest request,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            CampaignId = campaignId;
            RespondentRulePreviewRequest = request;

            return Task.FromResult(respondentRulePreviewResult ??
                Result.Failure<RespondentRulePreviewResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")));
        }

        public Task<Result<CampaignSeriesHubResponse>> GetCampaignSeriesHubAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(campaignSeriesHubResult ?? Result.Failure<CampaignSeriesHubResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesSetupWorkspaceResponse>> GetCampaignSeriesSetupWorkspaceAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(campaignSeriesSetupWorkspaceResult ??
                Result.Failure<CampaignSeriesSetupWorkspaceResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesOperationsWorkspaceResponse>> GetCampaignSeriesOperationsWorkspaceAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(campaignSeriesOperationsWorkspaceResult ??
                Result.Failure<CampaignSeriesOperationsWorkspaceResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesReportsWorkspaceResponse>> GetCampaignSeriesReportsWorkspaceAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(campaignSeriesReportsWorkspaceResult ??
                Result.Failure<CampaignSeriesReportsWorkspaceResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesReportsWidgetManifestResponse>> GetCampaignSeriesReportsWidgetManifestAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            bool canManageSetup,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            CanManageSetup = canManageSetup;

            return Task.FromResult(campaignSeriesReportsWidgetManifestResult ??
                Result.Failure<CampaignSeriesReportsWidgetManifestResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesWavesWorkspaceResponse>> GetCampaignSeriesWavesWorkspaceAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;

            return Task.FromResult(campaignSeriesWavesWorkspaceResult ??
                Result.Failure<CampaignSeriesWavesWorkspaceResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }
    }

    private sealed class FakeProductSurfaceWriteStore(
        Result<CampaignSeriesRenameResponse>? renameResult = null,
        Result<CampaignSeriesDuplicateResponse>? duplicateResult = null,
        Result<CampaignSeriesArchiveStateResponse>? archiveResult = null,
        Result<CampaignSeriesArchiveStateResponse>? restoreResult = null,
        Result<CampaignCloseStateResponse>? closeResult = null,
        Result<CampaignSeriesScoreRemediationResponse>? scoreRemediationResult = null,
        Result<TenantMemberMutationResponse>? createTenantMemberResult = null,
        Result<TenantMemberMutationResponse>? changeTenantMemberRoleResult = null,
        Result<SubjectDirectoryItemResponse>? createSubjectResult = null,
        Result<SubjectDirectoryItemResponse>? updateSubjectResult = null,
        Result<SubjectDirectoryCsvImportResponse>? importSubjectDirectoryCsvResult = null,
        Result<SubjectGroupResponse>? createSubjectGroupResult = null,
        Result<SubjectGroupMembershipResponse>? addSubjectGroupMemberResult = null,
        Result<SubjectDirectoryItemResponse>? setSubjectManagerResult = null)
        : IProductSurfaceWriteStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignSeriesId { get; private set; }

        public Guid CampaignId { get; private set; }

        public Guid TargetUserId { get; private set; }

        public Guid SubjectId { get; private set; }

        public Guid SubjectGroupId { get; private set; }

        public Guid? ActorUserId { get; private set; }

        public RenameCampaignSeriesRequest? Request { get; private set; }

        public DuplicateCampaignSeriesRequest? DuplicateRequest { get; private set; }

        public ArchiveCampaignSeriesRequest? ArchiveRequest { get; private set; }

        public CloseCampaignRequest? CloseRequest { get; private set; }

        public CreateTenantMemberRequest? CreateTenantMemberRequest { get; private set; }

        public ChangeTenantMemberRoleRequest? ChangeTenantMemberRoleRequest { get; private set; }

        public CreateSubjectRequest? CreateSubjectRequest { get; private set; }

        public UpdateSubjectRequest? UpdateSubjectRequest { get; private set; }

        public SubjectDirectoryCsvImportRequest? ImportSubjectDirectoryCsvRequest { get; private set; }

        public CreateSubjectGroupRequest? CreateSubjectGroupRequest { get; private set; }

        public AddSubjectGroupMemberRequest? AddSubjectGroupMemberRequest { get; private set; }

        public SetSubjectManagerRequest? SetSubjectManagerRequest { get; private set; }

        public int CallCount { get; private set; }

        public Task<Result<CampaignSeriesRenameResponse>> RenameCampaignSeriesAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            RenameCampaignSeriesRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            Request = request;

            return Task.FromResult(renameResult ?? Result.Failure<CampaignSeriesRenameResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesDuplicateResponse>> DuplicateCampaignSeriesAsync(
            Guid tenantId,
            Guid sourceCampaignSeriesId,
            Guid actorUserId,
            DuplicateCampaignSeriesRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = sourceCampaignSeriesId;
            ActorUserId = actorUserId;
            DuplicateRequest = request;

            return Task.FromResult(duplicateResult ?? Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesArchiveStateResponse>> ArchiveCampaignSeriesAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid actorUserId,
            ArchiveCampaignSeriesRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            ActorUserId = actorUserId;
            ArchiveRequest = request;

            return Task.FromResult(archiveResult ?? Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignSeriesArchiveStateResponse>> RestoreCampaignSeriesAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            ActorUserId = actorUserId;

            return Task.FromResult(restoreResult ?? Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<CampaignCloseStateResponse>> CloseCampaignAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid campaignId,
            Guid actorUserId,
            CloseCampaignRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            CampaignId = campaignId;
            ActorUserId = actorUserId;
            CloseRequest = request;

            return Task.FromResult(closeResult ?? Result.Failure<CampaignCloseStateResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found.")));
        }

        public Task<Result<CampaignSeriesScoreRemediationResponse>> RemediateCampaignSeriesScoresAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            ActorUserId = actorUserId;

            return Task.FromResult(scoreRemediationResult ??
                Result.Failure<CampaignSeriesScoreRemediationResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<TenantMemberMutationResponse>> CreateTenantMemberAsync(
            Guid tenantId,
            Guid actorUserId,
            CreateTenantMemberRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            ActorUserId = actorUserId;
            CreateTenantMemberRequest = request;

            return Task.FromResult(createTenantMemberResult ??
                Result.Failure<TenantMemberMutationResponse>(
                    Error.NotFound("tenant_role.not_found", "Tenant role was not found.")));
        }

        public Task<Result<TenantMemberMutationResponse>> ChangeTenantMemberRoleAsync(
            Guid tenantId,
            Guid targetUserId,
            Guid actorUserId,
            ChangeTenantMemberRoleRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            TargetUserId = targetUserId;
            ActorUserId = actorUserId;
            ChangeTenantMemberRoleRequest = request;

            return Task.FromResult(changeTenantMemberRoleResult ??
                Result.Failure<TenantMemberMutationResponse>(
                    Error.NotFound("tenant_member.not_found", "Tenant member was not found.")));
        }

        public Task<Result<SubjectDirectoryItemResponse>> CreateSubjectAsync(
            Guid tenantId,
            Guid actorUserId,
            CreateSubjectRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            ActorUserId = actorUserId;
            CreateSubjectRequest = request;

            return Task.FromResult(createSubjectResult ??
                Result.Failure<SubjectDirectoryItemResponse>(
                    Error.Validation("subject.invalid", "Subject is invalid.")));
        }

        public Task<Result<SubjectDirectoryItemResponse>> UpdateSubjectAsync(
            Guid tenantId,
            Guid subjectId,
            Guid actorUserId,
            UpdateSubjectRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            SubjectId = subjectId;
            ActorUserId = actorUserId;
            UpdateSubjectRequest = request;

            return Task.FromResult(updateSubjectResult ??
                Result.Failure<SubjectDirectoryItemResponse>(
                    Error.NotFound("subject.not_found", "Subject was not found.")));
        }

        public Task<Result<SubjectDirectoryCsvImportResponse>> ImportSubjectDirectoryCsvAsync(
            Guid tenantId,
            Guid actorUserId,
            SubjectDirectoryCsvImportRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            ActorUserId = actorUserId;
            ImportSubjectDirectoryCsvRequest = request;

            return Task.FromResult(importSubjectDirectoryCsvResult ??
                Result.Failure<SubjectDirectoryCsvImportResponse>(
                    Error.Validation("subject_directory_import.invalid", "CSV import is invalid.")));
        }

        public Task<Result<SubjectGroupResponse>> CreateSubjectGroupAsync(
            Guid tenantId,
            Guid actorUserId,
            CreateSubjectGroupRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            ActorUserId = actorUserId;
            CreateSubjectGroupRequest = request;

            return Task.FromResult(createSubjectGroupResult ??
                Result.Failure<SubjectGroupResponse>(
                    Error.Validation("subject_group.invalid", "Subject group is invalid.")));
        }

        public Task<Result<SubjectGroupMembershipResponse>> AddSubjectGroupMemberAsync(
            Guid tenantId,
            Guid groupId,
            Guid actorUserId,
            AddSubjectGroupMemberRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            SubjectGroupId = groupId;
            ActorUserId = actorUserId;
            AddSubjectGroupMemberRequest = request;

            return Task.FromResult(addSubjectGroupMemberResult ??
                Result.Failure<SubjectGroupMembershipResponse>(
                    Error.NotFound("subject_group.not_found", "Subject group was not found.")));
        }

        public Task<Result<SubjectDirectoryItemResponse>> SetSubjectManagerAsync(
            Guid tenantId,
            Guid subjectId,
            Guid actorUserId,
            SetSubjectManagerRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            TenantId = tenantId;
            SubjectId = subjectId;
            ActorUserId = actorUserId;
            SetSubjectManagerRequest = request;

            return Task.FromResult(setSubjectManagerResult ??
                Result.Failure<SubjectDirectoryItemResponse>(
                    Error.NotFound("subject.not_found", "Subject was not found.")));
        }
    }
}
