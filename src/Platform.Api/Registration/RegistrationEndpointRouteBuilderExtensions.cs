using Platform.SharedKernel;

namespace Platform.Api.Registration;

public sealed record CreateRegistrationIntentRequest(
    string Email,
    string OrganizationName,
    string AccessCode,
    string ReturnUrl = "/app");

public sealed record CreateRegistrationIntentResponse(
    string LoginUrl,
    DateTimeOffset ExpiresAt);

public interface IRegistrationIntentService
{
    Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
        CreateRegistrationIntentRequest request,
        CancellationToken cancellationToken);
}

public sealed class RegistrationIntentService : IRegistrationIntentService
{
    public Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
        CreateRegistrationIntentRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CreateRegistrationIntentResponse>(
            Error.Forbidden("registration.disabled", "Private beta registration is not enabled.")));
    }
}

public static class RegistrationServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformRegistration(this IServiceCollection services)
    {
        services.AddScoped<IRegistrationIntentService, RegistrationIntentService>();

        return services;
    }
}

public static class RegistrationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registration/intents", CreateRegistrationIntent)
            .AllowAnonymous()
            .WithName("CreateRegistrationIntent")
            .WithTags("Registration");

        return app;
    }

    private static async Task<IResult> CreateRegistrationIntent(
        CreateRegistrationIntentRequest request,
        IRegistrationIntentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error);
    }

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
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
            statusCode: statusCode);
    }
}