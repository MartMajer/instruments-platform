using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Setup;
using Platform.SharedKernel;

namespace Platform.UnitTests.Application;

public sealed class SetupHttpResultsTests
{
    [Fact]
    public async Task ToResult_maps_success_to_ok()
    {
        var result = Result.Success(new SetupIdResponse(Guid.NewGuid()));

        var httpResult = SetupHttpResults.ToOk(result);
        var context = CreateHttpContext();
        await httpResult.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ToResult_maps_not_found_to_404()
    {
        var result = Result.Failure<SetupIdResponse>(
            Error.NotFound("setup.not_found", "Missing."));

        var httpResult = SetupHttpResults.ToOk(result);
        var context = CreateHttpContext();
        await httpResult.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task ToResult_maps_validation_to_400()
    {
        var result = Result.Failure<SetupIdResponse>(
            Error.Validation("setup.invalid", "Invalid."));

        var httpResult = SetupHttpResults.ToOk(result);
        var context = CreateHttpContext();
        await httpResult.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services
        };
    }
}
