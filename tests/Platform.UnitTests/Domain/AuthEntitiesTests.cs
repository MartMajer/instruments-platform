using Platform.Domain.Auth;

namespace Platform.UnitTests.Domain;

public sealed class AuthEntitiesTests
{
    private static readonly DateTimeOffset FixedNow =
        DateTimeOffset.Parse("2026-05-14T10:00:00+00:00");

    [Fact]
    public void User_account_defaults_to_active_local_account_state()
    {
        var tenantId = Guid.NewGuid();
        var user = new UserAccount(Guid.NewGuid(), tenantId, "researcher@example.com", "en");

        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal("researcher@example.com", user.Email);
        Assert.Equal("en", user.Locale);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.PasswordHash);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public void Role_assignment_uses_tenant_scope_without_scope_id()
    {
        var tenantId = Guid.NewGuid();
        var roleAssignment = new RoleAssignment(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            RoleAssignmentScopes.Tenant);

        Assert.Equal(tenantId, roleAssignment.TenantId);
        Assert.Equal(RoleAssignmentScopes.Tenant, roleAssignment.ScopeType);
        Assert.Null(roleAssignment.ScopeId);
    }

    [Fact]
    public void Role_assignment_rejects_tenant_scope_with_scope_id()
    {
        Assert.Throws<ArgumentException>(() => new RoleAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RoleAssignmentScopes.Tenant,
            Guid.NewGuid()));
    }

    [Fact]
    public void Role_assignment_rejects_resource_scope_without_scope_id()
    {
        Assert.Throws<ArgumentException>(() => new RoleAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RoleAssignmentScopes.Campaign));
    }

    [Fact]
    public void Role_assignment_rejects_unknown_scope_type()
    {
        Assert.Throws<ArgumentException>(() => new RoleAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "organization"));
    }

    [Fact]
    public void Role_permission_uses_role_and_permission_as_identity()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var rolePermission = new RolePermission(roleId, permissionId);

        Assert.Equal(roleId, rolePermission.RoleId);
        Assert.Equal(permissionId, rolePermission.PermissionId);
    }

    [Fact]
    public void External_auth_identity_stores_provider_binding_metadata()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var identity = new ExternalAuthIdentity(
            Guid.NewGuid(),
            tenantId,
            userId,
            "auth0",
            "provider-subject-hash",
            "researcher@example.com",
            FixedNow);

        Assert.Equal(tenantId, identity.TenantId);
        Assert.Equal(userId, identity.UserId);
        Assert.Equal("auth0", identity.Provider);
        Assert.Equal("provider-subject-hash", identity.ProviderSubjectHash);
        Assert.Equal("researcher@example.com", identity.EmailAtBinding);
        Assert.Equal(FixedNow, identity.CreatedAt);
        Assert.Equal(FixedNow, identity.LastSeenAt);
        Assert.Null(identity.DisabledAt);
    }

    [Fact]
    public void External_auth_identity_records_last_seen()
    {
        var identity = new ExternalAuthIdentity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "auth0",
            "provider-subject-hash",
            "researcher@example.com",
            FixedNow);
        var seenAt = FixedNow.AddMinutes(15);

        identity.RecordSeen(seenAt);

        Assert.Equal(seenAt, identity.LastSeenAt);
    }

    [Fact]
    public void External_auth_identity_can_be_disabled()
    {
        var identity = new ExternalAuthIdentity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "auth0",
            "provider-subject-hash",
            "researcher@example.com",
            FixedNow);
        var disabledAt = FixedNow.AddMinutes(30);

        identity.Disable(disabledAt);

        Assert.Equal(disabledAt, identity.DisabledAt);
    }

    [Fact]
    public void Auth_session_stores_local_session_metadata()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var externalIdentityId = Guid.NewGuid();
        var expiresAt = FixedNow.AddHours(8);
        var session = new AuthSession(
            Guid.NewGuid(),
            tenantId,
            userId,
            externalIdentityId,
            FixedNow,
            expiresAt);

        Assert.Equal(tenantId, session.TenantId);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(externalIdentityId, session.ExternalAuthIdentityId);
        Assert.Equal(FixedNow, session.CreatedAt);
        Assert.Equal(expiresAt, session.ExpiresAt);
        Assert.Null(session.RevokedAt);
        Assert.Null(session.RevokedReason);
        Assert.True(session.IsActive(FixedNow.AddMinutes(1)));
    }

    [Fact]
    public void Auth_session_revoke_sets_reason_and_inactive_state()
    {
        var session = new AuthSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FixedNow,
            FixedNow.AddHours(8));
        var revokedAt = FixedNow.AddMinutes(5);

        session.Revoke(revokedAt, "logout");

        Assert.Equal(revokedAt, session.RevokedAt);
        Assert.Equal("logout", session.RevokedReason);
        Assert.False(session.IsActive(revokedAt.AddSeconds(1)));
    }

    [Fact]
    public void Auth_session_is_inactive_after_expiry()
    {
        var session = new AuthSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FixedNow,
            FixedNow.AddHours(8));

        Assert.False(session.IsActive(FixedNow.AddHours(8).AddSeconds(1)));
    }

    [Fact]
    public void Auth_session_rejects_invalid_revoke_reasons()
    {
        var session = new AuthSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FixedNow,
            FixedNow.AddHours(8));

        Assert.Throws<ArgumentException>(() => session.Revoke(FixedNow, ""));
        Assert.Throws<ArgumentException>(() => session.Revoke(FixedNow, new string('x', 65)));
    }
}
