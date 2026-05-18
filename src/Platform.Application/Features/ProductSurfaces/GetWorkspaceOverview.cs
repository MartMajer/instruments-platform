using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetWorkspaceOverviewQuery
    : IRequest<WorkspaceOverviewResponse>;

public sealed class GetWorkspaceOverviewHandler(
    ICurrentTenant currentTenant,
    ICurrentActor currentActor,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetWorkspaceOverviewQuery, WorkspaceOverviewResponse>
{
    public Task<WorkspaceOverviewResponse> Handle(
        GetWorkspaceOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var canManageSetup = currentActor.Permissions.Contains(
            PlatformPermissions.SetupManage,
            StringComparer.Ordinal);
        var canManageTeam = currentActor.Permissions.Contains(
            PlatformPermissions.TeamManage,
            StringComparer.Ordinal);

        return store.GetWorkspaceOverviewAsync(
            currentTenant.TenantId,
            canManageSetup,
            canManageTeam,
            cancellationToken);
    }
}
