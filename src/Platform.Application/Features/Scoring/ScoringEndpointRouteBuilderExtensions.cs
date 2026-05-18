using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Scoring;

public static class ScoringEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapScoringEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/respondent/sessions/{sessionId:guid}/scores", ComputeResponseScores)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ComputeResponseScores")
            .WithTags("Scoring");

        return app;
    }

    private static async Task<IResult> ComputeResponseScores(
        Guid sessionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ComputeResponseScoresCommand(sessionId), cancellationToken);

        return ScoringHttpResults.ToOk(result);
    }
}
