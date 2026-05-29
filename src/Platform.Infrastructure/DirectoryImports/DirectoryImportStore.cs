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

    public static string BuildSubjectExternalId(string externalTenantId, string graphUserId)
    {
        return $"msgraph:{externalTenantId}:{graphUserId}";
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
}
