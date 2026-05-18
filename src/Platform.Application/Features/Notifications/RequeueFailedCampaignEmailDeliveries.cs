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
