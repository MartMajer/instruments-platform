using FluentValidation;
using Platform.Api.Auth;
using Platform.Api.Health;
using Platform.Api.RateLimiting;
using Platform.Application;
using Platform.Application.Features.Auth.GetCurrentSession;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.Reports;
using Platform.Application.Features.Responses;
using Platform.Application.Features.Retention;
using Platform.Application.Features.Scoring;
using Platform.Application.Features.Setup;
using Platform.Application.Features.System.GetHealth;
using Platform.Application.Tenancy;
using Platform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var developmentAuthenticationEnabled =
    PlatformAuthServiceCollectionExtensions.IsDevelopmentAuthenticationEnabled(
        builder.Configuration,
        builder.Environment);
var browserCorsOrigins = PlatformAuthServiceCollectionExtensions.GetBrowserCorsOrigins(
    builder.Configuration,
    developmentAuthenticationEnabled);

builder.Services.AddPlatformAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddPlatformApplication();
builder.Services.AddPlatformInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IPlatformHealthCheck, RequiredConfigurationHealthCheck>();
builder.Services.AddSingleton<IPlatformHealthCheck, DevelopmentAuthenticationHealthCheck>();
builder.Services.AddSingleton<IPlatformHealthCheck, OidcConfigurationHealthCheck>();
builder.Services.AddPublicRespondentRateLimiting(builder.Configuration);

if (browserCorsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            PlatformAuthServiceCollectionExtensions.BrowserCorsPolicyName,
            policy => policy
                .WithOrigins(browserCorsOrigins)
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (ValidationException exception) when (!context.Response.HasStarted)
    {
        var detail = string.Join(
            "; ",
            exception.Errors
                .Select(failure => failure.ErrorMessage)
                .Distinct(StringComparer.Ordinal));

        await Results.Problem(
                title: "validation.failed",
                detail: detail,
                statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(context);
    }
});

app.UseRouting();
app.UseTenantContext();
if (browserCorsOrigins.Length > 0)
{
    app.UseCors(PlatformAuthServiceCollectionExtensions.BrowserCorsPolicyName);
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseCookieCsrfProtection();
app.UseAuthorization();

app.MapGetHealth();
app.MapPlatformAuthEndpoints();
app.MapGetCurrentSession();
app.MapSetupEndpoints();
app.MapProductSurfaceEndpoints();
app.MapNotificationDeliveryEndpoints();
app.MapWithdrawalEndpoints();
app.MapResponseCaptureEndpoints();
app.MapScoringEndpoints();
app.MapReportProofEndpoints();

app.Run();

public partial class Program;
