using Microsoft.EntityFrameworkCore;
using Platform.Api.Registration;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Auth;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Api.Auth;

public sealed class EfPlatformRegistrationLoginResolver(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    ICurrentTenant currentTenant,
    IProviderSubjectHasher providerSubjectHasher,
    IRegistrationTokenProtector registrationTokenProtector,
    IConfiguration configuration) : IPlatformRegistrationLoginResolver
{
    private const string OwnerRoleCode = "tenant_owner";
    private const string ResearcherRoleCode = "researcher";
    private const string AnalystRoleCode = "analyst";
    private const string ViewerRoleCode = "viewer";
    private const string Provider = "auth0";

    public async Task<PlatformOidcLoginResolution?> ResolveAsync(
        string registrationToken,
        string email,
        bool emailVerified,
        string provider,
        string providerSubject,
        CancellationToken cancellationToken)
    {
        if (currentTenant.HasTenant ||
            string.IsNullOrWhiteSpace(registrationToken) ||
            !string.Equals(provider, Provider, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var registrationTokenHash = registrationTokenProtector.Hash(registrationToken);
        var providerSubjectHash = providerSubjectHasher.Hash(normalizedProvider, providerSubject);
        var now = DateTimeOffset.UtcNow;
        var sessionMinutes = Math.Max(1, configuration.GetValue("Authentication:Oidc:SessionMinutes", 480));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var intent = await db.RegistrationIntents
            .SingleOrDefaultAsync(
                candidate => candidate.RegistrationTokenHash == registrationTokenHash,
                cancellationToken);
        if (intent is null ||
            !intent.IsPending(now) ||
            !string.Equals(intent.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }


        var setupPermission = await EnsurePermissionAsync(PlatformPermissions.SetupManage, cancellationToken);
        var teamPermission = await EnsurePermissionAsync(PlatformPermissions.TeamManage, cancellationToken);
        var exportPermission = await EnsurePermissionAsync(PlatformPermissions.ExportRead, cancellationToken);

        var tenantId = PlatformIds.NewId();
        currentTenant.SetTenant(tenantId, "registration");
        await tenantDbScope.SetTenantAsync(tenantId, cancellationToken: cancellationToken);

        db.Tenants.Add(new Tenant(tenantId, intent.Slug, intent.OrganizationName));
        await db.SaveChangesAsync(cancellationToken);

        var ownerRole = new Role(PlatformIds.NewId(), tenantId, OwnerRoleCode, "Tenant owner");
        var researcherRole = new Role(PlatformIds.NewId(), tenantId, ResearcherRoleCode, "Researcher");
        var analystRole = new Role(PlatformIds.NewId(), tenantId, AnalystRoleCode, "Analyst");
        var viewerRole = new Role(PlatformIds.NewId(), tenantId, ViewerRoleCode, "Viewer");
        db.Roles.AddRange(ownerRole, researcherRole, analystRole, viewerRole);
        db.RolePermissions.AddRange(
            new RolePermission(ownerRole.Id, setupPermission.Id),
            new RolePermission(ownerRole.Id, teamPermission.Id),
            new RolePermission(ownerRole.Id, exportPermission.Id),
            new RolePermission(researcherRole.Id, setupPermission.Id),
            new RolePermission(analystRole.Id, exportPermission.Id));

        var user = new UserAccount(PlatformIds.NewId(), tenantId, intent.Email);
        db.UserAccounts.Add(user);
        db.RoleAssignments.Add(new RoleAssignment(
            PlatformIds.NewId(),
            tenantId,
            user.Id,
            ownerRole.Id,
            RoleAssignmentScopes.Tenant));

        var binding = new ExternalAuthIdentity(
            PlatformIds.NewId(),
            tenantId,
            user.Id,
            normalizedProvider,
            providerSubjectHash,
            email,
            now);
        if (emailVerified)
        {
            binding.RecordEmailVerified(now);
        }
        else
        {
            binding.RecordEmailVerificationGrace(now);
        }
        db.ExternalAuthIdentities.Add(binding);

        var session = new AuthSession(
            PlatformIds.NewId(),
            tenantId,
            user.Id,
            binding.Id,
            now,
            now.AddMinutes(sessionMinutes));
        db.AuthSessions.Add(session);

        intent.Consume(tenantId, now);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PlatformOidcLoginResolution(
            user.Id,
            tenantId,
            session.Id,
            [PlatformPermissions.ExportRead, PlatformPermissions.SetupManage, PlatformPermissions.TeamManage],
            email);
    }

    private async Task<Permission> EnsurePermissionAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(
            candidate => candidate.Code == code,
            cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        permission = new Permission(PlatformIds.NewId(), code);
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(cancellationToken);

        return permission;
    }
}

