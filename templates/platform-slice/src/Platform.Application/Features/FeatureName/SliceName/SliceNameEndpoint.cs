using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.SharedKernel;

namespace Platform.Application.Features.FeatureName.SliceName;

public static class SliceNameEndpoint
{
    public static IEndpointRouteBuilder MapSliceNameEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/FeatureName/SliceName",
                async (ISender sender, CancellationToken cancellationToken) =>
                    ToHttpResult(await sender.Send(new SliceNameCommand(), cancellationToken)))
            .WithName("SliceName");

        return app;
    }

    private static IResult ToHttpResult(Result<SliceNameResponse> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: ToStatusCode(result.Error.Type));
    }

    private static int ToStatusCode(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
