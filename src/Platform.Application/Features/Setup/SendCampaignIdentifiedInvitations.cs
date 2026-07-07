using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record SendCampaignIdentifiedInvitationsCommand(Guid CampaignId)
    : IRequest<Result<CampaignIdentifiedInvitationResponse>>;

public sealed class SendCampaignIdentifiedInvitationsValidator
    : AbstractValidator<SendCampaignIdentifiedInvitationsCommand>
{
    public SendCampaignIdentifiedInvitationsValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class SendCampaignIdentifiedInvitationsHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<SendCampaignIdentifiedInvitationsCommand, Result<CampaignIdentifiedInvitationResponse>>
{
    public Task<Result<CampaignIdentifiedInvitationResponse>> Handle(
        SendCampaignIdentifiedInvitationsCommand command,
        CancellationToken cancellationToken)
    {
        return store.SendCampaignIdentifiedInvitationsAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
