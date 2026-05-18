using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record MarkOperationalNotificationReadCommand(Guid NotificationId)
    : IRequest<Result<OperationalNotificationResponse>>;

public sealed class MarkOperationalNotificationReadValidator
    : AbstractValidator<MarkOperationalNotificationReadCommand>
{
    public MarkOperationalNotificationReadValidator()
    {
        RuleFor(command => command.NotificationId).NotEmpty();
    }
}

public sealed class MarkOperationalNotificationReadHandler(
    ICurrentTenant currentTenant,
    IOperationalNotificationStore store)
    : IRequestHandler<MarkOperationalNotificationReadCommand, Result<OperationalNotificationResponse>>
{
    public Task<Result<OperationalNotificationResponse>> Handle(
        MarkOperationalNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        return store.MarkOperationalNotificationReadAsync(
            currentTenant.TenantId,
            command.NotificationId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
