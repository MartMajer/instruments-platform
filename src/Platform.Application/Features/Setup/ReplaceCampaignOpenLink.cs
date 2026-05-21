using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record ReplaceCampaignOpenLinkCommand(Guid CampaignId)
    : IRequest<Result<CampaignOpenLinkResponse>>;

public sealed class ReplaceCampaignOpenLinkValidator : AbstractValidator<ReplaceCampaignOpenLinkCommand>
{
    public ReplaceCampaignOpenLinkValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class ReplaceCampaignOpenLinkHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<ReplaceCampaignOpenLinkCommand, Result<CampaignOpenLinkResponse>>
{
    public Task<Result<CampaignOpenLinkResponse>> Handle(
        ReplaceCampaignOpenLinkCommand command,
        CancellationToken cancellationToken)
    {
        return store.ReplaceCampaignOpenLinkAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
