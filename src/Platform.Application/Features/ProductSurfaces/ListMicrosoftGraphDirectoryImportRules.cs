using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListMicrosoftGraphDirectoryImportRulesQuery : IRequest<DirectoryImportRuleListResponse>;

public sealed class ListMicrosoftGraphDirectoryImportRulesHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListMicrosoftGraphDirectoryImportRulesQuery, DirectoryImportRuleListResponse>
{
    public Task<DirectoryImportRuleListResponse> Handle(
        ListMicrosoftGraphDirectoryImportRulesQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListMicrosoftGraphDirectoryImportRulesAsync(currentTenant.TenantId, cancellationToken);
    }
}
