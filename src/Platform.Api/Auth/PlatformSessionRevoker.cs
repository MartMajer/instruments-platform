using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;

namespace Platform.Api.Auth;

public interface IPlatformSessionRevoker
{
    Task RevokeAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken);
}

public interface IPlatformSessionRevocationStore
{
    Task RevokeAsync(
        Guid sessionId,
        Guid userId,
        Guid tenantId,
        string reason,
        CancellationToken cancellationToken);
}

/// <summary>
/// Used when no revocable session store exists (development header auth and
/// JWT bearer modes). Logout has nothing to revoke; the endpoint must still work.
/// </summary>
public sealed class NoOpPlatformSessionRevoker : IPlatformSessionRevoker
{
    public Task RevokeAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class PlatformSessionRevoker(
    IPlatformSessionRevocationStore store) : IPlatformSessionRevoker
{
    private const string LogoutReason = "logout";

    public Task RevokeAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        return TryGetPlatformSessionClaims(principal, out var sessionId, out var userId, out var tenantId)
            ? store.RevokeAsync(sessionId, userId, tenantId, LogoutReason, cancellationToken)
            : Task.CompletedTask;
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
}

public sealed class EfPlatformSessionRevocationStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    ICurrentTenant currentTenant) : IPlatformSessionRevocationStore
{
    public async Task RevokeAsync(
        Guid sessionId,
        Guid userId,
        Guid tenantId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (currentTenant.HasTenant && currentTenant.TenantId != tenantId)
        {
            return;
        }

        if (!currentTenant.HasTenant)
        {
            currentTenant.SetTenant(tenantId, "session");
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            userId,
            cancellationToken: cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var session = await db.AuthSessions
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == sessionId &&
                candidate.TenantId == tenantId &&
                candidate.UserId == userId,
                cancellationToken);

        if (session is not null && session.IsActive(now))
        {
            session.Revoke(now, reason);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
