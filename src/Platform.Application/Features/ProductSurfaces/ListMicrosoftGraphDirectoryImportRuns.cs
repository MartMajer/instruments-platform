using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListMicrosoftGraphDirectoryImportRunsQuery : IRequest<DirectoryImportRunHistoryResponse>;

public sealed class ListMicrosoftGraphDirectoryImportRunsHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListMicrosoftGraphDirectoryImportRunsQuery, DirectoryImportRunHistoryResponse>
{
    public Task<DirectoryImportRunHistoryResponse> Handle(
        ListMicrosoftGraphDirectoryImportRunsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListMicrosoftGraphDirectoryImportRunsAsync(currentTenant.TenantId, cancellationToken);
    }
}
