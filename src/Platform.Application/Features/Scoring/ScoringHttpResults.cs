using Microsoft.AspNetCore.Http;
using Platform.SharedKernel;

namespace Platform.Application.Features.Scoring;

public static class ScoringHttpResults
{
    public static IResult ToOk<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var status = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: status);
    }
}
