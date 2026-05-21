namespace Platform.Domain.Campaigns;

public sealed class EmailSuppression
{
    public const string DefaultManualReason = "operator_do_not_contact";
    public const string RecipientUnsubscribedReason = "recipient_unsubscribed";
    public const string ProviderBouncedReason = "provider_bounced";
    public const string ProviderComplainedReason = "provider_complained";
    public const string ManualSource = "tenant_operator";
    public const string RespondentInvitationSource = "respondent_invitation_link";
    public const string ProviderEventSource = "provider_delivery_event";
    public const int ReasonMaxLength = 128;
    public const int SourceMaxLength = 128;
    public const int NoteMaxLength = 1000;
    public const int ReleaseReasonMaxLength = 256;

    private EmailSuppression()
    {
    }

    public EmailSuppression(
        Guid id,
        Guid tenantId,
        string recipient,
        string reason,
        string source,
        string? note,
        DateTimeOffset createdAt)
    {
        Id = id;
        TenantId = tenantId;
        Recipient = NormalizeEmail(recipient, nameof(recipient));
        Reason = NormalizeBounded(reason, nameof(reason), ReasonMaxLength);
        Source = NormalizeBounded(source, nameof(source), SourceMaxLength);
        Note = NormalizeOptionalBounded(note, NoteMaxLength);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public string? ReleaseReason { get; private set; }

    public bool Active => ReleasedAt is null;

    public void Release(string? reason, DateTimeOffset releasedAt)
    {
        if (ReleasedAt is not null)
        {
            return;
        }

        ReleasedAt = releasedAt;
        ReleaseReason = NormalizeOptionalBounded(reason, ReleaseReasonMaxLength) ?? "released_by_operator";
    }

    private static string NormalizeEmail(string value, string parameterName)
    {
        var normalized = NormalizeBounded(value, parameterName, 512).ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Recipient must be an email address.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeBounded(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized;
    }

    private static string? NormalizeOptionalBounded(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? normalized[..maxLength]
            : normalized;
    }
}
