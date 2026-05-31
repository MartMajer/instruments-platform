using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Campaigns;
using Platform.Domain.Subjects;
using Platform.Infrastructure.Data;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Campaigns.RespondentRules;

public sealed class RespondentRuleResolver(ApplicationDbContext db)
{
    public const string Self = "self";
    public const string AllActivePeople = "all_active_people";
    public const string SelectedPeople = "selected_people";
    public const string AllInGroup = "all_in_group";
    public const string ManagerOfTarget = "manager_of_target";
    public const string ReportsOfTarget = "reports_of_target";
    public const string ExternalEmails = "external_emails";
    private const string SampleStudyAttributeProbe = """{"sample_study":true}""";
    private const int MaxExternalEmailRecipients = 500;
    private const int MaxSelectedSubjectIds = 1000;
    private const int MaxSelectedGroupIds = 200;

    public async Task<Result<RespondentRuleResolution>> ResolveAsync(
        RespondentRuleResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var campaignExists = await db.Campaigns
            .AsNoTracking()
            .AnyAsync(
                campaign =>
                    campaign.TenantId == request.TenantId &&
                    campaign.Id == request.CampaignId &&
                    (!request.CampaignSeriesId.HasValue ||
                        campaign.CampaignSeriesId == request.CampaignSeriesId.Value),
                cancellationToken);
        if (!campaignExists)
        {
            return Result.Failure<RespondentRuleResolution>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var parsed = ParseRule(request.Rule);
        if (parsed.IsFailure)
        {
            return Result.Failure<RespondentRuleResolution>(parsed.Error);
        }

        var rule = parsed.Value;
        var targetSubjectIds = MergeSelectedIds(request.TargetSubjectId, rule.TargetSubjectIds);
        var groupIds = MergeSelectedIds(request.GroupId, rule.GroupIds);
        var issues = new List<RespondentRuleResolutionIssue>();
        var candidatesResult = rule.Kind switch
        {
            Self => await ResolveSelfAsync(
                request.TenantId,
                request.CampaignId,
                issues,
                cancellationToken),
            AllActivePeople => await ResolveAllActivePeopleAsync(
                request.TenantId,
                issues,
                cancellationToken),
            SelectedPeople => await ResolveSelectedPeopleAsync(
                request.TenantId,
                rule.SubjectIds,
                cancellationToken),
            AllInGroup => await ResolveAllInGroupAsync(
                request.TenantId,
                groupIds,
                cancellationToken),
            ManagerOfTarget => await ResolveManagerOfTargetAsync(
                request.TenantId,
                targetSubjectIds,
                rule.TargetGroupIds,
                issues,
                cancellationToken),
            ReportsOfTarget => await ResolveReportsOfTargetAsync(
                request.TenantId,
                targetSubjectIds,
                rule.TargetGroupIds,
                issues,
                cancellationToken),
            ExternalEmails => ResolveExternalEmails(rule.ExternalEmails),
            _ => Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.Validation(
                    "respondent_rule_preview.unsupported_kind",
                    "Respondent rule kind is not supported for preview."))
        };
        if (candidatesResult.IsFailure)
        {
            return Result.Failure<RespondentRuleResolution>(candidatesResult.Error);
        }

        var orderedCandidates = candidatesResult.Value
            .GroupBy(
                candidate => new
                {
                    TargetId = candidate.Target?.Id,
                    RespondentId = candidate.Respondent.Id
                })
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Target?.Label ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Respondent.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Target?.Id ?? Guid.Empty)
            .ThenBy(candidate => candidate.Respondent.Id)
            .ToArray();

        if (orderedCandidates.Length == 0)
        {
            issues.Add(CreateEmptyWarning(
                rule.Kind,
                targetSubjectIds.Count == 1 ? targetSubjectIds[0] : null,
                groupIds.Count == 1 ? groupIds[0] : null));
        }

        return Result.Success(new RespondentRuleResolution(
            rule.Kind,
            rule.Role,
            targetSubjectIds.Count == 1 ? targetSubjectIds[0] : null,
            groupIds.Count == 1 ? groupIds[0] : null,
            orderedCandidates,
            issues));
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveSelfAsync(
        Guid tenantId,
        Guid campaignId,
        List<RespondentRuleResolutionIssue> issues,
        CancellationToken cancellationToken)
    {
        var audienceSubjectIds = await (
                from audience in db.Audiences.AsNoTracking()
                join member in db.AudienceMembers.AsNoTracking()
                    on audience.Id equals member.AudienceId
                join subject in db.Subjects.AsNoTracking()
                    on member.SubjectId equals subject.Id
                where audience.CampaignId == campaignId &&
                    member.RemovedAt == null &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null &&
                    !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe)
                select subject.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (audienceSubjectIds.Count == 0)
        {
            issues.Add(new RespondentRuleResolutionIssue(
                "respondent_rule_preview.audience_missing",
                "warning",
                "Campaign audience has no active members; preview uses all active tenant subjects."));
        }

        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(subject =>
                subject.TenantId == tenantId &&
                subject.DeletedAt == null &&
                !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe) &&
                (audienceSubjectIds.Count == 0 || audienceSubjectIds.Contains(subject.Id)))
            .Select(subject => new RespondentRuleSubjectRow(
                subject.Id,
                subject.DisplayName,
                subject.Email,
                subject.ExternalId,
                subject.Locale))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            subjects
                .Select(subject =>
                {
                    var responseSubject = CreateSubject(subject);

                    return new RespondentRuleCandidate(responseSubject, responseSubject);
                })
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveAllActivePeopleAsync(
        Guid tenantId,
        List<RespondentRuleResolutionIssue> issues,
        CancellationToken cancellationToken)
    {
        issues.Add(new RespondentRuleResolutionIssue(
            "respondent_rule_preview.all_active_people",
            "warning",
            "This selection includes every active Directory person in the workspace."));

        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(subject =>
                subject.TenantId == tenantId &&
                subject.DeletedAt == null &&
                !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe))
            .Select(subject => new RespondentRuleSubjectRow(
                subject.Id,
                subject.DisplayName,
                subject.Email,
                subject.ExternalId,
                subject.Locale))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            subjects
                .Select(subject =>
                {
                    var responseSubject = CreateSubject(subject);

                    return new RespondentRuleCandidate(responseSubject, responseSubject);
                })
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveSelectedPeopleAsync(
        Guid tenantId,
        IReadOnlyList<Guid> subjectIds,
        CancellationToken cancellationToken)
    {
        if (subjectIds.Count == 0)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.Validation(
                    "respondent_rule_preview.subjects_required",
                    "At least one Directory person is required for this recipient selection."));
        }

        var subjectsResult = await LoadSubjectsByIdsAsync(
            tenantId,
            subjectIds,
            "subject.not_found",
            "One or more selected people were not found.",
            cancellationToken);
        if (subjectsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(subjectsResult.Error);
        }

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            subjectsResult.Value
                .Select(subject =>
                {
                    var responseSubject = CreateSubject(subject);

                    return new RespondentRuleCandidate(responseSubject, responseSubject);
                })
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveAllInGroupAsync(
        Guid tenantId,
        IReadOnlyList<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.Validation(
                    "respondent_rule_preview.group_required",
                    "A subject group is required for this preview rule."));
        }

        var foundGroupIds = await db.SubjectGroups
            .AsNoTracking()
            .Where(group =>
                group.TenantId == tenantId &&
                groupIds.Contains(group.Id) &&
                group.DeletedAt == null &&
                !EF.Functions.JsonContains(group.Attributes, SampleStudyAttributeProbe))
            .Select(group => group.Id)
            .ToListAsync(cancellationToken);
        if (foundGroupIds.Count != groupIds.Count)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.NotFound("subject_group.not_found", "Subject group was not found."));
        }

        var subjects = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subject in db.Subjects.AsNoTracking()
                    on membership.SubjectId equals subject.Id
                where groupIds.Contains(membership.GroupId) &&
                    membership.ValidTo == null &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null &&
                    !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe)
                select new RespondentRuleSubjectRow(
                    subject.Id,
                    subject.DisplayName,
                    subject.Email,
                    subject.ExternalId,
                    subject.Locale))
            .Distinct()
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            subjects
                .Select(subject => new RespondentRuleCandidate(null, CreateSubject(subject)))
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveManagerOfTargetAsync(
        Guid tenantId,
        IReadOnlyList<Guid> targetSubjectIds,
        IReadOnlyList<Guid> targetGroupIds,
        List<RespondentRuleResolutionIssue> issues,
        CancellationToken cancellationToken)
    {
        var targetsResult = await LoadTargetSubjectsAsync(
            tenantId,
            targetSubjectIds,
            targetGroupIds,
            cancellationToken);
        if (targetsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(targetsResult.Error);
        }

        var targets = targetsResult.Value;
        var targetIds = targets.Select(target => target.Id).ToArray();
        var targetById = targets.ToDictionary(target => target.Id);
        if (targetIds.Length == 0)
        {
            return Result.Success<IReadOnlyList<RespondentRuleCandidate>>([]);
        }

        var managerRows = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join manager in db.Subjects.AsNoTracking()
                    on relationship.SubjectId equals manager.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    targetIds.Contains(relationship.RelatedSubjectId) &&
                    relationship.ValidTo == null &&
                    manager.TenantId == tenantId &&
                    manager.DeletedAt == null &&
                    !EF.Functions.JsonContains(manager.Attributes, SampleStudyAttributeProbe)
                select new
                {
                    TargetId = relationship.RelatedSubjectId,
                    Manager = new RespondentRuleSubjectRow(
                        manager.Id,
                        manager.DisplayName,
                        manager.Email,
                        manager.ExternalId,
                        manager.Locale)
                })
            .ToListAsync(cancellationToken);

        foreach (var targetWithoutManager in targetIds.Except(managerRows.Select(row => row.TargetId)))
        {
            issues.Add(new RespondentRuleResolutionIssue(
                "respondent_rule_preview.target_has_no_manager",
                "warning",
                "Selected target subject has no active manager relationship.",
                SubjectId: targetWithoutManager));
        }

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            managerRows
                .Select(row => new RespondentRuleCandidate(
                    targetById[row.TargetId],
                    CreateSubject(row.Manager)))
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveReportsOfTargetAsync(
        Guid tenantId,
        IReadOnlyList<Guid> targetSubjectIds,
        IReadOnlyList<Guid> targetGroupIds,
        List<RespondentRuleResolutionIssue> issues,
        CancellationToken cancellationToken)
    {
        var targetsResult = await LoadTargetSubjectsAsync(
            tenantId,
            targetSubjectIds,
            targetGroupIds,
            cancellationToken);
        if (targetsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(targetsResult.Error);
        }

        var targets = targetsResult.Value;
        var targetIds = targets.Select(target => target.Id).ToArray();
        var targetById = targets.ToDictionary(target => target.Id);
        if (targetIds.Length == 0)
        {
            return Result.Success<IReadOnlyList<RespondentRuleCandidate>>([]);
        }

        var reportRows = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join report in db.Subjects.AsNoTracking()
                    on relationship.RelatedSubjectId equals report.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    targetIds.Contains(relationship.SubjectId) &&
                    relationship.ValidTo == null &&
                    report.TenantId == tenantId &&
                    report.DeletedAt == null &&
                    !EF.Functions.JsonContains(report.Attributes, SampleStudyAttributeProbe)
                select new
                {
                    TargetId = relationship.SubjectId,
                    Report = new RespondentRuleSubjectRow(
                        report.Id,
                        report.DisplayName,
                        report.Email,
                        report.ExternalId,
                        report.Locale)
                })
            .ToListAsync(cancellationToken);

        foreach (var targetWithoutReports in targetIds.Except(reportRows.Select(row => row.TargetId)))
        {
            issues.Add(new RespondentRuleResolutionIssue(
                "respondent_rule_preview.target_has_no_reports",
                "warning",
                "Selected target subject has no active direct reports.",
                SubjectId: targetWithoutReports));
        }

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            reportRows
                .Select(row => new RespondentRuleCandidate(
                    targetById[row.TargetId],
                    CreateSubject(row.Report)))
                .ToArray());
    }

    private static Result<IReadOnlyList<RespondentRuleCandidate>> ResolveExternalEmails(
        IReadOnlyList<string> emails)
    {
        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            emails
                .Select((email, index) => new RespondentRuleCandidate(
                    Target: null,
                    Respondent: new RespondentRuleSubject(
                        ExternalEmailPreviewSubjectId(index),
                        email,
                        DisplayName: null,
                        Email: email,
                        ExternalId: null,
                        Locale: EmailTemplateLocales.English)))
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleSubject>>> LoadTargetSubjectsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> targetSubjectIds,
        IReadOnlyList<Guid> targetGroupIds,
        CancellationToken cancellationToken)
    {
        if (targetSubjectIds.Count == 0 && targetGroupIds.Count == 0)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleSubject>>(
                Error.Validation(
                    "respondent_rule_preview.target_required",
                    "A target subject is required for this preview rule."));
        }

        var directSubjectsResult = await LoadSubjectsByIdsAsync(
            tenantId,
            targetSubjectIds,
            "subject.not_found",
            "One or more selected target people were not found.",
            cancellationToken);
        if (directSubjectsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleSubject>>(directSubjectsResult.Error);
        }

        if (targetGroupIds.Count > 0)
        {
            var foundGroupIds = await db.SubjectGroups
                .AsNoTracking()
                .Where(group =>
                    group.TenantId == tenantId &&
                    targetGroupIds.Contains(group.Id) &&
                    group.DeletedAt == null &&
                    !EF.Functions.JsonContains(group.Attributes, SampleStudyAttributeProbe))
                .Select(group => group.Id)
                .ToListAsync(cancellationToken);
            if (foundGroupIds.Count != targetGroupIds.Count)
            {
                return Result.Failure<IReadOnlyList<RespondentRuleSubject>>(
                    Error.NotFound("subject_group.not_found", "Subject group was not found."));
            }
        }

        var groupSubjects = new List<RespondentRuleSubjectRow>();
        if (targetGroupIds.Count > 0)
        {
            groupSubjects = await (
                    from membership in db.SubjectMemberships.AsNoTracking()
                    join subject in db.Subjects.AsNoTracking()
                        on membership.SubjectId equals subject.Id
                    where targetGroupIds.Contains(membership.GroupId) &&
                        membership.ValidTo == null &&
                        subject.TenantId == tenantId &&
                        subject.DeletedAt == null &&
                        !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe)
                    select new RespondentRuleSubjectRow(
                        subject.Id,
                        subject.DisplayName,
                        subject.Email,
                        subject.ExternalId,
                        subject.Locale))
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        var subjects = directSubjectsResult.Value
            .Concat(groupSubjects)
            .GroupBy(subject => subject.Id)
            .Select(group => group.First())
            .Select(CreateSubject)
            .ToArray();

        return Result.Success<IReadOnlyList<RespondentRuleSubject>>(subjects);
    }

    private async Task<Result<IReadOnlyList<RespondentRuleSubjectRow>>> LoadSubjectsByIdsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> subjectIds,
        string notFoundCode,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        if (subjectIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<RespondentRuleSubjectRow>>([]);
        }

        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(subject =>
                subject.TenantId == tenantId &&
                subjectIds.Contains(subject.Id) &&
                subject.DeletedAt == null &&
                !EF.Functions.JsonContains(subject.Attributes, SampleStudyAttributeProbe))
            .Select(subject => new RespondentRuleSubjectRow(
                subject.Id,
                subject.DisplayName,
                subject.Email,
                subject.ExternalId,
                subject.Locale))
            .ToListAsync(cancellationToken);
        if (subjects.Count != subjectIds.Count)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleSubjectRow>>(
                Error.NotFound(notFoundCode, notFoundMessage));
        }

        return Result.Success<IReadOnlyList<RespondentRuleSubjectRow>>(subjects);
    }

    private static Result<ParsedRespondentRule> ParseRule(string rule)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            return InvalidRule();
        }

        try
        {
            using var document = JsonDocument.Parse(rule);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return InvalidRule();
            }

            if (!document.RootElement.TryGetProperty("kind", out var kindElement) ||
                kindElement.ValueKind != JsonValueKind.String)
            {
                return Result.Failure<ParsedRespondentRule>(
                    Error.Validation(
                        "respondent_rule_preview.kind_required",
                        "Respondent rule kind is required."));
            }

            var kind = NormalizeText(kindElement.GetString())?.ToLowerInvariant();
            if (kind is not Self and not AllActivePeople and not SelectedPeople and not AllInGroup and not ManagerOfTarget and not ReportsOfTarget and not ExternalEmails)
            {
                return Result.Failure<ParsedRespondentRule>(
                    Error.Validation(
                        "respondent_rule_preview.unsupported_kind",
                        "Respondent rule kind is not supported for preview."));
            }

            var role = TryGetText(document.RootElement, "role") ?? DefaultRole(kind);
            if (role.Length > 64)
            {
                return Result.Failure<ParsedRespondentRule>(
                    Error.Validation(
                        "respondent_rule_preview.role_invalid",
                        "Respondent rule role must be 64 characters or fewer."));
            }

            var subjectIds = kind == SelectedPeople
                ? ParseGuidArray(document.RootElement, "subject_ids", MaxSelectedSubjectIds)
                : Result.Success<IReadOnlyList<Guid>>([]);
            if (subjectIds.IsFailure)
            {
                return Result.Failure<ParsedRespondentRule>(subjectIds.Error);
            }

            var groupIds = ParseGuidArray(document.RootElement, "group_ids", MaxSelectedGroupIds);
            if (groupIds.IsFailure)
            {
                return Result.Failure<ParsedRespondentRule>(groupIds.Error);
            }

            var targetSubjectIds = ParseGuidArray(document.RootElement, "target_subject_ids", MaxSelectedSubjectIds);
            if (targetSubjectIds.IsFailure)
            {
                return Result.Failure<ParsedRespondentRule>(targetSubjectIds.Error);
            }

            var targetGroupIds = ParseGuidArray(document.RootElement, "target_group_ids", MaxSelectedGroupIds);
            if (targetGroupIds.IsFailure)
            {
                return Result.Failure<ParsedRespondentRule>(targetGroupIds.Error);
            }

            var externalEmails = kind == ExternalEmails
                ? ParseExternalEmails(document.RootElement)
                : Result.Success<IReadOnlyList<string>>([]);
            if (externalEmails.IsFailure)
            {
                return Result.Failure<ParsedRespondentRule>(externalEmails.Error);
            }

            return Result.Success(new ParsedRespondentRule(
                kind,
                role,
                subjectIds.Value,
                MergeSelectedIds(TryGetGuid(document.RootElement, "group_id"), groupIds.Value),
                MergeSelectedIds(TryGetGuid(document.RootElement, "target_subject_id"), targetSubjectIds.Value),
                targetGroupIds.Value,
                externalEmails.Value));
        }
        catch (JsonException)
        {
            return InvalidRule();
        }
    }

    private static Result<ParsedRespondentRule> InvalidRule()
    {
        return Result.Failure<ParsedRespondentRule>(
            Error.Validation(
                "respondent_rule_preview.rule_invalid",
                "Respondent rule must be a JSON object."));
    }

    private static Result<IReadOnlyList<Guid>> ParseGuidArray(
        JsonElement root,
        string propertyName,
        int maxItems)
    {
        if (!root.TryGetProperty(propertyName, out var valuesElement))
        {
            return Result.Success<IReadOnlyList<Guid>>([]);
        }

        if (valuesElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<Guid>>(
                Error.Validation(
                    $"respondent_rule_preview.{propertyName}_invalid",
                    $"{propertyName} must be an array of ids."));
        }

        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var valueElement in valuesElement.EnumerateArray())
        {
            if (valueElement.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(valueElement.GetString(), out var id))
            {
                return Result.Failure<IReadOnlyList<Guid>>(
                    Error.Validation(
                        $"respondent_rule_preview.{propertyName}_invalid",
                        $"{propertyName} must contain valid ids."));
            }

            if (seen.Add(id))
            {
                ids.Add(id);
            }

            if (ids.Count > maxItems)
            {
                return Result.Failure<IReadOnlyList<Guid>>(
                    Error.Validation(
                        $"respondent_rule_preview.{propertyName}_too_many",
                        $"{propertyName} supports at most {maxItems} ids."));
            }
        }

        return Result.Success<IReadOnlyList<Guid>>(ids);
    }

    private static IReadOnlyList<Guid> MergeSelectedIds(Guid? firstId, IReadOnlyList<Guid> selectedIds)
    {
        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        if (firstId.HasValue && seen.Add(firstId.Value))
        {
            ids.Add(firstId.Value);
        }

        foreach (var id in selectedIds)
        {
            if (seen.Add(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static Result<IReadOnlyList<string>> ParseExternalEmails(JsonElement root)
    {
        if (!root.TryGetProperty("emails", out var emailsElement) ||
            emailsElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation(
                    "respondent_rule_preview.emails_required",
                    "External email recipient rules require an emails array."));
        }

        var emails = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var emailElement in emailsElement.EnumerateArray())
        {
            if (emailElement.ValueKind != JsonValueKind.String)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "respondent_rule_preview.email_invalid",
                        "Every external email recipient must be an email string."));
            }

            var email = NormalizeEmail(emailElement.GetString());
            if (email is null)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "respondent_rule_preview.email_invalid",
                        "Every external email recipient must be a valid email address."));
            }

            if (!seen.Add(email))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "respondent_rule_preview.duplicate_external_email",
                        "External email recipient rules cannot contain duplicate email addresses."));
            }

            if (emails.Count >= MaxExternalEmailRecipients)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "respondent_rule_preview.too_many_external_emails",
                        $"External email recipient rules support at most {MaxExternalEmailRecipients} recipients."));
            }

            emails.Add(email);
        }

        return Result.Success<IReadOnlyList<string>>(emails);
    }

    private static Guid? TryGetGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(property.GetString(), out var id))
        {
            return null;
        }

        return id;
    }

    private static string? TryGetText(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return NormalizeText(property.GetString());
    }

    private static string DefaultRole(string kind)
    {
        return kind switch
        {
            AllInGroup => "group_member",
            ManagerOfTarget => "manager",
            ReportsOfTarget => "direct_report",
            ExternalEmails => "email_recipient",
            _ => "self"
        };
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeText(value)?.ToLowerInvariant();
        if (normalized is null ||
            normalized.Length > 320 ||
            normalized.Contains('\r', StringComparison.Ordinal) ||
            normalized.Contains('\n', StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var address = new MailAddress(normalized);
            return string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase)
                ? address.Address.ToLowerInvariant()
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Guid ExternalEmailPreviewSubjectId(int index)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[12..], index + 1);
        return new Guid(bytes);
    }

    private static RespondentRuleSubject CreateSubject(RespondentRuleSubjectRow subject)
    {
        return new RespondentRuleSubject(
            subject.Id,
            CreateSubjectLabel(subject),
            subject.DisplayName,
            subject.Email,
            subject.ExternalId,
            EmailTemplateLocales.Normalize(subject.Locale));
    }

    private static string CreateSubjectLabel(RespondentRuleSubjectRow subject)
    {
        return NormalizeText(subject.DisplayName) ??
            NormalizeText(subject.Email) ??
            NormalizeText(subject.ExternalId) ??
            subject.Id.ToString("D");
    }

    private static RespondentRuleResolutionIssue CreateEmptyWarning(
        string kind,
        Guid? subjectId,
        Guid? groupId)
    {
        return kind switch
        {
            AllInGroup => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.group_empty",
                "warning",
                "Selected subject group has no active members.",
                GroupId: groupId),
            AllActivePeople => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.empty",
                "warning",
                "No active Directory people were found."),
            SelectedPeople => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.empty",
                "warning",
                "Selected people did not resolve active recipients."),
            ManagerOfTarget => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.target_has_no_manager",
                "warning",
                "Selected target subject has no active manager relationship.",
                SubjectId: subjectId),
            ReportsOfTarget => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.target_has_no_reports",
                "warning",
                "Selected target subject has no active direct reports.",
                SubjectId: subjectId),
            ExternalEmails => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.empty",
                "warning",
                "No external email recipients were provided."),
            _ => new RespondentRuleResolutionIssue(
                "respondent_rule_preview.empty",
                "warning",
                "Preview did not resolve any respondents.")
        };
    }

    private sealed record ParsedRespondentRule(
        string Kind,
        string Role,
        IReadOnlyList<Guid> SubjectIds,
        IReadOnlyList<Guid> GroupIds,
        IReadOnlyList<Guid> TargetSubjectIds,
        IReadOnlyList<Guid> TargetGroupIds,
        IReadOnlyList<string> ExternalEmails);

    private sealed record RespondentRuleSubjectRow(
        Guid Id,
        string? DisplayName,
        string? Email,
        string? ExternalId,
        string Locale);
}

public sealed record RespondentRuleResolutionRequest(
    Guid TenantId,
    Guid CampaignId,
    Guid? CampaignSeriesId,
    string Rule,
    Guid? TargetSubjectId = null,
    Guid? GroupId = null,
    int MaxRows = 200);

public sealed record RespondentRuleResolution(
    string RuleKind,
    string Role,
    Guid? TargetSubjectId,
    Guid? GroupId,
    IReadOnlyList<RespondentRuleCandidate> Candidates,
    IReadOnlyList<RespondentRuleResolutionIssue> Issues);

public sealed record RespondentRuleCandidate(
    RespondentRuleSubject? Target,
    RespondentRuleSubject Respondent);

public sealed record RespondentRuleSubject(
    Guid Id,
    string Label,
    string? DisplayName,
    string? Email,
    string? ExternalId,
    string Locale);

public sealed record RespondentRuleResolutionIssue(
    string Code,
    string Severity,
    string Message,
    Guid? SubjectId = null,
    Guid? GroupId = null);
