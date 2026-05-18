using Platform.Application.Features.System.GetHealth;

namespace Platform.Api.Auth;

public sealed class OidcConfigurationHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment)
    : IPlatformHealthCheck
{
    public const string CheckName = "oidc_configuration";

    public string Name => CheckName;

    public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            return Task.FromResult(PlatformHealthCheckResult.Ok(Name));
        }

        var oidc = configuration.GetSection("Authentication:Oidc");
        var authority = oidc["Authority"];
        var audience = oidc["Audience"];
        var clientId = oidc["ClientId"];
        var clientSecret = oidc["ClientSecret"];
        var requireHttpsMetadata = oidc.GetValue("RequireHttpsMetadata", true);
        var interactiveEnabled = oidc.GetValue<bool>("InteractiveEnabled");

        var isReady =
            !string.IsNullOrWhiteSpace(authority) &&
            HasRequiredClientConfiguration(
                interactiveEnabled,
                audience,
                clientId,
                clientSecret) &&
            requireHttpsMetadata &&
            IsAbsoluteHttpsUri(authority);

        return Task.FromResult(isReady
            ? PlatformHealthCheckResult.Ok(Name)
            : PlatformHealthCheckResult.Unready(Name));
    }

    private static bool HasRequiredClientConfiguration(
        bool interactiveEnabled,
        string? audience,
        string? clientId,
        string? clientSecret)
    {
        if (interactiveEnabled)
        {
            return !string.IsNullOrWhiteSpace(clientId) &&
                !string.IsNullOrWhiteSpace(clientSecret);
        }

        return !string.IsNullOrWhiteSpace(audience);
    }

    internal static bool IsAbsoluteHttpsUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
