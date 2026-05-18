using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.Responses;
using Platform.Domain.Campaigns;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Notifications;

public sealed class NotificationDeliveryStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IEmailDeliveryProvider emailDeliveryProvider) : INotificationDeliveryStore
{
    private const int MaxBatchSize = 25;
    private const int MaxProviderMessageIdLength = 200;
    private const string RedactedProviderMessageId = "redacted";
    private const string SanitizedDeliveryError = "delivery_failed";
    private const string UnknownProvider = "unknown";

    public async Task<Result<ProcessCampaignEmailDeliveriesResponse>> ProcessCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        ProcessCampaignEmailDeliveriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        if (!IsValidBatchSize(request.BatchSize))
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(
                Error.Validation("notification_delivery.batch_size_invalid", "Batch size must be between 1 and 25."));
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => new
            {
                campaign.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (!CanProcessCampaignStatus(campaign.Status))
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(
                Error.Validation(
                    "notification_delivery.campaign_not_live",
                    "Campaign must be live before invitation notifications can be delivered."));
        }

        var notifications = await db.Notifications
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM notification
                WHERE tenant_id = {tenantId}
                  AND campaign_id = {campaignId}
                  AND channel = {NotificationChannels.Email}
                  AND template_code = {Notification.InvitationTemplateCode}
                  AND status = {NotificationStatuses.Queued}
                  AND (scheduled_for IS NULL OR scheduled_for <= now())
                ORDER BY created_at, id
                LIMIT {request.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var assignments = await LoadAssignmentsAsync(notifications, cancellationToken);
        var invitationTokens = await LoadInvitationTokensAsync(assignments.Values, cancellationToken);
        var deliveries = new List<NotificationDeliveryProofResponse>(notifications.Count);

        foreach (var notification in notifications)
        {
            var delivery = await ProcessNotificationAsync(
                tenantId,
                notification,
                assignments,
                invitationTokens,
                cancellationToken);
            deliveries.Add(delivery);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ProcessCampaignEmailDeliveriesResponse(
            campaignId,
            request.BatchSize,
            deliveries.Count,
            deliveries.Count(delivery => delivery.Status == NotificationStatuses.Sent),
            deliveries.Count(delivery => delivery.Status == NotificationStatuses.Failed),
            deliveries));
    }

    public async Task<Result<RequeueFailedCampaignEmailDeliveriesResponse>> RequeueFailedCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        RequeueFailedCampaignEmailDeliveriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        if (!IsValidBatchSize(request.BatchSize))
        {
            return Result.Failure<RequeueFailedCampaignEmailDeliveriesResponse>(
                Error.Validation("notification_delivery.batch_size_invalid", "Batch size must be between 1 and 25."));
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => new
            {
                campaign.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<RequeueFailedCampaignEmailDeliveriesResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (!CanProcessCampaignStatus(campaign.Status))
        {
            return Result.Failure<RequeueFailedCampaignEmailDeliveriesResponse>(
                Error.Validation(
                    "notification_delivery.campaign_not_live",
                    "Campaign must be live before invitation notifications can be requeued."));
        }

        var notifications = await db.Notifications
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM notification
                WHERE tenant_id = {tenantId}
                  AND campaign_id = {campaignId}
                  AND channel = {NotificationChannels.Email}
                  AND template_code = {Notification.InvitationTemplateCode}
                  AND status = {NotificationStatuses.Failed}
                  AND recipient <> {Notification.WithdrawnRecipient}
                  AND COALESCE(error, '') <> {Notification.WithdrawalScrubbedError}
                ORDER BY updated_at, id
                LIMIT {request.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var requeuedAt = DateTimeOffset.UtcNow;
        foreach (var notification in notifications)
        {
            notification.RequeueForRetry(requeuedAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new RequeueFailedCampaignEmailDeliveriesResponse(
            campaignId,
            request.BatchSize,
            notifications.Count));
    }

    private async Task<NotificationDeliveryProofResponse> ProcessNotificationAsync(
        Guid tenantId,
        Notification notification,
        IReadOnlyDictionary<Guid, Assignment> assignments,
        IReadOnlyDictionary<Guid, InvitationToken> invitationTokens,
        CancellationToken cancellationToken)
    {
        string? respondentPath = null;

        try
        {
            var assignment = ResolveAssignment(notification, assignments);
            var invitationToken = ResolveInvitationToken(assignment, invitationTokens);
            var issued = OpenLinkTokens.IssueInvitation(tenantId);
            respondentPath = $"/r/{issued.RawToken}";
            invitationToken.ReissueHash(issued.TokenHash);

            var sendResult = await emailDeliveryProvider.SendAsync(
                new EmailDeliveryMessage(
                    notification.Id,
                    notification.Recipient,
                    "Survey invitation",
                    $"Open the survey: {respondentPath}"),
                cancellationToken);

            notification.MarkSent(sendResult.SentAt);
            var provider = SanitizeProvider(sendResult.Provider);
            var providerMessageId = SanitizeProviderMessageId(sendResult.ProviderMessageId);
            db.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
                PlatformIds.NewId(),
                tenantId,
                notification.Id,
                provider,
                notification.Recipient,
                providerMessageId,
                sendResult.SentAt));

            return new NotificationDeliveryProofResponse(
                notification.Id,
                notification.Recipient,
                NotificationStatuses.Sent,
                provider,
                providerMessageId,
                CreateProofRespondentPath(provider, respondentPath),
                Error: null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var failedAt = DateTimeOffset.UtcNow;
            var provider = SanitizeProvider(emailDeliveryProvider.Provider);
            var error = SanitizeDeliveryError(exception.Message);
            notification.MarkFailed(error, failedAt);
            db.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateFailed(
                PlatformIds.NewId(),
                tenantId,
                notification.Id,
                provider,
                notification.Recipient,
                error,
                failedAt));

            return new NotificationDeliveryProofResponse(
                notification.Id,
                notification.Recipient,
                NotificationStatuses.Failed,
                provider,
                ProviderMessageId: null,
                RespondentPath: null,
                error);
        }
    }

    private static Assignment ResolveAssignment(
        Notification notification,
        IReadOnlyDictionary<Guid, Assignment> assignments)
    {
        if (!assignments.TryGetValue(notification.AssignmentId, out var assignment))
        {
            throw new InvalidOperationException("Notification assignment was not found.");
        }

        if (!assignment.Anonymous || !assignment.InviteTokenId.HasValue)
        {
            throw new InvalidOperationException("Notification assignment is not an anonymous invitation.");
        }

        return assignment;
    }

    private static InvitationToken ResolveInvitationToken(
        Assignment assignment,
        IReadOnlyDictionary<Guid, InvitationToken> invitationTokens)
    {
        if (!assignment.InviteTokenId.HasValue ||
            !invitationTokens.TryGetValue(assignment.InviteTokenId.Value, out var token))
        {
            throw new InvalidOperationException("Invitation token was not found.");
        }

        if (token.Channel != InvitationTokenChannels.Email)
        {
            throw new InvalidOperationException("Invitation token is not an email token.");
        }

        return token;
    }

    private async Task<Dictionary<Guid, Assignment>> LoadAssignmentsAsync(
        IReadOnlyList<Notification> notifications,
        CancellationToken cancellationToken)
    {
        var assignmentIds = notifications.Select(notification => notification.AssignmentId).ToArray();

        return await db.Assignments
            .Where(assignment => assignmentIds.Contains(assignment.Id))
            .ToDictionaryAsync(assignment => assignment.Id, cancellationToken);
    }

    private async Task<Dictionary<Guid, InvitationToken>> LoadInvitationTokensAsync(
        IReadOnlyCollection<Assignment> assignments,
        CancellationToken cancellationToken)
    {
        var tokenIds = assignments
            .Where(assignment => assignment.InviteTokenId.HasValue)
            .Select(assignment => assignment.InviteTokenId!.Value)
            .ToArray();

        return await db.InvitationTokens
            .Where(token => tokenIds.Contains(token.Id))
            .ToDictionaryAsync(token => token.Id, cancellationToken);
    }

    private static string SanitizeDeliveryError(string? error)
    {
        return SanitizedDeliveryError;
    }

    private static string SanitizeProviderMessageId(string? providerMessageId)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return RedactedProviderMessageId;
        }

        var normalized = providerMessageId.Trim();
        if (ContainsSensitiveDeliveryValue(normalized))
        {
            return RedactedProviderMessageId;
        }

        return normalized.Length > MaxProviderMessageIdLength
            ? normalized[..MaxProviderMessageIdLength]
            : normalized;
    }

    private static string? CreateProofRespondentPath(string provider, string? respondentPath)
    {
        return string.Equals(provider, EmailDeliveryProviderNames.LocalDev, StringComparison.OrdinalIgnoreCase)
            ? respondentPath
            : null;
    }

    private static bool IsValidBatchSize(int batchSize)
    {
        return batchSize is >= 1 and <= MaxBatchSize;
    }

    private static bool CanProcessCampaignStatus(string status)
    {
        return status == CampaignStatuses.Live;
    }

    private static string SanitizeProvider(string? provider)
    {
        if (string.Equals(provider, EmailDeliveryProviderNames.LocalDev, StringComparison.OrdinalIgnoreCase))
        {
            return EmailDeliveryProviderNames.LocalDev;
        }

        if (string.Equals(provider, EmailDeliveryProviderNames.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            return EmailDeliveryProviderNames.Smtp;
        }

        return UnknownProvider;
    }

    private static bool ContainsSensitiveDeliveryValue(string value)
    {
        return value.Contains("/r/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inv_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains('@');
    }
}
