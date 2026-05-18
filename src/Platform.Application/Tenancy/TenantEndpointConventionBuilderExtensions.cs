using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Platform.Application.Tenancy;

public static class TenantEndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequireTenantContext(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<RequireTenantContextFilter>();
        return builder;
    }

    public static RouteGroupBuilder RequireTenantContext(this RouteGroupBuilder builder)
    {
        builder.AddEndpointFilter<RequireTenantContextFilter>();
        return builder;
    }
}
