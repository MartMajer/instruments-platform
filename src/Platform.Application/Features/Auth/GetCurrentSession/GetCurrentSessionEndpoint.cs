using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Auth.GetCurrentSession;

public static class GetCurrentSessionEndpoint
{
    public static RouteHandlerBuilder MapGetCurrentSession(this IEndpointRouteBuilder app)
    {
        return app
            .MapGet("/auth/session", GetCurrentSession)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCurrentSession")
            .WithTags("Auth");
    }

    private static IResult GetCurrentSession(ICurrentActor actor)
    {
        if (!actor.UserId.HasValue || !actor.TenantId.HasValue)
        {
            return Results.Forbid();
        }

        return Results.Ok(new GetCurrentSessionResponse(
            actor.UserId.Value,
            actor.TenantId.Value,
            actor.Email,
            actor.EmailVerificationRequired,
            actor.Permissions.ToArray()));
    }
}
