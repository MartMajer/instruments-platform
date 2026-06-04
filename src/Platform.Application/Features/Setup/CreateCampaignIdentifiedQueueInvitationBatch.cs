using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateCampaignIdentifiedQueueInvitationBatchCommand(Guid CampaignId)
    : IRequest<Result<CampaignInvitationBatchResponse>>;

public sealed class CreateCampaignIdentifiedQueueInvitationBatchValidator
    : AbstractValidator<CreateCampaignIdentifiedQueueInvitationBatchCommand>
{
    public CreateCampaignIdentifiedQueueInvitationBatchValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateCampaignIdentifiedQueueInvitationBatchHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateCampaignIdentifiedQueueInvitationBatchCommand, Result<CampaignInvitationBatchResponse>>
{
    public Task<Result<CampaignInvitationBatchResponse>> Handle(
        CreateCampaignIdentifiedQueueInvitationBatchCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignIdentifiedQueueInvitationBatchAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
