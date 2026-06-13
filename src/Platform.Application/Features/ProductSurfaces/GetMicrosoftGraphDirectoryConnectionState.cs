using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record GetMicrosoftGraphDirectoryConnectionStateQuery : IRequest<DirectoryConnectionStateResponse>;

public sealed class GetMicrosoftGraphDirectoryConnectionStateHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<GetMicrosoftGraphDirectoryConnectionStateQuery, DirectoryConnectionStateResponse>
{
    public Task<DirectoryConnectionStateResponse> Handle(
        GetMicrosoftGraphDirectoryConnectionStateQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetMicrosoftGraphDirectoryConnectionStateAsync(currentTenant.TenantId, cancellationToken);
    }
}
