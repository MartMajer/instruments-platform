using System.Text.RegularExpressions;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record EmailTemplateContent(
    string TemplateCode,
    string Locale,
    string Subject,
    string BodyText,
    bool IsBuiltInDefault = false);

public sealed record EmailTemplateRenderContext(
    string TemplateCode,
    string Locale,
    string WorkspaceName,
    string RespondentLink,
    string UnsubscribeLink);

public sealed record EmailTemplateRenderedMessage(
    string Subject,
    string BodyText);

public sealed record EmailTemplateValidationIssue(
    string Code,
    string Message);

public sealed record EmailTemplateValidationResult(
    IReadOnlyList<EmailTemplateValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

public static class EmailTemplateDefaults
{
    public static EmailTemplateContent Get(string templateCode, string locale)
    {
        var normalizedLocale = EmailTemplateLocales.Normalize(locale);

        return templateCode switch
        {
            EmailTemplateCodes.Invitation => CreateInvitationDefault(normalizedLocale),
            EmailTemplateCodes.Reminder => CreateReminderDefault(normalizedLocale),
            _ => CreateInvitationDefault(normalizedLocale)
        };
    }

    private static EmailTemplateContent CreateInvitationDefault(string locale)
    {
        if (locale == EmailTemplateLocales.Croatian)
        {
            return new EmailTemplateContent(
                EmailTemplateCodes.Invitation,
                EmailTemplateLocales.Croatian,
                "Poziv na istraživanje",
                """
                Pozvani ste ispuniti istraživanje.

                Radi privatnosti ova poruka ne navodi naziv ni temu istraživanja. Poveznica otvara stranicu istraživanja prije nego odlučite hoćete li sudjelovati.

                Otvorite poveznicu:
                {{respondent_link}}

                Ako ste već odgovorili, ovu poruku možete zanemariti.

                Ako ubuduće ne želite primati pozive iz ovog radnog prostora, odjavite se ovdje:
                {{unsubscribe_link}}

                Poslao radni prostor {{workspace_name}}.
                """,
                IsBuiltInDefault: true);
        }

        return new EmailTemplateContent(
            EmailTemplateCodes.Invitation,
            EmailTemplateLocales.English,
            "Study invitation",
            """
            You have been invited to complete a study.

            For privacy, this email does not include the study title or topic. The link opens the study page before you decide whether to respond.

            Open your study link:
            {{respondent_link}}

            If you already responded, you can ignore this email.

            If you should not receive future study invitations from this workspace, unsubscribe here:
            {{unsubscribe_link}}

            Sent by {{workspace_name}}.
            """,
            IsBuiltInDefault: true);
    }

    private static EmailTemplateContent CreateReminderDefault(string locale)
    {
        if (locale == EmailTemplateLocales.Croatian)
        {
            return new EmailTemplateContent(
                EmailTemplateCodes.Reminder,
                EmailTemplateLocales.Croatian,
                "Podsjetnik na istraživanje",
                """
                Ovo je podsjetnik za istraživanje koje još možete ispuniti.

                Radi privatnosti ova poruka ne navodi naziv ni temu istraživanja. Poveznica otvara stranicu istraživanja prije nego odlučite hoćete li sudjelovati.

                Otvorite poveznicu:
                {{respondent_link}}

                Ako ste već odgovorili, ovu poruku možete zanemariti.

                Ako ubuduće ne želite primati pozive iz ovog radnog prostora, odjavite se ovdje:
                {{unsubscribe_link}}

                Poslao radni prostor {{workspace_name}}.
                """,
                IsBuiltInDefault: true);
        }

        return new EmailTemplateContent(
            EmailTemplateCodes.Reminder,
            EmailTemplateLocales.English,
            "Study reminder",
            """
            This is a reminder for a study that is still available to complete.

            For privacy, this email does not include the study title or topic. The link opens the study page before you decide whether to respond.

            Open your study link:
            {{respondent_link}}

            If you already responded, you can ignore this email.

            If you should not receive future study invitations from this workspace, unsubscribe here:
            {{unsubscribe_link}}

            Sent by {{workspace_name}}.
            """,
            IsBuiltInDefault: true);
    }
}

public static partial class EmailTemplateValidator
{
    public const string RespondentLinkVariable = "respondent_link";
    public const string UnsubscribeLinkVariable = "unsubscribe_link";
    public const string WorkspaceNameVariable = "workspace_name";

    private static readonly HashSet<string> AllowedVariables = new(StringComparer.Ordinal)
    {
        RespondentLinkVariable,
        UnsubscribeLinkVariable,
        WorkspaceNameVariable
    };

    public static EmailTemplateValidationResult Validate(EmailTemplateContent template)
    {
        var issues = new List<EmailTemplateValidationIssue>();
        var subject = template.Subject?.Trim() ?? string.Empty;
        var bodyText = template.BodyText?.Trim() ?? string.Empty;

        if (!EmailTemplateCodes.IsKnown(template.TemplateCode))
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.template_code_invalid",
                "Email template code must be invitation or reminder."));
        }

        if (!EmailTemplateLocales.IsSupported(template.Locale))
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.locale_invalid",
                "Email template locale must be en or hr-HR."));
        }

        if (subject.Length == 0)
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.subject_required",
                "Email template subject is required."));
        }
        else if (subject.Length > EmailTemplate.MaxSubjectLength)
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.subject_too_long",
                "Email template subject must be 160 characters or fewer."));
        }

        if (bodyText.Length < EmailTemplate.MinBodyTextLength)
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.body_too_short",
                "Email template body must be at least 80 characters."));
        }
        else if (bodyText.Length > EmailTemplate.MaxBodyTextLength)
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.body_too_long",
                "Email template body must be 4000 characters or fewer."));
        }

        AddUnknownVariableIssues(subject, issues);
        AddUnknownVariableIssues(bodyText, issues);

        if (RequiresRespondentLinks(template.TemplateCode))
        {
            if (!ContainsVariable(bodyText, RespondentLinkVariable))
            {
                issues.Add(new EmailTemplateValidationIssue(
                    "email_template.respondent_link_required",
                    "Invitation templates must include {{respondent_link}}."));
            }

            if (!ContainsVariable(bodyText, UnsubscribeLinkVariable))
            {
                issues.Add(new EmailTemplateValidationIssue(
                    "email_template.unsubscribe_link_required",
                    "Invitation templates must include {{unsubscribe_link}}."));
            }
        }

        if (LooksLikeHtml(subject) || LooksLikeHtml(bodyText))
        {
            issues.Add(new EmailTemplateValidationIssue(
                "email_template.html_not_allowed",
                "Email templates are plain text and cannot contain HTML."));
        }

        return new EmailTemplateValidationResult(issues);
    }

    private static bool RequiresRespondentLinks(string templateCode)
    {
        return templateCode is EmailTemplateCodes.Invitation or EmailTemplateCodes.Reminder;
    }

    private static bool ContainsVariable(string value, string variableName)
    {
        return VariablePattern().Matches(value)
            .Any(match => string.Equals(match.Groups[1].Value, variableName, StringComparison.Ordinal));
    }

    private static void AddUnknownVariableIssues(
        string value,
        ICollection<EmailTemplateValidationIssue> issues)
    {
        foreach (Match match in VariablePattern().Matches(value))
        {
            var variableName = match.Groups[1].Value;
            if (!AllowedVariables.Contains(variableName))
            {
                issues.Add(new EmailTemplateValidationIssue(
                    "email_template.variable_unknown",
                    $"Unknown email template variable: {variableName}."));
            }
        }
    }

    private static bool LooksLikeHtml(string value)
    {
        return HtmlPattern().IsMatch(value);
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex VariablePattern();

    [GeneratedRegex(@"</?[a-zA-Z][^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlPattern();
}

public static partial class EmailTemplateRenderer
{
    public static Result<EmailTemplateRenderedMessage> Render(
        EmailTemplateContent template,
        EmailTemplateRenderContext context)
    {
        var validation = EmailTemplateValidator.Validate(template);
        if (!validation.IsValid)
        {
            var issue = validation.Issues[0];
            return Result.Failure<EmailTemplateRenderedMessage>(
                Error.Validation(issue.Code, issue.Message));
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EmailTemplateValidator.RespondentLinkVariable] = context.RespondentLink.Trim(),
            [EmailTemplateValidator.UnsubscribeLinkVariable] = context.UnsubscribeLink.Trim(),
            [EmailTemplateValidator.WorkspaceNameVariable] = context.WorkspaceName.Trim()
        };

        var renderedSubject = RenderText(template.Subject.Trim(), values);
        var renderedBody = RenderText(template.BodyText.Trim(), values);

        if (ContainsUnresolvedTemplateVariable(renderedSubject) ||
            ContainsUnresolvedTemplateVariable(renderedBody))
        {
            return Result.Failure<EmailTemplateRenderedMessage>(
                Error.Validation(
                    "email_template.unresolved_variable",
                    "Rendered email template contains an unresolved variable."));
        }

        return Result.Success(new EmailTemplateRenderedMessage(
            renderedSubject,
            renderedBody));
    }

    private static string RenderText(string value, IReadOnlyDictionary<string, string> values)
    {
        return VariablePattern().Replace(
            value,
            match => values.TryGetValue(match.Groups[1].Value, out var replacement)
                ? replacement
                : match.Value);
    }

    private static bool ContainsUnresolvedTemplateVariable(string value)
    {
        return value.Contains("{{", StringComparison.Ordinal) ||
            value.Contains("}}", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex VariablePattern();
}
