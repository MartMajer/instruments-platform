using Platform.Application.Auth;
using Platform.Application.Features.DirectoryImports;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.UnitTests.Application;

public sealed class ApplyDirectoryImportTests
{
    [Fact]
    public async Task Handler_requires_preview_run_and_applies_refetched_graph_candidates()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var previewRunId = Guid.NewGuid();
        var credentials = new GraphDirectoryConnectionCredentials(
            connectionId,
            "customer-tenant",
            "client-id",
            "client-secret");
        var context = new DirectoryImportApplyExecutionContext(
            previewRunId,
            new DirectoryImportRuleExecutionContext(
                ruleId,
                connectionId,
                "customer-tenant",
                CriteriaJson: """{"accountEnabled":true}""",
                FieldSelectionJson: "{}",
                MirrorMode: false,
                MirrorConfirmedAt: null,
                credentials));
        var response = new DirectoryImportApplyResponse(
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
        var handler = new ApplyDirectoryImportHandler(
            currentTenant,
            new TestCurrentActor(actorUserId),
            store,
            graphClient);

        var result = await handler.Handle(
            new ApplyDirectoryImportCommand(new ApplyDirectoryImportRequest(previewRunId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(response, result.Value);
        Assert.Equal(tenantId, store.ApplyContextTenantId);
        Assert.Equal(previewRunId, store.ApplyContextPreviewRunId);
        Assert.Equal(tenantId, store.ApplyTenantId);
        Assert.Equal(actorUserId, store.ApplyActorUserId);
        Assert.Same(context, store.ApplyContext);
        Assert.NotNull(store.ApplyPlan);
        Assert.Equal("accountEnabled eq true", store.ApplyPlan.UserFilter);
        Assert.Equal(credentials, graphClient.Credentials);
        var candidate = Assert.Single(store.ApplyUsers);
        Assert.Equal("graph-user-1", candidate.GraphUserId);
    }

    [Fact]
    public async Task Handler_rejects_apply_when_actor_is_missing()
    {
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(Guid.NewGuid(), "test");
        var store = new RecordingDirectoryImportStore();
        var handler = new ApplyDirectoryImportHandler(
            currentTenant,
            new TestCurrentActor(null),
            store,
            new RecordingGraphDirectoryClient([]));

        var result = await handler.Handle(
            new ApplyDirectoryImportCommand(new ApplyDirectoryImportRequest(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("actor.required", result.Error.Code);
        Assert.False(store.GetApplyContextCalled);
    }

    private sealed class RecordingDirectoryImportStore(
        DirectoryImportApplyExecutionContext? context = null,
        DirectoryImportApplyResponse? response = null)
        : IDirectoryImportStore
    {
        public bool GetApplyContextCalled { get; private set; }

        public Guid ApplyContextTenantId { get; private set; }

        public Guid ApplyContextPreviewRunId { get; private set; }

        public Guid ApplyTenantId { get; private set; }

        public Guid ApplyActorUserId { get; private set; }

        public DirectoryImportApplyExecutionContext? ApplyContext { get; private set; }

        public DirectoryImportPlan? ApplyPlan { get; private set; }

        public IReadOnlyList<GraphDirectoryUserCandidate> ApplyUsers { get; private set; } = [];

        public IReadOnlyList<GraphDirectoryManagerCandidate> ApplyManagers { get; private set; } = [];

        public Task<Result<DirectoryImportRuleExecutionContext>> GetRuleExecutionContextAsync(
            Guid tenantId,
            Guid ruleId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Failure<DirectoryImportRuleExecutionContext>(
                Error.NotFound("directory_import_rule.not_found", "Directory import rule was not found.")));
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
            return Task.FromResult(Result.Failure<DirectoryImportPreviewResponse>(
                Error.Validation("directory_import_preview.invalid", "Preview is not part of this test.")));
        }

        public Task<Result<DirectoryImportApplyExecutionContext>> GetApplyExecutionContextAsync(
            Guid tenantId,
            Guid previewRunId,
            CancellationToken cancellationToken)
        {
            GetApplyContextCalled = true;
            ApplyContextTenantId = tenantId;
            ApplyContextPreviewRunId = previewRunId;

            return Task.FromResult(context is null
                ? Result.Failure<DirectoryImportApplyExecutionContext>(
                    Error.NotFound("directory_import_run.not_found", "Directory import preview run was not found."))
                : Result.Success(context));
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
            ApplyActorUserId = actorUserId;
            ApplyContext = executionContext;
            ApplyPlan = plan;
            ApplyUsers = users;
            ApplyManagers = managers;

            return Task.FromResult(response is null
                ? Result.Failure<DirectoryImportApplyResponse>(
                    Error.Validation("directory_import_apply.invalid", "Apply response was not configured."))
                : Result.Success(response));
        }
    }

    private sealed class RecordingGraphDirectoryClient(IReadOnlyList<GraphDirectoryUserCandidate> users)
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

    private sealed record TestCurrentActor(Guid? UserId) : ICurrentActor
    {
        public bool IsAuthenticated => UserId.HasValue;

        public Guid? TenantId => null;

        public string? Email => null;

        public bool EmailVerificationRequired => false;

        public IReadOnlyCollection<string> Permissions => [];
    }
}
