using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListTenantRolesQuery : IRequest<TenantRoleListResponse>;

public sealed class ListTenantRolesHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListTenantRolesQuery, TenantRoleListResponse>
{
    public Task<TenantRoleListResponse> Handle(
        ListTenantRolesQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListTenantRolesAsync(currentTenant.TenantId, cancellationToken);
    }
}

public sealed record TenantRoleListResponse(TenantRoleResponse[] Roles);

public sealed record TenantRoleResponse(
    Guid RoleId,
    string Code,
    string Name,
    string[] Permissions);
