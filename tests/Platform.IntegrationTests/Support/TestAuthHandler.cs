using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Auth;

namespace Platform.IntegrationTests.Support;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-User-Id";
    public const string TenantMembershipsHeader = "X-Test-Tenant-Memberships";
    public const string PermissionsHeader = "X-Test-Permissions";
    public const string EmailHeader = "X-Test-Email";
    public const string EmailVerifiedHeader = "X-Test-Email-Verified";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
            !Guid.TryParse(userIdValues.SingleOrDefault(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(PlatformClaimTypes.UserId, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (Request.Headers.TryGetValue(EmailHeader, out var emailValues))
        {
            var email = emailValues.SingleOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }
        }

        if (Request.Headers.TryGetValue(EmailVerifiedHeader, out var emailVerifiedValues))
        {
            var emailVerified = emailVerifiedValues.SingleOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(emailVerified))
            {
                claims.Add(new Claim(PlatformClaimTypes.EmailVerified, emailVerified));
            }
        }

        foreach (var tenantId in SplitHeader(TenantMembershipsHeader))
        {
            claims.Add(new Claim(PlatformClaimTypes.TenantMembership, tenantId));
        }

        foreach (var permission in SplitHeader(PermissionsHeader))
        {
            claims.Add(new Claim(PlatformClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private IEnumerable<string> SplitHeader(string headerName)
    {
        if (!Request.Headers.TryGetValue(headerName, out var values))
        {
            return [];
        }

        return values
            .SelectMany(value => value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [])
            .Select(value => value.Trim())
            .Where(value => value.Length > 0);
    }
}
