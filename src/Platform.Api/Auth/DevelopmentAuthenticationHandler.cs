using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Platform.Application.Auth;

namespace Platform.Api.Auth;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<DevelopmentAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<DevelopmentAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DevelopmentAuthenticationOptions.UserIdHeader, out var userIdValues) ||
            !Guid.TryParse(userIdValues.SingleOrDefault(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(PlatformClaimTypes.UserId, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (Request.Headers.TryGetValue(DevelopmentAuthenticationOptions.EmailHeader, out var emailValues))
        {
            var email = emailValues.SingleOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }
        }

        foreach (var tenantId in SplitHeader(DevelopmentAuthenticationOptions.TenantMembershipsHeader))
        {
            claims.Add(new Claim(PlatformClaimTypes.TenantMembership, tenantId));
        }

        foreach (var permission in SplitHeader(DevelopmentAuthenticationOptions.PermissionsHeader))
        {
            claims.Add(new Claim(PlatformClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, DevelopmentAuthenticationOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevelopmentAuthenticationOptions.SchemeName);

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
