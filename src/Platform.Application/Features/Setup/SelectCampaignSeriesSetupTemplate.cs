using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record SelectCampaignSeriesSetupTemplateCommand(
    Guid CampaignSeriesId,
    SelectCampaignSeriesSetupTemplateRequest Request)
    : IRequest<Result<SelectCampaignSeriesSetupTemplateResponse>>;

public sealed class SelectCampaignSeriesSetupTemplateValidator
    : AbstractValidator<SelectCampaignSeriesSetupTemplateCommand>
{
    public SelectCampaignSeriesSetupTemplateValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request.TemplateVersionId).NotEmpty();
    }
}

public sealed class SelectCampaignSeriesSetupTemplateHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<SelectCampaignSeriesSetupTemplateCommand, Result<SelectCampaignSeriesSetupTemplateResponse>>
{
    public Task<Result<SelectCampaignSeriesSetupTemplateResponse>> Handle(
        SelectCampaignSeriesSetupTemplateCommand command,
        CancellationToken cancellationToken)
    {
        return store.SelectCampaignSeriesSetupTemplateAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.CampaignSeriesId,
            command.Request,
            cancellationToken);
    }
}
