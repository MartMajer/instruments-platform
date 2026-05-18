namespace Platform.Domain.Auth;

public sealed class AuthSession
{
    public const int RevokedReasonMaxLength = 64;

    private AuthSession()
    {
    }

    public AuthSession(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid externalAuthIdentityId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Session expiry must be after creation time.", nameof(expiresAt));
        }

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        ExternalAuthIdentityId = externalAuthIdentityId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ExternalAuthIdentityId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevokedReason { get; private set; }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && ExpiresAt > now;
    }

    public void Revoke(DateTimeOffset revokedAt, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Revoke reason is required.", nameof(reason));
        }

        if (reason.Length > RevokedReasonMaxLength)
        {
            throw new ArgumentException(
                $"Revoke reason cannot be longer than {RevokedReasonMaxLength} characters.",
                nameof(reason));
        }

        RevokedAt = revokedAt;
        RevokedReason = reason;
    }
}
