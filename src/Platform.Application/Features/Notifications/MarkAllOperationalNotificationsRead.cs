using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record MarkAllOperationalNotificationsReadCommand
    : IRequest<Result<MarkAllOperationalNotificationsReadResponse>>;

public sealed class MarkAllOperationalNotificationsReadHandler(
    ICurrentTenant currentTenant,
    IOperationalNotificationStore store)
    : IRequestHandler<MarkAllOperationalNotificationsReadCommand, Result<MarkAllOperationalNotificationsReadResponse>>
{
    public Task<Result<MarkAllOperationalNotificationsReadResponse>> Handle(
        MarkAllOperationalNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        return store.MarkAllOperationalNotificationsReadAsync(
            currentTenant.TenantId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
