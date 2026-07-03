using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public interface INotificationDeliveryStore
{
    Task<Result<ProcessCampaignEmailDeliveriesResponse>> ProcessCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        ProcessCampaignEmailDeliveriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<RequeueFailedCampaignEmailDeliveriesResponse>> RequeueFailedCampaignEmailDeliveriesAsync(
        Guid tenantId,
        Guid campaignId,
        RequeueFailedCampaignEmailDeliveriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignEmailDeliveryRepairReadinessResponse>> GetCampaignEmailDeliveryRepairReadinessAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<ListEmailSuppressionsResponse>> ListEmailSuppressionsAsync(
        Guid tenantId,
        int limit,
        bool includeReleased,
        CancellationToken cancellationToken);

    Task<Result<EmailSuppressionResponse>> AddEmailSuppressionAsync(
        Guid tenantId,
        AddEmailSuppressionRequest request,
        CancellationToken cancellationToken);

    Task<Result<EmailSuppressionResponse>> ReleaseEmailSuppressionAsync(
        Guid tenantId,
        Guid suppressionId,
        ReleaseEmailSuppressionRequest request,
        CancellationToken cancellationToken);

    Task<Result<RecordProviderDeliveryEventResponse>> RecordProviderDeliveryEventAsync(
        Guid tenantId,
        RecordProviderDeliveryEventRequest request,
        CancellationToken cancellationToken);

    Task<Result<ListProviderDeliveryEventsResponse>> ListProviderDeliveryEventsAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);

    Task<Result<RecordProviderDeliveryEventResponse>> RecordProviderDeliveryEventByProviderMessageIdAsync(
        RecordProviderDeliveryEventByProviderMessageIdRequest request,
        CancellationToken cancellationToken);
}
