using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListExportArtifactsQuery
    : IRequest<ExportArtifactLibraryResponse>;

public sealed class ListExportArtifactsHandler(
    ICurrentTenant currentTenant,
    ICurrentActor currentActor,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListExportArtifactsQuery, ExportArtifactLibraryResponse>
{
    public Task<ExportArtifactLibraryResponse> Handle(
        ListExportArtifactsQuery query,
        CancellationToken cancellationToken)
    {
        var canManageSetup = currentActor.Permissions.Contains(
            PlatformPermissions.SetupManage,
            StringComparer.Ordinal);

        return store.ListExportArtifactsAsync(
            currentTenant.TenantId,
            canManageSetup,
            cancellationToken);
    }
}
