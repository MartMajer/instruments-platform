using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

/// <summary>
/// Lightweight authenticated read for the researcher app shell: just the
/// tenant's branding tokens (accent + logo presence), without the full
/// tenant-settings aggregate. The tenant is the authenticated context.
/// </summary>
public sealed record GetTenantAppBrandingQuery : IRequest<Result<TenantSettingsAppBrandingResponse>>;

public sealed class GetTenantAppBrandingHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetTenantAppBrandingQuery, Result<TenantSettingsAppBrandingResponse>>
{
    public Task<Result<TenantSettingsAppBrandingResponse>> Handle(
        GetTenantAppBrandingQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetTenantAppBrandingAsync(currentTenant.TenantId, cancellationToken);
    }
}
