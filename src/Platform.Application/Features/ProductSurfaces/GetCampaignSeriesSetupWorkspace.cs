using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesSetupWorkspaceQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesSetupWorkspaceResponse>>;

public sealed class GetCampaignSeriesSetupWorkspaceValidator
    : AbstractValidator<GetCampaignSeriesSetupWorkspaceQuery>
{
    public GetCampaignSeriesSetupWorkspaceValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesSetupWorkspaceHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesSetupWorkspaceQuery, Result<CampaignSeriesSetupWorkspaceResponse>>
{
    public Task<Result<CampaignSeriesSetupWorkspaceResponse>> Handle(
        GetCampaignSeriesSetupWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesSetupWorkspaceAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
