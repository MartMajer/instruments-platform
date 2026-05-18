using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Api.Auth;

public static class AuthEndpointRouteBuilderExtensions
{
    public const string TenantIdPropertyName = "tenant_id";

    public const string RegistrationTokenPropertyName = "registration_token";

    public const string RegistrationBootstrapPropertyName = "registration_bootstrap";

    public static IEndpointRouteBuilder MapPlatformAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .WithTags("Auth");

        app.MapGet("/auth/logout", Logout)
            .AllowAnonymous()
            .WithName("Logout")
            .WithTags("Auth");

        app.MapGet("/auth/csrf", GetCsrf)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCsrf")
            .WithTags("Auth");

        return app;
    }

    private static IResult Login(
        HttpContext context,
        [FromServices] IConfiguration configuration)
    {
        var tenantIdValue = context.Request.Query["tenantId"].SingleOrDefault();
        var registrationToken = context.Request.Query["registrationToken"].SingleOrDefault()?.Trim();
        var hasRegistrationBootstrap = IsRegistrationBootstrap(
            context.Request.Query["registration"].SingleOrDefault());
        var hasTenantId = !string.IsNullOrWhiteSpace(tenantIdValue);
        var hasRegistrationToken = !string.IsNullOrWhiteSpace(registrationToken);

        var loginContextCount = Convert.ToInt32(hasTenantId) +
            Convert.ToInt32(hasRegistrationToken) +
            Convert.ToInt32(hasRegistrationBootstrap);
        if (loginContextCount != 1)
        {
            return Results.Problem(
                title: "Invalid login context",
                detail: "Provide tenantId for existing tenant login, registrationToken for legacy registration completion, or registration=1 for new workspace signup.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Guid tenantId = default;
        if (hasTenantId && !Guid.TryParse(tenantIdValue, out tenantId))
        {
            return Results.Problem(
                title: "Invalid login tenant",
                detail: "tenantId must be a UUID value.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasRegistrationToken && registrationToken!.Length > 512)
        {
            return Results.Problem(
                title: "Invalid registration token",
                detail: "registrationToken is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var returnUrl = AuthReturnUrl.Normalize(
            context.Request.Query["returnUrl"].SingleOrDefault(),
            hasRegistrationBootstrap ? "/register" : "/app",
            configuration);
        if (returnUrl is null)
        {
            return Results.Problem(
                title: "Invalid return URL",
                detail: "returnUrl must be a local application path.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var requestedPrompt = NormalizePrompt(context.Request.Query["prompt"].SingleOrDefault());
        if (requestedPrompt is null)
        {
            return Results.Problem(
                title: "Invalid login prompt",
                detail: "prompt must be login, consent, or select_account when provided.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        var screenHint = NormalizeScreenHint(context.Request.Query["screen_hint"].SingleOrDefault());
        if (screenHint is null)
        {
            return Results.Problem(
                title: "Invalid login screen hint",
                detail: "screen_hint must be signup when provided.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var prompt = hasTenantId ? requestedPrompt : "login";
        if (hasRegistrationBootstrap && string.IsNullOrEmpty(screenHint))
        {
            screenHint = "signup";
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };
        if (hasTenantId)
        {
            properties.Items[TenantIdPropertyName] = tenantId.ToString();
        }
        else if (hasRegistrationToken)
        {
            properties.Items[RegistrationTokenPropertyName] = registrationToken!;
        }
        else
        {
            properties.Items[RegistrationBootstrapPropertyName] = "true";
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            properties.SetParameter("prompt", prompt);
        }
        if (!string.IsNullOrEmpty(screenHint))
        {
            properties.SetParameter("screen_hint", screenHint);
        }

        return Results.Challenge(properties, [PlatformAuthenticationSchemes.Oidc]);
    }

    private static async Task<IResult> Logout(
        HttpContext context,
        [FromServices] IConfiguration configuration,
        [FromServices] IPlatformSessionRevoker revoker)
    {
        var returnUrl = AuthReturnUrl.Normalize(
            context.Request.Query["returnUrl"].SingleOrDefault(),
            "/",
            configuration);
        if (returnUrl is null)
        {
            return Results.Problem(
                title: "Invalid return URL",
                detail: "returnUrl must be a local application path.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await revoker.RevokeAsync(context.User, context.RequestAborted);

        await context.SignOutAsync(PlatformAuthenticationSchemes.AppCookie);
        return Results.Redirect(returnUrl);
    }

    private static IResult GetCsrf(
        HttpContext context,
        [FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);

        return Results.Ok(new { csrfToken = tokens.RequestToken ?? string.Empty });
    }

    private static string? NormalizePrompt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var prompt = value.Trim();

        return prompt is "login" or "consent" or "select_account"
            ? prompt
            : null;
    }

    private static bool IsRegistrationBootstrap(string? value)
    {
        return string.Equals(value?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeScreenHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var screenHint = value.Trim();

        return screenHint is "signup" ? screenHint : null;
    }
}
