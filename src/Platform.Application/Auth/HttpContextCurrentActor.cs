using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Platform.Application.Tenancy;

namespace Platform.Application.Auth;

public sealed class HttpContextCurrentActor(
    IHttpContextAccessor httpContextAccessor,
    ICurrentTenant currentTenant) : ICurrentActor
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return null;
            }

            var value = principal.FindFirstValue(PlatformClaimTypes.UserId) ??
                principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public Guid? TenantId => currentTenant.HasTenant ? currentTenant.TenantId : null;

    public string? Email
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public bool EmailVerificationRequired
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(PlatformClaimTypes.EmailVerified);

            return bool.TryParse(value, out var emailVerified) && !emailVerified;
        }
    }

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return [];
            }

            return PlatformClaimValues
                .Read(principal, PlatformClaimTypes.Permission)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
