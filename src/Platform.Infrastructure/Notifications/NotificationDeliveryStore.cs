using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
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
    IEmailDeliveryProvider emailDeliveryProvider,
    IOptions<EmailDeliveryOptions>? emailDeliveryOptions = null) : INotificationDeliveryStore
{
    private const int MaxBatchSize = 25;
    private const int MaxProviderMessageIdLength = 200;
    private const string RedactedProviderMessageId = "redacted";
    private const string SanitizedDeliveryError = "delivery_failed";
    private const string DeliveryAmbiguousError = "delivery_ambiguous";
    private const string RecipientSuppressedError = "recipient_suppressed";
    private const string ProviderReasonRedacted = "provider_reason_redacted";
    private const string ProviderEventIdHashPrefix = "sha256:";
    private const string UnknownProvider = "unknown";
    private const int MaxSuppressionLimit = 100;
    private const int MaxProviderEventLimit = 100;
    private static readonly TimeSpan PreparedAttemptStaleAfter = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ProviderEventFutureTolerance = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ProviderEventNotificationClockSkew = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ProviderEventPastLimit = TimeSpan.FromDays(90);
    private readonly EmailDeliveryOptions deliveryOptions = emailDeliveryOptions?.Value ?? new EmailDeliveryOptions();

    public async Task<Result<ListEmailSuppressionsResponse>> ListEmailSuppressionsAsync(
        Guid tenantId,
        int limit,
        bool includeReleased,
        CancellationToken cancellationToken)
    {
        var requestedLimit = Math.Clamp(limit, 1, MaxSuppressionLimit);
        var query = db.EmailSuppressions
            .AsNoTracking()
            .Where(suppression => suppression.TenantId == tenantId);

        if (!includeReleased)
        {
            query = query.Where(suppression => suppression.ReleasedAt == null);
        }

        var suppressions = await query
            .OrderBy(suppression => suppression.ReleasedAt != null)
            .ThenByDescending(suppression => suppression.CreatedAt)
            .ThenBy(suppression => suppression.Recipient)
            .Take(requestedLimit)
            .Select(suppression => ToEmailSuppressionResponse(suppression))
            .ToListAsync(cancellationToken);

        return Result.Success(new ListEmailSuppressionsResponse(
            requestedLimit,
            suppressions.Count(suppression => suppression.Active),
            suppressions.Count(suppression => !suppression.Active),
            suppressions));
    }

    public async Task<Result<ListProviderDeliveryEventsResponse>> ListProviderDeliveryEventsAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        var requestedLimit = Math.Clamp(limit, 1, MaxProviderEventLimit);

        var events = await (
                from deliveryEvent in db.NotificationDeliveryEvents.AsNoTracking()
                join notification in db.Notifications.AsNoTracking()
                    on deliveryEvent.NotificationId equals notification.Id
                join attempt in db.NotificationDeliveryAttempts.AsNoTracking()
                    on deliveryEvent.DeliveryAttemptId equals attempt.Id
                where deliveryEvent.TenantId == tenantId &&
                    notification.TenantId == tenantId &&
                    attempt.TenantId == tenantId
                orderby deliveryEvent.ReceivedAt descending,
                    deliveryEvent.OccurredAt descending,
                    deliveryEvent.EventType
                select new ProviderDeliveryEventResponse(
                    deliveryEvent.Provider,
                    deliveryEvent.EventType,
                    deliveryEvent.OccurredAt,
                    deliveryEvent.ReceivedAt,
                    notification.Status,
                    attempt.Status,
                    deliveryEvent.ProviderEventId != null,
                    deliveryEvent.ProviderMessageId != null))
            .Take(requestedLimit)
            .ToListAsync(cancellationToken);

        return Result.Success(new ListProviderDeliveryEventsResponse(
            requestedLimit,
            events));
    }

    public async Task<Result<EmailSuppressionResponse>> AddEmailSuppressionAsync(
        Guid tenantId,
        AddEmailSuppressionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeEmail(request.Recipient, out var recipient))
        {
            return Result.Failure<EmailSuppressionResponse>(
                Error.Validation("email_suppression.recipient_invalid", "Enter a valid email address to suppress."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var suppressedAt = DateTimeOffset.UtcNow;
        var existing = await db.EmailSuppressions
            .SingleOrDefaultAsync(
                suppression =>
                    suppression.TenantId == tenantId &&
                    suppression.Recipient == recipient &&
                    suppression.ReleasedAt == null,
                cancellationToken);
        if (existing is not null)
        {
            await MarkPendingEmailInvitationNotificationsSuppressedAsync(
                tenantId,
                recipient,
                suppressedAt,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(ToEmailSuppressionResponse(existing));
        }

        var suppression = new EmailSuppression(
            PlatformIds.NewId(),
            tenantId,
            recipient,
            NormalizeSuppressionReason(request.Reason),
            EmailSuppression.ManualSource,
            request.Note,
            suppressedAt);

        db.EmailSuppressions.Add(suppression);
        await MarkPendingEmailInvitationNotificationsSuppressedAsync(
            tenantId,
            recipient,
            suppressedAt,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToEmailSuppressionResponse(suppression));
    }

    public async Task<Result<EmailSuppressionResponse>> ReleaseEmailSuppressionAsync(
        Guid tenantId,
        Guid suppressionId,
        ReleaseEmailSuppressionRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var suppression = await db.EmailSuppressions
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == suppressionId,
                cancellationToken);
        if (suppression is null)
        {
            return Result.Failure<EmailSuppressionResponse>(
                Error.NotFound("email_suppression.not_found", "Email suppression record was not found."));
        }

        suppression.Release(request.Reason, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToEmailSuppressionResponse(suppression));
    }

    public async Task<Result<RecordProviderDeliveryEventResponse>> RecordProviderDeliveryEventAsync(
        Guid tenantId,
        RecordProviderDeliveryEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!NotificationDeliveryEventTypes.IsKnown(request.EventType))
        {
            return Result.Failure<RecordProviderDeliveryEventResponse>(
                Error.Validation(
                    "notification_delivery_event.type_invalid",
                    "Delivery event type must be accepted, delivered, bounced, or complained."));
        }

        if (!TryParseDeliveryAttemptKey(
            request.DeliveryAttemptKey,
            out var keyTenantId,
            out var providerDeliveryKey) ||
            keyTenantId != tenantId)
        {
            return Result.Failure<RecordProviderDeliveryEventResponse>(
                Error.Validation(
                    "notification_delivery_event.delivery_attempt_key_invalid",
                    "Delivery attempt key is invalid for this tenant."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var attempt = await db.NotificationDeliveryAttempts
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.ProviderDeliveryKey == providerDeliveryKey,
                cancellationToken);
        var notification = attempt is null
            ? null
            : await db.Notifications.SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == attempt.NotificationId,
                cancellationToken);
        if (attempt is null || notification is null)
        {
            return Result.Failure<RecordProviderDeliveryEventResponse>(
                Error.NotFound(
                    "notification_delivery_event.delivery_attempt_not_found",
                    "Delivery attempt was not found."));
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var occurredAt = request.OccurredAt ?? receivedAt;
        var timestampValidation = ValidateProviderEventOccurredAt(notification, occurredAt, receivedAt);
        if (timestampValidation.HasValue)
        {
            return Result.Failure<RecordProviderDeliveryEventResponse>(timestampValidation.Value);
        }

        var providerEventId = HashProviderEventId(request.ProviderEventId);
        if (providerEventId is not null)
        {
            var duplicate = await db.NotificationDeliveryEvents
                .AsNoTracking()
                .AnyAsync(
                    deliveryEvent =>
                        deliveryEvent.TenantId == tenantId &&
                        deliveryEvent.Provider == attempt.Provider &&
                        deliveryEvent.ProviderEventId == providerEventId,
                    cancellationToken);
            if (duplicate)
            {
                return Result.Success(new RecordProviderDeliveryEventResponse(
                    notification.Id,
                    attempt.Id,
                    request.EventType,
                    notification.Status,
                    SuppressionCreated: false,
                    DuplicateEvent: true));
            }
        }
        else
        {
            var duplicate = await db.NotificationDeliveryEvents
                .AsNoTracking()
                .AnyAsync(
                    deliveryEvent =>
                        deliveryEvent.TenantId == tenantId &&
                        deliveryEvent.DeliveryAttemptId == attempt.Id &&
                        deliveryEvent.EventType == request.EventType &&
                        deliveryEvent.ProviderEventId == null,
                    cancellationToken);
            if (duplicate)
            {
                return Result.Success(new RecordProviderDeliveryEventResponse(
                    notification.Id,
                    attempt.Id,
                    request.EventType,
                    notification.Status,
                    SuppressionCreated: false,
                    DuplicateEvent: true));
            }
        }

        var staleForStateReconciliation = await HasNewerProviderDeliveryEventAsync(
            tenantId,
            attempt.Id,
            request.EventType,
            occurredAt,
            cancellationToken);
        db.NotificationDeliveryEvents.Add(new NotificationDeliveryEvent(
            PlatformIds.NewId(),
            tenantId,
            notification.Id,
            attempt.Id,
            attempt.Provider,
            request.EventType,
            occurredAt,
            receivedAt,
            providerEventId,
            request.ProviderMessageId,
            SanitizeProviderEventReason(request.Reason)));

        var suppressionCreated = false;
        if (staleForStateReconciliation)
        {
            suppressionCreated = false;
        }
        else if (request.EventType is NotificationDeliveryEventTypes.Accepted or NotificationDeliveryEventTypes.Delivered)
        {
            ReconcilePositiveProviderEvent(notification, attempt, request.ProviderMessageId, occurredAt);
        }
        else if (request.EventType is NotificationDeliveryEventTypes.Bounced or NotificationDeliveryEventTypes.Complained)
        {
            var suppressionReason = request.EventType == NotificationDeliveryEventTypes.Complained
                ? EmailSuppression.ProviderComplainedReason
                : EmailSuppression.ProviderBouncedReason;
            if (CanNegativeProviderEventMarkBounced(notification, occurredAt))
            {
                notification.MarkBounced(suppressionReason, occurredAt);
            }

            ReconcileNegativeProviderEvent(attempt, suppressionReason, occurredAt);

            suppressionCreated = await EnsureProviderSuppressionAsync(
                tenantId,
                notification.Recipient,
                suppressionReason,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new RecordProviderDeliveryEventResponse(
            notification.Id,
            attempt.Id,
            request.EventType,
            notification.Status,
            suppressionCreated,
            DuplicateEvent: false));
    }

    public async Task<Result<ProcessCampaignEmailDeliveriesResponse>> ProcessCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        ProcessCampaignEmailDeliveriesRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidBatchSize(request.BatchSize))
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(
                Error.Validation("notification_delivery.batch_size_invalid", "Batch size must be between 1 and 25."));
        }

        var providerReadinessError = ValidateEmailProviderBeforeDelivery();
        if (providerReadinessError.HasValue)
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(providerReadinessError.Value);
        }

        var prepared = await PrepareCampaignEmailDeliveriesAsync(
            tenantId,
            campaignId,
            request.BatchSize,
            cancellationToken);
        if (prepared.IsFailure)
        {
            return Result.Failure<ProcessCampaignEmailDeliveriesResponse>(prepared.Error);
        }

        var deliveries = new List<NotificationDeliveryProofResponse>(
            prepared.Value.SuppressedDeliveries.Count + prepared.Value.WorkItems.Count);
        deliveries.AddRange(prepared.Value.SuppressedDeliveries);

        foreach (var workItem in prepared.Value.WorkItems)
        {
            deliveries.Add(await SendPreparedDeliveryAsync(tenantId, workItem, cancellationToken));
        }

        return Result.Success(new ProcessCampaignEmailDeliveriesResponse(
            campaignId,
            request.BatchSize,
            deliveries.Count,
            deliveries.Count(delivery => delivery.Status == NotificationStatuses.Sent),
            deliveries.Count(delivery => delivery.Status == NotificationStatuses.Failed),
            deliveries,
            deliveries.Count(delivery => delivery.Status == NotificationStatuses.Bounced)));
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

        if (!request.ConfirmedAnotherEmailAppropriate &&
            !request.ConfirmedNoPriorDelivery)
        {
            return Result.Failure<RequeueFailedCampaignEmailDeliveriesResponse>(
                Error.Validation(
                    "notification_delivery.retry_confirmation_required",
                    "Confirm another invitation email is appropriate before requeueing."));
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && campaign.Id == campaignId)
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
                  AND NOT EXISTS (
                    SELECT 1
                    FROM email_suppression suppression
                    WHERE suppression.tenant_id = {tenantId}
                      AND suppression.recipient = notification.recipient
                      AND suppression.released_at IS NULL
                  )
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

    public async Task<Result<CampaignEmailDeliveryRepairReadinessResponse>> GetCampaignEmailDeliveryRepairReadinessAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && campaign.Id == campaignId)
            .Select(campaign => new
            {
                campaign.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<CampaignEmailDeliveryRepairReadinessResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var staleBefore = DateTimeOffset.UtcNow.Subtract(PreparedAttemptStaleAfter);
        var stalePreparedAttemptCount = await (
                from attempt in db.NotificationDeliveryAttempts.AsNoTracking()
                join notification in db.Notifications.AsNoTracking()
                    on attempt.NotificationId equals notification.Id
                where attempt.TenantId == tenantId &&
                    attempt.Status == NotificationDeliveryAttempt.PreparedStatus &&
                    attempt.CreatedAt <= staleBefore &&
                    notification.TenantId == tenantId &&
                    notification.CampaignId == campaignId &&
                    notification.Status == NotificationStatuses.Queued
                select attempt.Id)
            .CountAsync(cancellationToken);

        var activeSuppressedRecipients = db.EmailSuppressions
            .AsNoTracking()
            .Where(suppression => suppression.TenantId == tenantId && suppression.ReleasedAt == null)
            .Select(suppression => suppression.Recipient);
        var failedRows = await db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.CampaignId == campaignId &&
                notification.Channel == NotificationChannels.Email &&
                notification.TemplateCode == Notification.InvitationTemplateCode &&
                notification.Status == NotificationStatuses.Failed &&
                notification.Recipient != Notification.WithdrawnRecipient &&
                notification.Error != Notification.WithdrawalScrubbedError)
            .Select(notification => new
            {
                notification.Error,
                HasActiveSuppression = activeSuppressedRecipients.Contains(notification.Recipient)
            })
            .ToListAsync(cancellationToken);

        var providerEventSummary = await (
                from deliveryEvent in db.NotificationDeliveryEvents.AsNoTracking()
                join notification in db.Notifications.AsNoTracking()
                    on deliveryEvent.NotificationId equals notification.Id
                where deliveryEvent.TenantId == tenantId &&
                    notification.TenantId == tenantId &&
                    notification.CampaignId == campaignId
                select deliveryEvent.ReceivedAt)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Latest = group.Max()
            })
            .SingleOrDefaultAsync(cancellationToken);

        var retryableFailedNotificationCount = failedRows.Count(row => !row.HasActiveSuppression);
        var suppressedFailedNotificationCount = failedRows.Count(row => row.HasActiveSuppression);
        var ambiguousFailedNotificationCount = failedRows.Count(row => row.Error == DeliveryAmbiguousError);
        var providerEventCount = providerEventSummary?.Count ?? 0;
        var latestProviderEventAt = providerEventSummary?.Latest;
        var issues = CreateRepairReadinessIssues(
            campaign.Status,
            stalePreparedAttemptCount,
            ambiguousFailedNotificationCount,
            retryableFailedNotificationCount,
            suppressedFailedNotificationCount);
        var canRetryFailed = CanProcessCampaignStatus(campaign.Status) && retryableFailedNotificationCount > 0;
        var hasRepairWork =
            stalePreparedAttemptCount > 0 ||
            retryableFailedNotificationCount > 0 ||
            suppressedFailedNotificationCount > 0;

        return Result.Success(new CampaignEmailDeliveryRepairReadinessResponse(
            stalePreparedAttemptCount,
            ambiguousFailedNotificationCount,
            retryableFailedNotificationCount,
            suppressedFailedNotificationCount,
            providerEventCount,
            latestProviderEventAt,
            canRetryFailed,
            hasRepairWork,
            issues));
    }

    private async Task<Result<PreparedDeliveryBatch>> PrepareCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && campaign.Id == campaignId)
            .Select(campaign => new
            {
                campaign.Status,
                campaign.Name
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<PreparedDeliveryBatch>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (!CanProcessCampaignStatus(campaign.Status))
        {
            return Result.Failure<PreparedDeliveryBatch>(
                Error.Validation(
                    "notification_delivery.campaign_not_live",
                    "Campaign must be live before invitation notifications can be delivered."));
        }

        await FailStalePreparedAttemptsAsync(tenantId, campaignId, cancellationToken);

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
                  AND NOT EXISTS (
                    SELECT 1
                    FROM notification_delivery_attempt attempt
                    WHERE attempt.tenant_id = {tenantId}
                      AND attempt.notification_id = notification.id
                      AND attempt.status = {NotificationDeliveryAttempt.PreparedStatus}
                  )
                ORDER BY created_at, id
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var assignments = await LoadAssignmentsAsync(tenantId, campaignId, notifications, cancellationToken);
        var workItems = new List<PreparedDeliveryWorkItem>(notifications.Count);
        var suppressedDeliveries = new List<NotificationDeliveryProofResponse>();
        var provider = SanitizeProvider(emailDeliveryProvider.Provider);

        foreach (var notification in notifications)
        {
            var assignment = ResolveAssignment(notification, assignments);
            if (await IsSuppressedEmailRecipientAsync(
                tenantId,
                notification.Recipient,
                cancellationToken))
            {
                var bouncedAt = DateTimeOffset.UtcNow;
                notification.MarkBounced(RecipientSuppressedError, bouncedAt);
                db.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateFailed(
                    PlatformIds.NewId(),
                    tenantId,
                    notification.Id,
                    provider,
                    notification.Recipient,
                    RecipientSuppressedError,
                    bouncedAt));

                suppressedDeliveries.Add(new NotificationDeliveryProofResponse(
                    notification.Id,
                    CreateProofRecipient(provider, notification.Recipient),
                    NotificationStatuses.Bounced,
                    provider,
                    ProviderMessageId: null,
                    RespondentPath: null,
                    Error: RecipientSuppressedError));
                continue;
            }

            var issued = OpenLinkTokens.IssueInvitation(tenantId);
            var respondentPath = $"/r/{issued.RawToken}";
            var respondentUrl = BuildPublicAppUrl(respondentPath);
            var unsubscribeUrl = BuildPublicAppUrl($"{respondentPath}/unsubscribe");
            db.InvitationTokens.Add(new InvitationToken(
                PlatformIds.NewId(),
                tenantId,
                notification.CampaignId,
                issued.TokenHash,
                InvitationTokenChannels.Email,
                notification.Recipient,
                assignmentId: assignment.Id));

            var attemptId = PlatformIds.NewId();
            var providerDeliveryKey = GenerateProviderDeliveryKey();
            var preparedAt = DateTimeOffset.UtcNow;
            db.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreatePrepared(
                attemptId,
                tenantId,
                notification.Id,
                provider,
                notification.Recipient,
                providerDeliveryKey,
                preparedAt));
            workItems.Add(new PreparedDeliveryWorkItem(
                notification.Id,
                attemptId,
                notification.Recipient,
                BuildDeliveryAttemptKey(tenantId, providerDeliveryKey),
                BuildEmailSubject(campaign.Name),
                BuildEmailBody(campaign.Name, respondentUrl, unsubscribeUrl, deliveryOptions.InvitationFooterText),
                respondentPath,
                unsubscribeUrl));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new PreparedDeliveryBatch(workItems, suppressedDeliveries));
    }

    private async Task<NotificationDeliveryProofResponse> SendPreparedDeliveryAsync(
        Guid tenantId,
        PreparedDeliveryWorkItem workItem,
        CancellationToken cancellationToken)
    {
        try
        {
            var suppressed = await CancelPreparedDeliveryIfSuppressedAsync(
                tenantId,
                workItem,
                cancellationToken);
            if (suppressed is not null)
            {
                return suppressed;
            }

            var sendResult = await emailDeliveryProvider.SendAsync(
                new EmailDeliveryMessage(
                    workItem.NotificationId,
                    workItem.DeliveryAttemptKey,
                    workItem.Recipient,
                    workItem.Subject,
                    workItem.BodyText),
                cancellationToken);

            return await CompletePreparedDeliveryAsync(
                tenantId,
                workItem,
                sendResult,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var failedAt = DateTimeOffset.UtcNow;
            var provider = SanitizeProvider(emailDeliveryProvider.Provider);
            var error = SanitizeDeliveryError(exception.Message);
            return await FailPreparedDeliveryAsync(
                tenantId,
                workItem,
                provider,
                error,
                failedAt,
                cancellationToken);
        }
    }

    private async Task<NotificationDeliveryProofResponse> CompletePreparedDeliveryAsync(
        Guid tenantId,
        PreparedDeliveryWorkItem workItem,
        EmailDeliveryResult sendResult,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await tenantDbScope.BeginTransactionAsync(
                tenantId,
                cancellationToken: cancellationToken);

            var notification = await db.Notifications
                .SingleOrDefaultAsync(
                    notification =>
                        notification.TenantId == tenantId &&
                        notification.Id == workItem.NotificationId,
                    cancellationToken);
            var attempt = await db.NotificationDeliveryAttempts
                .SingleOrDefaultAsync(
                    attempt =>
                        attempt.TenantId == tenantId &&
                        attempt.Id == workItem.AttemptId &&
                        attempt.NotificationId == workItem.NotificationId,
                    cancellationToken);

            if (notification is null ||
                attempt is null ||
                attempt.Status != NotificationDeliveryAttempt.PreparedStatus ||
                notification.Status != NotificationStatuses.Queued)
            {
                return CreateAmbiguousDeliveryProof(workItem, sendResult.Provider);
            }

            notification.MarkSent(sendResult.SentAt);
            var provider = SanitizeProvider(sendResult.Provider);
            var providerMessageId = SanitizeProviderMessageId(sendResult.ProviderMessageId);
            attempt.MarkSent(providerMessageId, sendResult.SentAt);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new NotificationDeliveryProofResponse(
                workItem.NotificationId,
                CreateProofRecipient(provider, workItem.Recipient),
                NotificationStatuses.Sent,
                provider,
                providerMessageId,
                CreateProofRespondentPath(provider, workItem.RespondentPath),
                Error: null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CreateAmbiguousDeliveryProof(workItem, sendResult.Provider);
        }
    }

    private async Task<NotificationDeliveryProofResponse> FailPreparedDeliveryAsync(
        Guid tenantId,
        PreparedDeliveryWorkItem workItem,
        string provider,
        string error,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await tenantDbScope.BeginTransactionAsync(
                tenantId,
                cancellationToken: cancellationToken);

            var notification = await db.Notifications
                .SingleOrDefaultAsync(
                    notification =>
                        notification.TenantId == tenantId &&
                        notification.Id == workItem.NotificationId,
                    cancellationToken);
            var attempt = await db.NotificationDeliveryAttempts
                .SingleOrDefaultAsync(
                    attempt =>
                        attempt.TenantId == tenantId &&
                        attempt.Id == workItem.AttemptId &&
                        attempt.NotificationId == workItem.NotificationId,
                    cancellationToken);

            if (notification is null ||
                attempt is null ||
                attempt.Status != NotificationDeliveryAttempt.PreparedStatus)
            {
                return CreateAmbiguousDeliveryProof(workItem, provider);
            }

            notification.MarkFailed(error, failedAt);
            attempt.MarkFailed(error, failedAt);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new NotificationDeliveryProofResponse(
                workItem.NotificationId,
                CreateProofRecipient(provider, workItem.Recipient),
                NotificationStatuses.Failed,
                provider,
                ProviderMessageId: null,
                RespondentPath: null,
                error);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CreateAmbiguousDeliveryProof(workItem, provider);
        }
    }

    private async Task FailStalePreparedAttemptsAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var staleBefore = DateTimeOffset.UtcNow.Subtract(PreparedAttemptStaleAfter);
        var staleAttempts = await (
                from attempt in db.NotificationDeliveryAttempts
                join notification in db.Notifications
                    on attempt.NotificationId equals notification.Id
                where attempt.TenantId == tenantId &&
                    attempt.Status == NotificationDeliveryAttempt.PreparedStatus &&
                    attempt.CreatedAt <= staleBefore &&
                    notification.TenantId == tenantId &&
                    notification.CampaignId == campaignId &&
                    notification.Status == NotificationStatuses.Queued
                select new
                {
                    Attempt = attempt,
                    Notification = notification
                })
            .ToListAsync(cancellationToken);

        var failedAt = DateTimeOffset.UtcNow;
        foreach (var stale in staleAttempts)
        {
            stale.Notification.MarkFailed(DeliveryAmbiguousError, failedAt);
            stale.Attempt.MarkFailed(DeliveryAmbiguousError, failedAt);
        }
    }

    private static NotificationDeliveryProofResponse CreateAmbiguousDeliveryProof(
        PreparedDeliveryWorkItem workItem,
        string provider)
    {
        var sanitizedProvider = SanitizeProvider(provider);

        return new NotificationDeliveryProofResponse(
            workItem.NotificationId,
            CreateProofRecipient(sanitizedProvider, workItem.Recipient),
            NotificationStatuses.Failed,
            sanitizedProvider,
            ProviderMessageId: null,
            RespondentPath: null,
            Error: DeliveryAmbiguousError);
    }

    private static Assignment ResolveAssignment(
        Notification notification,
        IReadOnlyDictionary<Guid, Assignment> assignments)
    {
        if (!assignments.TryGetValue(notification.AssignmentId, out var assignment))
        {
            throw new InvalidOperationException("Notification assignment was not found.");
        }

        if (assignment.TenantId != notification.TenantId ||
            assignment.CampaignId != notification.CampaignId ||
            !assignment.Anonymous ||
            !assignment.InviteTokenId.HasValue)
        {
            throw new InvalidOperationException("Notification assignment is not an anonymous invitation.");
        }

        return assignment;
    }

    private async Task<Dictionary<Guid, Assignment>> LoadAssignmentsAsync(
        Guid tenantId,
        Guid campaignId,
        IReadOnlyList<Notification> notifications,
        CancellationToken cancellationToken)
    {
        var assignmentIds = notifications.Select(notification => notification.AssignmentId).ToArray();

        return await db.Assignments
            .Where(assignment =>
                assignment.TenantId == tenantId &&
                assignment.CampaignId == campaignId &&
                assignmentIds.Contains(assignment.Id))
            .ToDictionaryAsync(assignment => assignment.Id, cancellationToken);
    }

    private async Task<bool> IsSuppressedEmailRecipientAsync(
        Guid tenantId,
        string recipient,
        CancellationToken cancellationToken)
    {
        if (await db.EmailSuppressions
            .AsNoTracking()
            .AnyAsync(suppression =>
                suppression.TenantId == tenantId &&
                suppression.Recipient == recipient &&
                suppression.ReleasedAt == null,
                cancellationToken))
        {
            return true;
        }

        return false;
    }

    private async Task<NotificationDeliveryProofResponse?> CancelPreparedDeliveryIfSuppressedAsync(
        Guid tenantId,
        PreparedDeliveryWorkItem workItem,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var notification = await db.Notifications
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.Id == workItem.NotificationId,
                cancellationToken);
        var attempt = await db.NotificationDeliveryAttempts
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.Id == workItem.AttemptId &&
                    entity.NotificationId == workItem.NotificationId,
                cancellationToken);

        if (notification is null ||
            attempt is null ||
            attempt.Status != NotificationDeliveryAttempt.PreparedStatus)
        {
            return CreateAmbiguousDeliveryProof(workItem, emailDeliveryProvider.Provider);
        }

        if (notification.Status != NotificationStatuses.Queued)
        {
            var failedAt = DateTimeOffset.UtcNow;
            attempt.MarkFailed(DeliveryAmbiguousError, failedAt);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreateAmbiguousDeliveryProof(workItem, emailDeliveryProvider.Provider);
        }

        if (!await IsSuppressedEmailRecipientAsync(
            tenantId,
            notification.Recipient,
            cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var suppressedAt = DateTimeOffset.UtcNow;
        notification.MarkBounced(RecipientSuppressedError, suppressedAt);
        attempt.MarkFailed(RecipientSuppressedError, suppressedAt);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var provider = SanitizeProvider(emailDeliveryProvider.Provider);
        return new NotificationDeliveryProofResponse(
            notification.Id,
            CreateProofRecipient(provider, notification.Recipient),
            NotificationStatuses.Bounced,
            provider,
            ProviderMessageId: null,
            RespondentPath: null,
            Error: RecipientSuppressedError);
    }

    private void ReconcilePositiveProviderEvent(
        Notification notification,
        NotificationDeliveryAttempt attempt,
        string? providerMessageId,
        DateTimeOffset occurredAt)
    {
        if (CanPositiveProviderEventMarkSent(notification))
        {
            notification.MarkSent(occurredAt);
        }

        if (CanPositiveProviderEventCompleteAttempt(attempt))
        {
            attempt.MarkSent(SanitizeProviderMessageId(providerMessageId), occurredAt);
        }
    }

    private static Error? ValidateProviderEventOccurredAt(
        Notification notification,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt)
    {
        if (occurredAt > receivedAt.Add(ProviderEventFutureTolerance))
        {
            return Error.Validation(
                "notification_delivery_event.occurred_at_future",
                "Provider event timestamp is too far in the future.");
        }

        if (occurredAt < receivedAt.Subtract(ProviderEventPastLimit))
        {
            return Error.Validation(
                "notification_delivery_event.occurred_at_too_old",
                "Provider event timestamp is too old for delivery reconciliation.");
        }

        if (occurredAt < notification.CreatedAt.Subtract(ProviderEventNotificationClockSkew))
        {
            return Error.Validation(
                "notification_delivery_event.occurred_at_before_notification",
                "Provider event timestamp is before the invitation delivery record.");
        }

        return null;
    }

    private async Task<bool> HasNewerProviderDeliveryEventAsync(
        Guid tenantId,
        Guid deliveryAttemptId,
        string currentEventType,
        DateTimeOffset currentOccurredAt,
        CancellationToken cancellationToken)
    {
        var currentRank = ProviderEventStateRank(currentEventType);
        var events = await db.NotificationDeliveryEvents
            .AsNoTracking()
            .Where(deliveryEvent =>
                deliveryEvent.TenantId == tenantId &&
                deliveryEvent.DeliveryAttemptId == deliveryAttemptId &&
                deliveryEvent.OccurredAt >= currentOccurredAt)
            .Select(deliveryEvent => new
            {
                deliveryEvent.EventType,
                deliveryEvent.OccurredAt
            })
            .ToListAsync(cancellationToken);

        return events.Any(deliveryEvent =>
            deliveryEvent.OccurredAt > currentOccurredAt ||
            (deliveryEvent.OccurredAt == currentOccurredAt &&
                ProviderEventStateRank(deliveryEvent.EventType) > currentRank));
    }

    private static int ProviderEventStateRank(string eventType)
    {
        return eventType switch
        {
            NotificationDeliveryEventTypes.Accepted => 10,
            NotificationDeliveryEventTypes.Delivered => 20,
            NotificationDeliveryEventTypes.Bounced => 30,
            NotificationDeliveryEventTypes.Complained => 40,
            _ => 0
        };
    }

    private static bool CanPositiveProviderEventMarkSent(Notification notification)
    {
        return notification.Status != NotificationStatuses.Sent &&
            notification.Status != NotificationStatuses.Bounced &&
            notification.Recipient != Notification.WithdrawnRecipient &&
            notification.Error is not (
                Notification.WithdrawalScrubbedError or
                RecipientSuppressedError or
                EmailSuppression.RecipientUnsubscribedReason or
                EmailSuppression.ProviderBouncedReason or
                EmailSuppression.ProviderComplainedReason);
    }

    private static bool CanPositiveProviderEventCompleteAttempt(NotificationDeliveryAttempt attempt)
    {
        return attempt.Status != NotificationStatuses.Sent &&
            attempt.Recipient != Notification.WithdrawnRecipient &&
            attempt.Error is not (
                Notification.WithdrawalScrubbedError or
                RecipientSuppressedError or
                EmailSuppression.RecipientUnsubscribedReason or
                EmailSuppression.ProviderBouncedReason or
                EmailSuppression.ProviderComplainedReason);
    }

    private static bool CanNegativeProviderEventMarkBounced(
        Notification notification,
        DateTimeOffset occurredAt)
    {
        return !(notification.Status == NotificationStatuses.Sent &&
                notification.SentAt.HasValue &&
                notification.SentAt.Value > occurredAt) &&
            notification.Recipient != Notification.WithdrawnRecipient &&
            notification.Error != Notification.WithdrawalScrubbedError;
    }

    private static void ReconcileNegativeProviderEvent(
        NotificationDeliveryAttempt attempt,
        string suppressionReason,
        DateTimeOffset occurredAt)
    {
        if (attempt.Status == NotificationStatuses.Failed &&
            attempt.Error == suppressionReason)
        {
            return;
        }

        if (attempt.Status == NotificationStatuses.Sent &&
            attempt.CreatedAt > occurredAt)
        {
            return;
        }

        if (attempt.Recipient == Notification.WithdrawnRecipient ||
            attempt.Error == Notification.WithdrawalScrubbedError)
        {
            return;
        }

        attempt.MarkFailed(suppressionReason, occurredAt);
    }

    private async Task MarkPendingEmailInvitationNotificationsSuppressedAsync(
        Guid tenantId,
        string recipient,
        DateTimeOffset suppressedAt,
        CancellationToken cancellationToken)
    {
        var notifications = await db.Notifications
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.Channel == NotificationChannels.Email &&
                notification.TemplateCode == Notification.InvitationTemplateCode &&
                notification.Recipient == recipient &&
                (notification.Status == NotificationStatuses.Queued ||
                    notification.Status == NotificationStatuses.Failed))
            .ToListAsync(cancellationToken);
        if (notifications.Count == 0)
        {
            return;
        }

        var notificationIds = notifications
            .Select(notification => notification.Id)
            .ToArray();
        var preparedAttempts = await db.NotificationDeliveryAttempts
            .Where(attempt =>
                attempt.TenantId == tenantId &&
                notificationIds.Contains(attempt.NotificationId) &&
                attempt.Status == NotificationDeliveryAttempt.PreparedStatus)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkBounced(RecipientSuppressedError, suppressedAt);
        }

        foreach (var attempt in preparedAttempts)
        {
            attempt.MarkFailed(RecipientSuppressedError, suppressedAt);
        }
    }

    private async Task<bool> EnsureProviderSuppressionAsync(
        Guid tenantId,
        string recipient,
        string reason,
        CancellationToken cancellationToken)
    {
        if (recipient == Notification.WithdrawnRecipient)
        {
            return false;
        }

        var exists = await db.EmailSuppressions
            .AnyAsync(
                suppression =>
                    suppression.TenantId == tenantId &&
                    suppression.Recipient == recipient &&
                    suppression.ReleasedAt == null,
                cancellationToken);
        if (exists)
        {
            await MarkPendingEmailInvitationNotificationsSuppressedAsync(
                tenantId,
                recipient,
                DateTimeOffset.UtcNow,
                cancellationToken);
            return false;
        }

        var suppressedAt = DateTimeOffset.UtcNow;
        db.EmailSuppressions.Add(new EmailSuppression(
            PlatformIds.NewId(),
            tenantId,
            recipient,
            reason,
            EmailSuppression.ProviderEventSource,
            note: null,
            suppressedAt));
        await MarkPendingEmailInvitationNotificationsSuppressedAsync(
            tenantId,
            recipient,
            suppressedAt,
            cancellationToken);
        return true;
    }

    private static EmailSuppressionResponse ToEmailSuppressionResponse(EmailSuppression suppression)
    {
        return new EmailSuppressionResponse(
            suppression.Id,
            suppression.Recipient,
            suppression.Reason,
            suppression.Source,
            suppression.Note,
            suppression.CreatedAt,
            suppression.ReleasedAt,
            suppression.ReleaseReason,
            suppression.ReleasedAt is null);
    }

    private static bool TryNormalizeEmail(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value.Trim()).Address;
            if (string.IsNullOrWhiteSpace(address) || !address.Contains('@', StringComparison.Ordinal))
            {
                return false;
            }

            normalized = address.Trim().ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeSuppressionReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return EmailSuppression.DefaultManualReason;
        }

        var normalized = reason.Trim();
        return normalized.Length > EmailSuppression.ReasonMaxLength
            ? normalized[..EmailSuppression.ReasonMaxLength]
            : normalized;
    }

    private static string SanitizeDeliveryError(string? error)
    {
        return SanitizedDeliveryError;
    }

    private static string? SanitizeProviderEventReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? null
            : ProviderReasonRedacted;
    }

    private static string? HashProviderEventId(string? providerEventId)
    {
        if (string.IsNullOrWhiteSpace(providerEventId))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(providerEventId.Trim()));
        return ProviderEventIdHashPrefix + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsProviderEventDuplicate(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_notification_delivery_event_tenant_provider_event_id" or
                "ux_notification_delivery_event_tenant_attempt_type_without_provider_id"
        };
    }

    private static string? SanitizeProviderMessageId(string? providerMessageId)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return null;
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

    private static string? CreateProofRecipient(string provider, string recipient)
    {
        return string.Equals(provider, EmailDeliveryProviderNames.LocalDev, StringComparison.OrdinalIgnoreCase)
            ? recipient
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

    private static IReadOnlyList<CampaignEmailDeliveryRepairReadinessIssueResponse> CreateRepairReadinessIssues(
        string campaignStatus,
        int stalePreparedAttemptCount,
        int ambiguousFailedNotificationCount,
        int retryableFailedNotificationCount,
        int suppressedFailedNotificationCount)
    {
        var issues = new List<CampaignEmailDeliveryRepairReadinessIssueResponse>();

        if (stalePreparedAttemptCount > 0)
        {
            issues.Add(new CampaignEmailDeliveryRepairReadinessIssueResponse(
                "notification_delivery_repair.stale_prepared_attempts",
                "warning",
                "Some email handoffs were prepared but not settled. Run delivery processing before deciding whether another email is appropriate."));
        }

        if (ambiguousFailedNotificationCount > 0)
        {
            issues.Add(new CampaignEmailDeliveryRepairReadinessIssueResponse(
                "notification_delivery_repair.ambiguous_failures",
                "warning",
                "Some failed invitation emails have ambiguous provider handoff state. Confirm no prior delivery before retrying."));
        }

        if (retryableFailedNotificationCount > 0 && !CanProcessCampaignStatus(campaignStatus))
        {
            issues.Add(new CampaignEmailDeliveryRepairReadinessIssueResponse(
                "notification_delivery_repair.campaign_not_live",
                "blocking",
                "Failed invitation emails can only be requeued while the collection wave is live."));
        }

        if (suppressedFailedNotificationCount > 0)
        {
            issues.Add(new CampaignEmailDeliveryRepairReadinessIssueResponse(
                "notification_delivery_repair.suppressed_recipients",
                "info",
                "Some failed invitation emails are blocked by active do-not-contact or provider suppression records."));
        }

        return issues;
    }

    private static string BuildEmailSubject(string campaignName)
    {
        return "Study invitation";
    }

    private static string BuildEmailBody(
        string campaignName,
        string respondentPath,
        string unsubscribePath,
        string? footerText)
    {
        var footer = string.IsNullOrWhiteSpace(footerText)
            ? "This invitation was sent by the workspace running this study."
            : footerText.Trim();

        return $"""
            You have been invited to complete a study.

            For privacy, this email does not include the study title or topic. The link opens the study page before you decide whether to respond.

            Open your study link:
            {respondentPath}

            If you already responded, you can ignore this email.

            If you should not receive future study invitations from this workspace, unsubscribe here:
            {unsubscribePath}

            {footer}
            """;
    }

    private string BuildPublicAppUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(deliveryOptions.PublicAppBaseUrl) ||
            !Uri.TryCreate(deliveryOptions.PublicAppBaseUrl.Trim(), UriKind.Absolute, out var baseUri))
        {
            return path;
        }

        return new Uri(baseUri, path).ToString();
    }

    private Error? ValidateEmailProviderBeforeDelivery()
    {
        try
        {
            deliveryOptions.EnsureValidProviderConfiguration();
            return null;
        }
        catch (InvalidOperationException)
        {
            return Error.Validation(
                "notification_delivery.email_provider_not_ready",
                "Email sending setup has blocking issues. Review email delivery readiness before sending invitations.");
        }
    }

    private static string BuildDeliveryAttemptKey(
        Guid tenantId,
        string providerDeliveryKey)
    {
        return $"campaign-email:{tenantId:N}:{providerDeliveryKey}";
    }

    private static bool TryParseDeliveryAttemptKey(
        string value,
        out Guid tenantId,
        out string providerDeliveryKey)
    {
        tenantId = Guid.Empty;
        providerDeliveryKey = string.Empty;

        var parts = value.Split(':', StringSplitOptions.None);
        if (parts.Length != 3 ||
            !string.Equals(parts[0], "campaign-email", StringComparison.Ordinal) ||
            !Guid.TryParseExact(parts[1], "N", out tenantId) ||
            !IsSafeProviderDeliveryKey(parts[2]))
        {
            return false;
        }

        providerDeliveryKey = parts[2];
        return true;
    }

    private static string GenerateProviderDeliveryKey()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return "pdk_" + Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static bool IsSafeProviderDeliveryKey(string value)
    {
        return value.Length is >= 16 and <= 80 &&
            value.All(character =>
                char.IsAsciiLetterOrDigit(character) ||
                character is '_' or '-');
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
            value.Contains("campaign-email:", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inv_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("opn_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wdr_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            value.Contains('@');
    }

    private sealed record PreparedDeliveryBatch(
        IReadOnlyList<PreparedDeliveryWorkItem> WorkItems,
        IReadOnlyList<NotificationDeliveryProofResponse> SuppressedDeliveries);

    private sealed record PreparedDeliveryWorkItem(
        Guid NotificationId,
        Guid AttemptId,
        string Recipient,
        string DeliveryAttemptKey,
        string Subject,
        string BodyText,
        string RespondentPath,
        string UnsubscribeUrl);
}
