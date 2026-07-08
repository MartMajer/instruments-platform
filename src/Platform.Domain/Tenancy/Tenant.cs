namespace Platform.Domain.Tenancy;

public sealed class Tenant
{
    public const int ReportBrandingOrganizationLabelMaxLength = 256;
    public const int ReportBrandingReportTitleMaxLength = 256;
    public const int ReportBrandingAccentColorHexLength = 7;
    public const int ReportBrandingLayoutVariantMaxLength = 32;
    public const string DefaultReportBrandingAccentColorHex = "#2563eb";
    public const string DefaultReportBrandingLayoutVariant = "standard";

    // App-level (respondent + shell) branding — ADR-0017's typed-token model
    // extended to the product surface. Distinct columns from report branding so
    // the two stay independent (a report reskin never touches the survey chrome).
    public const int AppBrandingAccentColorHexLength = 7;
    public const int AppBrandingLogoObjectKeyMaxLength = 512;
    public const int AppBrandingLogoContentTypeMaxLength = 128;

    // Suggested starting accent for the Settings picker when a tenant has none
    // set — the platform "stain" identity color the runner falls back to.
    public const string DefaultAppBrandingAccentColorHex = "#4530a6";

    // Allow-list for platform-hosted logos. No SVG in v1: SVG can carry script,
    // so it needs sanitization/rasterization before it can join this list.
    public static readonly IReadOnlyList<string> AppBrandingAllowedLogoContentTypes =
        ["image/png", "image/jpeg", "image/webp"];

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

    public string? AppBrandingAccentColorHex { get; private set; }

    public string? AppBrandingTopbarColorHex { get; private set; }

    public string? AppBrandingBackgroundColorHex { get; private set; }

    public string? AppBrandingSurfaceColorHex { get; private set; }

    public string? AppBrandingInkColorHex { get; private set; }

    public string? AppBrandingLogoObjectKey { get; private set; }

    public string? AppBrandingLogoContentType { get; private set; }

    public DateTimeOffset? AppBrandingUpdatedAt { get; private set; }

    public Guid? AppBrandingUpdatedBy { get; private set; }

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

    /// <summary>
    /// Set the respondent/app-shell branding tokens. Accent is a strict hex; the
    /// logo (nullable) is a platform-hosted object key + an allow-listed content
    /// type. Passing a blank object key clears the logo. The org label is not a
    /// v1 app-branding token — it is reused from the report-branding label / name.
    /// </summary>
    public void UpdateAppBranding(
        string accentColorHex,
        string? logoObjectKey,
        string? logoContentType,
        Guid updatedBy,
        DateTimeOffset updatedAt,
        string? topbarColorHex = null,
        string? backgroundColorHex = null,
        string? surfaceColorHex = null,
        string? inkColorHex = null)
    {
        AppBrandingAccentColorHex = NormalizeAppBrandingAccentColorHex(accentColorHex);
        AppBrandingTopbarColorHex = NormalizeOptionalAppBrandingColorHex(topbarColorHex);
        AppBrandingBackgroundColorHex = NormalizeOptionalAppBrandingColorHex(backgroundColorHex);
        AppBrandingSurfaceColorHex = NormalizeOptionalAppBrandingColorHex(surfaceColorHex);
        AppBrandingInkColorHex = NormalizeOptionalAppBrandingColorHex(inkColorHex);

        if (string.IsNullOrWhiteSpace(logoObjectKey))
        {
            AppBrandingLogoObjectKey = null;
            AppBrandingLogoContentType = null;
        }
        else
        {
            AppBrandingLogoObjectKey = RequiredText(
                logoObjectKey,
                AppBrandingLogoObjectKeyMaxLength,
                "App branding logo object key is required.",
                "App branding logo object key is too long.");
            AppBrandingLogoContentType = NormalizeAppBrandingLogoContentType(logoContentType);
        }

        AppBrandingUpdatedBy = updatedBy;
        AppBrandingUpdatedAt = updatedAt;
        UpdatedAt = updatedAt;
    }

    public static bool IsAppBrandingAccentColorHex(string? value)
    {
        return IsReportBrandingAccentColorHex(value);
    }

    public static bool IsAppBrandingLogoContentType(string? value)
    {
        var candidate = value?.Trim().ToLowerInvariant();
        return candidate is not null && AppBrandingAllowedLogoContentTypes.Contains(candidate);
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

    private static string NormalizeAppBrandingAccentColorHex(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsAppBrandingAccentColorHex(normalized))
        {
            throw new ArgumentException("App branding accent color must be a hex color token.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptionalAppBrandingColorHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!IsAppBrandingAccentColorHex(normalized))
        {
            throw new ArgumentException("App branding color must be a hex color token.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeAppBrandingLogoContentType(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsAppBrandingLogoContentType(normalized))
        {
            throw new ArgumentException("App branding logo content type is not supported.", nameof(value));
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
