using Microsoft.AspNetCore.Authorization;
using Platform.Application.Tenancy;

namespace Platform.Application.Auth;

public sealed class TenantMemberAuthorizationHandler(ICurrentTenant currentTenant)
    : AuthorizationHandler<TenantMemberRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMemberRequirement requirement)
    {
        if (!currentTenant.HasTenant)
        {
            return Task.CompletedTask;
        }

        var hasMembership = PlatformClaimValues
            .Read(context.User, PlatformClaimTypes.TenantMembership)
            .Any(value => Guid.TryParse(value, out var tenantId) && tenantId == currentTenant.TenantId);

        if (hasMembership)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
