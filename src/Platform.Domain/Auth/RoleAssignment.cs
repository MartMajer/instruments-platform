namespace Platform.Domain.Auth;

public sealed class RoleAssignment
{
    private RoleAssignment()
    {
    }

    public RoleAssignment(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid roleId,
        string scopeType,
        Guid? scopeId = null,
        Guid? grantedBy = null)
    {
        ValidateScope(scopeType, scopeId);

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        ScopeType = scopeType;
        ScopeId = scopeId;
        GrantedAt = DateTimeOffset.UtcNow;
        GrantedBy = grantedBy;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public string ScopeType { get; private set; } = RoleAssignmentScopes.Tenant;

    public Guid? ScopeId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    public Guid? GrantedBy { get; private set; }

    private static void ValidateScope(string scopeType, Guid? scopeId)
    {
        if (!RoleAssignmentScopes.IsKnown(scopeType))
        {
            throw new ArgumentException("Unknown role assignment scope type.", nameof(scopeType));
        }

        if (scopeType == RoleAssignmentScopes.Tenant && scopeId.HasValue)
        {
            throw new ArgumentException("Tenant-scoped role assignments cannot have a scope id.", nameof(scopeId));
        }

        if (scopeType != RoleAssignmentScopes.Tenant && !scopeId.HasValue)
        {
            throw new ArgumentException("Resource-scoped role assignments require a scope id.", nameof(scopeId));
        }
    }
}
