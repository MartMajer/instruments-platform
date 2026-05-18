using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record PreviewRespondentRuleQuery(
    Guid CampaignSeriesId,
    Guid CampaignId,
    RespondentRulePreviewRequest Request)
    : IRequest<Result<RespondentRulePreviewResponse>>;

public sealed class PreviewRespondentRuleValidator : AbstractValidator<PreviewRespondentRuleQuery>
{
    public PreviewRespondentRuleValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
        RuleFor(query => query.CampaignId).NotEmpty();
        RuleFor(query => query.Request).NotNull();
        RuleFor(query => query.Request.Rule)
            .NotEmpty()
            .When(query => query.Request is not null);
        RuleFor(query => query.Request.MaxRows)
            .InclusiveBetween(1, 200)
            .When(query => query.Request is not null);
    }
}

public sealed class PreviewRespondentRuleHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<PreviewRespondentRuleQuery, Result<RespondentRulePreviewResponse>>
{
    public Task<Result<RespondentRulePreviewResponse>> Handle(
        PreviewRespondentRuleQuery query,
        CancellationToken cancellationToken)
    {
        return store.PreviewRespondentRuleAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            query.CampaignId,
            query.Request,
            cancellationToken);
    }
}
