using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetCampaignSeriesReportsWidgetManifestQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesReportsWidgetManifestResponse>>;

public sealed class GetCampaignSeriesReportsWidgetManifestValidator
    : AbstractValidator<GetCampaignSeriesReportsWidgetManifestQuery>
{
    public GetCampaignSeriesReportsWidgetManifestValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesReportsWidgetManifestHandler(
    ICurrentTenant currentTenant,
    ICurrentActor currentActor,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetCampaignSeriesReportsWidgetManifestQuery, Result<CampaignSeriesReportsWidgetManifestResponse>>
{
    public Task<Result<CampaignSeriesReportsWidgetManifestResponse>> Handle(
        GetCampaignSeriesReportsWidgetManifestQuery query,
        CancellationToken cancellationToken)
    {
        var canManageSetup = currentActor.Permissions.Contains(
            PlatformPermissions.SetupManage,
            StringComparer.Ordinal);

        return store.GetCampaignSeriesReportsWidgetManifestAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            canManageSetup,
            cancellationToken);
    }
}
