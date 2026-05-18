using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Platform.Application.Features.System.GetHealth;

public static class GetHealthEndpoint
{
    public static IEndpointRouteBuilder MapGetHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/health",
                async (ISender sender, CancellationToken cancellationToken) =>
                    ToHttpResult(await sender.Send(new GetHealthQuery(), cancellationToken)))
            .AllowAnonymous()
            .WithName("Health");

        app.MapGet(
                "/health/live",
                async (ISender sender, CancellationToken cancellationToken) =>
                    ToHttpResult(await sender.Send(new GetHealthQuery(HealthProbeKind.Live), cancellationToken)))
            .AllowAnonymous()
            .WithName("HealthLive");

        app.MapGet(
                "/health/ready",
                async (ISender sender, CancellationToken cancellationToken) =>
                    ToHttpResult(await sender.Send(new GetHealthQuery(HealthProbeKind.Ready), cancellationToken)))
            .AllowAnonymous()
            .WithName("HealthReady");

        app.MapGet(
                "/health/startup",
                async (ISender sender, CancellationToken cancellationToken) =>
                    ToHttpResult(await sender.Send(new GetHealthQuery(HealthProbeKind.Startup), cancellationToken)))
            .AllowAnonymous()
            .WithName("HealthStartup");

        return app;
    }

    private static IResult ToHttpResult(GetHealthResponse response)
    {
        return response.Status == "ok"
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
