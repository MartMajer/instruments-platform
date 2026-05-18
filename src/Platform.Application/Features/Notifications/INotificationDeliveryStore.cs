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
}
