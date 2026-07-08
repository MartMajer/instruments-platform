using Platform.Domain.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

/// <summary>
/// Builds the authenticated app-branding view from stored tenant columns,
/// pre-computing the contrast-guarded accent so the Settings preview and the
/// write response agree on what respondents actually see. One source of truth
/// for the read store and the write store alike.
/// </summary>
public static class TenantAppBrandingResponseFactory
{
    public static TenantSettingsAppBrandingResponse Create(
        string? organizationLabel,
        string tenantName,
        string? accentColorHex,
        string? logoObjectKey,
        string? logoContentType,
        DateTimeOffset? updatedAt)
    {
        var effectiveAccent = string.IsNullOrWhiteSpace(accentColorHex)
            ? null
            : AccentContrastGuard.EnsureLegibleOnWhite(accentColorHex);

        var orgLabel = string.IsNullOrWhiteSpace(organizationLabel) ? tenantName : organizationLabel!;

        return new TenantSettingsAppBrandingResponse(
            orgLabel,
            accentColorHex,
            effectiveAccent,
            HasLogo: !string.IsNullOrWhiteSpace(logoObjectKey),
            logoContentType,
            Tenant.DefaultAppBrandingAccentColorHex,
            Tenant.AppBrandingAllowedLogoContentTypes,
            TenantAppBrandingLogo.MaxBytes,
            TenantAppBrandingLogo.MaxDimension,
            updatedAt);
    }
}
