using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateCampaignOpenLinkCommand(Guid CampaignId)
    : IRequest<Result<CampaignOpenLinkResponse>>;

public sealed class CreateCampaignOpenLinkValidator : AbstractValidator<CreateCampaignOpenLinkCommand>
{
    public CreateCampaignOpenLinkValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateCampaignOpenLinkHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateCampaignOpenLinkCommand, Result<CampaignOpenLinkResponse>>
{
    public Task<Result<CampaignOpenLinkResponse>> Handle(
        CreateCampaignOpenLinkCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignOpenLinkAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
