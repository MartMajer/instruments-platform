using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.DirectoryImports;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class DirectoryImportEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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

    private HttpClient CreateClient(
        IDirectoryImportStore store,
        IGraphDirectoryClient graphClient)
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

    private sealed class FakeDirectoryImportStore(
        DirectoryImportRuleExecutionContext? context = null,
        DirectoryImportPreviewResponse? response = null)
        : IDirectoryImportStore
    {
        public Guid ContextTenantId { get; private set; }

        public Guid ContextRuleId { get; private set; }

        public Guid PreviewTenantId { get; private set; }

        public DirectoryImportRuleExecutionContext? PreviewContext { get; private set; }

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
