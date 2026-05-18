using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record ProcessCampaignEmailDeliveriesCommand(
    Guid CampaignId,
    ProcessCampaignEmailDeliveriesRequest Request)
    : IRequest<Result<ProcessCampaignEmailDeliveriesResponse>>;

public sealed class ProcessCampaignEmailDeliveriesValidator
    : AbstractValidator<ProcessCampaignEmailDeliveriesCommand>
{
    public const int MaxBatchSize = 25;

    public ProcessCampaignEmailDeliveriesValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.BatchSize)
            .InclusiveBetween(1, MaxBatchSize);
    }
}

public sealed class ProcessCampaignEmailDeliveriesHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<ProcessCampaignEmailDeliveriesCommand, Result<ProcessCampaignEmailDeliveriesResponse>>
{
    public Task<Result<ProcessCampaignEmailDeliveriesResponse>> Handle(
        ProcessCampaignEmailDeliveriesCommand command,
        CancellationToken cancellationToken)
    {
        return store.ProcessCampaignEmailDeliveriesAsync(
            currentTenant.TenantId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
