using Microsoft.Extensions.Configuration;

namespace Platform.Api.Auth;

public static class AuthReturnUrl
{
    public static string? Normalize(
        string? value,
        string fallback,
        IConfiguration configuration)
    {
        var returnUrl = string.IsNullOrWhiteSpace(value) ? fallback : value;

        if (IsLocalReturnUrl(returnUrl) || IsAllowedBrowserOriginReturnUrl(returnUrl, configuration))
        {
            return returnUrl;
        }

        return null;
    }

    public static string AppendQuery(string returnUrl, string name, string value)
    {
        var hashIndex = returnUrl.IndexOf('#', StringComparison.Ordinal);
        var pathAndQuery = hashIndex >= 0 ? returnUrl[..hashIndex] : returnUrl;
        var hash = hashIndex >= 0 ? returnUrl[hashIndex..] : string.Empty;
        var separator = pathAndQuery.Contains('?', StringComparison.Ordinal) ? "&" : "?";

        return string.Concat(
            pathAndQuery,
            separator,
            Uri.EscapeDataString(name),
            "=",
            Uri.EscapeDataString(value),
            hash);
    }

    private static bool IsLocalReturnUrl(string returnUrl)
    {
        return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.Contains("\\", StringComparison.Ordinal);
    }

    private static bool IsAllowedBrowserOriginReturnUrl(
        string returnUrl,
        IConfiguration configuration)
    {
        if (returnUrl.Contains("\\", StringComparison.Ordinal) ||
            !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        var allowedOrigins = PlatformAuthServiceCollectionExtensions.GetBrowserCorsOrigins(
            configuration,
            includeDevelopmentFallback: false);
        var returnOrigin = uri.GetLeftPart(UriPartial.Authority);

        return allowedOrigins.Contains(returnOrigin, StringComparer.Ordinal);
    }
}
