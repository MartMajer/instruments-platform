using Platform.Application.Features.System.GetHealth;

namespace Platform.Api.Health;

public sealed class RequiredConfigurationHealthCheck(IConfiguration configuration) : IPlatformHealthCheck
{
    public string Name => "configuration";

    public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var isReady = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("PlatformDb"));

        return Task.FromResult(isReady
            ? PlatformHealthCheckResult.Ok(Name)
            : PlatformHealthCheckResult.Unready(Name));
    }
}
