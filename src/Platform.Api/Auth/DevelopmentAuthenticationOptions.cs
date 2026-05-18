using Microsoft.AspNetCore.Authentication;

namespace Platform.Api.Auth;

public sealed class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "Development";
    public const string SectionName = "Authentication:Dev";
    public const string CorsPolicyName = "DevelopmentFrontend";

    public const string UserIdHeader = "X-Dev-User-Id";
    public const string TenantMembershipsHeader = "X-Dev-Tenant-Memberships";
    public const string PermissionsHeader = "X-Dev-Permissions";
    public const string EmailHeader = "X-Dev-Email";

    public bool Enabled { get; init; }

    public string[] AllowedOrigins { get; init; } =
    [
        "http://127.0.0.1:5173",
        "http://localhost:5173"
    ];
}
