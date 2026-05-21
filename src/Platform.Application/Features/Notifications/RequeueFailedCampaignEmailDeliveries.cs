using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record RequeueFailedCampaignEmailDeliveriesCommand(
    Guid CampaignId,
    RequeueFailedCampaignEmailDeliveriesRequest Request)
    : IRequest<Result<RequeueFailedCampaignEmailDeliveriesResponse>>;

public sealed class RequeueFailedCampaignEmailDeliveriesValidator
    : AbstractValidator<RequeueFailedCampaignEmailDeliveriesCommand>
{
    public const int MaxBatchSize = 25;

    public RequeueFailedCampaignEmailDeliveriesValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.BatchSize)
            .InclusiveBetween(1, MaxBatchSize);
        RuleFor(command => command.Request)
            .Must(request =>
                request.ConfirmedAnotherEmailAppropriate ||
                request.ConfirmedNoPriorDelivery)
            .WithMessage("Confirm another invitation email is appropriate before requeueing.");
    }
}

public sealed class RequeueFailedCampaignEmailDeliveriesHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<RequeueFailedCampaignEmailDeliveriesCommand, Result<RequeueFailedCampaignEmailDeliveriesResponse>>
{
    public Task<Result<RequeueFailedCampaignEmailDeliveriesResponse>> Handle(
        RequeueFailedCampaignEmailDeliveriesCommand command,
        CancellationToken cancellationToken)
    {
        return store.RequeueFailedCampaignEmailDeliveriesAsync(
            currentTenant.TenantId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
