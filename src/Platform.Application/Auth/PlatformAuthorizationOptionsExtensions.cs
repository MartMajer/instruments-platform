using Microsoft.AspNetCore.Authorization;

namespace Platform.Application.Auth;

public static class PlatformAuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddPlatformPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(
            PlatformPolicies.AuthenticatedUser,
            policy => policy.RequireAuthenticatedUser());

        options.AddPolicy(
            PlatformPolicies.TenantMember,
            policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new TenantMemberRequirement()));

        return options;
    }
}
