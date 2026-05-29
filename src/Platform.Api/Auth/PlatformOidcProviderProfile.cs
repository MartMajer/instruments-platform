using System.Security.Claims;

namespace Platform.Api.Auth;

public sealed class PlatformOidcProviderProfile
{
    public const string Auth0LogoutMode = "auth0";
    public const string MicrosoftLogoutMode = "microsoft";
    public const string NoProviderLogoutMode = "none";

    private PlatformOidcProviderProfile(
        string providerKey,
        string emailClaim,
        string subjectClaim,
        string? subjectTenantClaim,
        string emailVerifiedClaim,
        bool assumeEmailVerifiedWhenClaimMissing,
        string providerLogoutMode)
    {
        ProviderKey = providerKey;
        EmailClaim = emailClaim;
        SubjectClaim = subjectClaim;
        SubjectTenantClaim = subjectTenantClaim;
        EmailVerifiedClaim = emailVerifiedClaim;
        AssumeEmailVerifiedWhenClaimMissing = assumeEmailVerifiedWhenClaimMissing;
        ProviderLogoutMode = providerLogoutMode;
    }

    public string ProviderKey { get; }

    public string EmailClaim { get; }

    public string SubjectClaim { get; }

    public string? SubjectTenantClaim { get; }

    public string EmailVerifiedClaim { get; }

    public bool AssumeEmailVerifiedWhenClaimMissing { get; }

    public string ProviderLogoutMode { get; }

    public static PlatformOidcProviderProfile From(IConfiguration configuration)
    {
        var oidc = configuration.GetSection("Authentication:Oidc");

        return new PlatformOidcProviderProfile(
            NormalizeProviderKey(oidc["ProviderKey"]),
            NormalizeClaimName(oidc["EmailClaim"], "email"),
            NormalizeClaimName(oidc["SubjectClaim"], "sub"),
            NormalizeOptionalClaimName(oidc["SubjectTenantClaim"]),
            NormalizeClaimName(oidc["EmailVerifiedClaim"], "email_verified"),
            oidc.GetValue("AssumeEmailVerifiedWhenClaimMissing", false),
            NormalizeProviderLogoutMode(oidc["ProviderLogoutMode"]));
    }

    public string? GetEmail(ClaimsPrincipal? principal)
    {
        return GetClaimValue(principal, EmailClaim);
    }

    public string? GetProviderSubject(ClaimsPrincipal? principal)
    {
        var subject = GetClaimValue(principal, SubjectClaim);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(SubjectTenantClaim))
        {
            return subject;
        }

        var subjectTenant = GetClaimValue(principal, SubjectTenantClaim);
        return string.IsNullOrWhiteSpace(subjectTenant)
            ? null
            : $"{subjectTenant}:{subject}";
    }

    public bool IsEmailVerified(ClaimsPrincipal? principal)
    {
        var value = GetClaimValue(principal, EmailVerifiedClaim);
        if (string.IsNullOrWhiteSpace(value))
        {
            return AssumeEmailVerifiedWhenClaimMissing;
        }

        return bool.TryParse(value, out var verified) && verified;
    }

    private static string NormalizeProviderKey(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "auth0"
            : value.Trim().ToLowerInvariant();

        return normalized.Length > 80
            ? normalized[..80]
            : normalized;
    }

    private static string NormalizeClaimName(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static string? NormalizeOptionalClaimName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeProviderLogoutMode(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? Auth0LogoutMode
            : value.Trim().ToLowerInvariant();

        return normalized is MicrosoftLogoutMode or NoProviderLogoutMode
            ? normalized
            : Auth0LogoutMode;
    }

    private static string? GetClaimValue(ClaimsPrincipal? principal, string claimType)
    {
        return principal?.FindFirst(claimType)?.Value?.Trim();
    }
}
