namespace Platform.Application.Features.System.GetHealth;

public interface IPlatformHealthCheck
{
    string Name { get; }

    Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken);
}
