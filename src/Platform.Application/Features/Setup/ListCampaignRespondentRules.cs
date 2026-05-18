using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record ListCampaignRespondentRulesQuery(Guid CampaignId)
    : IRequest<Result<CampaignRespondentRuleListResponse>>;

public sealed class ListCampaignRespondentRulesValidator : AbstractValidator<ListCampaignRespondentRulesQuery>
{
    public ListCampaignRespondentRulesValidator()
    {
        RuleFor(query => query.CampaignId).NotEmpty();
    }
}

public sealed class ListCampaignRespondentRulesHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<ListCampaignRespondentRulesQuery, Result<CampaignRespondentRuleListResponse>>
{
    public Task<Result<CampaignRespondentRuleListResponse>> Handle(
        ListCampaignRespondentRulesQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListCampaignRespondentRulesAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
