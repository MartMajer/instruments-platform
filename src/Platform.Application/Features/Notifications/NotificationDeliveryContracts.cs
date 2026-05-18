namespace Platform.Application.Features.Notifications;

public sealed record ProcessCampaignEmailDeliveriesRequest(int BatchSize = 25);

public sealed record ProcessCampaignEmailDeliveriesResponse(
    Guid CampaignId,
    int RequestedBatchSize,
    int ProcessedCount,
    int SentCount,
    int FailedCount,
    IReadOnlyList<NotificationDeliveryProofResponse> Deliveries);

public sealed record RequeueFailedCampaignEmailDeliveriesRequest(int BatchSize = 25);

public sealed record RequeueFailedCampaignEmailDeliveriesResponse(
    Guid CampaignId,
    int RequestedBatchSize,
    int RequeuedCount);

public sealed record NotificationDeliveryProofResponse(
    Guid NotificationId,
    string Recipient,
    string Status,
    string Provider,
    string? ProviderMessageId,
    string? RespondentPath,
    string? Error);

public sealed record ListOperationalNotificationsResponse(
    int RequestedLimit,
    IReadOnlyList<OperationalNotificationResponse> Notifications);

public sealed record OperationalNotificationSummaryResponse(
    int UnreadCount,
    int InfoUnreadCount,
    int WarningUnreadCount,
    DateTimeOffset? LatestUnreadAt);

public sealed record MarkAllOperationalNotificationsReadResponse(
    int MarkedReadCount,
    DateTimeOffset ReadAt);
