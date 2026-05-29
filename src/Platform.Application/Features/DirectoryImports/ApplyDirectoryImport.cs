using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.DirectoryImports;

public sealed record ApplyDirectoryImportRequest(Guid PreviewRunId);

public sealed record ApplyDirectoryImportCommand(ApplyDirectoryImportRequest Request)
    : IRequest<Result<DirectoryImportApplyResponse>>;

public sealed class ApplyDirectoryImportValidator
    : AbstractValidator<ApplyDirectoryImportCommand>
{
    public ApplyDirectoryImportValidator()
    {
        RuleFor(command => command.Request.PreviewRunId)
            .NotEmpty();
    }
}

public sealed class ApplyDirectoryImportHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IDirectoryImportStore store,
    IGraphDirectoryClient graphClient)
    : IRequestHandler<ApplyDirectoryImportCommand, Result<DirectoryImportApplyResponse>>
{
    public async Task<Result<DirectoryImportApplyResponse>> Handle(
        ApplyDirectoryImportCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<DirectoryImportApplyResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var contextResult = await store.GetApplyExecutionContextAsync(
            currentTenant.TenantId,
            command.Request.PreviewRunId,
            cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure<DirectoryImportApplyResponse>(contextResult.Error);
        }

        DirectoryImportPlan plan;
        try
        {
            plan = DirectoryImportRulePlanner.Plan(
                contextResult.Value.RuleContext.CriteriaJson,
                contextResult.Value.RuleContext.MirrorMode,
                contextResult.Value.RuleContext.MirrorConfirmedAt);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<DirectoryImportApplyResponse>(
                Error.Validation("directory_import_rule.invalid", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<DirectoryImportApplyResponse>(
                Error.Conflict("directory_import_rule.invalid_state", exception.Message));
        }

        IReadOnlyList<GraphDirectoryUserCandidate> users;
        IReadOnlyList<GraphDirectoryManagerCandidate> managers;
        try
        {
            users = await FetchUsersAsync(contextResult.Value.RuleContext.Credentials, plan, cancellationToken);
            users = ApplyLocalPostFilters(users, plan.LocalPostFilters);
            managers = await FetchManagersAsync(
                contextResult.Value.RuleContext.Credentials,
                users,
                plan,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<DirectoryImportApplyResponse>(
                Error.Conflict(
                    "directory_import.graph_request_failed",
                    "Microsoft Graph request failed. Reconnect the directory or adjust the import rule permissions."));
        }

        return await store.ApplyPreviewAsync(
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

public sealed record DirectoryImportApplyExecutionContext(
    Guid PreviewRunId,
    DirectoryImportRuleExecutionContext RuleContext);

public sealed record DirectoryImportApplyResponse(
    Guid RunId,
    Guid PreviewRunId,
    Guid RuleId,
    string Status,
    DirectoryImportApplySummaryResponse Summary);

public sealed record DirectoryImportApplySummaryResponse(
    int CreatedSubjectCount,
    int UpdatedSubjectCount,
    int NoChangeSubjectCount,
    int CreatedGroupCount,
    int AddedMembershipCount,
    int SetManagerCount,
    int WarningCount);
