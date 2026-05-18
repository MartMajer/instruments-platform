using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record GetOperationalNotificationSummaryQuery
    : IRequest<Result<OperationalNotificationSummaryResponse>>;

public sealed class GetOperationalNotificationSummaryHandler(
    ICurrentTenant currentTenant,
    IOperationalNotificationStore store)
    : IRequestHandler<GetOperationalNotificationSummaryQuery, Result<OperationalNotificationSummaryResponse>>
{
    public Task<Result<OperationalNotificationSummaryResponse>> Handle(
        GetOperationalNotificationSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetOperationalNotificationSummaryAsync(
            currentTenant.TenantId,
            cancellationToken);
    }
}
