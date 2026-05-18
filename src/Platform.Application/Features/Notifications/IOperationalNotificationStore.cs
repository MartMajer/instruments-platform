using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public interface IOperationalNotificationStore
{
    Task<Result<OperationalNotificationResponse>> RecordReportPdfArtifactTerminalStateAsync(
        Guid tenantId,
        Guid exportArtifactId,
        Guid campaignSeriesId,
        string status,
        string? failureReasonCode,
        CancellationToken cancellationToken);

    Task<Result<ListOperationalNotificationsResponse>> ListOperationalNotificationsAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);

    Task<Result<OperationalNotificationSummaryResponse>> GetOperationalNotificationSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Result<OperationalNotificationResponse>> MarkOperationalNotificationReadAsync(
        Guid tenantId,
        Guid notificationId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken);

    Task<Result<MarkAllOperationalNotificationsReadResponse>> MarkAllOperationalNotificationsReadAsync(
        Guid tenantId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken);
}

public sealed record OperationalNotificationResponse(
    Guid Id,
    string NotificationType,
    string Severity,
    string Status,
    Guid SourceAggregateId,
    string SourceEventType,
    DateTimeOffset CreatedAt,
    Guid? CampaignSeriesId = null,
    string? ArtifactStatus = null,
    string? FailureReasonCode = null,
    DateTimeOffset? ReadAt = null,
    DateTimeOffset? UpdatedAt = null,
    string? SourceStatus = null);
