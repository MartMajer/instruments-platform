using Platform.Application.Features.System.GetHealth;

namespace Platform.Api.Auth;

public sealed class DevelopmentAuthenticationHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment)
    : IPlatformHealthCheck
{
    public string Name => "development_auth";

    public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var developmentAuthenticationConfigured =
            configuration.GetValue<bool>($"{DevelopmentAuthenticationOptions.SectionName}:Enabled");

        var isReady = environment.IsDevelopment() || !developmentAuthenticationConfigured;

        return Task.FromResult(isReady
            ? PlatformHealthCheckResult.Ok(Name)
            : PlatformHealthCheckResult.Unready(Name));
    }
}
