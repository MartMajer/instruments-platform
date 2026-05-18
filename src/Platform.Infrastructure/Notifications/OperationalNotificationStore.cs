using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Notifications;
using Platform.Domain.Operations;
using Platform.Domain.Reports;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Notifications;

public sealed class OperationalNotificationStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope) : IOperationalNotificationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<OperationalNotificationResponse>> RecordReportPdfArtifactTerminalStateAsync(
        Guid tenantId,
        Guid exportArtifactId,
        Guid campaignSeriesId,
        string status,
        string? failureReasonCode,
        CancellationToken cancellationToken)
    {
        if (status is not (ExportArtifactStatuses.Succeeded or ExportArtifactStatuses.Failed))
        {
            return Result.Failure<OperationalNotificationResponse>(
                Error.Validation(
                    "operational_notification.invalid_report_pdf_terminal_status",
                    "Report PDF artifact notification status must be succeeded or failed."));
        }

        if (status == ExportArtifactStatuses.Succeeded && failureReasonCode is not null)
        {
            return Result.Failure<OperationalNotificationResponse>(
                Error.Validation(
                    "operational_notification.invalid_report_pdf_failure_reason",
                    "Succeeded report PDF artifact notifications must not include a failure reason."));
        }

        if (failureReasonCode is { Length: > OperationalNotification.MaxFailureReasonCodeLength })
        {
            return Result.Failure<OperationalNotificationResponse>(
                Error.Validation(
                    "operational_notification.invalid_report_pdf_failure_reason",
                    "Report PDF artifact failure reason is invalid."));
        }

        var existingTransaction = db.Database.CurrentTransaction;
        await using var transaction = existingTransaction is null
            ? await tenantDbScope.BeginTransactionAsync(
                tenantId,
                cancellationToken: cancellationToken)
            : null;

        if (existingTransaction is not null)
        {
            await tenantDbScope.SetTenantAsync(
                tenantId,
                cancellationToken: cancellationToken);
        }

        var existing = await db.OperationalNotifications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                notification =>
                    notification.TenantId == tenantId &&
                    notification.SourceAggregateId == exportArtifactId &&
                    notification.SourceEventType == OperationalNotification.SourceEventTypeReportPdfArtifactTerminalStateReached &&
                    notification.NotificationType == OperationalNotification.ReportPdfArtifactTerminalNotificationType,
                cancellationToken);

        if (existing is not null)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Result.Success(ToResponse(existing));
        }

        var payloadJson = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                exportArtifactId,
                campaignSeriesId,
                status,
                failureReasonCode
            },
            JsonOptions);
        var notification = OperationalNotification.CreateReportPdfArtifactTerminal(
            PlatformIds.NewId(),
            tenantId,
            exportArtifactId,
            status,
            payloadJson);

        db.OperationalNotifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return Result.Success(ToResponse(notification));
    }

    public async Task<Result<ListOperationalNotificationsResponse>> ListOperationalNotificationsAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 50)
        {
            return Result.Failure<ListOperationalNotificationsResponse>(
                Error.Validation(
                    "operational_notification.limit_invalid",
                    "Operational notification limit must be between 1 and 50."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var notifications = await db.OperationalNotifications
            .AsNoTracking()
            .Where(notification => notification.TenantId == tenantId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ListOperationalNotificationsResponse(
            limit,
            notifications.Select(ToResponse).ToArray()));
    }

    public async Task<Result<OperationalNotificationSummaryResponse>> GetOperationalNotificationSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var summary = await db.OperationalNotifications
            .AsNoTracking()
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.Status == OperationalNotification.StatusUnread)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                UnreadCount = group.Count(),
                InfoUnreadCount = group.Count(notification =>
                    notification.Severity == OperationalNotification.SeverityInfo),
                WarningUnreadCount = group.Count(notification =>
                    notification.Severity == OperationalNotification.SeverityWarning),
                LatestUnreadAt = group.Max(notification => (DateTimeOffset?)notification.CreatedAt)
            })
            .SingleOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(summary is null
            ? new OperationalNotificationSummaryResponse(
                0,
                0,
                0,
                LatestUnreadAt: null)
            : new OperationalNotificationSummaryResponse(
                summary.UnreadCount,
                summary.InfoUnreadCount,
                summary.WarningUnreadCount,
                summary.LatestUnreadAt));
    }

    public async Task<Result<OperationalNotificationResponse>> MarkOperationalNotificationReadAsync(
        Guid tenantId,
        Guid notificationId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var notification = await db.OperationalNotifications
            .SingleOrDefaultAsync(
                entity => entity.Id == notificationId && entity.TenantId == tenantId,
                cancellationToken);

        if (notification is null)
        {
            return Result.Failure<OperationalNotificationResponse>(NotFound());
        }

        notification.MarkRead(readAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(notification));
    }

    public async Task<Result<MarkAllOperationalNotificationsReadResponse>> MarkAllOperationalNotificationsReadAsync(
        Guid tenantId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var notifications = await db.OperationalNotifications
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.Status == OperationalNotification.StatusUnread)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkRead(readAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new MarkAllOperationalNotificationsReadResponse(
            notifications.Count,
            readAt));
    }

    private static OperationalNotificationResponse ToResponse(OperationalNotification notification)
    {
        var payload = ParsePayload(notification.PayloadJson);

        return new OperationalNotificationResponse(
            notification.Id,
            notification.NotificationType,
            notification.Severity,
            notification.Status,
            notification.SourceAggregateId,
            notification.SourceEventType,
            notification.CreatedAt,
            payload.CampaignSeriesId,
            payload.SourceStatus,
            payload.FailureReasonCode,
            notification.ReadAt,
            notification.UpdatedAt,
            payload.SourceStatus);
    }

    private static OperationalNotificationPayloadResponse ParsePayload(string payloadJson)
    {
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        return new OperationalNotificationPayloadResponse(
            root.TryGetProperty("campaignSeriesId", out var campaignSeriesId) &&
                campaignSeriesId.ValueKind == JsonValueKind.String &&
                campaignSeriesId.TryGetGuid(out var parsedCampaignSeriesId)
                ? parsedCampaignSeriesId
                : null,
            root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String
                ? status.GetString()
                : null,
            root.TryGetProperty("failureReasonCode", out var failureReasonCode) &&
                failureReasonCode.ValueKind == JsonValueKind.String
                ? failureReasonCode.GetString()
                : null);
    }

    private sealed record OperationalNotificationPayloadResponse(
        Guid? CampaignSeriesId,
        string? SourceStatus,
        string? FailureReasonCode);

    private static Error NotFound()
    {
        return Error.NotFound(
            "operational_notification.not_found",
            "Operational notification was not found.");
    }
}
