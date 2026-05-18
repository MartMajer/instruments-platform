using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListTenantMembersQuery : IRequest<TenantMemberRosterResponse>;

public sealed class ListTenantMembersHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListTenantMembersQuery, TenantMemberRosterResponse>
{
    public Task<TenantMemberRosterResponse> Handle(
        ListTenantMembersQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListTenantMembersAsync(currentTenant.TenantId, cancellationToken);
    }
}

public sealed record TenantMemberRosterResponse(
    Guid TenantId,
    TenantMemberResponse[] Members);

public sealed record TenantMemberResponse(
    Guid UserId,
    string Email,
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    TenantMemberRoleResponse[] Roles,
    string[] Permissions,
    string IdentityStatus = TenantMemberIdentityStatuses.PendingProviderLink);

public sealed record TenantMemberRoleResponse(
    Guid RoleId,
    string Code,
    string Name,
    string ScopeType,
    Guid? ScopeId,
    DateTimeOffset GrantedAt);

public static class TenantMemberIdentityStatuses
{
    public const string Active = "active";
    public const string PendingProviderLink = "pending_provider_link";
}
