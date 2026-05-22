using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Platform.Api.Auth;
using Platform.Api.Health;
using Platform.Api.RateLimiting;
using Platform.Api.Registration;
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
using Platform.Application.Features.TestData;
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
var reverseProxyForwardedHeadersEnabled =
    builder.Configuration.GetValue("ReverseProxy:ForwardedHeaders:Enabled", false);

if (reverseProxyForwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddPlatformAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddPlatformApplication();
builder.Services.AddPlatformInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IPlatformHealthCheck, RequiredConfigurationHealthCheck>();
builder.Services.AddSingleton<IPlatformHealthCheck, DevelopmentAuthenticationHealthCheck>();
builder.Services.AddSingleton<IPlatformHealthCheck, OidcConfigurationHealthCheck>();
builder.Services.AddPublicRespondentRateLimiting(builder.Configuration);
builder.Services.AddPlatformRegistration();

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

if (reverseProxyForwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

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
if (browserCorsOrigins.Length > 0)
{
    app.UseCors(PlatformAuthServiceCollectionExtensions.BrowserCorsPolicyName);
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseTenantContext();
app.UseCookieCsrfProtection();
app.UseAuthorization();

app.MapGetHealth();
app.MapPlatformAuthEndpoints();
app.MapRegistrationEndpoints();
app.MapGetCurrentSession();
app.MapSetupEndpoints();
app.MapTestDataSimulatorEndpoints();
app.MapProductSurfaceEndpoints();
app.MapNotificationDeliveryEndpoints();
app.MapWithdrawalEndpoints();
app.MapResponseCaptureEndpoints();
app.MapScoringEndpoints();
app.MapReportProofEndpoints();

app.Run();

public partial class Program;
