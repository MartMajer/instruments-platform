using Microsoft.AspNetCore.Http;

namespace Platform.Application.Tenancy;

public sealed class RequireTenantContextFilter(ICurrentTenant currentTenant) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!currentTenant.HasTenant)
        {
            return ValueTask.FromResult<object?>(
                Results.Problem(
                    title: "Tenant context required",
                    detail: "This endpoint requires a resolved tenant context.",
                    statusCode: StatusCodes.Status400BadRequest));
        }

        return next(context);
    }
}
