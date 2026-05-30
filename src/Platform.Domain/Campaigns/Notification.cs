namespace Platform.Domain.Campaigns;

public sealed class Notification
{
    public const string InvitationTemplateCode = "invitation";
    public const string WithdrawnRecipient = "withdrawn@example.invalid";
    public const string WithdrawalScrubbedError = "withdrawal_scrubbed";

    private Notification()
    {
    }

    public Notification(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        Guid assignmentId,
        string channel,
        string templateCode,
        string status,
        string recipient,
        DateTimeOffset? scheduledFor = null,
        DateTimeOffset? sentAt = null,
        string? error = null,
        string locale = EmailTemplateLocales.English)
    {
        if (!NotificationChannels.IsKnown(channel))
        {
            throw new ArgumentException("Unknown notification channel.", nameof(channel));
        }

        if (!NotificationStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown notification status.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        CampaignId = campaignId;
        AssignmentId = assignmentId;
        Channel = channel;
        TemplateCode = NormalizeRequired(templateCode, nameof(templateCode));
        Status = status;
        Recipient = NormalizeEmail(recipient, nameof(recipient));
        Locale = EmailTemplateLocales.Normalize(locale);
        ScheduledFor = scheduledFor;
        SentAt = sentAt;
        Error = error is null ? null : NotificationDeliveryTextSafety.SanitizeFailureError(error);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid AssignmentId { get; private set; }

    public string Channel { get; private set; } = string.Empty;

    public string TemplateCode { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string Recipient { get; private set; } = string.Empty;

    public string Locale { get; private set; } = EmailTemplateLocales.English;

    public DateTimeOffset? ScheduledFor { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Notification QueueEmailInvitation(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        Guid assignmentId,
        string recipient,
        DateTimeOffset? scheduledFor = null,
        string locale = EmailTemplateLocales.English)
    {
        return new Notification(
            id,
            tenantId,
            campaignId,
            assignmentId,
            NotificationChannels.Email,
            InvitationTemplateCode,
            NotificationStatuses.Queued,
            recipient,
            scheduledFor,
            locale: locale);
    }

    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatuses.Sent;
        SentAt = sentAt;
        Error = null;
        UpdatedAt = sentAt;
    }

    public void MarkFailed(string error, DateTimeOffset failedAt)
    {
        Status = NotificationStatuses.Failed;
        SentAt = null;
        Error = NotificationDeliveryTextSafety.SanitizeFailureError(error);
        UpdatedAt = failedAt;
    }

    public void MarkBounced(string error, DateTimeOffset bouncedAt)
    {
        Status = NotificationStatuses.Bounced;
        SentAt = null;
        Error = NotificationDeliveryTextSafety.SanitizeFailureError(error);
        UpdatedAt = bouncedAt;
    }

    public void RequeueForRetry(DateTimeOffset queuedAt)
    {
        if (Status != NotificationStatuses.Failed)
        {
            throw new InvalidOperationException("Only failed notifications can be requeued for retry.");
        }

        if (Recipient == WithdrawnRecipient ||
            string.Equals(Error, WithdrawalScrubbedError, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Withdrawal-scrubbed notifications cannot be requeued.");
        }

        Status = NotificationStatuses.Queued;
        SentAt = null;
        Error = null;
        ScheduledFor = queuedAt;
        UpdatedAt = queuedAt;
    }

    public void ScrubForWithdrawal(DateTimeOffset scrubbedAt)
    {
        Recipient = WithdrawnRecipient;
        UpdatedAt = scrubbedAt;

        if (Status == NotificationStatuses.Sent)
        {
            Error = null;
            return;
        }

        Status = NotificationStatuses.Failed;
        SentAt = null;
        Error = WithdrawalScrubbedError;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string NormalizeEmail(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Recipient must be an email address.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
