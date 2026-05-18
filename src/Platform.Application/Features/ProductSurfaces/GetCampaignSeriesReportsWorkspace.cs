using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesReportsWorkspaceQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesReportsWorkspaceResponse>>;

public sealed class GetCampaignSeriesReportsWorkspaceValidator
    : AbstractValidator<GetCampaignSeriesReportsWorkspaceQuery>
{
    public GetCampaignSeriesReportsWorkspaceValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesReportsWorkspaceHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesReportsWorkspaceQuery, Result<CampaignSeriesReportsWorkspaceResponse>>
{
    public Task<Result<CampaignSeriesReportsWorkspaceResponse>> Handle(
        GetCampaignSeriesReportsWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesReportsWorkspaceAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
