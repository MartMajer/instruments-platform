namespace Platform.Application.Features.Notifications;

public static class EmailDeliveryProviderNames
{
    public const string LocalDev = "local-dev";
    public const string Smtp = "smtp";
}

public sealed record EmailDeliveryMessage(
    Guid NotificationId,
    string DeliveryAttemptKey,
    string Recipient,
    string Subject,
    string BodyText,
    string? UnsubscribeUrl = null);

public sealed record EmailDeliveryResult(
    string Provider,
    string? ProviderMessageId,
    DateTimeOffset SentAt);
