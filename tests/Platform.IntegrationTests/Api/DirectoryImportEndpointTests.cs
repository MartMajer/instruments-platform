using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.Application.Features.DirectoryImports;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class DirectoryImportEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Directory_import_workspace_endpoint_lists_connections_rules_and_runs()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var workspaceResponse = new DirectoryImportWorkspaceResponse(
            tenantId,
            [
                new DirectoryConnectionResponse(
                    connectionId,
                    "microsoft_graph",
                    "customer-tenant",
                    "Algebra sandbox",
                    "algebra.example",
                    ["User.Read.All", "Group.Read.All"],
                    "active",
                    LastSuccessfulSyncAt: null,
                    DateTimeOffset.Parse("2026-05-29T18:00:00Z"))
            ],
            [
                new DirectoryImportRuleResponse(
                    ruleId,
                    connectionId,
                    "Third year students",
                    JsonDocument.Parse("""{"departments":["Psychology"]}""").RootElement.Clone(),
                    JsonDocument.Parse("""{"fields":["displayName","mail","department"]}""").RootElement.Clone(),
                    MirrorMode: false,
                    MirrorConfirmedAt: null,
                    DateTimeOffset.Parse("2026-05-29T18:05:00Z"),
                    DateTimeOffset.Parse("2026-05-29T18:05:00Z"))
            ],
            [
                new DirectoryImportRunHistoryResponse(
                    runId,
                    ruleId,
                    "Third year students",
                    "preview",
                    "previewed",
                    DateTimeOffset.Parse("2026-05-29T18:10:00Z"),
                    FinishedAt: null,
                    JsonDocument.Parse("""{"createSubjectCount":3,"updateSubjectCount":1,"noChangeCount":2}""").RootElement.Clone())
            ]);
        var store = new FakeDirectoryImportStore(workspaceResponse: workspaceResponse);
        using var client = CreateClient(store, new FakeGraphDirectoryClient([]));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/directory-imports/workspace",
            tenantId,
            permissions: "setup.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<DirectoryImportWorkspaceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Single(payload.Connections);
        Assert.Single(payload.Rules);
        Assert.Single(payload.RecentRuns);
        Assert.Equal(tenantId, store.WorkspaceTenantId);
    }

    [Fact]
    public async Task Create_directory_connection_endpoint_binds_request_and_requires_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var connectionResponse = new DirectoryConnectionResponse(
            connectionId,
            "microsoft_graph",
            "customer-tenant",
            "Algebra sandbox",
            "algebra.example",
            ["User.Read.All", "Group.Read.All", "GroupMember.Read.All"],
            "active",
            LastSuccessfulSyncAt: null,
            DateTimeOffset.Parse("2026-05-29T18:00:00Z"));
        var store = new FakeDirectoryImportStore(connectionResponse: connectionResponse);
        using var client = CreateClient(store, new FakeGraphDirectoryClient([]));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/directory-connections",
            tenantId,
            permissions: "setup.manage");
        request.Content = JsonContent.Create(new CreateDirectoryConnectionRequest(
            "customer-tenant",
            "Algebra sandbox",
            "algebra.example",
            ["User.Read.All", "Group.Read.All", "GroupMember.Read.All"]));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<DirectoryConnectionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(connectionId, payload.Id);
        Assert.Equal(tenantId, store.CreateConnectionTenantId);
        Assert.Equal("customer-tenant", store.CreateConnectionRequest?.ExternalTenantId);
        Assert.Contains("GroupMember.Read.All", store.CreateConnectionRequest?.GrantedScopes ?? []);
    }

    [Fact]
    public async Task Create_directory_import_rule_endpoint_binds_filters_and_mirror_confirmation()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var ruleResponse = new DirectoryImportRuleResponse(
            ruleId,
            connectionId,
            "Mirror employees",
            JsonDocument.Parse("""{"accountEnabled":true}""").RootElement.Clone(),
            JsonDocument.Parse("""{"fields":["displayName","mail"]}""").RootElement.Clone(),
            MirrorMode: true,
            DateTimeOffset.Parse("2026-05-29T18:15:00Z"),
            DateTimeOffset.Parse("2026-05-29T18:15:00Z"),
            DateTimeOffset.Parse("2026-05-29T18:15:00Z"));
        var store = new FakeDirectoryImportStore(ruleResponse: ruleResponse);
        using var client = CreateClient(store, new FakeGraphDirectoryClient([]));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/directory-import-rules",
            tenantId,
            permissions: "setup.manage");
        request.Content = JsonContent.Create(new CreateDirectoryImportRuleRequest(
            connectionId,
            "Mirror employees",
            JsonDocument.Parse("""{"accountEnabled":true}""").RootElement.Clone(),
            JsonDocument.Parse("""{"fields":["displayName","mail"]}""").RootElement.Clone(),
            MirrorMode: true,
            MirrorConfirmation: "MIRROR MICROSOFT DIRECTORY"));

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<DirectoryImportRuleResponse>();
        Assert.NotNull(payload);
        Assert.Equal(ruleId, payload.Id);
        Assert.True(payload.MirrorMode);
        Assert.Equal(tenantId, store.CreateRuleTenantId);
        Assert.Equal(connectionId, store.CreateRuleRequest?.ConnectionId);
        Assert.Equal("MIRROR MICROSOFT DIRECTORY", store.CreateRuleRequest?.MirrorConfirmation);
    }

    [Fact]
    public async Task Preview_directory_import_endpoint_binds_rule_and_requires_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var response = new DirectoryImportPreviewResponse(
            Guid.NewGuid(),
            ruleId,
            "previewed",
            new DirectoryImportPreviewSummaryResponse(
                MatchedUserCount: 1,
                CreateSubjectCount: 1,
                UpdateSubjectCount: 0,
                NoChangeCount: 0,
                WarningCount: 0,
                RetainedFields: ["id", "displayName", "mail"]),
            []);
        var store = new FakeDirectoryImportStore(
            new DirectoryImportRuleExecutionContext(
                ruleId,
                connectionId,
                "customer-tenant",
                CriteriaJson: "{}",
                FieldSelectionJson: "{}",
                MirrorMode: false,
                MirrorConfirmedAt: null,
                new GraphDirectoryConnectionCredentials(
                    connectionId,
                    "customer-tenant",
                    "client-id",
                    "client-secret")),
            response);
        var graphClient = new FakeGraphDirectoryClient(
        [
            new GraphDirectoryUserCandidate(
                "graph-user-1",
                "ana@example.test",
                "ana@tenant.example",
                "Ana",
                "Psychology",
                "Researcher",
                "Faculty",
                "Zagreb",
                "en",
                AccountEnabled: true,
                "Member",
                [])
        ]);
        using var client = CreateClient(store, graphClient);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/directory-import-rules/{ruleId}/preview",
            tenantId,
            permissions: "setup.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<DirectoryImportPreviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(ruleId, payload.RuleId);
        Assert.Equal("previewed", payload.Status);
        Assert.Equal(1, payload.Summary.CreateSubjectCount);
        Assert.Equal(tenantId, store.ContextTenantId);
        Assert.Equal(ruleId, store.ContextRuleId);
        Assert.Equal(tenantId, store.PreviewTenantId);
        Assert.Equal(ruleId, store.PreviewContext?.RuleId);
        Assert.Equal("customer-tenant", graphClient.Credentials?.TenantId);
    }

    [Fact]
    public async Task Preview_directory_import_endpoint_rejects_missing_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        using var client = CreateClient(new FakeDirectoryImportStore(), new FakeGraphDirectoryClient([]));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/directory-import-rules/{ruleId}/preview",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Apply_directory_import_endpoint_binds_preview_run_and_requires_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var previewRunId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var applyContext = new DirectoryImportApplyExecutionContext(
            previewRunId,
            new DirectoryImportRuleExecutionContext(
                ruleId,
                connectionId,
                "customer-tenant",
                CriteriaJson: "{}",
                FieldSelectionJson: "{}",
                MirrorMode: false,
                MirrorConfirmedAt: null,
                new GraphDirectoryConnectionCredentials(
                    connectionId,
                    "customer-tenant",
                    "client-id",
                    "client-secret")));
        var applyResponse = new DirectoryImportApplyResponse(
            Guid.NewGuid(),
            previewRunId,
            ruleId,
            "applied",
            new DirectoryImportApplySummaryResponse(
                CreatedSubjectCount: 1,
                UpdatedSubjectCount: 0,
                NoChangeSubjectCount: 0,
                CreatedGroupCount: 1,
                AddedMembershipCount: 1,
                SetManagerCount: 0,
                WarningCount: 0));
        var store = new FakeDirectoryImportStore(
            applyContext: applyContext,
            applyResponse: applyResponse);
        var graphClient = new FakeGraphDirectoryClient(
        [
            new GraphDirectoryUserCandidate(
                "graph-user-1",
                "ana@example.test",
                "ana@tenant.example",
                "Ana",
                "Psychology",
                "Researcher",
                "Faculty",
                "Zagreb",
                "en",
                AccountEnabled: true,
                "Member",
                [])
        ]);
        using var client = CreateClient(store, graphClient);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/directory-import-runs/{previewRunId}/apply",
            tenantId,
            permissions: "setup.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content.ReadFromJsonAsync<DirectoryImportApplyResponse>();
        Assert.NotNull(payload);
        Assert.Equal(previewRunId, payload.PreviewRunId);
        Assert.Equal("applied", payload.Status);
        Assert.Equal(1, payload.Summary.CreatedSubjectCount);
        Assert.Equal(tenantId, store.ApplyContextTenantId);
        Assert.Equal(previewRunId, store.ApplyContextPreviewRunId);
        Assert.Equal(tenantId, store.ApplyTenantId);
        Assert.Equal("customer-tenant", graphClient.Credentials?.TenantId);
    }

    [Fact]
    public async Task Start_microsoft_graph_admin_consent_returns_authorization_url_with_protected_state()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeDirectoryImportStore(),
            new FakeGraphDirectoryClient([]),
            ConfigureGraphConsentOptions);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/directory-connections/microsoft-graph/admin-consent/start",
            tenantId,
            permissions: "setup.manage");

        var httpResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var payload = await httpResponse.Content
            .ReadFromJsonAsync<MicrosoftGraphAdminConsentStartResponse>();
        Assert.NotNull(payload);
        var uri = new Uri(payload.AuthorizationUrl);
        Assert.Equal("login.microsoftonline.com", uri.Host);
        Assert.Equal("/organizations/v2.0/adminconsent", uri.AbsolutePath);
        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal("graph-client-id", query["client_id"]);
        Assert.Equal("https://graph.microsoft.com/.default", query["scope"]);
        Assert.Equal(
            "https://api.example.test/directory-connections/microsoft-graph/admin-consent/callback",
            query["redirect_uri"]);
        Assert.True(query.ContainsKey("state"));
        Assert.DoesNotContain(tenantId.ToString(), query["state"].ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Microsoft_graph_admin_consent_callback_creates_connection_from_protected_state()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var connectionResponse = new DirectoryConnectionResponse(
            connectionId,
            "microsoft_graph",
            "customer-tenant-id",
            "Microsoft tenant customer-tenant-id",
            "customer-tenant-id",
            ["User.Read.All", "Group.Read.All", "GroupMember.Read.All"],
            "active",
            LastSuccessfulSyncAt: null,
            DateTimeOffset.Parse("2026-05-30T08:00:00Z"));
        var store = new FakeDirectoryImportStore(connectionResponse: connectionResponse);
        using var client = CreateClient(store, new FakeGraphDirectoryClient([]), ConfigureGraphConsentOptions);
        using var startRequest = AuthenticatedRequest(
            HttpMethod.Post,
            "/directory-connections/microsoft-graph/admin-consent/start",
            tenantId,
            permissions: "setup.manage");
        var startResponse = await client.SendAsync(startRequest);
        var startPayload = await startResponse.Content
            .ReadFromJsonAsync<MicrosoftGraphAdminConsentStartResponse>();
        Assert.NotNull(startPayload);
        var state = QueryHelpers.ParseQuery(new Uri(startPayload.AuthorizationUrl).Query)["state"].ToString();

        var callbackResponse = await client.GetAsync(
            "/directory-connections/microsoft-graph/admin-consent/callback" +
            "?admin_consent=True" +
            "&tenant=customer-tenant-id" +
            "&scope=https%3A%2F%2Fgraph.microsoft.com%2FUser.Read.All%20https%3A%2F%2Fgraph.microsoft.com%2FGroup.Read.All%20https%3A%2F%2Fgraph.microsoft.com%2FGroupMember.Read.All" +
            $"&state={Uri.EscapeDataString(state)}");

        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.Equal(
            "https://staging.example.test/app/directory?directoryConnection=connected",
            callbackResponse.Headers.Location?.ToString());
        Assert.Equal(tenantId, store.CreateConnectionTenantId);
        Assert.NotNull(store.CreateConnectionRequest);
        Assert.Equal("customer-tenant-id", store.CreateConnectionRequest.ExternalTenantId);
        Assert.Equal("Microsoft tenant customer-tenant-id", store.CreateConnectionRequest.DisplayName);
        Assert.Equal("customer-tenant-id", store.CreateConnectionRequest.PrimaryDomain);
        Assert.Equal(
            ["User.Read.All", "Group.Read.All", "GroupMember.Read.All"],
            store.CreateConnectionRequest.GrantedScopes);
    }

    private HttpClient CreateClient(
        IDirectoryImportStore store,
        IGraphDirectoryClient graphClient,
        Action<IServiceCollection>? configureServices = null)
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

                services.AddScoped(_ => store);
                services.AddScoped(_ => graphClient);
                configureServices?.Invoke(services);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static void ConfigureGraphConsentOptions(IServiceCollection services)
    {
        services.PostConfigure<MicrosoftGraphDirectoryImportOptions>(options =>
        {
            options.ClientId = "graph-client-id";
            options.ClientSecret = "graph-client-secret";
            options.AdminConsentRedirectUri =
                "https://api.example.test/directory-connections/microsoft-graph/admin-consent/callback";
            options.PostConsentRedirectUrl = "https://staging.example.test/app/directory";
        });
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

    private sealed class FakeDirectoryImportStore(
        DirectoryImportRuleExecutionContext? context = null,
        DirectoryImportPreviewResponse? response = null,
        DirectoryImportApplyExecutionContext? applyContext = null,
        DirectoryImportApplyResponse? applyResponse = null,
        DirectoryImportWorkspaceResponse? workspaceResponse = null,
        DirectoryConnectionResponse? connectionResponse = null,
        DirectoryImportRuleResponse? ruleResponse = null)
        : IDirectoryImportStore
    {
        public Guid WorkspaceTenantId { get; private set; }

        public Guid CreateConnectionTenantId { get; private set; }

        public CreateDirectoryConnectionRequest? CreateConnectionRequest { get; private set; }

        public Guid CreateRuleTenantId { get; private set; }

        public CreateDirectoryImportRuleRequest? CreateRuleRequest { get; private set; }

        public Guid ContextTenantId { get; private set; }

        public Guid ContextRuleId { get; private set; }

        public Guid PreviewTenantId { get; private set; }

        public DirectoryImportRuleExecutionContext? PreviewContext { get; private set; }

        public Guid ApplyContextTenantId { get; private set; }

        public Guid ApplyContextPreviewRunId { get; private set; }

        public Guid ApplyTenantId { get; private set; }

        public Task<Result<DirectoryImportWorkspaceResponse>> ListWorkspaceAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            WorkspaceTenantId = tenantId;

            return Task.FromResult(workspaceResponse is null
                ? Result.Failure<DirectoryImportWorkspaceResponse>(
                    Error.NotFound("directory_import_workspace.not_found", "Directory import workspace was not found."))
                : Result.Success(workspaceResponse));
        }

        public Task<Result<DirectoryConnectionResponse>> CreateConnectionAsync(
            Guid tenantId,
            CreateDirectoryConnectionRequest request,
            CancellationToken cancellationToken)
        {
            CreateConnectionTenantId = tenantId;
            CreateConnectionRequest = request;

            return Task.FromResult(connectionResponse is null
                ? Result.Failure<DirectoryConnectionResponse>(
                    Error.Validation("directory_connection.invalid", "Directory connection is not part of this test."))
                : Result.Success(connectionResponse));
        }

        public Task<Result<DirectoryImportRuleResponse>> CreateRuleAsync(
            Guid tenantId,
            CreateDirectoryImportRuleRequest request,
            CancellationToken cancellationToken)
        {
            CreateRuleTenantId = tenantId;
            CreateRuleRequest = request;

            return Task.FromResult(ruleResponse is null
                ? Result.Failure<DirectoryImportRuleResponse>(
                    Error.Validation("directory_import_rule.invalid", "Directory import rule is not part of this test."))
                : Result.Success(ruleResponse));
        }

        public Task<Result<DirectoryImportRuleExecutionContext>> GetRuleExecutionContextAsync(
            Guid tenantId,
            Guid ruleId,
            CancellationToken cancellationToken)
        {
            ContextTenantId = tenantId;
            ContextRuleId = ruleId;

            return Task.FromResult(context is null
                ? Result.Failure<DirectoryImportRuleExecutionContext>(
                    Error.NotFound("directory_import_rule.not_found", "Directory import rule was not found."))
                : Result.Success(context));
        }

        public Task<Result<DirectoryImportPreviewResponse>> SavePreviewAsync(
            Guid tenantId,
            Guid actorUserId,
            DirectoryImportRuleExecutionContext executionContext,
            DirectoryImportPlan plan,
            IReadOnlyList<GraphDirectoryUserCandidate> users,
            IReadOnlyList<GraphDirectoryManagerCandidate> managers,
            CancellationToken cancellationToken)
        {
            PreviewTenantId = tenantId;
            PreviewContext = executionContext;

            return Task.FromResult(response is null
                ? Result.Failure<DirectoryImportPreviewResponse>(
                    Error.NotFound("directory_import_rule.not_found", "Directory import rule was not found."))
                : Result.Success(response));
        }

        public Task<Result<DirectoryImportApplyExecutionContext>> GetApplyExecutionContextAsync(
            Guid tenantId,
            Guid previewRunId,
            CancellationToken cancellationToken)
        {
            ApplyContextTenantId = tenantId;
            ApplyContextPreviewRunId = previewRunId;

            return Task.FromResult(applyContext is null
                ? Result.Failure<DirectoryImportApplyExecutionContext>(
                    Error.NotFound("directory_import_run.not_found", "Directory import preview run was not found."))
                : Result.Success(applyContext));
        }

        public Task<Result<DirectoryImportApplyResponse>> ApplyPreviewAsync(
            Guid tenantId,
            Guid actorUserId,
            DirectoryImportApplyExecutionContext executionContext,
            DirectoryImportPlan plan,
            IReadOnlyList<GraphDirectoryUserCandidate> users,
            IReadOnlyList<GraphDirectoryManagerCandidate> managers,
            CancellationToken cancellationToken)
        {
            ApplyTenantId = tenantId;

            return Task.FromResult(applyResponse is null
                ? Result.Failure<DirectoryImportApplyResponse>(
                    Error.Validation("directory_import_apply.invalid", "Apply is not part of this test."))
                : Result.Success(applyResponse));
        }
    }

    private sealed class FakeGraphDirectoryClient(IReadOnlyList<GraphDirectoryUserCandidate> users)
        : IGraphDirectoryClient
    {
        public GraphDirectoryConnectionCredentials? Credentials { get; private set; }

        public Task<GraphDirectoryUserPage> ListUsersAsync(
            GraphDirectoryConnectionCredentials credentials,
            DirectoryImportPlan plan,
            CancellationToken cancellationToken)
        {
            Credentials = credentials;

            return Task.FromResult(new GraphDirectoryUserPage(users, NextLink: null));
        }

        public Task<GraphDirectoryGroupPage> ListGroupsAsync(
            GraphDirectoryConnectionCredentials credentials,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new GraphDirectoryGroupPage([], NextLink: null));
        }

        public Task<GraphDirectoryUserPage> ListGroupMembersAsync(
            GraphDirectoryConnectionCredentials credentials,
            string groupId,
            IReadOnlyList<string> selectFields,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new GraphDirectoryUserPage([], NextLink: null));
        }

        public Task<GraphDirectoryManagerCandidate?> GetManagerAsync(
            GraphDirectoryConnectionCredentials credentials,
            string userGraphId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<GraphDirectoryManagerCandidate?>(null);
        }
    }
}
