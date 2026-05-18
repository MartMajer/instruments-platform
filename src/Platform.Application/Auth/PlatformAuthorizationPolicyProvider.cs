using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Platform.Application.Auth;

public sealed class PlatformAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var configuredPolicy = await base.GetPolicyAsync(policyName);
        if (configuredPolicy is not null)
        {
            return configuredPolicy;
        }

        if (!policyName.StartsWith(PlatformPolicies.PermissionPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var permission = policyName[PlatformPolicies.PermissionPrefix.Length..];
        if (string.IsNullOrWhiteSpace(permission))
        {
            return null;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}
