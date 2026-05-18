namespace Platform.Domain.Consent;

public sealed class WithdrawalRequestToken
{
    private WithdrawalRequestToken()
    {
    }

    public WithdrawalRequestToken(
        Guid id,
        Guid tenantId,
        Guid responseSessionId,
        string tokenHash,
        string requestedAction,
        DateTimeOffset expiresAt,
        string? createdReason)
    {
        Id = id;
        TenantId = tenantId;
        ResponseSessionId = responseSessionId;
        TokenHash = NormalizeTokenHash(tokenHash);
        RequestedAction = NormalizeAction(requestedAction);
        ExpiresAt = expiresAt;
        CreatedReason = NormalizeReason(createdReason);
        CreatedAt = DateTimeOffset.UtcNow;

        if (ExpiresAt <= CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Withdrawal token expiry must be in the future.");
        }
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ResponseSessionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string RequestedAction { get; private set; } = RetentionPolicy.Anonymize;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public string? CreatedReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkConsumed(DateTimeOffset consumedAt)
    {
        if (ConsumedAt.HasValue)
        {
            throw new InvalidOperationException("Withdrawal token has already been consumed.");
        }

        if (consumedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedAt), "Consumed time cannot be before created time.");
        }

        ConsumedAt = consumedAt;
    }

    private static string NormalizeTokenHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Withdrawal token hash is too long.");
        }

        return normalized;
    }

    private static string NormalizeAction(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized is not (RetentionPolicy.Delete or RetentionPolicy.Anonymize))
        {
            throw new ArgumentException("Unknown withdrawal request action.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 64 || normalized.Any(character => !IsSafeReasonCodeCharacter(character)))
        {
            throw new ArgumentException("Withdrawal token reason is invalid.", nameof(value));
        }

        return normalized;
    }

    private static bool IsSafeReasonCodeCharacter(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.';
    }
}
