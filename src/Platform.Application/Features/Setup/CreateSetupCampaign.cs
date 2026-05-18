using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateSetupCampaignCommand(
    CreateCampaignRequest Request) : IRequest<Result<CampaignDraftResponse>>;

public sealed class CreateSetupCampaignValidator
    : AbstractValidator<CreateSetupCampaignCommand>
{
    public CreateSetupCampaignValidator()
    {
        RuleFor(command => command.Request.TemplateVersionId).NotEmpty();
        RuleFor(command => command.Request.Name).NotEmpty();
        RuleFor(command => command.Request.ResponseIdentityMode)
            .Must(ResponseIdentityModes.IsKnown)
            .WithMessage("Unknown response identity mode.");
        RuleFor(command => command.Request.Schedule).NotEmpty();
        RuleFor(command => command.Request.DefaultLocale).NotEmpty();
    }
}

public sealed class CreateSetupCampaignHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateSetupCampaignCommand, Result<CampaignDraftResponse>>
{
    public Task<Result<CampaignDraftResponse>> Handle(
        CreateSetupCampaignCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.Request,
            cancellationToken);
    }
}
