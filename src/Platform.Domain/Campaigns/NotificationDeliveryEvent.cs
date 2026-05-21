namespace Platform.Domain.Campaigns;

public static class NotificationDeliveryEventTypes
{
    public const string Accepted = "accepted";
    public const string Delivered = "delivered";
    public const string Bounced = "bounced";
    public const string Complained = "complained";

    public static bool IsKnown(string value)
    {
        return value is Accepted or Delivered or Bounced or Complained;
    }
}

public sealed class NotificationDeliveryEvent
{
    public const int ProviderEventIdMaxLength = 256;
    public const int EventTypeMaxLength = 64;
    public const int ReasonMaxLength = 256;

    private NotificationDeliveryEvent()
    {
    }

    public NotificationDeliveryEvent(
        Guid id,
        Guid tenantId,
        Guid notificationId,
        Guid deliveryAttemptId,
        string provider,
        string eventType,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt,
        string? providerEventId = null,
        string? providerMessageId = null,
        string? reason = null)
    {
        if (!NotificationDeliveryEventTypes.IsKnown(eventType))
        {
            throw new ArgumentException("Unknown notification delivery event type.", nameof(eventType));
        }

        Id = id;
        TenantId = tenantId;
        NotificationId = notificationId;
        DeliveryAttemptId = deliveryAttemptId;
        Provider = NotificationDeliveryTextSafety.SanitizeProvider(provider);
        EventType = eventType;
        ProviderEventId = NormalizeOptional(providerEventId, ProviderEventIdMaxLength);
        ProviderMessageId = providerMessageId is null
            ? null
            : NotificationDeliveryTextSafety.SanitizeProviderMessageId(providerMessageId);
        Reason = NormalizeOptional(reason, ReasonMaxLength);
        OccurredAt = occurredAt;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid NotificationId { get; private set; }

    public Guid DeliveryAttemptId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string? ProviderEventId { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    private static string? NormalizeOptional(string? value, int maxLength)
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
