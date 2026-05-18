namespace Platform.Domain.Auth;

public sealed class UserAccount
{
    private UserAccount()
    {
    }

    public UserAccount(Guid id, Guid tenantId, string email, string locale = "en")
    {
        Id = id;
        TenantId = tenantId;
        Email = email;
        Locale = locale;
        FailedLoginAttempts = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public string? MfaSecret { get; private set; }

    public string Locale { get; private set; } = "en";

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void ChangeLocale(string locale)
    {
        Locale = locale;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
