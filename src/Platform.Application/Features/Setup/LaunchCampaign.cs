using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record LaunchCampaignCommand(Guid CampaignId)
    : IRequest<Result<LaunchCampaignResponse>>;

public sealed class LaunchCampaignValidator : AbstractValidator<LaunchCampaignCommand>
{
    public LaunchCampaignValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class LaunchCampaignHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<LaunchCampaignCommand, Result<LaunchCampaignResponse>>
{
    public Task<Result<LaunchCampaignResponse>> Handle(
        LaunchCampaignCommand command,
        CancellationToken cancellationToken)
    {
        return store.LaunchCampaignAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.CampaignId,
            cancellationToken);
    }
}
