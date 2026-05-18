using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Platform.Application.Auth;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;

namespace Platform.Api.Auth;

public interface IPlatformSessionValidator
{
    Task<bool> ValidateAsync(
        Guid sessionId,
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken);
}

public sealed class PlatformSessionCookieEvents(
    IPlatformSessionValidator sessionValidator) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!TryGetPlatformSessionClaims(context.Principal, out var sessionId, out var userId, out var tenantId))
        {
            if (IsRegistrationBootstrapPrincipal(context.Principal))
            {
                return;
            }

            await RejectAsync(context);
            return;
        }

        var isValid = await sessionValidator.ValidateAsync(
            sessionId,
            userId,
            tenantId,
            context.HttpContext.RequestAborted);

        if (!isValid)
        {
            await RejectAsync(context);
        }
    }

    private static bool IsRegistrationBootstrapPrincipal(ClaimsPrincipal? principal)
    {
        return principal?.Identity?.IsAuthenticated == true &&
            string.Equals(
                principal.FindFirst(PlatformRegistrationClaimTypes.Pending)?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(principal.FindFirst(PlatformRegistrationClaimTypes.Email)?.Value) &&
            !string.IsNullOrWhiteSpace(principal.FindFirst(PlatformRegistrationClaimTypes.Provider)?.Value) &&
            !string.IsNullOrWhiteSpace(principal.FindFirst(PlatformRegistrationClaimTypes.ProviderSubjectHash)?.Value);
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        return CompleteApiRedirectAsync(context, StatusCodes.Status401Unauthorized);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        return CompleteApiRedirectAsync(context, StatusCodes.Status403Forbidden);
    }

    private static bool TryGetPlatformSessionClaims(
        ClaimsPrincipal? principal,
        out Guid sessionId,
        out Guid userId,
        out Guid tenantId)
    {
        sessionId = default;
        userId = default;
        tenantId = default;

        var tenantClaims = principal?
            .FindAll(PlatformClaimTypes.TenantMembership)
            .Select(claim => claim.Value)
            .ToArray() ?? [];

        return Guid.TryParse(principal?.FindFirst(PlatformClaimTypes.SessionId)?.Value, out sessionId) &&
            Guid.TryParse(principal?.FindFirst(PlatformClaimTypes.UserId)?.Value, out userId) &&
            tenantClaims.Length == 1 &&
            Guid.TryParse(tenantClaims[0], out tenantId);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(PlatformAuthenticationSchemes.AppCookie);
    }

    private static Task CompleteApiRedirectAsync(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        if (AcceptsHtml(context.Request))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }

    private static bool AcceptsHtml(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
            requestedWith.Contains("XMLHttpRequest"))
        {
            return false;
        }

        var accept = request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class EfPlatformSessionValidator(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope) : IPlatformSessionValidator
{
    public async Task<bool> ValidateAsync(
        Guid sessionId,
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var session = await (
                from authSession in db.AuthSessions.AsNoTracking()
                join identity in db.ExternalAuthIdentities.AsNoTracking()
                    on new { Id = authSession.ExternalAuthIdentityId, authSession.TenantId }
                    equals new { identity.Id, identity.TenantId }
                where authSession.Id == sessionId &&
                    authSession.TenantId == tenantId
                select new
                {
                    authSession.UserId,
                    authSession.ExpiresAt,
                    authSession.RevokedAt,
                    IdentityDisabledAt = identity.DisabledAt
                })
            .SingleOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return session is not null &&
            session.UserId == userId &&
            session.RevokedAt is null &&
            session.ExpiresAt > now &&
            session.IdentityDisabledAt is null;
    }
}
