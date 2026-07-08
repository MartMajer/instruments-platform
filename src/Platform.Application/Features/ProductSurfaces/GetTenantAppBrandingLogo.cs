using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

/// <summary>
/// The authenticated logo-serve path for the researcher app (Settings preview,
/// app shell). Resolves the logo bytes for the caller's own tenant only — the
/// tenant is the authenticated context, never a client-supplied id.
/// </summary>
public sealed record GetTenantAppBrandingLogoQuery : IRequest<Result<TenantAppBrandingLogoAsset>>;

public sealed class GetTenantAppBrandingLogoHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetTenantAppBrandingLogoQuery, Result<TenantAppBrandingLogoAsset>>
{
    public Task<Result<TenantAppBrandingLogoAsset>> Handle(
        GetTenantAppBrandingLogoQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetTenantAppBrandingLogoAsync(currentTenant.TenantId, cancellationToken);
    }
}
