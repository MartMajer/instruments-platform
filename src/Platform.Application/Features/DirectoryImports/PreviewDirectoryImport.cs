using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.DirectoryImports;

public sealed record PreviewDirectoryImportRequest(Guid RuleId);

public sealed record PreviewDirectoryImportCommand(PreviewDirectoryImportRequest Request)
    : IRequest<Result<DirectoryImportPreviewResponse>>;

public sealed class PreviewDirectoryImportValidator
    : AbstractValidator<PreviewDirectoryImportCommand>
{
    public PreviewDirectoryImportValidator()
    {
        RuleFor(command => command.Request.RuleId)
            .NotEmpty();
    }
}

public sealed class PreviewDirectoryImportHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IDirectoryImportStore store,
    IGraphDirectoryClient graphClient)
    : IRequestHandler<PreviewDirectoryImportCommand, Result<DirectoryImportPreviewResponse>>
{
    public async Task<Result<DirectoryImportPreviewResponse>> Handle(
        PreviewDirectoryImportCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var contextResult = await store.GetRuleExecutionContextAsync(
            currentTenant.TenantId,
            command.Request.RuleId,
            cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(contextResult.Error);
        }

        DirectoryImportPlan plan;
        try
        {
            plan = DirectoryImportRulePlanner.Plan(
                contextResult.Value.CriteriaJson,
                contextResult.Value.MirrorMode,
                contextResult.Value.MirrorConfirmedAt);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(
                Error.Validation("directory_import_rule.invalid", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(
                Error.Conflict("directory_import_rule.invalid_state", exception.Message));
        }

        IReadOnlyList<GraphDirectoryUserCandidate> users;
        IReadOnlyList<GraphDirectoryManagerCandidate> managers;
        try
        {
            users = await FetchUsersAsync(contextResult.Value.Credentials, plan, cancellationToken);
            users = ApplyLocalPostFilters(users, plan.LocalPostFilters);
            managers = await FetchManagersAsync(contextResult.Value.Credentials, users, plan, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(
                Error.Conflict(
                    "directory_import.graph_request_failed",
                    "Microsoft Graph request failed. Reconnect the directory or adjust the import rule permissions."));
        }

        return await store.SavePreviewAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            contextResult.Value,
            plan,
            users,
            managers,
            cancellationToken);
    }

    private async Task<IReadOnlyList<GraphDirectoryUserCandidate>> FetchUsersAsync(
        GraphDirectoryConnectionCredentials credentials,
        DirectoryImportPlan plan,
        CancellationToken cancellationToken)
    {
        var usersByGraphId = new Dictionary<string, GraphDirectoryUserCandidate>(StringComparer.OrdinalIgnoreCase);
        if (ShouldListUsers(plan))
        {
            var userPage = await graphClient.ListUsersAsync(credentials, plan, cancellationToken);
            AddUsers(usersByGraphId, userPage.Users);
        }

        foreach (var groupFetch in plan.GroupMemberFetches)
        {
            var groupPage = await graphClient.ListGroupMembersAsync(
                credentials,
                groupFetch.GroupId,
                plan.UserSelectFields,
                cancellationToken);
            AddUsers(usersByGraphId, groupPage.Users);
        }

        return usersByGraphId.Values.ToArray();
    }

    private async Task<IReadOnlyList<GraphDirectoryManagerCandidate>> FetchManagersAsync(
        GraphDirectoryConnectionCredentials credentials,
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        DirectoryImportPlan plan,
        CancellationToken cancellationToken)
    {
        if (plan.ManagerFetchMode == DirectoryImportManagerFetchModes.None)
        {
            return [];
        }

        var managers = new List<GraphDirectoryManagerCandidate>();
        foreach (var user in users)
        {
            var manager = await graphClient.GetManagerAsync(credentials, user.GraphUserId, cancellationToken);
            if (manager is not null)
            {
                managers.Add(manager);
            }
        }

        return managers;
    }

    private static bool ShouldListUsers(DirectoryImportPlan plan)
    {
        return plan.MirrorMode ||
            plan.GroupMemberFetches.Count == 0 ||
            !string.IsNullOrWhiteSpace(plan.UserFilter);
    }

    private static void AddUsers(
        Dictionary<string, GraphDirectoryUserCandidate> usersByGraphId,
        IReadOnlyList<GraphDirectoryUserCandidate> users)
    {
        foreach (var user in users)
        {
            usersByGraphId.TryAdd(user.GraphUserId, user);
        }
    }

    private static IReadOnlyList<GraphDirectoryUserCandidate> ApplyLocalPostFilters(
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        IReadOnlyList<DirectoryImportLocalPostFilter> localPostFilters)
    {
        IEnumerable<GraphDirectoryUserCandidate> filtered = users;
        foreach (var filter in localPostFilters)
        {
            if (filter.Kind == DirectoryImportLocalPostFilterKinds.JobTitleContains)
            {
                filtered = filtered.Where(user =>
                    user.JobTitle is not null &&
                    user.JobTitle.Contains(filter.Value, StringComparison.OrdinalIgnoreCase));
            }
        }

        return filtered.ToArray();
    }
}

public interface IDirectoryImportStore
{
    Task<Result<DirectoryImportRuleExecutionContext>> GetRuleExecutionContextAsync(
        Guid tenantId,
        Guid ruleId,
        CancellationToken cancellationToken);

    Task<Result<DirectoryImportPreviewResponse>> SavePreviewAsync(
        Guid tenantId,
        Guid actorUserId,
        DirectoryImportRuleExecutionContext executionContext,
        DirectoryImportPlan plan,
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        IReadOnlyList<GraphDirectoryManagerCandidate> managers,
        CancellationToken cancellationToken);
}

public sealed record DirectoryImportRuleExecutionContext(
    Guid RuleId,
    Guid ConnectionId,
    string ExternalTenantId,
    string CriteriaJson,
    string FieldSelectionJson,
    bool MirrorMode,
    DateTimeOffset? MirrorConfirmedAt,
    GraphDirectoryConnectionCredentials Credentials);

public sealed record DirectoryImportPreviewResponse(
    Guid RunId,
    Guid RuleId,
    string Status,
    DirectoryImportPreviewSummaryResponse Summary,
    IReadOnlyList<DirectoryImportPreviewItemResponse> Items);

public sealed record DirectoryImportPreviewSummaryResponse(
    int MatchedUserCount,
    int CreateSubjectCount,
    int UpdateSubjectCount,
    int NoChangeCount,
    int WarningCount,
    IReadOnlyList<string> RetainedFields);

public sealed record DirectoryImportPreviewItemResponse(
    string Action,
    string Status,
    string? IssueCode,
    string? DisplayName,
    string? Email);
