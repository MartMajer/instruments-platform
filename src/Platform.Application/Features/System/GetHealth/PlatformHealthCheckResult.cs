namespace Platform.Application.Features.System.GetHealth;

public sealed record PlatformHealthCheckResult(string Name, PlatformHealthCheckStatus Status)
{
    public static PlatformHealthCheckResult Ok(string name)
    {
        return new PlatformHealthCheckResult(name, PlatformHealthCheckStatus.Ok);
    }

    public static PlatformHealthCheckResult Unready(string name)
    {
        return new PlatformHealthCheckResult(name, PlatformHealthCheckStatus.Unready);
    }
}
