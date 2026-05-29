using Platform.Application.Auth;
using Platform.Application.Features.DirectoryImports;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.UnitTests.Application;

public sealed class PreviewDirectoryImportTests
{
    [Fact]
    public async Task Handler_fetches_graph_candidates_and_persists_preview_under_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var credentials = new GraphDirectoryConnectionCredentials(
            connectionId,
            "customer-tenant",
            "client-id",
            "client-secret");
        var context = new DirectoryImportRuleExecutionContext(
            ruleId,
            connectionId,
            "customer-tenant",
            CriteriaJson: """{"accountEnabled":true}""",
            FieldSelectionJson: "{}",
            MirrorMode: false,
            MirrorConfirmedAt: null,
            credentials);
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
        var store = new RecordingDirectoryImportStore(context, response);
        var graphClient = new RecordingGraphDirectoryClient(
        [
            new GraphDirectoryUserCandidate(
                "graph-user-1",
                "ana@example.edu",
                "ana@tenant.example",
                "Ana",
                "Psychology",
                "Researcher",
                "Faculty",
                "Zagreb",
                "hr-HR",
                AccountEnabled: true,
                "Member",
                [])
        ]);
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var handler = new PreviewDirectoryImportHandler(
            currentTenant,
            new TestCurrentActor(actorUserId),
            store,
            graphClient);

        var result = await handler.Handle(
            new PreviewDirectoryImportCommand(new PreviewDirectoryImportRequest(ruleId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(response, result.Value);
        Assert.Equal(tenantId, store.ContextTenantId);
        Assert.Equal(ruleId, store.ContextRuleId);
        Assert.Equal(tenantId, store.PreviewTenantId);
        Assert.Equal(actorUserId, store.PreviewActorUserId);
        Assert.Same(context, store.PreviewContext);
        Assert.NotNull(store.PreviewPlan);
        Assert.Equal("accountEnabled eq true", store.PreviewPlan.UserFilter);
        Assert.Equal(credentials, graphClient.Credentials);
        Assert.Same(store.PreviewPlan, graphClient.Plan);
        var candidate = Assert.Single(store.PreviewUsers);
        Assert.Equal("graph-user-1", candidate.GraphUserId);
    }

    [Fact]
    public async Task Handler_rejects_preview_when_actor_is_missing()
    {
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(Guid.NewGuid(), "test");
        var store = new RecordingDirectoryImportStore();
        var handler = new PreviewDirectoryImportHandler(
            currentTenant,
            new TestCurrentActor(null),
            store,
            new RecordingGraphDirectoryClient([]));

        var result = await handler.Handle(
            new PreviewDirectoryImportCommand(new PreviewDirectoryImportRequest(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("actor.required", result.Error.Code);
        Assert.False(store.GetContextCalled);
    }

    private sealed class RecordingDirectoryImportStore(
        DirectoryImportRuleExecutionContext? context = null,
        DirectoryImportPreviewResponse? response = null)
        : IDirectoryImportStore
    {
        public bool GetContextCalled { get; private set; }

        public Guid ContextTenantId { get; private set; }

        public Guid ContextRuleId { get; private set; }

        public Guid PreviewTenantId { get; private set; }

        public Guid PreviewActorUserId { get; private set; }

        public DirectoryImportRuleExecutionContext? PreviewContext { get; private set; }

        public DirectoryImportPlan? PreviewPlan { get; private set; }

        public IReadOnlyList<GraphDirectoryUserCandidate> PreviewUsers { get; private set; } = [];

        public IReadOnlyList<GraphDirectoryManagerCandidate> PreviewManagers { get; private set; } = [];

        public Task<Result<DirectoryImportRuleExecutionContext>> GetRuleExecutionContextAsync(
            Guid tenantId,
            Guid ruleId,
            CancellationToken cancellationToken)
        {
            GetContextCalled = true;
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
            PreviewActorUserId = actorUserId;
            PreviewContext = executionContext;
            PreviewPlan = plan;
            PreviewUsers = users;
            PreviewManagers = managers;

            return Task.FromResult(response is null
                ? Result.Failure<DirectoryImportPreviewResponse>(
                    Error.Validation("directory_import_preview.invalid", "Preview response was not configured."))
                : Result.Success(response));
        }
    }

    private sealed class RecordingGraphDirectoryClient(IReadOnlyList<GraphDirectoryUserCandidate> users)
        : IGraphDirectoryClient
    {
        public GraphDirectoryConnectionCredentials? Credentials { get; private set; }

        public DirectoryImportPlan? Plan { get; private set; }

        public Task<GraphDirectoryUserPage> ListUsersAsync(
            GraphDirectoryConnectionCredentials credentials,
            DirectoryImportPlan plan,
            CancellationToken cancellationToken)
        {
            Credentials = credentials;
            Plan = plan;

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

    private sealed record TestCurrentActor(Guid? UserId) : ICurrentActor
    {
        public bool IsAuthenticated => UserId.HasValue;

        public Guid? TenantId => null;

        public string? Email => null;

        public bool EmailVerificationRequired => false;

        public IReadOnlyCollection<string> Permissions => [];
    }
}
