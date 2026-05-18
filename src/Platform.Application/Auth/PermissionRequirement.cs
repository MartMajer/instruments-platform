using Microsoft.AspNetCore.Authorization;

namespace Platform.Application.Auth;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
