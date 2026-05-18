using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Platform.Application.Auth;

namespace Platform.Api.Auth;

public sealed class CookieCsrfProtectionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!RequiresValidation(context))
        {
            await next(context);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("csrf.validation_failed", context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static bool RequiresValidation(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
        {
            return false;
        }

        if (IsAuthPath(context.Request.Path))
        {
            return false;
        }

        if (context.User.Identity is not { IsAuthenticated: true } identity ||
            identity.AuthenticationType != PlatformAuthenticationSchemes.AppCookie)
        {
            return false;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        return authorizeData.Any(data =>
            string.Equals(data.Policy, PlatformPolicies.TenantMember, StringComparison.Ordinal));
    }

    private static bool IsAuthPath(PathString path)
    {
        return path.StartsWithSegments("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/logout", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/callback", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/signout-callback", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/csrf", StringComparison.OrdinalIgnoreCase);
    }
}

public static class CookieCsrfProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseCookieCsrfProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CookieCsrfProtectionMiddleware>();
    }
}
