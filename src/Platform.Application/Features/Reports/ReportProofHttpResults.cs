using System.Text;
using Microsoft.AspNetCore.Http;
using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public static class ReportProofHttpResults
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

    public static IResult ToFile(Result<ExportArtifactDownloadResponse> result)
    {
        if (result.IsSuccess)
        {
            return Results.File(
                result.Value.ContentBytes ?? Encoding.UTF8.GetBytes(result.Value.Content),
                result.Value.ContentType,
                result.Value.FileName);
        }

        return ToProblem(result.Error);
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
