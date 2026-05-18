using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesOperationsWorkspaceQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesOperationsWorkspaceResponse>>;

public sealed class GetCampaignSeriesOperationsWorkspaceValidator
    : AbstractValidator<GetCampaignSeriesOperationsWorkspaceQuery>
{
    public GetCampaignSeriesOperationsWorkspaceValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesOperationsWorkspaceHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesOperationsWorkspaceQuery, Result<CampaignSeriesOperationsWorkspaceResponse>>
{
    public Task<Result<CampaignSeriesOperationsWorkspaceResponse>> Handle(
        GetCampaignSeriesOperationsWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesOperationsWorkspaceAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
