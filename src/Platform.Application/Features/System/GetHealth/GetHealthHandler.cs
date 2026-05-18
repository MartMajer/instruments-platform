using MediatR;

namespace Platform.Application.Features.System.GetHealth;

public sealed class GetHealthHandler(IEnumerable<IPlatformHealthCheck> healthChecks)
    : IRequestHandler<GetHealthQuery, GetHealthResponse>
{
    public async Task<GetHealthResponse> Handle(
        GetHealthQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Probe is HealthProbeKind.Summary or HealthProbeKind.Live)
        {
            return new GetHealthResponse("instruments-platform", "ok", []);
        }

        var checks = new List<GetHealthCheckResponse>();
        var isReady = true;

        foreach (var healthCheck in healthChecks)
        {
            PlatformHealthCheckResult result;
            try
            {
                result = await healthCheck.CheckAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                result = PlatformHealthCheckResult.Unready(healthCheck.Name);
            }

            if (result.Status is PlatformHealthCheckStatus.Unready)
            {
                isReady = false;
            }

            checks.Add(new GetHealthCheckResponse(
                result.Name,
                FormatStatus(result.Status)));
        }

        return new GetHealthResponse(
            "instruments-platform",
            isReady ? "ok" : "unready",
            checks);
    }

    private static string FormatStatus(PlatformHealthCheckStatus status)
    {
        return status switch
        {
            PlatformHealthCheckStatus.Ok => "ok",
            PlatformHealthCheckStatus.Unready => "unready",
            _ => "unready"
        };
    }
}
