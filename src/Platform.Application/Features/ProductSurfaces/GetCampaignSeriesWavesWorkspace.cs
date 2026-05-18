using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesWavesWorkspaceQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesWavesWorkspaceResponse>>;

public sealed class GetCampaignSeriesWavesWorkspaceValidator
    : AbstractValidator<GetCampaignSeriesWavesWorkspaceQuery>
{
    public GetCampaignSeriesWavesWorkspaceValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesWavesWorkspaceHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesWavesWorkspaceQuery, Result<CampaignSeriesWavesWorkspaceResponse>>
{
    public Task<Result<CampaignSeriesWavesWorkspaceResponse>> Handle(
        GetCampaignSeriesWavesWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesWavesWorkspaceAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
