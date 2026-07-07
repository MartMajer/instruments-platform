using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record ListCampaignInvitationDeliveriesQuery(Guid CampaignId)
    : IRequest<Result<CampaignInvitationDeliveriesResponse>>;

public sealed class ListCampaignInvitationDeliveriesHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<ListCampaignInvitationDeliveriesQuery, Result<CampaignInvitationDeliveriesResponse>>
{
    public Task<Result<CampaignInvitationDeliveriesResponse>> Handle(
        ListCampaignInvitationDeliveriesQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListCampaignInvitationDeliveriesAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
