namespace Platform.Application.Features.Notifications;

public sealed record ProcessCampaignEmailDeliveriesRequest(int BatchSize = 25);

public sealed record ProcessCampaignEmailDeliveriesResponse(
    Guid CampaignId,
    int RequestedBatchSize,
    int ProcessedCount,
    int SentCount,
    int FailedCount,
    IReadOnlyList<NotificationDeliveryProofResponse> Deliveries,
    int BouncedCount = 0);

public sealed record RequeueFailedCampaignEmailDeliveriesRequest(
    int BatchSize = 25,
    bool ConfirmedAnotherEmailAppropriate = false,
    bool ConfirmedNoPriorDelivery = false);

public sealed record RequeueFailedCampaignEmailDeliveriesResponse(
    Guid CampaignId,
    int RequestedBatchSize,
    int RequeuedCount);

public sealed record CampaignEmailDeliveryRepairReadinessResponse(
    int StalePreparedAttemptCount,
    int AmbiguousFailedNotificationCount,
    int RetryableFailedNotificationCount,
    int SuppressedFailedNotificationCount,
    int ProviderEventCount,
    DateTimeOffset? LatestProviderEventAt,
    bool CanRetryFailed,
    bool HasRepairWork,
    IReadOnlyList<CampaignEmailDeliveryRepairReadinessIssueResponse> Issues);

public sealed record CampaignEmailDeliveryRepairReadinessIssueResponse(
    string Code,
    string Severity,
    string Message);

public sealed record RecordProviderDeliveryEventRequest(
    string DeliveryAttemptKey,
    string EventType,
    DateTimeOffset? OccurredAt = null,
    string? ProviderEventId = null,
    string? ProviderMessageId = null,
    string? Reason = null);

public sealed record RecordProviderDeliveryEventByProviderMessageIdRequest(
    string Provider,
    string ProviderMessageId,
    string EventType,
    DateTimeOffset? OccurredAt = null,
    string? ProviderEventId = null,
    string? Reason = null);

public sealed record RecordProviderDeliveryEventResponse(
    Guid NotificationId,
    Guid DeliveryAttemptId,
    string EventType,
    string NotificationStatus,
    bool SuppressionCreated,
    bool DuplicateEvent);

public sealed record ListProviderDeliveryEventsResponse(
    int RequestedLimit,
    IReadOnlyList<ProviderDeliveryEventResponse> Events);

public sealed record ProviderDeliveryEventResponse(
    string Provider,
    string EventType,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    string NotificationStatus,
    string DeliveryAttemptStatus,
    bool HasProviderEventId,
    bool HasProviderMessageId);

public sealed record EmailDeliveryReadinessResponse(
    string Provider,
    string Mode,
    bool CanSendRealEmail,
    bool WebhookConfigured,
    IReadOnlyList<EmailDeliveryReadinessIssueResponse> Issues);

public sealed record EmailDeliveryReadinessIssueResponse(
    string Code,
    string Message,
    string Severity);

public sealed record NotificationDeliveryProofResponse(
    Guid NotificationId,
    string? Recipient,
    string Status,
    string Provider,
    string? ProviderMessageId,
    string? RespondentPath,
    string? Error);

public sealed record CampaignInvitationDeliveryResponse(
    Guid NotificationId,
    string Recipient,
    string? DisplayName,
    string Status,
    DateTimeOffset? LastEventAt,
    string? Error);

public sealed record CampaignInvitationDeliveriesResponse(
    Guid CampaignId,
    int QueuedCount,
    int SentCount,
    int DeliveredCount,
    int BouncedCount,
    int FailedCount,
    IReadOnlyList<CampaignInvitationDeliveryResponse> Deliveries);

public sealed record ListEmailSuppressionsResponse(
    int RequestedLimit,
    int ActiveCount,
    int ReleasedCount,
    IReadOnlyList<EmailSuppressionResponse> Suppressions);

public sealed record EmailSuppressionResponse(
    Guid Id,
    string Recipient,
    string Reason,
    string Source,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReleasedAt,
    string? ReleaseReason,
    bool Active,
    Guid? CampaignSeriesId = null,
    string? CampaignSeriesName = null);

public sealed record AddEmailSuppressionRequest(
    string Recipient,
    string? Reason = null,
    string? Note = null);

public sealed record ReleaseEmailSuppressionRequest(string? Reason = null);

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
