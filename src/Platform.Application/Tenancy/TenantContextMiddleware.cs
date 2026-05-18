using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Platform.Application.Tenancy;

public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
    {
        if (IsHealthProbePath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) ||
            StringValues.IsNullOrEmpty(values))
        {
            await next(context);
            return;
        }

        if (values.Count != 1 || !Guid.TryParse(values[0], out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Results.Problem(
                    title: "Invalid tenant context",
                    detail: $"{HeaderName} must be a single UUID value.",
                    statusCode: StatusCodes.Status400BadRequest)
                .ExecuteAsync(context);
            return;
        }

        currentTenant.SetTenant(tenantId, "header");

        await next(context);
    }

    private static bool IsHealthProbePath(PathString path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }
}
