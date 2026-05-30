namespace Platform.Domain.Campaigns;

public sealed class EmailTemplate
{
    public const int MaxSubjectLength = 160;
    public const int MinBodyTextLength = 80;
    public const int MaxBodyTextLength = 4000;

    private EmailTemplate()
    {
    }

    public EmailTemplate(
        Guid id,
        Guid tenantId,
        string templateCode,
        string locale,
        string subject,
        string bodyText,
        string status = EmailTemplateStatuses.Active)
    {
        if (!EmailTemplateCodes.IsKnown(templateCode))
        {
            throw new ArgumentException("Unknown email template code.", nameof(templateCode));
        }

        if (!EmailTemplateStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown email template status.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        TemplateCode = templateCode.Trim();
        Locale = EmailTemplateLocales.Normalize(locale);
        Subject = NormalizeRequired(subject, nameof(subject));
        BodyText = NormalizeRequired(bodyText, nameof(bodyText));
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string TemplateCode { get; private set; } = string.Empty;

    public string Locale { get; private set; } = EmailTemplateLocales.English;

    public string Subject { get; private set; } = string.Empty;

    public string BodyText { get; private set; } = string.Empty;

    public string Status { get; private set; } = EmailTemplateStatuses.Active;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateContent(string subject, string bodyText, DateTimeOffset updatedAt)
    {
        Subject = NormalizeRequired(subject, nameof(subject));
        BodyText = NormalizeRequired(bodyText, nameof(bodyText));
        UpdatedAt = updatedAt;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}

public static class EmailTemplateCodes
{
    public const string Invitation = "invitation";
    public const string Reminder = "reminder";

    public static bool IsKnown(string? value)
    {
        return string.Equals(value, Invitation, StringComparison.Ordinal) ||
            string.Equals(value, Reminder, StringComparison.Ordinal);
    }
}

public static class EmailTemplateStatuses
{
    public const string Active = "active";

    public static bool IsKnown(string? value)
    {
        return string.Equals(value, Active, StringComparison.Ordinal);
    }
}

public static class EmailTemplateLocales
{
    public const string English = "en";
    public const string Croatian = "hr-HR";

    public static IReadOnlyList<string> Supported { get; } =
    [
        English,
        Croatian
    ];

    public static string Normalize(string? value)
    {
        return TryNormalize(value, out var normalized)
            ? normalized
            : English;
    }

    public static bool IsSupported(string? value)
    {
        return TryNormalize(value, out _);
    }

    private static bool TryNormalize(string? value, out string normalized)
    {
        normalized = English;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (string.Equals(candidate, Croatian, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate, "hr", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Croatian;
            return true;
        }

        if (string.Equals(candidate, English, StringComparison.OrdinalIgnoreCase))
        {
            normalized = English;
            return true;
        }

        return false;
    }
}
