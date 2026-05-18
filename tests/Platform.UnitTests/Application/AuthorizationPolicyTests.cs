using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Platform.Application.Auth;

namespace Platform.UnitTests.Application;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public async Task Permission_policy_names_resolve_to_permission_requirements()
    {
        var options = new AuthorizationOptions();
        options.AddPlatformPolicies();
        var provider = new PlatformAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetPolicyAsync(PlatformPolicies.Permission("campaign.launch"));

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
        var permission = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
        Assert.Equal("campaign.launch", permission.Permission);
    }
}
