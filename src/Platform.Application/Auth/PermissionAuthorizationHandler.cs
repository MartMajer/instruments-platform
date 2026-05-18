using Microsoft.AspNetCore.Authorization;

namespace Platform.Application.Auth;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = PlatformClaimValues
            .Read(context.User, PlatformClaimTypes.Permission)
            .Any(permission => string.Equals(permission, requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
