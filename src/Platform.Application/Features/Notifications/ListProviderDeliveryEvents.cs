using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record ListProviderDeliveryEventsQuery(int Limit = 50)
    : IRequest<Result<ListProviderDeliveryEventsResponse>>;

public sealed class ListProviderDeliveryEventsHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<ListProviderDeliveryEventsQuery, Result<ListProviderDeliveryEventsResponse>>
{
    public Task<Result<ListProviderDeliveryEventsResponse>> Handle(
        ListProviderDeliveryEventsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListProviderDeliveryEventsAsync(
            currentTenant.TenantId,
            query.Limit,
            cancellationToken);
    }
}
