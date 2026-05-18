using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListSubjectsQuery : IRequest<SubjectDirectoryResponse>;

public sealed class ListSubjectsHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListSubjectsQuery, SubjectDirectoryResponse>
{
    public Task<SubjectDirectoryResponse> Handle(
        ListSubjectsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListSubjectsAsync(currentTenant.TenantId, cancellationToken);
    }
}
