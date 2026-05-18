using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetTenantSettingsQuery
    : IRequest<Result<TenantSettingsWorkspaceResponse>>;

public sealed class GetTenantSettingsHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsWorkspaceResponse>>
{
    public Task<Result<TenantSettingsWorkspaceResponse>> Handle(
        GetTenantSettingsQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetTenantSettingsAsync(
            currentTenant.TenantId,
            cancellationToken);
    }
}
