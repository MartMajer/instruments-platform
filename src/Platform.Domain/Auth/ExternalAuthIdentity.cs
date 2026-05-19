namespace Platform.Domain.Auth;

public sealed class ExternalAuthIdentity
{
    public const int ProviderMaxLength = 64;
    public const int ProviderSubjectHashMaxLength = 128;
    public const int EmailAtBindingMaxLength = 320;

    private ExternalAuthIdentity()
    {
    }

    public ExternalAuthIdentity(
        Guid id,
        Guid tenantId,
        Guid userId,
        string provider,
        string providerSubjectHash,
        string emailAtBinding,
        DateTimeOffset createdAt)
    {
        ValidateRequired(provider, nameof(provider), ProviderMaxLength);
        ValidateRequired(providerSubjectHash, nameof(providerSubjectHash), ProviderSubjectHashMaxLength);
        ValidateRequired(emailAtBinding, nameof(emailAtBinding), EmailAtBindingMaxLength);

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Provider = provider;
        ProviderSubjectHash = providerSubjectHash;
        EmailAtBinding = emailAtBinding;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ProviderSubjectHash { get; private set; } = string.Empty;

    public string EmailAtBinding { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public DateTimeOffset? EmailVerificationGraceUsedAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt.HasValue;

    public void RecordSeen(DateTimeOffset seenAt)
    {
        LastSeenAt = seenAt;
    }

    public void RecordEmailVerified(DateTimeOffset verifiedAt)
    {
        EmailVerifiedAt ??= verifiedAt;
        LastSeenAt = verifiedAt;
    }

    public void RecordEmailVerificationGrace(DateTimeOffset graceUsedAt)
    {
        EmailVerificationGraceUsedAt ??= graceUsedAt;
        LastSeenAt = graceUsedAt;
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        DisabledAt ??= disabledAt;
    }

    private static void ValidateRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot be longer than {maxLength} characters.", parameterName);
        }
    }
}
