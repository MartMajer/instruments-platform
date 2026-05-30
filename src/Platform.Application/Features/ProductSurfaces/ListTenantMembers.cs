using System.Text.Json.Serialization;
using MediatR;
using Platform.Application.Auth;
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

public sealed record TenantMemberRosterResponse
{
    [JsonConstructor]
    public TenantMemberRosterResponse(
        Guid tenantId,
        TenantMemberResponse[] members,
        TenantMemberRosterSummaryResponse summary)
    {
        TenantId = tenantId;
        Members = members;
        Summary = summary;
    }

    public TenantMemberRosterResponse(Guid tenantId, TenantMemberResponse[] members)
        : this(tenantId, members, TenantMemberRosterSummaryResponse.FromMembers(members))
    {
    }

    public Guid TenantId { get; init; }

    public TenantMemberResponse[] Members { get; init; }

    public TenantMemberRosterSummaryResponse Summary { get; init; }
}

public sealed record TenantMemberRosterSummaryResponse(
    int TotalCount,
    int ActiveCount,
    int InvitedCount,
    int SuspendedCount,
    int TeamManagerCount)
{
    public static TenantMemberRosterSummaryResponse FromMembers(IEnumerable<TenantMemberResponse> members)
    {
        var materialized = members.ToArray();

        return new TenantMemberRosterSummaryResponse(
            materialized.Length,
            materialized.Count(member => member.Status == TenantMemberAccessStatuses.Active),
            materialized.Count(member => member.Status == TenantMemberAccessStatuses.Invited),
            materialized.Count(member => member.Status == TenantMemberAccessStatuses.Suspended),
            materialized.Count(member => member.Permissions.Contains(PlatformPermissions.TeamManage, StringComparer.Ordinal)));
    }
}

public sealed record TenantMemberResponse(
    Guid UserId,
    string Email,
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    TenantMemberRoleResponse[] Roles,
    string[] Permissions,
    string IdentityStatus = TenantMemberIdentityStatuses.PendingProviderLink,
    string Status = TenantMemberAccessStatuses.Invited,
    string StatusLabel = TenantMemberAccessStatusLabels.Invited);

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
    public const string Disabled = "disabled";
}

public static class TenantMemberAccessStatuses
{
    public const string Active = "active";
    public const string Invited = "invited";
    public const string Suspended = "suspended";
}

public static class TenantMemberAccessStatusLabels
{
    public const string Active = "Active";
    public const string Invited = "Invited";
    public const string Suspended = "Suspended";
}
