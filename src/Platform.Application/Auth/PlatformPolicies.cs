namespace Platform.Application.Auth;

public static class PlatformPolicies
{
    public const string AuthenticatedUser = "platform.authenticated_user";
    public const string TenantMember = "platform.tenant_member";

    public const string PermissionPrefix = "platform.permission:";

    public static string Permission(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return PermissionPrefix + permission;
    }
}
