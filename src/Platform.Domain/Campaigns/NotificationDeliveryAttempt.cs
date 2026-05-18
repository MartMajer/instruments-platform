namespace Platform.Domain.Campaigns;

public sealed class NotificationDeliveryAttempt
{
    private const string WithdrawnRecipient = "withdrawn@example.invalid";
    private const string WithdrawalScrubbedError = "withdrawal_scrubbed";

    private NotificationDeliveryAttempt()
    {
    }

    public NotificationDeliveryAttempt(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        string provider,
        string status,
        string recipient,
        string? providerMessageId = null,
        string? error = null,
        DateTimeOffset? createdAt = null)
    {
        if (status is not (NotificationStatuses.Sent or NotificationStatuses.Failed))
        {
            throw new ArgumentException("Delivery attempt status must be sent or failed.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        NotificationId = notificationId;
        Provider = NotificationDeliveryTextSafety.SanitizeProvider(provider);
        Status = status;
        Recipient = NormalizeRequired(recipient, nameof(recipient)).ToLowerInvariant();
        ProviderMessageId = providerMessageId is null
            ? null
            : NotificationDeliveryTextSafety.SanitizeProviderMessageId(providerMessageId);
        Error = status == NotificationStatuses.Failed
            ? NotificationDeliveryTextSafety.SanitizeFailureError(error)
            : NormalizeOptional(error);
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid NotificationId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string Recipient { get; private set; } = string.Empty;

    public string? ProviderMessageId { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static NotificationDeliveryAttempt CreateSent(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        string provider,
        string recipient,
        string providerMessageId,
        DateTimeOffset createdAt)
    {
        return new NotificationDeliveryAttempt(
            id,
            tenantId,
            notificationId,
            provider,
            NotificationStatuses.Sent,
            recipient,
            providerMessageId,
            createdAt: createdAt);
    }

    public static NotificationDeliveryAttempt CreateFailed(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        string provider,
        string recipient,
        string error,
        DateTimeOffset createdAt)
    {
        return new NotificationDeliveryAttempt(
            id,
            tenantId,
            notificationId,
            provider,
            NotificationStatuses.Failed,
            recipient,
            error: error,
            createdAt: createdAt);
    }

    public void ScrubForWithdrawal()
    {
        Recipient = WithdrawnRecipient;
        ProviderMessageId = null;
        Error = Status == NotificationStatuses.Failed ? WithdrawalScrubbedError : null;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
