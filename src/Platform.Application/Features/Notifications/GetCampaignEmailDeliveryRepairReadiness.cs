using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record GetCampaignEmailDeliveryRepairReadinessQuery(Guid CampaignId)
    : IRequest<Result<CampaignEmailDeliveryRepairReadinessResponse>>;

public sealed class GetCampaignEmailDeliveryRepairReadinessHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<GetCampaignEmailDeliveryRepairReadinessQuery, Result<CampaignEmailDeliveryRepairReadinessResponse>>
{
    public Task<Result<CampaignEmailDeliveryRepairReadinessResponse>> Handle(
        GetCampaignEmailDeliveryRepairReadinessQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignEmailDeliveryRepairReadinessAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
