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
    public const string AllInGroup = "all_in_group";
    public const string ManagerOfTarget = "manager_of_target";
    public const string ReportsOfTarget = "reports_of_target";
    public const string ExternalEmails = "external_emails";
    private const int MaxExternalEmailRecipients = 500;

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
        var targetSubjectId = request.TargetSubjectId ?? rule.TargetSubjectId;
        var groupId = request.GroupId ?? rule.GroupId;
        var issues = new List<RespondentRuleResolutionIssue>();
        var candidatesResult = rule.Kind switch
        {
            Self => await ResolveSelfAsync(
                request.TenantId,
                request.CampaignId,
                issues,
                cancellationToken),
            AllInGroup => await ResolveAllInGroupAsync(
                request.TenantId,
                groupId,
                cancellationToken),
            ManagerOfTarget => await ResolveManagerOfTargetAsync(
                request.TenantId,
                targetSubjectId,
                cancellationToken),
            ReportsOfTarget => await ResolveReportsOfTargetAsync(
                request.TenantId,
                targetSubjectId,
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
            issues.Add(CreateEmptyWarning(rule.Kind, targetSubjectId, groupId));
        }

        return Result.Success(new RespondentRuleResolution(
            rule.Kind,
            rule.Role,
            targetSubjectId,
            groupId,
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
                    subject.DeletedAt == null
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

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveAllInGroupAsync(
        Guid tenantId,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        if (!groupId.HasValue)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.Validation(
                    "respondent_rule_preview.group_required",
                    "A subject group is required for this preview rule."));
        }

        var groupExists = await db.SubjectGroups
            .AsNoTracking()
            .AnyAsync(
                group =>
                    group.TenantId == tenantId &&
                    group.Id == groupId.Value &&
                    group.DeletedAt == null,
                cancellationToken);
        if (!groupExists)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(
                Error.NotFound("subject_group.not_found", "Subject group was not found."));
        }

        var subjects = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subject in db.Subjects.AsNoTracking()
                    on membership.SubjectId equals subject.Id
                where membership.GroupId == groupId.Value &&
                    membership.ValidTo == null &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null
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
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        var targetResult = await LoadTargetSubjectAsync(tenantId, targetSubjectId, cancellationToken);
        if (targetResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(targetResult.Error);
        }

        var target = targetResult.Value;
        var managers = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join manager in db.Subjects.AsNoTracking()
                    on relationship.SubjectId equals manager.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.RelatedSubjectId == target.Id &&
                    relationship.ValidTo == null &&
                    manager.TenantId == tenantId &&
                    manager.DeletedAt == null
                select new RespondentRuleSubjectRow(
                    manager.Id,
                    manager.DisplayName,
                    manager.Email,
                    manager.ExternalId,
                    manager.Locale))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            managers
                .Select(manager => new RespondentRuleCandidate(target, CreateSubject(manager)))
                .ToArray());
    }

    private async Task<Result<IReadOnlyList<RespondentRuleCandidate>>> ResolveReportsOfTargetAsync(
        Guid tenantId,
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        var targetResult = await LoadTargetSubjectAsync(tenantId, targetSubjectId, cancellationToken);
        if (targetResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRuleCandidate>>(targetResult.Error);
        }

        var target = targetResult.Value;
        var reports = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join report in db.Subjects.AsNoTracking()
                    on relationship.RelatedSubjectId equals report.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.SubjectId == target.Id &&
                    relationship.ValidTo == null &&
                    report.TenantId == tenantId &&
                    report.DeletedAt == null
                select new RespondentRuleSubjectRow(
                    report.Id,
                    report.DisplayName,
                    report.Email,
                    report.ExternalId,
                    report.Locale))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RespondentRuleCandidate>>(
            reports
                .Select(report => new RespondentRuleCandidate(target, CreateSubject(report)))
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

    private async Task<Result<RespondentRuleSubject>> LoadTargetSubjectAsync(
        Guid tenantId,
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        if (!targetSubjectId.HasValue)
        {
            return Result.Failure<RespondentRuleSubject>(
                Error.Validation(
                    "respondent_rule_preview.target_required",
                    "A target subject is required for this preview rule."));
        }

        var subject = await db.Subjects
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.Id == targetSubjectId.Value &&
                item.DeletedAt == null)
            .Select(item => new RespondentRuleSubjectRow(
                item.Id,
                item.DisplayName,
                item.Email,
                item.ExternalId,
                item.Locale))
            .SingleOrDefaultAsync(cancellationToken);
        if (subject is null)
        {
            return Result.Failure<RespondentRuleSubject>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        return Result.Success(CreateSubject(subject));
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
            if (kind is not Self and not AllInGroup and not ManagerOfTarget and not ReportsOfTarget and not ExternalEmails)
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

            var targetSubjectId = TryGetGuid(document.RootElement, "target_subject_id");
            var groupId = TryGetGuid(document.RootElement, "group_id");
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
                targetSubjectId,
                groupId,
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
        Guid? TargetSubjectId,
        Guid? GroupId,
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
