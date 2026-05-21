namespace Platform.Domain.Campaigns;

public sealed class NotificationDeliveryAttempt
{
    public const string PreparedStatus = "prepared";
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
        string? providerDeliveryKey = null,
        string? error = null,
        DateTimeOffset? createdAt = null)
    {
        if (status is not (PreparedStatus or NotificationStatuses.Sent or NotificationStatuses.Failed))
        {
            throw new ArgumentException("Delivery attempt status must be prepared, sent, or failed.", nameof(status));
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
        ProviderDeliveryKey = NormalizeOptional(providerDeliveryKey);
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

    public string? ProviderDeliveryKey { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static NotificationDeliveryAttempt CreateSent(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        string provider,
        string recipient,
        string? providerMessageId,
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
            providerDeliveryKey: null,
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
            providerDeliveryKey: null,
            error: error,
            createdAt: createdAt);
    }

    public static NotificationDeliveryAttempt CreatePrepared(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        string provider,
        string recipient,
        string providerDeliveryKey,
        DateTimeOffset createdAt)
    {
        return new NotificationDeliveryAttempt(
            id,
            tenantId,
            notificationId,
            provider,
            PreparedStatus,
            recipient,
            providerDeliveryKey: providerDeliveryKey,
            createdAt: createdAt);
    }

    public void MarkSent(string? providerMessageId, DateTimeOffset sentAt)
    {
        Status = NotificationStatuses.Sent;
        ProviderMessageId = providerMessageId is null
            ? null
            : NotificationDeliveryTextSafety.SanitizeProviderMessageId(providerMessageId);
        Error = null;
        CreatedAt = sentAt;
    }

    public void MarkFailed(string error, DateTimeOffset failedAt)
    {
        Status = NotificationStatuses.Failed;
        ProviderMessageId = null;
        Error = NotificationDeliveryTextSafety.SanitizeFailureError(error);
        CreatedAt = failedAt;
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
