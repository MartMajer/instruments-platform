using System.Security.Claims;

namespace Platform.Api.Auth;

public sealed class PlatformOidcProviderProfile
{
    public const string Auth0ProviderKey = "auth0";
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
        string providerLogoutMode,
        string? signupScreenHint)
    {
        ProviderKey = providerKey;
        EmailClaim = emailClaim;
        SubjectClaim = subjectClaim;
        SubjectTenantClaim = subjectTenantClaim;
        EmailVerifiedClaim = emailVerifiedClaim;
        AssumeEmailVerifiedWhenClaimMissing = assumeEmailVerifiedWhenClaimMissing;
        ProviderLogoutMode = providerLogoutMode;
        SignupScreenHint = signupScreenHint;
    }

    public string ProviderKey { get; }

    public string EmailClaim { get; }

    public string SubjectClaim { get; }

    public string? SubjectTenantClaim { get; }

    public string EmailVerifiedClaim { get; }

    public bool AssumeEmailVerifiedWhenClaimMissing { get; }

    public string ProviderLogoutMode { get; }

    public string? SignupScreenHint { get; }

    public static PlatformOidcProviderProfile From(IConfiguration configuration)
    {
        var oidc = configuration.GetSection("Authentication:Oidc");
        var providerKey = NormalizeProviderKey(oidc["ProviderKey"]);

        return new PlatformOidcProviderProfile(
            providerKey,
            NormalizeClaimName(oidc["EmailClaim"], "email"),
            NormalizeClaimName(oidc["SubjectClaim"], "sub"),
            NormalizeOptionalClaimName(oidc["SubjectTenantClaim"]),
            NormalizeClaimName(oidc["EmailVerifiedClaim"], "email_verified"),
            oidc.GetValue("AssumeEmailVerifiedWhenClaimMissing", false),
            NormalizeProviderLogoutMode(oidc["ProviderLogoutMode"]),
            NormalizeSignupScreenHint(oidc["SignupScreenHint"], providerKey));
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
            ? Auth0ProviderKey
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

    private static string? NormalizeSignupScreenHint(string? value, string providerKey)
    {
        if (value is null)
        {
            return string.Equals(providerKey, Auth0ProviderKey, StringComparison.Ordinal)
                ? "signup"
                : null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized == "signup" ? normalized : null;
    }

    private static string? GetClaimValue(ClaimsPrincipal? principal, string claimType)
    {
        return principal?.FindFirst(claimType)?.Value?.Trim();
    }
}
