using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record UpdateCampaignRespondentRulesCommand(
    Guid CampaignId,
    UpdateCampaignRespondentRulesRequest Request)
    : IRequest<Result<CampaignRespondentRuleListResponse>>;

public sealed class UpdateCampaignRespondentRulesValidator : AbstractValidator<UpdateCampaignRespondentRulesCommand>
{
    public UpdateCampaignRespondentRulesValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.Rules).NotNull();
        RuleForEach(command => command.Request.Rules)
            .ChildRules(rule =>
            {
                rule.RuleFor(item => item.Rule).NotEmpty();
            });
    }
}

public sealed class UpdateCampaignRespondentRulesHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<UpdateCampaignRespondentRulesCommand, Result<CampaignRespondentRuleListResponse>>
{
    public Task<Result<CampaignRespondentRuleListResponse>> Handle(
        UpdateCampaignRespondentRulesCommand command,
        CancellationToken cancellationToken)
    {
        return store.UpdateCampaignRespondentRulesAsync(
            currentTenant.TenantId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
