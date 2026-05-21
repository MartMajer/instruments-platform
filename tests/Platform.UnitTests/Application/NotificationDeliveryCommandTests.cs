using Platform.Application.Features.Notifications;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.UnitTests.Application;

public sealed class NotificationDeliveryCommandTests
{
    [Fact]
    public void Validator_rejects_empty_campaign_id_and_invalid_batch_size()
    {
        var validator = new ProcessCampaignEmailDeliveriesValidator();

        var result = validator.Validate(new ProcessCampaignEmailDeliveriesCommand(
            Guid.Empty,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 0)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "CampaignId");
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Request.BatchSize");
    }

    [Fact]
    public void Validator_accepts_default_batch_size()
    {
        var validator = new ProcessCampaignEmailDeliveriesValidator();
        var request = new ProcessCampaignEmailDeliveriesRequest();

        var result = validator.Validate(new ProcessCampaignEmailDeliveriesCommand(Guid.NewGuid(), request));

        Assert.Equal(25, request.BatchSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Requeue_validator_rejects_empty_campaign_id_and_invalid_batch_size()
    {
        var validator = new RequeueFailedCampaignEmailDeliveriesValidator();

        var result = validator.Validate(new RequeueFailedCampaignEmailDeliveriesCommand(
            Guid.Empty,
            new RequeueFailedCampaignEmailDeliveriesRequest(BatchSize: 0)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "CampaignId");
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Request.BatchSize");
    }

    [Fact]
    public void Requeue_validator_accepts_default_batch_size()
    {
        var validator = new RequeueFailedCampaignEmailDeliveriesValidator();
        var request = new RequeueFailedCampaignEmailDeliveriesRequest(
            ConfirmedAnotherEmailAppropriate: true);

        var result = validator.Validate(new RequeueFailedCampaignEmailDeliveriesCommand(Guid.NewGuid(), request));

        Assert.Equal(25, request.BatchSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Handler_passes_current_tenant_campaign_and_request_to_store()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var response = new ProcessCampaignEmailDeliveriesResponse(
            campaignId,
            RequestedBatchSize: 7,
            ProcessedCount: 1,
            SentCount: 1,
            FailedCount: 0,
            Deliveries:
            [
                new NotificationDeliveryProofResponse(
                    Guid.NewGuid(),
                    "researcher@example.com",
                    "sent",
                    "local-dev",
                    "local-dev-message",
                    "/r/inv_token",
                    Error: null)
            ]);
        var store = new RecordingNotificationDeliveryStore(response);
        var handler = new ProcessCampaignEmailDeliveriesHandler(currentTenant, store);
        var request = new ProcessCampaignEmailDeliveriesRequest(BatchSize: 7);

        var result = await handler.Handle(
            new ProcessCampaignEmailDeliveriesCommand(campaignId, request),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(response, result.Value);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(campaignId, store.CampaignId);
        Assert.Same(request, store.Request);
    }

    [Fact]
    public async Task Requeue_handler_passes_current_tenant_campaign_and_request_to_store()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var response = new RequeueFailedCampaignEmailDeliveriesResponse(
            campaignId,
            RequestedBatchSize: 7,
            RequeuedCount: 2);
        var store = new RecordingNotificationDeliveryStore(
            CreateEmptyProcessResponse(campaignId),
            response);
        var handler = new RequeueFailedCampaignEmailDeliveriesHandler(currentTenant, store);
        var request = new RequeueFailedCampaignEmailDeliveriesRequest(BatchSize: 7);

        var result = await handler.Handle(
            new RequeueFailedCampaignEmailDeliveriesCommand(campaignId, request),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(response, result.Value);
        Assert.Equal(tenantId, store.RequeueTenantId);
        Assert.Equal(campaignId, store.RequeueCampaignId);
        Assert.Same(request, store.RequeueRequest);
    }

    private static ProcessCampaignEmailDeliveriesResponse CreateEmptyProcessResponse(Guid campaignId)
    {
        return new ProcessCampaignEmailDeliveriesResponse(
            campaignId,
            RequestedBatchSize: 25,
            ProcessedCount: 0,
            SentCount: 0,
            FailedCount: 0,
            Deliveries: []);
    }

    private sealed class RecordingNotificationDeliveryStore(
        ProcessCampaignEmailDeliveriesResponse response,
        RequeueFailedCampaignEmailDeliveriesResponse? requeueResponse = null)
        : INotificationDeliveryStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignId { get; private set; }

        public ProcessCampaignEmailDeliveriesRequest? Request { get; private set; }

        public Guid RequeueTenantId { get; private set; }

        public Guid RequeueCampaignId { get; private set; }

        public RequeueFailedCampaignEmailDeliveriesRequest? RequeueRequest { get; private set; }

        public Task<Result<ProcessCampaignEmailDeliveriesResponse>> ProcessCampaignEmailDeliveriesAsync(
            Guid tenantId,
            Guid campaignId,
            ProcessCampaignEmailDeliveriesRequest request,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignId = campaignId;
            Request = request;

            return Task.FromResult(Result.Success(response));
        }

        public Task<Result<RequeueFailedCampaignEmailDeliveriesResponse>> RequeueFailedCampaignEmailDeliveriesAsync(
            Guid tenantId,
            Guid campaignId,
            RequeueFailedCampaignEmailDeliveriesRequest request,
            CancellationToken cancellationToken)
        {
            RequeueTenantId = tenantId;
            RequeueCampaignId = campaignId;
            RequeueRequest = request;

            return Task.FromResult(Result.Success(requeueResponse!));
        }

        public Task<Result<CampaignEmailDeliveryRepairReadinessResponse>> GetCampaignEmailDeliveryRepairReadinessAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new CampaignEmailDeliveryRepairReadinessResponse(
                StalePreparedAttemptCount: 0,
                AmbiguousFailedNotificationCount: 0,
                RetryableFailedNotificationCount: 0,
                SuppressedFailedNotificationCount: 0,
                ProviderEventCount: 0,
                LatestProviderEventAt: null,
                CanRetryFailed: false,
                HasRepairWork: false,
                Issues: [])));
        }

        public Task<Result<ListEmailSuppressionsResponse>> ListEmailSuppressionsAsync(
            Guid tenantId,
            int limit,
            bool includeReleased,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ListEmailSuppressionsResponse(
                limit,
                ActiveCount: 0,
                ReleasedCount: 0,
                Suppressions: [])));
        }

        public Task<Result<EmailSuppressionResponse>> AddEmailSuppressionAsync(
            Guid tenantId,
            AddEmailSuppressionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new EmailSuppressionResponse(
                Guid.NewGuid(),
                request.Recipient,
                request.Reason ?? "manual",
                "manual",
                request.Note,
                DateTimeOffset.UtcNow,
                ReleasedAt: null,
                ReleaseReason: null,
                Active: true)));
        }

        public Task<Result<EmailSuppressionResponse>> ReleaseEmailSuppressionAsync(
            Guid tenantId,
            Guid suppressionId,
            ReleaseEmailSuppressionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new EmailSuppressionResponse(
                suppressionId,
                "released@example.test",
                "manual",
                "manual",
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                request.Reason,
                Active: false)));
        }

        public Task<Result<RecordProviderDeliveryEventResponse>> RecordProviderDeliveryEventAsync(
            Guid tenantId,
            RecordProviderDeliveryEventRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new RecordProviderDeliveryEventResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                request.EventType,
                "sent",
                SuppressionCreated: false,
                DuplicateEvent: false)));
        }

        public Task<Result<ListProviderDeliveryEventsResponse>> ListProviderDeliveryEventsAsync(
            Guid tenantId,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ListProviderDeliveryEventsResponse(
                limit,
                Events: [])));
        }
    }
}
