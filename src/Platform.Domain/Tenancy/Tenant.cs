namespace Platform.Domain.Tenancy;

public sealed class Tenant
{
    public const int ReportBrandingOrganizationLabelMaxLength = 256;
    public const int ReportBrandingReportTitleMaxLength = 256;
    public const int ReportBrandingAccentColorHexLength = 7;
    public const int ReportBrandingLayoutVariantMaxLength = 32;
    public const string DefaultReportBrandingAccentColorHex = "#2563eb";
    public const string DefaultReportBrandingLayoutVariant = "standard";

    private Tenant()
    {
    }

    public Tenant(Guid id, string slug, string name, string region = "eu", string defaultLocale = "en")
    {
        Id = id;
        Slug = slug;
        Name = name;
        Region = region;
        DefaultLocale = defaultLocale;
        Status = "active";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? ReportBrandingOrganizationLabel { get; private set; }

    public string? ReportBrandingReportTitle { get; private set; }

    public string? ReportBrandingAccentColorHex { get; private set; }

    public string? ReportBrandingLayoutVariant { get; private set; }

    public DateTimeOffset? ReportBrandingUpdatedAt { get; private set; }

    public string Region { get; private set; } = "eu";

    public string DefaultLocale { get; private set; } = "en";

    public string Status { get; private set; } = "active";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void UpdateReportBranding(
        string organizationLabel,
        string reportTitle,
        string accentColorHex,
        string layoutVariant,
        DateTimeOffset updatedAt)
    {
        ReportBrandingOrganizationLabel = RequiredText(
            organizationLabel,
            ReportBrandingOrganizationLabelMaxLength,
            "Report branding organization label is required.",
            "Report branding organization label is too long.");
        ReportBrandingReportTitle = RequiredText(
            reportTitle,
            ReportBrandingReportTitleMaxLength,
            "Report branding report title is required.",
            "Report branding report title is too long.");
        ReportBrandingAccentColorHex = NormalizeAccentColorHex(accentColorHex);
        ReportBrandingLayoutVariant = NormalizeLayoutVariant(layoutVariant);
        ReportBrandingUpdatedAt = updatedAt;
        UpdatedAt = updatedAt;
    }

    public static bool IsReportBrandingLayoutVariantKnown(string? value)
    {
        return value?.Trim().ToLowerInvariant() is "standard" or "compact" or "compliance";
    }

    public static bool IsReportBrandingAccentColorHex(string? value)
    {
        var candidate = value?.Trim();
        if (candidate is null || candidate.Length != ReportBrandingAccentColorHexLength || candidate[0] != '#')
        {
            return false;
        }

        for (var index = 1; index < candidate.Length; index++)
        {
            var character = candidate[index];
            var isHex =
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F');
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    private static string RequiredText(
        string value,
        int maxLength,
        string requiredMessage,
        string tooLongMessage)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(requiredMessage, nameof(value));
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(tooLongMessage, nameof(value));
        }

        return normalized;
    }

    private static string NormalizeAccentColorHex(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsReportBrandingAccentColorHex(normalized))
        {
            throw new ArgumentException("Report branding accent color must be a hex color token.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeLayoutVariant(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsReportBrandingLayoutVariantKnown(normalized))
        {
            throw new ArgumentException("Report branding layout variant is not supported.", nameof(value));
        }

        return normalized;
    }
}
