using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record RecordProviderDeliveryEventCommand(
    RecordProviderDeliveryEventRequest Request,
    Guid? TenantIdOverride = null)
    : IRequest<Result<RecordProviderDeliveryEventResponse>>;

public sealed class RecordProviderDeliveryEventValidator : AbstractValidator<RecordProviderDeliveryEventCommand>
{
    public RecordProviderDeliveryEventValidator()
    {
        RuleFor(command => command.Request.DeliveryAttemptKey).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Request.EventType)
            .NotEmpty()
            .Must(NotificationDeliveryEventTypes.IsKnown)
            .WithMessage("Delivery event type must be accepted, delivered, bounced, or complained.");
        RuleFor(command => command.Request.ProviderEventId)
            .MaximumLength(NotificationDeliveryEvent.ProviderEventIdMaxLength);
        RuleFor(command => command.Request.ProviderMessageId)
            .MaximumLength(256);
        RuleFor(command => command.Request.Reason)
            .MaximumLength(NotificationDeliveryEvent.ReasonMaxLength);
    }
}

public sealed class RecordProviderDeliveryEventHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<RecordProviderDeliveryEventCommand, Result<RecordProviderDeliveryEventResponse>>
{
    public Task<Result<RecordProviderDeliveryEventResponse>> Handle(
        RecordProviderDeliveryEventCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.TenantIdOverride ?? currentTenant.TenantId;

        return store.RecordProviderDeliveryEventAsync(
            tenantId,
            command.Request,
            cancellationToken);
    }
}

public sealed record RecordProviderDeliveryEventByProviderMessageIdCommand(
    RecordProviderDeliveryEventByProviderMessageIdRequest Request)
    : IRequest<Result<RecordProviderDeliveryEventResponse>>;

public sealed class RecordProviderDeliveryEventByProviderMessageIdValidator
    : AbstractValidator<RecordProviderDeliveryEventByProviderMessageIdCommand>
{
    public RecordProviderDeliveryEventByProviderMessageIdValidator()
    {
        RuleFor(command => command.Request.Provider).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Request.ProviderMessageId).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Request.EventType)
            .NotEmpty()
            .Must(NotificationDeliveryEventTypes.IsKnown)
            .WithMessage("Delivery event type must be accepted, delivered, bounced, or complained.");
        RuleFor(command => command.Request.ProviderEventId)
            .MaximumLength(NotificationDeliveryEvent.ProviderEventIdMaxLength);
        RuleFor(command => command.Request.Reason)
            .MaximumLength(NotificationDeliveryEvent.ReasonMaxLength);
    }
}

public sealed class RecordProviderDeliveryEventByProviderMessageIdHandler(
    INotificationDeliveryStore store)
    : IRequestHandler<RecordProviderDeliveryEventByProviderMessageIdCommand, Result<RecordProviderDeliveryEventResponse>>
{
    public Task<Result<RecordProviderDeliveryEventResponse>> Handle(
        RecordProviderDeliveryEventByProviderMessageIdCommand command,
        CancellationToken cancellationToken)
    {
        return store.RecordProviderDeliveryEventByProviderMessageIdAsync(
            command.Request,
            cancellationToken);
    }
}
