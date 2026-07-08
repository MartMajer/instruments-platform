using Platform.Domain.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

/// <summary>
/// Builds the authenticated app-branding view from stored tenant columns,
/// resolving the anchor colors into the contrast-guarded palette the app applies
/// (via <see cref="AppBrandingTheme"/>). One source of truth for the read store
/// and the write store alike.
/// </summary>
public static class TenantAppBrandingResponseFactory
{
    public static TenantSettingsAppBrandingResponse Create(
        string? organizationLabel,
        string tenantName,
        string? accentColorHex,
        string? logoObjectKey,
        string? logoContentType,
        DateTimeOffset? updatedAt,
        string? topbarColorHex = null,
        string? backgroundColorHex = null,
        string? surfaceColorHex = null,
        string? inkColorHex = null)
    {
        var tokens = new AppBrandingThemeTokens(
            accentColorHex,
            topbarColorHex,
            backgroundColorHex,
            surfaceColorHex,
            inkColorHex);
        var theme = AppBrandingTheme.Resolve(tokens);

        var effectiveAccent = string.IsNullOrWhiteSpace(accentColorHex)
            ? null
            : theme.Accent;

        var orgLabel = string.IsNullOrWhiteSpace(organizationLabel) ? tenantName : organizationLabel!;

        return new TenantSettingsAppBrandingResponse(
            orgLabel,
            accentColorHex,
            effectiveAccent,
            HasLogo: !string.IsNullOrWhiteSpace(logoObjectKey),
            logoObjectKey,
            logoContentType,
            Tenant.DefaultAppBrandingAccentColorHex,
            Tenant.AppBrandingAllowedLogoContentTypes,
            TenantAppBrandingLogo.MaxBytes,
            TenantAppBrandingLogo.MaxDimension,
            updatedAt,
            topbarColorHex,
            backgroundColorHex,
            surfaceColorHex,
            inkColorHex)
        {
            Theme = AppBrandingThemeResponse.From(theme),
            Defaults = AppBrandingThemeResponse.Default
        };
    }
}
