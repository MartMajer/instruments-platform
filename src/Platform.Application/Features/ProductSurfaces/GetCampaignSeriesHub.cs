using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesHubQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesHubResponse>>;

public sealed class GetCampaignSeriesHubValidator
    : AbstractValidator<GetCampaignSeriesHubQuery>
{
    public GetCampaignSeriesHubValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesHubHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesHubQuery, Result<CampaignSeriesHubResponse>>
{
    public Task<Result<CampaignSeriesHubResponse>> Handle(
        GetCampaignSeriesHubQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesHubAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
