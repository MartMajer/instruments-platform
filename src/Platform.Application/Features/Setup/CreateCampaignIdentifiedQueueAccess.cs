using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateCampaignIdentifiedQueueAccessCommand(Guid CampaignId)
    : IRequest<Result<CampaignIdentifiedQueueAccessResponse>>;

public sealed class CreateCampaignIdentifiedQueueAccessValidator
    : AbstractValidator<CreateCampaignIdentifiedQueueAccessCommand>
{
    public CreateCampaignIdentifiedQueueAccessValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateCampaignIdentifiedQueueAccessHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateCampaignIdentifiedQueueAccessCommand, Result<CampaignIdentifiedQueueAccessResponse>>
{
    public Task<Result<CampaignIdentifiedQueueAccessResponse>> Handle(
        CreateCampaignIdentifiedQueueAccessCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignIdentifiedQueueAccessAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
