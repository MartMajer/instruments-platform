using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record ListOperationalNotificationsQuery(int Limit = 25)
    : IRequest<Result<ListOperationalNotificationsResponse>>;

public sealed class ListOperationalNotificationsValidator
    : AbstractValidator<ListOperationalNotificationsQuery>
{
    public const int MaxLimit = 50;

    public ListOperationalNotificationsValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
    }
}

public sealed class ListOperationalNotificationsHandler(
    ICurrentTenant currentTenant,
    IOperationalNotificationStore store)
    : IRequestHandler<ListOperationalNotificationsQuery, Result<ListOperationalNotificationsResponse>>
{
    public Task<Result<ListOperationalNotificationsResponse>> Handle(
        ListOperationalNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListOperationalNotificationsAsync(
            currentTenant.TenantId,
            query.Limit,
            cancellationToken);
    }
}
