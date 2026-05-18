using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListSubjectGroupsQuery : IRequest<SubjectGroupListResponse>;

public sealed class ListSubjectGroupsHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListSubjectGroupsQuery, SubjectGroupListResponse>
{
    public Task<SubjectGroupListResponse> Handle(
        ListSubjectGroupsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListSubjectGroupsAsync(currentTenant.TenantId, cancellationToken);
    }
}
