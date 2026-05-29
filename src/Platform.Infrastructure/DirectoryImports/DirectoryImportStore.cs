using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Application.Features.DirectoryImports;
using Platform.Domain.DirectoryImports;
using Platform.Domain.Subjects;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.DirectoryImports;

public sealed class DirectoryImportStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IOptions<MicrosoftGraphDirectoryImportOptions> graphOptions)
    : IDirectoryImportStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<DirectoryImportRuleExecutionContext>> GetRuleExecutionContextAsync(
        Guid tenantId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        var options = graphOptions.Value;
        if (!options.IsConfigured)
        {
            return Result.Failure<DirectoryImportRuleExecutionContext>(
                Error.Conflict(
                    "directory_import.microsoft_graph_not_configured",
                    "Microsoft Graph directory import credentials are not configured."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var context = await (
                from rule in db.DirectoryImportRules.AsNoTracking()
                join connection in db.DirectoryConnections.AsNoTracking()
                    on new { rule.ConnectionId, rule.TenantId } equals new { ConnectionId = connection.Id, connection.TenantId }
                where rule.TenantId == tenantId &&
                    rule.Id == ruleId &&
                    rule.DeletedAt == null &&
                    connection.DeletedAt == null &&
                    connection.Status == DirectoryConnectionStatuses.Active
                select new DirectoryImportRuleExecutionContext(
                    rule.Id,
                    connection.Id,
                    connection.ExternalTenantId,
                    rule.CriteriaJson,
                    rule.FieldSelectionJson,
                    rule.MirrorMode,
                    rule.MirrorConfirmedAt,
                    new GraphDirectoryConnectionCredentials(
                        connection.Id,
                        connection.ExternalTenantId,
                        options.ClientId!,
                        options.ClientSecret!)))
            .SingleOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return context is null
            ? Result.Failure<DirectoryImportRuleExecutionContext>(
                Error.NotFound("directory_import_rule.not_found", "Directory import rule was not found."))
            : Result.Success(context);
    }

    public async Task<Result<DirectoryImportPreviewResponse>> SavePreviewAsync(
        Guid tenantId,
        Guid actorUserId,
        DirectoryImportRuleExecutionContext executionContext,
        DirectoryImportPlan plan,
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        IReadOnlyList<GraphDirectoryManagerCandidate> managers,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var ruleExists = await db.DirectoryImportRules.AnyAsync(
            rule =>
                rule.TenantId == tenantId &&
                rule.Id == executionContext.RuleId &&
                rule.DeletedAt == null,
            cancellationToken);
        if (!ruleExists)
        {
            return Result.Failure<DirectoryImportPreviewResponse>(
                Error.NotFound("directory_import_rule.not_found", "Directory import rule was not found."));
        }

        var existingSubjects = await db.Subjects
            .AsNoTracking()
            .Where(subject => subject.TenantId == tenantId && subject.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var subjectsByExternalId = existingSubjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject.ExternalId))
            .GroupBy(subject => subject.ExternalId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var run = new DirectoryImportRun(
            Guid.NewGuid(),
            tenantId,
            executionContext.RuleId,
            DirectoryImportRunModes.Preview,
            actorUserId);
        var previewItems = new List<DirectoryImportPreviewItemResponse>();
        var runItems = new List<DirectoryImportRunItem>();
        var createSubjectCount = 0;
        var updateSubjectCount = 0;
        var noChangeCount = 0;
        var warningCount = 0;

        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var externalId = BuildSubjectExternalId(executionContext.ExternalTenantId, user.GraphUserId);
            var sourceHash = HashSourceObjectId(tenantId, executionContext.ExternalTenantId, user.GraphUserId);
            var action = DirectoryImportRunItemActions.CreateSubject;
            if (subjectsByExternalId.TryGetValue(externalId, out var existingSubject))
            {
                action = SubjectDiffers(existingSubject, user)
                    ? DirectoryImportRunItemActions.UpdateSubject
                    : DirectoryImportRunItemActions.NoChange;
            }

            switch (action)
            {
                case DirectoryImportRunItemActions.CreateSubject:
                    createSubjectCount++;
                    break;
                case DirectoryImportRunItemActions.UpdateSubject:
                    updateSubjectCount++;
                    break;
                case DirectoryImportRunItemActions.NoChange:
                    noChangeCount++;
                    break;
            }

            runItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantId,
                run.Id,
                "user",
                sourceHash,
                action,
                DirectoryImportRunItemStatuses.Planned,
                safeSummaryJson: CreateUserSafeSummaryJson(index, user, action)));
            previewItems.Add(new DirectoryImportPreviewItemResponse(
                action,
                DirectoryImportRunItemStatuses.Planned,
                IssueCode: null,
                user.DisplayName,
                user.Email));

            foreach (var warning in user.Warnings)
            {
                warningCount++;
                runItems.Add(new DirectoryImportRunItem(
                    Guid.NewGuid(),
                    tenantId,
                    run.Id,
                    "user",
                    sourceHash,
                    DirectoryImportRunItemActions.Warning,
                    DirectoryImportRunItemStatuses.Warning,
                    warning.Code,
                    CreateWarningSafeSummaryJson(index, warning.Code)));
                previewItems.Add(new DirectoryImportPreviewItemResponse(
                    DirectoryImportRunItemActions.Warning,
                    DirectoryImportRunItemStatuses.Warning,
                    warning.Code,
                    user.DisplayName,
                    user.Email));
            }
        }

        foreach (var manager in managers)
        {
            var sourceHash = HashSourceObjectId(
                tenantId,
                executionContext.ExternalTenantId,
                $"{manager.UserGraphId}:{manager.ManagerGraphId}");
            runItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantId,
                run.Id,
                "manager",
                sourceHash,
                DirectoryImportRunItemActions.SetManager,
                DirectoryImportRunItemStatuses.Planned,
                safeSummaryJson: CreateManagerSafeSummaryJson()));
            previewItems.Add(new DirectoryImportPreviewItemResponse(
                DirectoryImportRunItemActions.SetManager,
                DirectoryImportRunItemStatuses.Planned,
                IssueCode: null,
                manager.ManagerDisplayName,
                manager.ManagerEmail));
        }

        var summary = new DirectoryImportPreviewSummaryResponse(
            users.Count,
            createSubjectCount,
            updateSubjectCount,
            noChangeCount,
            warningCount,
            plan.UserSelectFields);
        run.MarkPreviewed(CreateSummaryJson(summary), DateTimeOffset.UtcNow);

        db.DirectoryImportRuns.Add(run);
        db.DirectoryImportRunItems.AddRange(runItems);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new DirectoryImportPreviewResponse(
            run.Id,
            executionContext.RuleId,
            run.Status,
            summary,
            previewItems));
    }

    public async Task<Result<DirectoryImportApplyExecutionContext>> GetApplyExecutionContextAsync(
        Guid tenantId,
        Guid previewRunId,
        CancellationToken cancellationToken)
    {
        var options = graphOptions.Value;
        if (!options.IsConfigured)
        {
            return Result.Failure<DirectoryImportApplyExecutionContext>(
                Error.Conflict(
                    "directory_import.microsoft_graph_not_configured",
                    "Microsoft Graph directory import credentials are not configured."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var context = await (
                from run in db.DirectoryImportRuns.AsNoTracking()
                join rule in db.DirectoryImportRules.AsNoTracking()
                    on new { RuleId = run.RuleId, run.TenantId } equals new { RuleId = rule.Id, rule.TenantId }
                join connection in db.DirectoryConnections.AsNoTracking()
                    on new { rule.ConnectionId, rule.TenantId } equals new { ConnectionId = connection.Id, connection.TenantId }
                where run.TenantId == tenantId &&
                    run.Id == previewRunId &&
                    run.Mode == DirectoryImportRunModes.Preview &&
                    run.Status == DirectoryImportRunStatuses.Previewed &&
                    rule.DeletedAt == null &&
                    connection.DeletedAt == null &&
                    connection.Status == DirectoryConnectionStatuses.Active
                select new DirectoryImportApplyExecutionContext(
                    run.Id,
                    new DirectoryImportRuleExecutionContext(
                        rule.Id,
                        connection.Id,
                        connection.ExternalTenantId,
                        rule.CriteriaJson,
                        rule.FieldSelectionJson,
                        rule.MirrorMode,
                        rule.MirrorConfirmedAt,
                        new GraphDirectoryConnectionCredentials(
                            connection.Id,
                            connection.ExternalTenantId,
                            options.ClientId!,
                            options.ClientSecret!))))
            .SingleOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return context is null
            ? Result.Failure<DirectoryImportApplyExecutionContext>(
                Error.NotFound("directory_import_run.not_found", "Directory import preview run was not found."))
            : Result.Success(context);
    }

    public async Task<Result<DirectoryImportApplyResponse>> ApplyPreviewAsync(
        Guid tenantId,
        Guid actorUserId,
        DirectoryImportApplyExecutionContext executionContext,
        DirectoryImportPlan plan,
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        IReadOnlyList<GraphDirectoryManagerCandidate> managers,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var previewRunExists = await db.DirectoryImportRuns.AnyAsync(
            run =>
                run.TenantId == tenantId &&
                run.Id == executionContext.PreviewRunId &&
                run.Mode == DirectoryImportRunModes.Preview &&
                run.Status == DirectoryImportRunStatuses.Previewed,
            cancellationToken);
        if (!previewRunExists)
        {
            return Result.Failure<DirectoryImportApplyResponse>(
                Error.NotFound("directory_import_run.not_found", "Directory import preview run was not found."));
        }

        var run = new DirectoryImportRun(
            Guid.NewGuid(),
            tenantId,
            executionContext.RuleContext.RuleId,
            DirectoryImportRunModes.Apply,
            actorUserId);
        run.StartApplying(DateTimeOffset.UtcNow);

        var subjects = await db.Subjects
            .Where(subject => subject.TenantId == tenantId && subject.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var subjectsByExternalId = subjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject.ExternalId))
            .GroupBy(subject => subject.ExternalId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var subjectsByGraphId = new Dictionary<string, Subject>(StringComparer.OrdinalIgnoreCase);
        var runItems = new List<DirectoryImportRunItem>();
        var createdSubjectCount = 0;
        var updatedSubjectCount = 0;
        var noChangeSubjectCount = 0;
        var createdGroupCount = 0;
        var addedMembershipCount = 0;
        var setManagerCount = 0;
        var warningCount = 0;

        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var externalId = BuildSubjectExternalId(executionContext.RuleContext.ExternalTenantId, user.GraphUserId);
            var sourceHash = HashSourceObjectId(tenantId, executionContext.RuleContext.ExternalTenantId, user.GraphUserId);
            var attributes = BuildSubjectAttributesJson(user);
            var locale = MapLocale(user.PreferredLanguage);
            var action = DirectoryImportRunItemActions.NoChange;

            if (!subjectsByExternalId.TryGetValue(externalId, out var subject))
            {
                subject = new Subject(
                    Guid.NewGuid(),
                    tenantId,
                    externalId: externalId,
                    email: user.Email,
                    displayName: user.DisplayName,
                    locale: locale,
                    attributes: attributes);
                db.Subjects.Add(subject);
                subjectsByExternalId[externalId] = subject;
                createdSubjectCount++;
                action = DirectoryImportRunItemActions.CreateSubject;
            }
            else if (SubjectDiffers(subject, user) || !string.Equals(subject.Locale, locale, StringComparison.Ordinal))
            {
                subject.ChangeDirectoryProfile(
                    user.DisplayName,
                    user.Email,
                    externalId,
                    locale,
                    attributes);
                updatedSubjectCount++;
                action = DirectoryImportRunItemActions.UpdateSubject;
            }
            else
            {
                noChangeSubjectCount++;
            }

            subjectsByGraphId[user.GraphUserId] = subject;
            runItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantId,
                run.Id,
                "user",
                sourceHash,
                action,
                action == DirectoryImportRunItemActions.NoChange
                    ? DirectoryImportRunItemStatuses.Skipped
                    : DirectoryImportRunItemStatuses.Applied,
                safeSummaryJson: CreateUserSafeSummaryJson(index, user, action)));

            foreach (var warning in user.Warnings)
            {
                warningCount++;
                runItems.Add(new DirectoryImportRunItem(
                    Guid.NewGuid(),
                    tenantId,
                    run.Id,
                    "user",
                    sourceHash,
                    DirectoryImportRunItemActions.Warning,
                    DirectoryImportRunItemStatuses.Warning,
                    warning.Code,
                    CreateWarningSafeSummaryJson(index, warning.Code)));
            }
        }

        (createdGroupCount, addedMembershipCount) = await ApplyDepartmentGroupsAsync(
            tenantId,
            executionContext.RuleContext.ExternalTenantId,
            users,
            subjectsByGraphId,
            run.Id,
            runItems,
            cancellationToken);

        setManagerCount = await ApplyManagersAsync(
            tenantId,
            executionContext.RuleContext.ExternalTenantId,
            managers,
            subjectsByGraphId,
            run.Id,
            runItems,
            cancellationToken);

        var summary = new DirectoryImportApplySummaryResponse(
            createdSubjectCount,
            updatedSubjectCount,
            noChangeSubjectCount,
            createdGroupCount,
            addedMembershipCount,
            setManagerCount,
            warningCount);
        run.MarkApplied(CreateSummaryJson(summary), DateTimeOffset.UtcNow);

        var connection = await db.DirectoryConnections.SingleOrDefaultAsync(
            item =>
                item.TenantId == tenantId &&
                item.Id == executionContext.RuleContext.ConnectionId &&
                item.DeletedAt == null,
            cancellationToken);
        connection?.MarkSuccessfulSync(DateTimeOffset.UtcNow);

        db.DirectoryImportRuns.Add(run);
        db.DirectoryImportRunItems.AddRange(runItems);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new DirectoryImportApplyResponse(
            run.Id,
            executionContext.PreviewRunId,
            executionContext.RuleContext.RuleId,
            run.Status,
            summary));
    }

    public static string BuildSubjectExternalId(string externalTenantId, string graphUserId)
    {
        return $"msgraph:{externalTenantId}:{graphUserId}";
    }

    private async Task<(int CreatedGroupCount, int AddedMembershipCount)> ApplyDepartmentGroupsAsync(
        Guid tenantId,
        string externalTenantId,
        IReadOnlyList<GraphDirectoryUserCandidate> users,
        IReadOnlyDictionary<string, Subject> subjectsByGraphId,
        Guid runId,
        List<DirectoryImportRunItem> runItems,
        CancellationToken cancellationToken)
    {
        var existingGroups = await db.SubjectGroups
            .Where(group => group.TenantId == tenantId && group.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var groupsByKey = existingGroups
            .GroupBy(group => SubjectGroupImportKey(group.Type, group.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var membershipKeys = await (
                from membership in db.SubjectMemberships
                join subjectGroup in db.SubjectGroups on membership.GroupId equals subjectGroup.Id
                where subjectGroup.TenantId == tenantId && subjectGroup.DeletedAt == null
                select new SubjectMembershipImportKey(membership.SubjectId, membership.GroupId))
            .ToListAsync(cancellationToken);
        var memberships = membershipKeys.ToHashSet();
        var createdGroupCount = 0;
        var addedMembershipCount = 0;

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Department) ||
                !subjectsByGraphId.TryGetValue(user.GraphUserId, out var subject))
            {
                continue;
            }

            var groupKey = SubjectGroupImportKey(SubjectGroupTypes.Department, user.Department);
            if (!groupsByKey.TryGetValue(groupKey, out var group))
            {
                group = new SubjectGroup(
                    Guid.NewGuid(),
                    tenantId,
                    SubjectGroupTypes.Department,
                    user.Department);
                db.SubjectGroups.Add(group);
                groupsByKey[groupKey] = group;
                createdGroupCount++;
                runItems.Add(new DirectoryImportRunItem(
                    Guid.NewGuid(),
                    tenantId,
                    runId,
                    "department",
                    HashSourceObjectId(tenantId, externalTenantId, $"department:{user.Department}"),
                    DirectoryImportRunItemActions.CreateGroup,
                    DirectoryImportRunItemStatuses.Applied,
                    safeSummaryJson: CreateGroupSafeSummaryJson(SubjectGroupTypes.Department)));
            }

            var membershipKey = new SubjectMembershipImportKey(subject.Id, group.Id);
            if (memberships.Contains(membershipKey))
            {
                continue;
            }

            db.SubjectMemberships.Add(new SubjectMembership(
                subject.Id,
                group.Id,
                SubjectGroupRoles.Member));
            memberships.Add(membershipKey);
            addedMembershipCount++;
            runItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantId,
                runId,
                "department_membership",
                HashSourceObjectId(tenantId, externalTenantId, $"{user.GraphUserId}:department:{user.Department}"),
                DirectoryImportRunItemActions.AddMembership,
                DirectoryImportRunItemStatuses.Applied,
                safeSummaryJson: CreateMembershipSafeSummaryJson(SubjectGroupTypes.Department)));
        }

        return (createdGroupCount, addedMembershipCount);
    }

    private async Task<int> ApplyManagersAsync(
        Guid tenantId,
        string externalTenantId,
        IReadOnlyList<GraphDirectoryManagerCandidate> managers,
        IReadOnlyDictionary<string, Subject> subjectsByGraphId,
        Guid runId,
        List<DirectoryImportRunItem> runItems,
        CancellationToken cancellationToken)
    {
        var activeManagerRelationships = await db.SubjectRelationships
            .Where(relationship =>
                relationship.TenantId == tenantId &&
                relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                relationship.ValidTo == null)
            .ToListAsync(cancellationToken);
        var setManagerCount = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var manager in managers)
        {
            if (!subjectsByGraphId.TryGetValue(manager.UserGraphId, out var employee) ||
                !subjectsByGraphId.TryGetValue(manager.ManagerGraphId, out var managerSubject))
            {
                continue;
            }

            var existingForEmployee = activeManagerRelationships
                .Where(relationship => relationship.RelatedSubjectId == employee.Id)
                .ToList();
            if (existingForEmployee.Any(relationship => relationship.SubjectId == managerSubject.Id))
            {
                continue;
            }

            foreach (var existingRelationship in existingForEmployee)
            {
                existingRelationship.End(today);
            }

            var relationship = new SubjectRelationship(
                Guid.NewGuid(),
                tenantId,
                managerSubject.Id,
                employee.Id,
                SubjectRelationshipTypes.ManagerOf,
                validFrom: today);
            db.SubjectRelationships.Add(relationship);
            activeManagerRelationships.Add(relationship);
            setManagerCount++;
            runItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantId,
                runId,
                "manager",
                HashSourceObjectId(tenantId, externalTenantId, $"{manager.UserGraphId}:{manager.ManagerGraphId}"),
                DirectoryImportRunItemActions.SetManager,
                DirectoryImportRunItemStatuses.Applied,
                safeSummaryJson: CreateManagerSafeSummaryJson()));
        }

        return setManagerCount;
    }

    private static bool SubjectDiffers(Subject subject, GraphDirectoryUserCandidate user)
    {
        return !string.Equals(subject.DisplayName, user.DisplayName, StringComparison.Ordinal) ||
            !string.Equals(subject.Email, user.Email, StringComparison.OrdinalIgnoreCase) ||
            SubjectAttributesDiffer(subject.Attributes, user);
    }

    private static bool SubjectAttributesDiffer(string attributesJson, GraphDirectoryUserCandidate user)
    {
        using var document = JsonDocument.Parse(attributesJson);
        var root = document.RootElement;

        return !StringPropertyEquals(root, "department", user.Department) ||
            !StringPropertyEquals(root, "job_title", user.JobTitle) ||
            !StringPropertyEquals(root, "employee_type", user.EmployeeType) ||
            !StringPropertyEquals(root, "office_location", user.OfficeLocation) ||
            !StringPropertyEquals(root, "msgraph_user_type", user.UserType);
    }

    private static string BuildSubjectAttributesJson(GraphDirectoryUserCandidate user)
    {
        var attributes = new Dictionary<string, string>();
        AddIfPresent(attributes, "department", user.Department);
        AddIfPresent(attributes, "job_title", user.JobTitle);
        AddIfPresent(attributes, "employee_type", user.EmployeeType);
        AddIfPresent(attributes, "office_location", user.OfficeLocation);
        AddIfPresent(attributes, "msgraph_user_type", user.UserType);

        return JsonSerializer.Serialize(attributes, JsonOptions);
    }

    private static void AddIfPresent(Dictionary<string, string> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[key] = value;
        }
    }

    private static string MapLocale(string? preferredLanguage)
    {
        var normalized = string.IsNullOrWhiteSpace(preferredLanguage)
            ? "en"
            : preferredLanguage.Trim().ToLowerInvariant();

        return normalized.Length <= 16 ? normalized : normalized[..16];
    }

    private static bool StringPropertyEquals(JsonElement root, string propertyName, string? expected)
    {
        var normalizedExpected = string.IsNullOrWhiteSpace(expected) ? null : expected;
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null ||
            property.ValueKind == JsonValueKind.Undefined)
        {
            return normalizedExpected is null;
        }

        return property.ValueKind == JsonValueKind.String &&
            string.Equals(property.GetString(), normalizedExpected, StringComparison.Ordinal);
    }

    private static string HashSourceObjectId(Guid tenantId, string externalTenantId, string sourceObjectId)
    {
        var material = Encoding.UTF8.GetBytes($"{tenantId:N}\n{externalTenantId}\n{sourceObjectId}");
        return "sha256:" + Convert.ToHexString(SHA256.HashData(material)).ToLowerInvariant();
    }

    private static string CreateSummaryJson(DirectoryImportPreviewSummaryResponse summary)
    {
        return JsonSerializer.Serialize(new
        {
            summary.MatchedUserCount,
            summary.CreateSubjectCount,
            summary.UpdateSubjectCount,
            summary.NoChangeCount,
            summary.WarningCount,
            summary.RetainedFields
        }, JsonOptions);
    }

    private static string CreateSummaryJson(DirectoryImportApplySummaryResponse summary)
    {
        return JsonSerializer.Serialize(new
        {
            summary.CreatedSubjectCount,
            summary.UpdatedSubjectCount,
            summary.NoChangeSubjectCount,
            summary.CreatedGroupCount,
            summary.AddedMembershipCount,
            summary.SetManagerCount,
            summary.WarningCount
        }, JsonOptions);
    }

    private static string CreateUserSafeSummaryJson(
        int candidateIndex,
        GraphDirectoryUserCandidate user,
        string action)
    {
        return JsonSerializer.Serialize(new
        {
            CandidateIndex = candidateIndex,
            Action = action,
            HasEmail = !string.IsNullOrWhiteSpace(user.Email),
            HasDisplayName = !string.IsNullOrWhiteSpace(user.DisplayName),
            HasDepartment = !string.IsNullOrWhiteSpace(user.Department),
            HasJobTitle = !string.IsNullOrWhiteSpace(user.JobTitle),
            WarningCount = user.Warnings.Count
        }, JsonOptions);
    }

    private static string CreateWarningSafeSummaryJson(int candidateIndex, string warningCode)
    {
        return JsonSerializer.Serialize(new
        {
            CandidateIndex = candidateIndex,
            WarningCode = warningCode
        }, JsonOptions);
    }

    private static string CreateManagerSafeSummaryJson()
    {
        return JsonSerializer.Serialize(new
        {
            Relationship = "manager"
        }, JsonOptions);
    }

    private static string CreateGroupSafeSummaryJson(string groupType)
    {
        return JsonSerializer.Serialize(new
        {
            GroupType = groupType
        }, JsonOptions);
    }

    private static string CreateMembershipSafeSummaryJson(string groupType)
    {
        return JsonSerializer.Serialize(new
        {
            GroupType = groupType
        }, JsonOptions);
    }

    private static string SubjectGroupImportKey(string type, string name)
    {
        return $"{type.Trim().ToLowerInvariant()}|{name.Trim().ToLowerInvariant()}";
    }

    private sealed record SubjectMembershipImportKey(Guid SubjectId, Guid GroupId);
}
