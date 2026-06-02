using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Platform.Api.Auth;
using Platform.Application.Auditing;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Features.Reports;
using Platform.Application.Features.Responses;
using Platform.Application.Features.Setup;
using Platform.Application.Outbox;
using Platform.Application.Tenancy;
using Platform.Domain.Auditing;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.DirectoryImports;
using Platform.Domain.Instruments;
using Platform.Domain.Outbox;
using Platform.Domain.Operations;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Interceptors;
using Platform.Infrastructure.Notifications;
using Platform.Infrastructure.ParticipantCodes;
using Platform.Infrastructure.Reports;
using Platform.Infrastructure.Responses;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Setup;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Platform.IntegrationTests.Support.Logging;
using Platform.SharedKernel;
using Platform.Workers.Outbox;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class PostgresMigrationTests : IAsyncLifetime
{
    private const string RuntimeUsername = "platform_app_runtime";
    private const string RuntimePassword = "platform_app_runtime";
    private const string WorkerUsername = "platform_worker";
    private const string WorkerPassword = "platform_worker";
    private const string TenantAttestedScoreInterpretationProduces = """
        {
          "scores": ["total"],
          "interpretation": {
            "status": "tenant_attested",
            "source": "tenant_defined",
            "provenance": "Tenant-defined score bands for this setup; not validated; not official.",
            "scores": {
              "total": [
                { "code": "lower", "label": "Tenant lower range", "min": 1, "max": 2.49 },
                { "code": "middle", "label": "Tenant middle range", "min": 2.5, "max": 3.49 },
                { "code": "higher", "label": "Tenant higher range", "min": 3.5, "max": 5 }
              ]
            }
          }
        }
        """;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Migrations_create_auth_core_tables_and_allow_role_assignment_insert()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roleAssignmentId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var migratorDb = new ApplicationDbContext(migratorOptions))
        {
            await migratorDb.Database.MigrateAsync();

            var pendingMigrations = await migratorDb.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        db.Tenants.Add(new Tenant(tenantId, "acme", "Acme Research"));
        db.UserAccounts.Add(new UserAccount(userId, tenantId, "researcher@example.com"));
        db.Roles.Add(new Role(roleId, tenantId, "tenant_admin", "Tenant Admin"));
        db.Permissions.Add(new Permission(permissionId, "tenant.manage"));
        db.RolePermissions.Add(new RolePermission(roleId, permissionId));
        db.RoleAssignments.Add(new RoleAssignment(
            roleAssignmentId,
            tenantId,
            userId,
            roleId,
            RoleAssignmentScopes.Tenant,
            grantedBy: userId));

        await db.SaveChangesAsync();

        var savedUser = await db.UserAccounts.SingleAsync(user => user.Id == userId);
        var savedAssignment = await db.RoleAssignments.SingleAsync(assignment => assignment.Id == roleAssignmentId);

        Assert.Equal("researcher@example.com", savedUser.Email);
        Assert.Equal(RoleAssignmentScopes.Tenant, savedAssignment.ScopeType);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_role_assignment_to_user_from_another_tenant()
    {
        var tenantA = TenantGraph.Create("tenant-a", "Tenant A", "admin-a@example.com");
        var tenantB = TenantGraph.Create("tenant-b", "Tenant B", "admin-b@example.com");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenantA);
        await SeedTenantGraphAsync(runtimeOptions, tenantB);

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.RoleAssignments.Add(new RoleAssignment(
            Guid.NewGuid(),
            tenantA.TenantId,
            tenantB.UserId,
            tenantA.RoleId,
            RoleAssignmentScopes.Tenant,
            grantedBy: tenantA.UserId));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_role_assignment_to_tenant_role_from_another_tenant()
    {
        var tenantA = TenantGraph.Create("tenant-a", "Tenant A", "admin-a@example.com");
        var tenantB = TenantGraph.Create("tenant-b", "Tenant B", "admin-b@example.com");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenantA);
        await SeedTenantGraphAsync(runtimeOptions, tenantB);

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.RoleAssignments.Add(new RoleAssignment(
            Guid.NewGuid(),
            tenantA.TenantId,
            tenantA.UserId,
            tenantB.RoleId,
            RoleAssignmentScopes.Tenant,
            grantedBy: tenantA.UserId));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Migrations_create_auth_identity_and_session_tables_with_rls()
    {
        var tenant = TenantGraph.Create("auth-tenant", "Auth Tenant", "admin@example.com");
        var externalIdentityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-05-14T10:00:00+00:00", CultureInfo.InvariantCulture);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenant);

        await using var tenantDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenant.TenantId);

        tenantDb.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
            externalIdentityId,
            tenant.TenantId,
            tenant.UserId,
            "auth0",
            "provider-subject-hash",
            tenant.UserEmail,
            now));
        await tenantDb.SaveChangesAsync();

        tenantDb.AuthSessions.Add(new AuthSession(
            sessionId,
            tenant.TenantId,
            tenant.UserId,
            externalIdentityId,
            now,
            now.AddHours(8)));
        await tenantDb.SaveChangesAsync();

        var savedIdentity = await tenantDb.ExternalAuthIdentities.SingleAsync(identity => identity.Id == externalIdentityId);
        var savedSession = await tenantDb.AuthSessions.SingleAsync(session => session.Id == sessionId);

        Assert.Equal("provider-subject-hash", savedIdentity.ProviderSubjectHash);
        Assert.True(savedSession.IsActive(now.AddMinutes(1)));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_external_auth_identity_for_user_from_another_tenant()
    {
        var tenantA = TenantGraph.Create("auth-tenant-a", "Auth Tenant A", "admin-a@example.com");
        var tenantB = TenantGraph.Create("auth-tenant-b", "Auth Tenant B", "admin-b@example.com");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenantA);
        await SeedTenantGraphAsync(runtimeOptions, tenantB);

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
            Guid.NewGuid(),
            tenantA.TenantId,
            tenantB.UserId,
            "auth0",
            "provider-subject-hash",
            tenantA.UserEmail,
            DateTimeOffset.Parse("2026-05-14T10:00:00+00:00", CultureInfo.InvariantCulture)));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_auth_session_with_external_identity_from_another_tenant()
    {
        var tenantA = TenantGraph.Create("auth-session-tenant-a", "Auth Session Tenant A", "admin-a@example.com");
        var tenantB = TenantGraph.Create("auth-session-tenant-b", "Auth Session Tenant B", "admin-b@example.com");
        var tenantBExternalIdentityId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-05-14T10:00:00+00:00", CultureInfo.InvariantCulture);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenantA);
        await SeedTenantGraphAsync(runtimeOptions, tenantB);

        await using (var tenantBDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantBDbScope = new TenantDbScope(tenantBDb);
            await using var tenantBTransaction = await tenantBDbScope.BeginTransactionAsync(tenantB.TenantId);

            tenantBDb.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
                tenantBExternalIdentityId,
                tenantB.TenantId,
                tenantB.UserId,
                "auth0",
                "provider-subject-hash-b",
                tenantB.UserEmail,
                now));

            await tenantBDb.SaveChangesAsync();
            await tenantBTransaction.CommitAsync();
        }

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantADbScope = new TenantDbScope(tenantADb);
        await using var tenantATransaction = await tenantADbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.AuthSessions.Add(new AuthSession(
            Guid.NewGuid(),
            tenantA.TenantId,
            tenantA.UserId,
            tenantBExternalIdentityId,
            now,
            now.AddHours(8)));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Oidc_login_resolver_creates_provider_binding_and_local_session()
    {
        var tenant = TenantGraph.Create("oidc-resolver-tenant", "OIDC Resolver Tenant", "admin@example.com");
        var permissionId = Guid.NewGuid();
        var roleAssignmentId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenant);

        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var seedTenantDbScope = new TenantDbScope(seedDb);
            await using var seedTransaction = await seedTenantDbScope.BeginTransactionAsync(tenant.TenantId);

            seedDb.Permissions.Add(new Permission(permissionId, "setup.manage"));
            seedDb.RolePermissions.Add(new RolePermission(tenant.RoleId, permissionId));
            seedDb.RoleAssignments.Add(new RoleAssignment(
                roleAssignmentId,
                tenant.TenantId,
                tenant.UserId,
                tenant.RoleId,
                RoleAssignmentScopes.Tenant,
                grantedBy: tenant.UserId));
            await seedDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var currentTenant = new CurrentTenant();
        var auditContext = new CurrentAuditContext();
        var resolverOptions = CreateRuntimeOptions(
            new OutboxSaveChangesInterceptor(currentTenant, auditContext, new OutboxEventBuffer()),
            new AuditSaveChangesInterceptor(currentTenant, auditContext));
        await using var resolverDb = new ApplicationDbContext(resolverOptions);
        var resolverTenantDbScope = new TenantDbScope(resolverDb);
        var resolver = new EfPlatformOidcLoginResolver(
            resolverDb,
            resolverTenantDbScope,
            currentTenant,
            new Sha256ProviderSubjectHasher(),
            CreateOidcResolverConfiguration(sessionMinutes: 15));

        var resolution = await resolver.ResolveAsync(
            tenant.TenantId,
            tenant.UserEmail,
            true,
            "auth0",
            "auth0|resolver-subject",
            CancellationToken.None);

        Assert.NotNull(resolution);
        Assert.Equal(tenant.UserId, resolution.UserId);
        Assert.Equal(tenant.TenantId, resolution.TenantId);
        Assert.Contains("setup.manage", resolution.Permissions);

        await using var verifyTransaction = await resolverTenantDbScope.BeginTransactionAsync(tenant.TenantId);
        var binding = await resolverDb.ExternalAuthIdentities.SingleAsync();
        var session = await resolverDb.AuthSessions.SingleAsync();

        Assert.Equal(tenant.UserId, binding.UserId);
        Assert.Equal("auth0", binding.Provider);
        Assert.Equal(resolution.SessionId, session.Id);
        Assert.Equal(binding.Id, session.ExternalAuthIdentityId);
        Assert.Equal(session.CreatedAt.AddMinutes(15), session.ExpiresAt);
    }

    [DockerFact]
    public async Task Oidc_login_resolver_rejects_provider_subject_mismatch()
    {
        var tenant = TenantGraph.Create(
            "oidc-resolver-mismatch",
            "OIDC Resolver Mismatch",
            "admin@example.com");
        var permissionId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantGraphAsync(runtimeOptions, tenant);

        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var seedTenantDbScope = new TenantDbScope(seedDb);
            await using var seedTransaction = await seedTenantDbScope.BeginTransactionAsync(tenant.TenantId);

            seedDb.Permissions.Add(new Permission(permissionId, "setup.manage"));
            seedDb.RolePermissions.Add(new RolePermission(tenant.RoleId, permissionId));
            seedDb.RoleAssignments.Add(new RoleAssignment(
                Guid.NewGuid(),
                tenant.TenantId,
                tenant.UserId,
                tenant.RoleId,
                RoleAssignmentScopes.Tenant,
                grantedBy: tenant.UserId));
            await seedDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var currentTenant = new CurrentTenant();
        var resolverOptions = CreateRuntimeOptions();
        await using var resolverDb = new ApplicationDbContext(resolverOptions);
        var resolver = new EfPlatformOidcLoginResolver(
            resolverDb,
            new TenantDbScope(resolverDb),
            currentTenant,
            new Sha256ProviderSubjectHasher(),
            CreateOidcResolverConfiguration(sessionMinutes: 480));

        var firstResolution = await resolver.ResolveAsync(
            tenant.TenantId,
            tenant.UserEmail,
            true,
            "auth0",
            "auth0|original-subject",
            CancellationToken.None);
        var mismatchResolution = await resolver.ResolveAsync(
            tenant.TenantId,
            tenant.UserEmail,
            true,
            "auth0",
            "auth0|different-subject",
            CancellationToken.None);

        Assert.NotNull(firstResolution);
        Assert.Null(mismatchResolution);

        await using var verifyTransaction = await new TenantDbScope(resolverDb)
            .BeginTransactionAsync(tenant.TenantId);

        Assert.Equal(1, await resolverDb.ExternalAuthIdentities.CountAsync());
        Assert.Equal(1, await resolverDb.AuthSessions.CountAsync());
    }

    [DockerFact]
    public async Task Migrations_create_subject_graph_tables_and_allow_same_tenant_links()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await using var tenantDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "subject-tenant", "Subject Tenant"));
        tenantDb.UserAccounts.Add(new UserAccount(userId, tenantId, "subject@example.com"));
        tenantDb.Subjects.Add(new Subject(
            subjectId,
            tenantId,
            externalId: "emp-001",
            userAccountId: userId,
            email: "subject@example.com",
            displayName: "Subject One",
            attributes: """{"department":"Research","role":"Analyst"}"""));
        tenantDb.SubjectGroups.Add(new SubjectGroup(
            groupId,
            tenantId,
            SubjectGroupTypes.Department,
            "Research"));
        tenantDb.SubjectMemberships.Add(new SubjectMembership(
            subjectId,
            groupId,
            SubjectGroupRoles.Member,
            new DateOnly(2026, 5, 1),
            null));
        tenantDb.SubjectRelationships.Add(new SubjectRelationship(
            relationshipId,
            tenantId,
            subjectId,
            subjectId,
            SubjectRelationshipTypes.Self));

        await tenantDb.SaveChangesAsync();

        var savedSubject = await tenantDb.Subjects.SingleAsync(subject => subject.Id == subjectId);
        var savedMembership = await tenantDb.SubjectMemberships.SingleAsync();
        var savedRelationship = await tenantDb.SubjectRelationships.SingleAsync();

        Assert.Contains("Research", savedSubject.Attributes);
        Assert.Equal(groupId, savedMembership.GroupId);
        Assert.Equal(SubjectRelationshipTypes.Self, savedRelationship.RelationshipType);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_subject_reads()
    {
        var tenantA = SubjectGraph.Create("tenant-a", "Tenant A", "a@example.com");
        var tenantB = SubjectGraph.Create("tenant-b", "Tenant B", "b@example.com");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedSubjectGraphAsync(runtimeOptions, tenantA);
        await SeedSubjectGraphAsync(runtimeOptions, tenantB);

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        var visibleSubjects = await tenantADb.Subjects.ToListAsync();

        var visibleSubject = Assert.Single(visibleSubjects);
        Assert.Equal(tenantA.SubjectId, visibleSubject.Id);
    }

    [DockerFact]
    public async Task Rls_blocks_subject_relationship_to_related_subject_from_another_tenant()
    {
        var tenantA = SubjectGraph.Create("tenant-a", "Tenant A", "a@example.com");
        var tenantB = SubjectGraph.Create("tenant-b", "Tenant B", "b@example.com");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await SeedSubjectGraphAsync(runtimeOptions, tenantA);
        await SeedSubjectGraphAsync(runtimeOptions, tenantB);

        await using var tenantADb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.SubjectRelationships.Add(new SubjectRelationship(
            Guid.NewGuid(),
            tenantA.TenantId,
            tenantA.SubjectId,
            tenantB.SubjectId,
            SubjectRelationshipTypes.Peer));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_subject_group_parent_from_another_tenant()
    {
        var tenantA = SubjectGraph.Create("tenant-a", "Tenant A", "a@example.com");
        var tenantB = SubjectGraph.Create("tenant-b", "Tenant B", "b@example.com");
        var tenantBGroupId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA.TenantId, tenantA.TenantSlug, tenantA.TenantName));
            db.Tenants.Add(new Tenant(tenantB.TenantId, tenantB.TenantSlug, tenantB.TenantName));
            db.SubjectGroups.Add(new SubjectGroup(
                tenantBGroupId,
                tenantB.TenantId,
                SubjectGroupTypes.Department,
                "Tenant B Department"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA.TenantId);

        tenantADb.SubjectGroups.Add(new SubjectGroup(
            Guid.NewGuid(),
            tenantA.TenantId,
            SubjectGroupTypes.Team,
            "Cross Tenant Child",
            parentGroupId: tenantBGroupId));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Migrations_create_directory_import_tables_with_rls_and_allow_tenant_writes()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await AssertTenantScopedTableAsync(migratorOptions, "directory_connection");
        await AssertTenantScopedTableAsync(migratorOptions, "directory_import_rule");
        await AssertTenantScopedTableAsync(migratorOptions, "directory_import_run");
        await AssertTenantScopedTableAsync(migratorOptions, "directory_import_run_item");

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "directory-import", "Directory Import"));
        tenantDb.DirectoryConnections.Add(new DirectoryConnection(
            connectionId,
            tenantId,
            DirectoryConnectionProviders.MicrosoftGraph,
            "graph-tenant",
            "Graph Tenant",
            "graph.example",
            """{"scopes":["User.Read.All","GroupMember.Read.All"]}"""));
        tenantDb.DirectoryImportRules.Add(new DirectoryImportRule(
            ruleId,
            tenantId,
            connectionId,
            "Psychology students",
            """{"departments":["Psychology"],"userTypes":["Member"]}""",
            """{"userFields":["displayName","mail","department"]}"""));
        tenantDb.DirectoryImportRuns.Add(new DirectoryImportRun(
            runId,
            tenantId,
            ruleId,
            DirectoryImportRunModes.Preview));
        tenantDb.DirectoryImportRunItems.Add(new DirectoryImportRunItem(
            itemId,
            tenantId,
            runId,
            "user",
            "sha256:abc123",
            DirectoryImportRunItemActions.CreateSubject,
            DirectoryImportRunItemStatuses.Planned,
            safeSummaryJson: """{"safe":"count-only"}"""));

        await tenantDb.SaveChangesAsync();

        Assert.Equal(connectionId, (await tenantDb.DirectoryConnections.SingleAsync()).Id);
        Assert.Equal(ruleId, (await tenantDb.DirectoryImportRules.SingleAsync()).Id);
        Assert.Equal(runId, (await tenantDb.DirectoryImportRuns.SingleAsync()).Id);
        Assert.Equal(itemId, (await tenantDb.DirectoryImportRunItems.SingleAsync()).Id);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_directory_import_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantADbScope = new TenantDbScope(tenantADb);
            await using var tenantATransaction = await tenantADbScope.BeginTransactionAsync(tenantA);

            tenantADb.Tenants.Add(new Tenant(tenantA, "directory-import-a", "Directory Import A"));
            tenantADb.Tenants.Add(new Tenant(tenantB, "directory-import-b", "Directory Import B"));
            tenantADb.DirectoryConnections.Add(new DirectoryConnection(
                connectionId,
                tenantA,
                DirectoryConnectionProviders.MicrosoftGraph,
                "graph-tenant-a",
                "Graph Tenant A",
                "a.example",
                """{"scopes":["User.Read.All"]}"""));
            tenantADb.DirectoryImportRules.Add(new DirectoryImportRule(
                ruleId,
                tenantA,
                connectionId,
                "Tenant A rule",
                "{}",
                "{}"));
            tenantADb.DirectoryImportRuns.Add(new DirectoryImportRun(
                runId,
                tenantA,
                ruleId,
                DirectoryImportRunModes.Preview));
            tenantADb.DirectoryImportRunItems.Add(new DirectoryImportRunItem(
                Guid.NewGuid(),
                tenantA,
                runId,
                "user",
                "sha256:tenant-a",
                DirectoryImportRunItemActions.NoChange,
                DirectoryImportRunItemStatuses.Planned,
                safeSummaryJson: "{}"));

            await tenantADb.SaveChangesAsync();
            await tenantATransaction.CommitAsync();
        }

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        await using var tenantBTransaction = await tenantBDbScope.BeginTransactionAsync(tenantB);

        Assert.Empty(await tenantBDb.DirectoryConnections.ToListAsync());
        Assert.Empty(await tenantBDb.DirectoryImportRules.ToListAsync());
        Assert.Empty(await tenantBDb.DirectoryImportRuns.ToListAsync());
        Assert.Empty(await tenantBDb.DirectoryImportRunItems.ToListAsync());

        await tenantBTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Migrations_create_instrument_metadata_tables_and_allow_global_catalog_reads()
    {
        var tenantId = Guid.NewGuid();
        var instrument = CreateCanonicalInstrument();
        var subscale = new InstrumentSubscale(
            Guid.NewGuid(),
            instrument.Id,
            "EX",
            "Exhaustion",
            8,
            InstrumentScoringMethods.Mean,
            0.83m);
        var item = new InstrumentItem(
            Guid.NewGuid(),
            instrument.Id,
            1,
            "OLBI_01",
            "EX",
            reverseCoded: false);
        var norm = new InstrumentNorm(
            Guid.NewGuid(),
            instrument.Id,
            "EX",
            InstrumentNormTypes.PublishedInstrument,
            "general workforce",
            100,
            "en",
            mean: 2.1m,
            sd: 0.7m,
            percentiles: """{"p50":2.1}""",
            sourceCitation: "Demerouti et al. (2003)",
            sourceYear: 2003);
        var translation = InstrumentTranslation.ForInstrument(
            Guid.NewGuid(),
            instrument.Id,
            "full_name",
            "hr",
            "Oldenburski inventar sagorijevanja",
            TranslationStatuses.DraftTranslation);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Instruments.Add(instrument);
            db.InstrumentSubscales.Add(subscale);
            db.InstrumentItems.Add(item);
            db.InstrumentNorms.Add(norm);
            db.InstrumentTranslations.Add(translation);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var visibleInstrument = await tenantDb.Instruments.SingleAsync(entity => entity.Id == instrument.Id);
        var visibleSubscale = await tenantDb.InstrumentSubscales.SingleAsync(entity => entity.Id == subscale.Id);
        var visibleTranslation = await tenantDb.InstrumentTranslations.SingleAsync(entity => entity.Id == translation.Id);

        Assert.True(visibleInstrument.IsGlobal);
        Assert.Equal("EX", visibleSubscale.Code);
        Assert.Equal("hr", visibleTranslation.Locale);
    }

    [DockerFact]
    public async Task Rls_blocks_tenant_runtime_from_modifying_global_instrument_metadata()
    {
        var tenantId = Guid.NewGuid();
        var instrument = CreateCanonicalInstrument();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Instruments.Add(instrument);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.InstrumentSubscales.Add(new InstrumentSubscale(
            Guid.NewGuid(),
            instrument.Id,
            "DI",
            "Disengagement",
            8,
            InstrumentScoringMethods.Mean));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_allows_tenant_derivative_instrument_with_global_parent()
    {
        var tenantId = Guid.NewGuid();
        var parent = CreateCanonicalInstrument();
        var derivative = Instrument.CreateDerivative(
            Guid.NewGuid(),
            tenantId,
            parent.Id,
            "olbi-custom",
            "1.0.0",
            "Custom OLBI derivative",
            InstrumentDomains.Psychometric,
            "Derived from OLBI canonical");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Instruments.Add(parent);
            db.Tenants.Add(new Tenant(tenantId, "instrument-tenant", "Instrument Tenant"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Instruments.Add(derivative);
        await tenantDb.SaveChangesAsync();

        var savedDerivative = await tenantDb.Instruments.SingleAsync(entity => entity.Id == derivative.Id);

        Assert.Equal(parent.Id, savedDerivative.ParentInstrumentId);
        Assert.False(savedDerivative.IsGlobal);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_allows_tenant_private_import_without_global_parent()
    {
        var tenantId = Guid.NewGuid();
        var tenantImport = Instrument.CreateTenantImport(
            Guid.NewGuid(),
            tenantId,
            "tenant-private-import",
            "1.0.0",
            "Tenant Private Import",
            InstrumentDomains.Psychometric,
            "Tenant attested source",
            InstrumentRightsStatuses.AttestedByTenant,
            InstrumentValidityLabels.TenantProvided);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, "private-import-tenant", "Private Import Tenant"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Instruments.Add(tenantImport);
        await tenantDb.SaveChangesAsync();

        var saved = await tenantDb.Instruments.SingleAsync(entity => entity.Id == tenantImport.Id);

        Assert.False(saved.IsGlobal);
        Assert.Null(saved.ParentInstrumentId);
        Assert.Equal(InstrumentRightsScopes.TenantProvided, saved.RightsScope);
        Assert.Equal(InstrumentRightsStatuses.AttestedByTenant, saved.RightsStatus);
        Assert.Equal(InstrumentValidityLabels.TenantProvided, saved.ValidityLabel);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_creates_and_lists_tenant_private_instruments()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, "setup-tenant", "Setup Tenant"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));

        var created = await store.CreatePrivateInstrumentImportAsync(
            tenantId,
            new CreatePrivateInstrumentImportRequest(
                "custom-olbi",
                "1.0.0",
                "Custom OLBI",
                InstrumentDomains.Psychometric,
                "Tenant attested source",
                InstrumentRightsStatuses.AttestedByTenant,
                InstrumentValidityLabels.TenantProvided),
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.True(created.Value.CanStartNewCampaign);

        var instruments = await store.ListInstrumentsAsync(tenantId, CancellationToken.None);
        var saved = Assert.Single(instruments, instrument => instrument.Id == created.Value.Id);

        Assert.Equal("custom-olbi", saved.Code);
        Assert.Equal(InstrumentRightsStatuses.AttestedByTenant, saved.RightsStatus);
        Assert.Equal(InstrumentValidityLabels.TenantProvided, saved.ValidityLabel);
    }

    [DockerFact]
    public async Task Setup_store_returns_conflict_when_private_instrument_code_version_already_exists()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, "setup-duplicate", "Setup Duplicate"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));
        var request = new CreatePrivateInstrumentImportRequest(
            "custom-pulse",
            "1.0.0",
            "Custom Pulse",
            InstrumentDomains.Psychometric,
            "Tenant attested source",
            InstrumentRightsStatuses.AttestedByTenant,
            InstrumentValidityLabels.TenantProvided);

        var first = await store.CreatePrivateInstrumentImportAsync(
            tenantId,
            request,
            CancellationToken.None);
        var duplicate = await store.CreatePrivateInstrumentImportAsync(
            tenantId,
            request,
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(ErrorType.Conflict, duplicate.Error.Type);
        Assert.Equal("instrument.duplicate_code_version", duplicate.Error.Code);
    }

    [DockerFact]
    public async Task Setup_store_creates_template_graph_for_tenant_private_ui_setup()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, "template-setup", "Template Setup"));
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));

        var created = await store.CreateTemplateVersionAsync(
            tenantId,
            actorId: null,
            SampleSetupTemplateRequest(),
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Single(created.Value.Sections);
        Assert.Single(created.Value.Scales);
        Assert.Single(created.Value.Questions);

        var loaded = await store.GetTemplateVersionAsync(
            tenantId,
            created.Value.TemplateVersionId,
            CancellationToken.None);

        Assert.True(loaded.IsSuccess);
        Assert.Equal(created.Value.TemplateVersionId, loaded.Value.TemplateVersionId);
        Assert.Equal("Private burnout pulse", loaded.Value.TemplateName);
    }

    [DockerFact]
    public async Task Setup_store_creates_scoring_rule_for_tenant_template_version()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));

        var created = await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                GraphReverseCodedScoringDocument(),
                """{"scores":["total"]}"""),
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.NotEqual(Guid.Empty, created.Value.Id);
    }

    [DockerFact]
    public async Task Setup_store_rejects_invalid_scoring_rule_before_persistence()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var created = await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                GraphReverseCodedScoringDocument(),
                """{"scores":["other"]}"""),
            CancellationToken.None);

        Assert.True(created.IsFailure);
        Assert.Equal("score.rule_produces_mismatch", created.Error.Code);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await tenantDb.ScoringRules.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_creates_campaign_series_and_draft_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Private study"),
            CancellationToken.None);

        Assert.True(series.IsSuccess);

        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        Assert.True(campaign.IsSuccess);
        Assert.Equal(CampaignStatuses.Draft, campaign.Value.Status);
        Assert.Equal(ResponseIdentityModes.Anonymous, campaign.Value.ResponseIdentityMode);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var savedSeries = await tenantDb.CampaignSeries.SingleAsync(entity => entity.Id == series.Value.Id);
        Assert.Equal(CampaignSeriesStudyKinds.Own, savedSeries.StudyKind);
        Assert.False(savedSeries.IsSample);
        Assert.Null(savedSeries.SampleScenario);
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_sample_metadata_constraints_reject_invalid_combinations()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-05-16T09:30:00+00:00", CultureInfo.InvariantCulture);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, $"tenant-{tenantId:N}", "Seeded Tenant"));
            await db.SaveChangesAsync();
        }

        await using var verifier = new ApplicationDbContext(migratorOptions);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            verifier.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO campaign_series (
                    id,
                    tenant_id,
                    name,
                    code_salt,
                    created_at,
                    updated_at,
                    study_kind,
                    sample_scenario
                )
                VALUES ({0}, {1}, 'Invalid sample', decode(repeat('00', 32), 'hex'), {2}, {2}, 'sample', NULL)
                """,
                seriesId,
                tenantId,
                now));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_campaign_series_sample_consistency", exception.ConstraintName);
    }

    [DockerFact]
    public async Task Setup_store_creates_default_consent_document_for_campaign_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        _ = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);
        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Consent study"),
            CancellationToken.None);

        Assert.True(series.IsSuccess);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var document = await tenantDb.ConsentDocuments.SingleAsync(entity => entity.CampaignSeriesId == series.Value.Id);

        Assert.Equal(tenantId, document.TenantId);
        Assert.Equal("en", document.Locale);
        Assert.Equal("1.0.0", document.Version);
        Assert.Contains("data_processing", document.RequiredGrants);
        Assert.True(document.IsUsableAt(DateTimeOffset.UtcNow));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_creates_default_retention_and_disclosure_policies_for_campaign_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        _ = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);
        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Policy defaults study"),
            CancellationToken.None);

        Assert.True(series.IsSuccess);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var retention = await tenantDb.RetentionPolicies.SingleAsync(entity => entity.CampaignSeriesId == series.Value.Id);
        var disclosure = await tenantDb.DisclosurePolicies.SingleAsync(entity => entity.CampaignSeriesId == series.Value.Id);

        Assert.Equal(tenantId, retention.TenantId);
        Assert.Equal("1.0.0", retention.Version);
        Assert.Equal(1, retention.RetainForYears);
        Assert.Equal(RetentionPolicy.ResponseSubmittedAt, retention.RetentionStartEvent);
        Assert.Equal(RetentionPolicy.Anonymize, retention.ActionAfter);
        Assert.Contains("proof_default_not_legal_advice", retention.PublicationLimits);
        Assert.True(retention.IsUsableAt(DateTimeOffset.UtcNow));

        Assert.Equal(tenantId, disclosure.TenantId);
        Assert.Equal("1.0.0", disclosure.Version);
        Assert.Equal(5, disclosure.KMin);
        Assert.Equal(DisclosurePolicy.HideCell, disclosure.SuppressionStrategy);
        Assert.Contains("wave_comparison", disclosure.AppliesToDimensions);
        Assert.True(disclosure.IsUsableAt(DateTimeOffset.UtcNow));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_blocks_campaign_without_scoring_rule()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(versionId, "Wave 1", ResponseIdentityModes.Identified),
            CancellationToken.None);

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(readiness.IsSuccess);
        Assert.False(readiness.Value.Ready);
        Assert.Contains(readiness.Value.Issues, issue => issue.Code == "scoring_rule.missing");
    }

    [DockerFact]
    public async Task Setup_store_respondent_rules_replace_ordered_campaign_rules()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            store,
            tenantId,
            "Saved respondent rules study");
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Saved respondent rules wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);

        var saved = await store.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}"""),
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"duplicate_self"}""")
            ]),
            CancellationToken.None);
        var replaced = await store.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var listed = await store.ListCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(replaced.IsSuccess, replaced.Error.ToString());
        Assert.True(listed.IsSuccess, listed.Error.ToString());
        var rule = Assert.Single(listed.Value.Rules);
        Assert.Equal(1, rule.Ordinal);
        Assert.Equal("self", rule.RuleKind);
        Assert.Equal("self", rule.Role);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await tenantDb.RespondentRules.CountAsync(entity => entity.CampaignId == campaign.Value.Id));
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_allows_anonymous_saved_rules_and_launch_queues_invitations()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var scoringRule = await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(new Subject(
                Guid.NewGuid(),
                tenantId,
                email: "respondent@example.com",
                displayName: "Respondent"));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var seriesId = await CreateSetupCampaignSeriesAsync(
            store,
            tenantId,
            "Anonymous saved rules study");
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Anonymous saved rules wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var saved = await store.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self"}""")
            ]),
            CancellationToken.None);

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(readiness.IsSuccess, readiness.Error.ToString());
        Assert.True(readiness.Value.Ready, string.Join(", ", readiness.Value.Issues.Select(issue => issue.Code)));

        var launch = await store.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(launch.IsSuccess, launch.Error.ToString());
        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await tenantDb.Assignments.CountAsync(assignment =>
            assignment.CampaignId == campaign.Value.Id &&
            assignment.Anonymous &&
            assignment.RespondentSubjectId == null &&
            assignment.InviteTokenId != null));
        Assert.Equal(1, await tenantDb.InvitationTokens.CountAsync(token =>
            token.CampaignId == campaign.Value.Id &&
            token.Channel == InvitationTokenChannels.Email));
        Assert.Equal(1, await tenantDb.Notifications.CountAsync(notification =>
            notification.CampaignId == campaign.Value.Id &&
            notification.TemplateCode == Notification.InvitationTemplateCode &&
            notification.Status == NotificationStatuses.Queued));
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_blocks_campaign_without_consent_document()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);
        await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Consent missing study"),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        await using (var deleteTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.ConsentDocuments.RemoveRange(tenantDb.ConsentDocuments);
            await tenantDb.SaveChangesAsync();
            await deleteTransaction.CommitAsync();
        }

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(readiness.IsSuccess);
        Assert.False(readiness.Value.Ready);
        Assert.Contains(readiness.Value.Issues, issue => issue.Code == "consent_document.missing");
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_blocks_campaign_without_retention_policy()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);
        await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Retention missing study"),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        await using (var deleteTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.RetentionPolicies.RemoveRange(tenantDb.RetentionPolicies);
            await tenantDb.SaveChangesAsync();
            await deleteTransaction.CommitAsync();
        }

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(readiness.IsSuccess);
        Assert.False(readiness.Value.Ready);
        Assert.Contains(readiness.Value.Issues, issue => issue.Code == "retention_policy.missing");
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_blocks_campaign_without_disclosure_policy()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var store = new SetupWorkflowStore(tenantDb, tenantDbScope);
        await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Disclosure missing study"),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        await using (var deleteTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.DisclosurePolicies.RemoveRange(tenantDb.DisclosurePolicies);
            await tenantDb.SaveChangesAsync();
            await deleteTransaction.CommitAsync();
        }

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(readiness.IsSuccess);
        Assert.False(readiness.Value.Ready);
        Assert.Contains(readiness.Value.Issues, issue => issue.Code == "disclosure_policy.missing");
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_blocks_scoring_rule_with_missing_template_item_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));
        var createdRule = await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01","q99"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Missing scoring item study"),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(createdRule.IsSuccess, createdRule.Error.ToString());
        Assert.True(series.IsSuccess, series.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(readiness.IsSuccess);
        Assert.False(readiness.Value.Ready);
        Assert.Contains(readiness.Value.Issues, issue => issue.Code == "scoring_rule.item_code_missing");
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_accepts_valid_scoring_rule_preview()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));
        var createdRule = await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var series = await store.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Valid scoring preview study"),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(createdRule.IsSuccess, createdRule.Error.ToString());
        Assert.True(series.IsSuccess, series.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(readiness.IsSuccess);
        Assert.True(readiness.Value.Ready, string.Join(", ", readiness.Value.Issues.Select(issue => issue.Code)));
        Assert.DoesNotContain(readiness.Value.Issues, issue => issue.Code.StartsWith("scoring_rule.", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task Setup_store_launch_readiness_no_longer_warns_for_participant_code_persistence()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new SetupWorkflowStore(tenantDb, new TenantDbScope(tenantDb));
        await store.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total"}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var campaign = await store.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(versionId, "Wave 1", ResponseIdentityModes.AnonymousLongitudinal),
            CancellationToken.None);

        var readiness = await store.GetLaunchReadinessAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(readiness.IsSuccess);
        Assert.DoesNotContain(
            readiness.Value.Issues,
            issue => issue.Code == "identity.participant_codes_not_implemented");
    }

    [DockerFact]
    public async Task Migrations_create_response_tables_and_allow_answer_capture()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantId,
            "response-capture");

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var session = new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            fixture.AssignmentId,
            "en");
        var answer = new Answer(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            fixture.QuestionId,
            "4");

        tenantDb.ResponseSessions.Add(session);
        tenantDb.Answers.Add(answer);
        await tenantDb.SaveChangesAsync();

        var savedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Id);
        var savedAnswer = await tenantDb.Answers.SingleAsync(entity => entity.SessionId == session.Id);

        Assert.Equal(fixture.AssignmentId, savedSession.AssignmentId);
        Assert.Equal("4", savedAnswer.Value);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_response_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantA,
            "response-tenant-a",
            includeSubmittedResponse: true);
        await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantB,
            "response-tenant-b",
            includeSubmittedResponse: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleSessions = await tenantADb.ResponseSessions.ToListAsync();
        var visibleAnswers = await tenantADb.Answers.ToListAsync();

        var visibleSession = Assert.Single(visibleSessions);
        var visibleAnswer = Assert.Single(visibleAnswers);
        Assert.Equal(tenantA, visibleSession.TenantId);
        Assert.Equal(tenantA, visibleAnswer.TenantId);
    }

    [DockerFact]
    public async Task Response_capture_store_collects_and_submits_answers_for_setup_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(versionId, "Response lab wave", ResponseIdentityModes.Anonymous),
            CancellationToken.None);

        Assert.True(campaign.IsSuccess);

        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var assignment = await responseStore.CreateLabAssignmentAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var session = await responseStore.CreateSessionAsync(
            tenantId,
            new CreateResponseSessionRequest(assignment.Value.AssignmentId, "en"),
            CancellationToken.None);
        var saved = await responseStore.SaveAnswersAsync(
            tenantId,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(respondentCampaign.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitSessionAsync(
            tenantId,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(respondentCampaign.IsSuccess);
        Assert.True(assignment.IsSuccess);
        Assert.True(session.IsSuccess);
        Assert.True(saved.IsSuccess);
        Assert.True(submitted.IsSuccess);
        Assert.Equal(1, saved.Value.SavedAnswerCount);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Value.Id);
        Assert.NotNull(storedSession.SubmittedAt);
        Assert.Equal(2400, storedSession.TimeTakenMs);
        Assert.Equal(1, await tenantDb.Answers.CountAsync(answer => answer.SessionId == session.Value.Id));

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Migrations_create_score_tables_and_allow_score_insert()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantId,
            "score-insert",
            includeSubmittedResponse: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var run = new ScoreRun(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            fixture.ResponseSessionId!.Value,
            fixture.ScoringRuleId,
            ScoreRunStatuses.Success);
        var score = new Score(
            Guid.NewGuid(),
            tenantId,
            run.Id,
            fixture.CampaignId,
            fixture.ResponseSessionId.Value,
            "total",
            4.25m,
            1);

        tenantDb.ScoreRuns.Add(run);
        tenantDb.Scores.Add(score);
        await tenantDb.SaveChangesAsync();

        var savedRun = await tenantDb.ScoreRuns.SingleAsync(entity => entity.Id == run.Id);
        var savedScore = await tenantDb.Scores.SingleAsync(entity => entity.ScoreRunId == run.Id);

        Assert.Equal(ScoreRunStatuses.Success, savedRun.Status);
        Assert.Equal(4.25m, savedScore.Value);
        Assert.Equal(1, savedScore.NValid);
        Assert.Equal(1, savedScore.NExpected);
        Assert.Equal("ok", savedScore.MissingPolicyStatus);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_score_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        var fixtureA = await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantA,
            "score-tenant-a",
            includeSubmittedResponse: true);
        var fixtureB = await SeedResponseAssignmentFixtureAsync(
            migratorOptions,
            tenantB,
            "score-tenant-b",
            includeSubmittedResponse: true);

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            db.ScoreRuns.Add(new ScoreRun(
                Guid.NewGuid(),
                tenantA,
                fixtureA.CampaignId,
                fixtureA.ResponseSessionId!.Value,
                fixtureA.ScoringRuleId,
                ScoreRunStatuses.Success));
            db.ScoreRuns.Add(new ScoreRun(
                Guid.NewGuid(),
                tenantB,
                fixtureB.CampaignId,
                fixtureB.ResponseSessionId!.Value,
                fixtureB.ScoringRuleId,
                ScoreRunStatuses.Success));
            await db.SaveChangesAsync();

            foreach (var run in await db.ScoreRuns.ToListAsync())
            {
                db.Scores.Add(new Score(
                    Guid.NewGuid(),
                    run.TenantId,
                    run.Id,
                    run.CampaignId,
                    run.ResponseSessionId,
                    "total",
                    4m,
                    1));
            }

            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleRuns = await tenantADb.ScoreRuns.ToListAsync();
        var visibleScores = await tenantADb.Scores.ToListAsync();

        var visibleRun = Assert.Single(visibleRuns);
        var visibleScore = Assert.Single(visibleScores);
        Assert.Equal(tenantA, visibleRun.TenantId);
        Assert.Equal(tenantA, visibleScore.TenantId);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Response_score_store_computes_scores_for_submitted_setup_session()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(versionId, "Score lab wave", ResponseIdentityModes.Anonymous),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);

        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var assignment = await responseStore.CreateLabAssignmentAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var session = await responseStore.CreateSessionAsync(
            tenantId,
            new CreateResponseSessionRequest(assignment.Value.AssignmentId, "en"),
            CancellationToken.None);
        var saved = await responseStore.SaveAnswersAsync(
            tenantId,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(respondentCampaign.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitSessionAsync(
            tenantId,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.True(submitted.IsSuccess);

        await using (var autoScoreTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            Assert.Equal(1, await tenantDb.ScoreRuns.CountAsync(run => run.ResponseSessionId == session.Value.Id));
            Assert.Equal(1, await tenantDb.Scores.CountAsync(entity => entity.ResponseSessionId == session.Value.Id));
            await autoScoreTransaction.CommitAsync();
        }

        var scored = await scoreStore.ComputeResponseScoresAsync(
            tenantId,
            session.Value.Id,
            CancellationToken.None);

        Assert.True(scored.IsSuccess);
        Assert.Equal(session.Value.Id, scored.Value.SessionId);
        var score = Assert.Single(scored.Value.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4m, score.Value);
        Assert.Equal(1, score.NValid);
        Assert.Equal(1, score.NExpected);
        Assert.Equal("ok", score.MissingPolicyStatus);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await tenantDb.ScoreRuns.CountAsync(run => run.ResponseSessionId == session.Value.Id));
        Assert.Equal(1, await tenantDb.Scores.CountAsync(entity => entity.ResponseSessionId == session.Value.Id));
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Response_score_store_computes_reverse_coded_graph_scores_for_submitted_setup_session()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionWithThreeQuestionsAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                GraphReverseCodedScoringDocument(),
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(versionId, "Graph score lab wave", ResponseIdentityModes.Anonymous),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);

        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(respondentCampaign.IsSuccess);
        var questionByCode = respondentCampaign.Value.Questions.ToDictionary(
            question => question.Code,
            StringComparer.OrdinalIgnoreCase);
        var assignment = await responseStore.CreateLabAssignmentAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var session = await responseStore.CreateSessionAsync(
            tenantId,
            new CreateResponseSessionRequest(assignment.Value.AssignmentId, "en"),
            CancellationToken.None);
        var saved = await responseStore.SaveAnswersAsync(
            tenantId,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(questionByCode["q01"].Id, "2"),
                new SaveAnswerRequest(questionByCode["q02"].Id, "3"),
                new SaveAnswerRequest(questionByCode["q03"].Id, "5")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitSessionAsync(
            tenantId,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        var scored = await scoreStore.ComputeResponseScoresAsync(
            tenantId,
            session.Value.Id,
            CancellationToken.None);

        Assert.True(assignment.IsSuccess);
        Assert.True(session.IsSuccess);
        Assert.True(saved.IsSuccess);
        Assert.True(submitted.IsSuccess);
        Assert.True(scored.IsSuccess, scored.Error.ToString());
        var score = Assert.Single(scored.Value.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(2.0000m, score.Value);
        Assert.Equal(3, score.NValid);
        Assert.Equal(3, score.NExpected);
        Assert.Equal("ok", score.MissingPolicyStatus);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await tenantDb.Scores.SingleAsync(entity => entity.ResponseSessionId == session.Value.Id);
        Assert.Equal("total", persisted.DimensionCode);
        Assert.Equal(2.0000m, persisted.Value);
        Assert.Equal(3, persisted.NValid);
        Assert.Equal(3, persisted.NExpected);
        Assert.Equal("ok", persisted.MissingPolicyStatus);
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Report_proof_store_suppresses_anonymous_aggregates_below_disclosure_k_min()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 1);

        Assert.Equal("proof_only", scenario.Report.ProofStatus);
        Assert.Equal("not_validated_interpretation", scenario.Report.InterpretationStatus);
        Assert.Equal(ResponseIdentityModes.Anonymous, scenario.Report.LaunchSnapshot.ResponseIdentityMode);
        Assert.Equal(5, scenario.Report.DisclosurePolicy.KMin);
        var score = Assert.Single(scenario.Report.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal("suppressed", score.Disclosure);
        Assert.Equal(1, score.SubmittedResponseCount);
        Assert.Null(score.ScoreCount);
        Assert.Null(score.NValidTotal);
        Assert.Null(score.NExpectedTotal);
        Assert.Null(score.MissingPolicyStatusSummary);
        Assert.Null(score.Mean);
        Assert.Null(score.Min);
        Assert.Null(score.Max);
        Assert.Equal("insufficient_responses", score.SuppressionReason);
    }

    [DockerFact]
    public async Task Report_proof_store_returns_anonymous_aggregates_at_disclosure_k_min()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);

        Assert.Null(scenario.Report.ClosedAt);
        Assert.Equal("preliminary_live", scenario.Report.DataFinality);
        var score = Assert.Single(scenario.Report.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal("visible", score.Disclosure);
        Assert.Equal(5, score.SubmittedResponseCount);
        Assert.Equal(5, score.ScoreCount);
        Assert.Equal(5, score.NValidTotal);
        Assert.Equal(5, score.NExpectedTotal);
        Assert.Equal("ok", score.MissingPolicyStatusSummary);
        Assert.NotNull(score.Mean);
        Assert.NotNull(score.Min);
        Assert.NotNull(score.Max);
        Assert.Null(score.SuppressionReason);
    }

    [DockerFact]
    public async Task Report_proof_store_suppresses_score_output_below_k_min_even_when_campaign_has_enough_submissions()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);

        await using (var db = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(db);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            var scores = await db.Scores
                .Where(entity => entity.CampaignId == scenario.Report.CampaignId)
                .OrderBy(entity => entity.Id)
                .ToListAsync();
            Assert.Equal(5, scores.Count);
            db.Scores.RemoveRange(scores.Skip(1));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var reportStore = new ReportProofStore(tenantDb, new TenantDbScope(tenantDb));

        var report = await reportStore.GetCampaignReportProofAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(report.IsSuccess, report.Error.ToString());
        var score = Assert.Single(report.Value.Scores);
        Assert.Equal("suppressed", score.Disclosure);
        Assert.Equal(5, score.SubmittedResponseCount);
        Assert.Null(score.ScoreCount);
        Assert.Null(score.Mean);
        Assert.Equal("insufficient_responses", score.SuppressionReason);
    }

    [DockerFact]
    public async Task Report_proof_store_returns_closed_wave_finality_metadata()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T15:30:00+00:00");
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);

        await CloseReportProofCampaignAsync(
            tenantId,
            scenario.Report.CampaignId,
            actorUserId,
            closedAt,
            "Report proof complete");

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var reportStore = new ReportProofStore(tenantDb, tenantDbScope);

        var report = await reportStore.GetCampaignReportProofAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(report.IsSuccess, report.Error.ToString());
        Assert.Equal(CampaignStatuses.Closed, report.Value.CampaignStatus);
        Assert.Equal(closedAt, report.Value.ClosedAt);
        Assert.Equal("closed_wave", report.Value.DataFinality);
    }

    [DockerFact]
    public async Task Report_proof_store_returns_tenant_attested_interpretation_for_visible_scores()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(
            tenantId,
            submittedResponseCount: 5,
            produces: TenantAttestedScoreInterpretationProduces);

        var score = Assert.Single(scenario.Report.Scores);
        Assert.Equal("visible", score.Disclosure);
        Assert.Equal(3.0000m, score.Mean);
        Assert.NotNull(score.Interpretation);
        var interpretation = score.Interpretation;
        Assert.Equal("tenant_attested", interpretation.Status);
        Assert.Equal("tenant_defined", interpretation.Source);
        Assert.Equal("middle", interpretation.BandCode);
        Assert.Equal("Tenant middle range", interpretation.Label);
        Assert.False(interpretation.IsValidated);
        Assert.False(interpretation.IsOfficial);
        Assert.Contains("not validated", interpretation.Provenance, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not official", interpretation.Provenance, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Report_proof_store_returns_score_output_method_metadata_for_visible_scores()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(
            tenantId,
            submittedResponseCount: 5,
            produces:
            """
            {
              "scores": ["total"],
              "outputs": [
                {
                  "code": "total",
                  "label": "Total recovery score",
                  "calculation": "normalized_weighted_mean_0_100",
                  "calculation_label": "Normalized 0-100 weighted average",
                  "score_range": { "min": 0, "max": 100 }
                }
              ]
            }
            """);

        var score = Assert.Single(scenario.Report.Scores);
        Assert.Equal("visible", score.Disclosure);
        Assert.Equal("Total recovery score", score.DisplayLabel);
        Assert.Equal("normalized_weighted_mean_0_100", score.Calculation);
        Assert.Equal("Normalized 0-100 weighted average", score.CalculationLabel);
        Assert.Equal(0m, score.ScoreRangeMin);
        Assert.Equal(100m, score.ScoreRangeMax);
    }

    [DockerFact]
    public async Task Report_proof_store_does_not_expose_interpretation_for_suppressed_scores()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(
            tenantId,
            submittedResponseCount: 1,
            produces: TenantAttestedScoreInterpretationProduces);

        var score = Assert.Single(scenario.Report.Scores);
        Assert.Equal("suppressed", score.Disclosure);
        Assert.Null(score.Mean);
        Assert.Null(score.Interpretation);
    }

    [DockerFact]
    public async Task Report_proof_store_includes_frozen_launch_policy_and_scoring_provenance()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);

        Assert.Equal(scenario.Launch.LaunchSnapshotId, scenario.Report.LaunchSnapshot.Id);
        Assert.Equal(scenario.Launch.TemplateVersionId, scenario.Report.LaunchSnapshot.TemplateVersionId);
        Assert.Equal(scenario.Launch.ScoringRuleId, scenario.Report.LaunchSnapshot.ScoringRuleId);
        Assert.Equal(scenario.Launch.RetentionPolicyId, scenario.Report.LaunchSnapshot.RetentionPolicyId);
        Assert.Equal(scenario.Launch.DisclosurePolicyId, scenario.Report.LaunchSnapshot.DisclosurePolicyId);
        Assert.NotEqual(Guid.Empty, scenario.Report.LaunchSnapshot.ConsentDocumentId);
        Assert.Equal("1.0.0", scenario.Report.DisclosurePolicy.Version);
        Assert.Equal("hide_cell", scenario.Report.DisclosurePolicy.SuppressionStrategy);

        var launchSnapshotJson = System.Text.Json.JsonSerializer.Serialize(
            scenario.Report.LaunchSnapshot,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Contains("\"launchPacket\"", launchSnapshotJson);
        Assert.Contains("\"schemaVersion\":1", launchSnapshotJson);
        Assert.Contains("scoring", launchSnapshotJson);
        Assert.Contains("policies", launchSnapshotJson);
    }

    [DockerFact]
    public async Task Report_proof_store_blocks_cross_tenant_campaign_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        var reportStore = new ReportProofStore(tenantBDb, tenantBDbScope);

        var report = await reportStore.GetCampaignReportProofAsync(
            tenantB,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(report.IsFailure);
        Assert.Equal("campaign.not_found", report.Error.Code);
    }

    [DockerFact]
    public async Task Report_proof_store_requires_launched_campaign_snapshot()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var reportStore = new ReportProofStore(tenantDb, tenantDbScope);
        var series = await setupStore.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Draft report proof series"),
            CancellationToken.None);
        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Draft report proof wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        var report = await reportStore.GetCampaignReportProofAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(series.IsSuccess);
        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(report.IsFailure);
        Assert.Equal("report.launch_snapshot_missing", report.Error.Code);
    }

    [DockerFact]
    public async Task Report_proof_export_store_persists_suppressed_csv_and_codebook()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 1);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var artifact = await exportStore.CreateCampaignReportProofExportAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(artifact.IsSuccess, artifact.Error.ToString());
        Assert.Equal("report_proof_csv_codebook", artifact.Value.ArtifactType);
        Assert.Equal("succeeded", artifact.Value.Status);
        Assert.Equal("csv_codebook", artifact.Value.Format);
        Assert.Equal(2, artifact.Value.RowCount);
        Assert.Contains("insufficient_responses", artifact.Value.CsvContent);
        Assert.Contains("launch_packet_schema_version", artifact.Value.CsvContent);
        Assert.Contains("launch_packet_sections", artifact.Value.CsvContent);
        Assert.Contains("launch_packet_source", artifact.Value.CsvContent);
        Assert.Contains("launchPacket", artifact.Value.CodebookJson);
        Assert.Contains("schemaVersion", artifact.Value.CodebookJson);
        Assert.Contains("scoring", artifact.Value.CodebookJson);
        Assert.DoesNotContain("recipient", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("score_count", artifact.Value.CsvContent);
        Assert.Contains("n_valid_total", artifact.Value.CsvContent);
        Assert.Contains("n_expected_total", artifact.Value.CsvContent);
        Assert.Contains("missing_policy_status_summary", artifact.Value.CsvContent);
        Assert.Contains("mean", artifact.Value.CsvContent);
        Assert.Contains("min", artifact.Value.CsvContent);
        Assert.Contains("max", artifact.Value.CsvContent);
        Assert.Contains("total", artifact.Value.CsvContent);
        Assert.Contains("suppressed", artifact.Value.CsvContent);
        Assert.Contains("same_suppression_as_report_proof", artifact.Value.CodebookJson);
        Assert.Contains("score_output_metadata", artifact.Value.CodebookJson);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await tenantDb.ExportArtifacts.SingleAsync(entity => entity.Id == artifact.Value.Id);
        Assert.Equal(scenario.Report.CampaignId, persisted.CampaignId);
        Assert.Equal(artifact.Value.ChecksumSha256, persisted.ChecksumSha256);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Results_matrix_export_store_rejects_empty_matrix_exports()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 0);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var artifact = await exportStore.CreateCampaignSeriesResultsMatrixExportAsync(
            tenantId,
            scenario.Report.CampaignSeriesId!.Value,
            CancellationToken.None);

        Assert.True(artifact.IsFailure);
        Assert.Equal("results_matrix.not_available", artifact.Error.Code);
    }

    [DockerFact]
    public async Task Report_proof_export_store_preserves_tenant_attested_interpretation_provenance()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(
            tenantId,
            submittedResponseCount: 5,
            produces: TenantAttestedScoreInterpretationProduces);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var artifact = await exportStore.CreateCampaignReportProofExportAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(artifact.IsSuccess, artifact.Error.ToString());
        Assert.Contains("interpretation_band_code", artifact.Value.CsvContent);
        Assert.Contains("tenant_defined", artifact.Value.CsvContent);
        Assert.Contains("Tenant middle range", artifact.Value.CsvContent);
        Assert.Contains("not official", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant_attested_score_interpretation", artifact.Value.CodebookJson);
        Assert.Contains("only_when_score_visible", artifact.Value.CodebookJson);
        Assert.Contains("not validated", artifact.Value.CodebookJson, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Report_proof_export_store_includes_closed_wave_finality_provenance()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T16:00:00+00:00");
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        await CloseReportProofCampaignAsync(
            tenantId,
            scenario.Report.CampaignId,
            actorUserId,
            closedAt,
            "Export proof complete");

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var artifact = await exportStore.CreateCampaignReportProofExportAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(artifact.IsSuccess, artifact.Error.ToString());
        Assert.Contains("campaign_status,campaign_closed_at,campaign_data_finality", artifact.Value.CsvContent);
        Assert.Contains(CampaignStatuses.Closed, artifact.Value.CsvContent);
        Assert.Contains(closedAt.ToString("O", CultureInfo.InvariantCulture), artifact.Value.CsvContent);
        Assert.Contains("closed_wave", artifact.Value.CsvContent);
        Assert.Contains("campaign_data_finality", artifact.Value.CodebookJson);
        Assert.Contains("closed_wave", artifact.Value.CodebookJson);
    }

    [DockerFact]
    public async Task Export_artifact_store_retrieves_stored_csv_codebook_and_download()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var created = await exportStore.CreateCampaignReportProofExportAsync(
            tenantId,
            scenario.Report.CampaignId,
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.True(download.IsSuccess, download.Error.ToString());
        Assert.Equal(created.Value.Id, retrieved.Value.Id);
        Assert.Equal(created.Value.CampaignId, retrieved.Value.CampaignId);
        Assert.Equal(created.Value.CsvContent, retrieved.Value.CsvContent);
        Assert.Equal(created.Value.ChecksumSha256, retrieved.Value.ChecksumSha256);
        Assert.Equal(created.Value.ByteSize, retrieved.Value.ByteSize);
        await using (var verifyDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
            var stored = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == created.Value.Id);
            Assert.Equal(ExportArtifactStorageKinds.InlineText, stored.StorageKind);
            Assert.Null(stored.StorageKey);
            await verifyTransaction.CommitAsync();
        }

        Assert.Equal(created.Value.FileName, download.Value.FileName);
        Assert.Equal(created.Value.ContentType, download.Value.ContentType);
        Assert.Equal(created.Value.ByteSize, download.Value.ByteSize);
        Assert.Equal(created.Value.ChecksumSha256, download.Value.ChecksumSha256);
        Assert.Equal(created.Value.CsvContent, download.Value.Content);
        Assert.DoesNotContain("recipient", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        using var codebook = JsonDocument.Parse(retrieved.Value.CodebookJson);
        Assert.Equal(
            "report_proof_csv_codebook",
            codebook.RootElement.GetProperty("artifactType").GetString());
        Assert.Equal(
            "overall rows follow report proof disclosure; group rows are suppressed below disclosure minimum",
            codebook.RootElement.GetProperty("suppressionBasis").GetString());
    }

    [DockerFact]
    public async Task Campaign_series_report_html_artifact_store_persists_safe_html_metadata_and_download()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var created = await exportStore.CreateCampaignSeriesReportHtmlArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());
        Assert.Equal("campaign_series_report_html", created.Value.ArtifactType);
        Assert.Equal("succeeded", created.Value.Status);
        Assert.Equal("html", created.Value.Format);
        Assert.Equal("campaign_series", created.Value.TargetKind);
        Assert.Equal(campaignSeriesId, created.Value.TargetId);
        Assert.Null(created.Value.CampaignId);
        Assert.Equal(campaignSeriesId, created.Value.CampaignSeriesId);
        Assert.Equal("text/html; charset=utf-8", created.Value.ContentType);
        Assert.EndsWith(".html", created.Value.FileName, StringComparison.Ordinal);
        Assert.Equal("", created.Value.CsvContent);
        Assert.True(created.Value.CanDownload);

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.True(download.IsSuccess, download.Error.ToString());
        Assert.Equal("", retrieved.Value.CsvContent);
        await using (var verifyDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
            var stored = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == created.Value.Id);
            Assert.Equal(ExportArtifactStorageKinds.InlineText, stored.StorageKind);
            Assert.Null(stored.StorageKey);
            await verifyTransaction.CommitAsync();
        }

        Assert.Equal(created.Value.FileName, download.Value.FileName);
        Assert.Equal("text/html; charset=utf-8", download.Value.ContentType);
        Assert.Equal(created.Value.ChecksumSha256, download.Value.ChecksumSha256);
        Assert.Contains("<!doctype html>", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Report proof series", download.Value.Content, StringComparison.Ordinal);
        Assert.Contains("generated-at", download.Value.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("token", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wdr_", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message", download.Value.Content, StringComparison.OrdinalIgnoreCase);
        using var codebook = JsonDocument.Parse(retrieved.Value.CodebookJson);
        Assert.Equal(
            "campaign_series_report_html",
            codebook.RootElement.GetProperty("artifactType").GetString());
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_artifact_store_persists_external_object_and_download()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0x46, 0x41, 0x4B, 0x45];

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));

        var created = await exportStore.CreateCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());
        Assert.Equal("campaign_series_report_pdf", created.Value.ArtifactType);
        Assert.Equal("succeeded", created.Value.Status);
        Assert.Equal("pdf", created.Value.Format);
        Assert.Equal("campaign_series", created.Value.TargetKind);
        Assert.Equal(campaignSeriesId, created.Value.TargetId);
        Assert.Null(created.Value.CampaignId);
        Assert.Equal(campaignSeriesId, created.Value.CampaignSeriesId);
        Assert.Equal("application/pdf", created.Value.ContentType);
        Assert.EndsWith(".pdf", created.Value.FileName, StringComparison.Ordinal);
        Assert.Equal("", created.Value.CsvContent);
        Assert.NotNull(created.Value.StartedAt);
        Assert.True(created.Value.CanDownload);

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            created.Value.Id,
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.True(download.IsSuccess, download.Error.ToString());
        Assert.Equal("", retrieved.Value.CsvContent);
        Assert.Equal(created.Value.FileName, download.Value.FileName);
        Assert.Equal("application/pdf", download.Value.ContentType);
        Assert.Equal(created.Value.ChecksumSha256, download.Value.ChecksumSha256);
        Assert.Equal(pdfBytes, download.Value.ContentBytes);
        Assert.Empty(download.Value.Content);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var stored = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == created.Value.Id);
        Assert.Equal(ExportArtifactStorageKinds.ExternalObject, stored.StorageKind);
        Assert.NotNull(stored.StorageKey);
        Assert.StartsWith("export-artifacts/", stored.StorageKey, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", stored.StorageKey, StringComparison.Ordinal);
        Assert.DoesNotContain(tenantId.ToString("N"), stored.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(campaignSeriesId.ToString("N"), stored.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenants/", stored.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("campaign-series/", stored.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(stored.StartedAt);
        Assert.Null(stored.Content);
        using var metadata = JsonDocument.Parse(stored.MetadataJson);
        Assert.Equal(
            "campaign_series_report_pdf",
            metadata.RootElement.GetProperty("artifactType").GetString());
        Assert.Equal(
            "fake-pdf",
            metadata.RootElement.GetProperty("renderer").GetProperty("name").GetString());
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_artifact_returns_safe_conflicts_when_dependencies_missing()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));

        await using var missingObjectStoreDb = new ApplicationDbContext(CreateRuntimeOptions());
        var missingObjectStoreScope = new TenantDbScope(missingObjectStoreDb);
        var missingObjectStore = new ReportProofExportStore(
            missingObjectStoreDb,
            missingObjectStoreScope,
            new ReportProofStore(missingObjectStoreDb, missingObjectStoreScope),
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));
        var missingObjectStoreResult = await missingObjectStore.CreateCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        await using var missingRendererDb = new ApplicationDbContext(CreateRuntimeOptions());
        var missingRendererScope = new TenantDbScope(missingRendererDb);
        var missingRenderer = new ReportProofExportStore(
            missingRendererDb,
            missingRendererScope,
            new ReportProofStore(missingRendererDb, missingRendererScope),
            objectStore: objectStore);
        var missingRendererResult = await missingRenderer.CreateCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(missingObjectStoreResult.IsFailure);
        Assert.Equal("export_artifact.object_store_unavailable", missingObjectStoreResult.Error.Code);
        Assert.True(missingRendererResult.IsFailure);
        Assert.Equal("report_pdf.renderer_unavailable", missingRendererResult.Error.Code);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var failedArtifacts = await verifyDb.ExportArtifacts
            .Where(artifact =>
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                artifact.Status == ExportArtifactStatuses.Failed)
            .OrderBy(artifact => artifact.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, failedArtifacts.Count);
        Assert.Contains(
            failedArtifacts,
            artifact => artifact.FailureReasonCode == "export_artifact.object_store_unavailable");
        Assert.Contains(
            failedArtifacts,
            artifact => artifact.FailureReasonCode == "report_pdf.renderer_unavailable");
        Assert.All(
            failedArtifacts,
            artifact => AssertFailedPdfAttemptShape(artifact, tenantId, campaignSeriesId));

        var failedDownload = await missingRenderer.GetExportArtifactDownloadAsync(
            tenantId,
            failedArtifacts[0].Id,
            CancellationToken.None);

        Assert.True(failedDownload.IsFailure);
        Assert.Equal("export_artifact.not_downloadable", failedDownload.Error.Code);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_artifact_persists_safe_failed_attempt_when_renderer_fails()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        var rendererError = Error.Conflict(
            "report_pdf.render_failed",
            "Renderer failed with wdr_sensitive_token and <!doctype html> details.");

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FailingReportPdfRenderer(rendererError));

        var result = await exportStore.CreateCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("report_pdf.render_failed", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var failedArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact =>
            artifact.CampaignSeriesId == campaignSeriesId &&
            artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
            artifact.Status == ExportArtifactStatuses.Failed);

        AssertFailedPdfAttemptShape(failedArtifact, tenantId, campaignSeriesId);
        Assert.Equal("report_pdf.render_failed", failedArtifact.FailureReasonCode);
        Assert.DoesNotContain("wdr_", failedArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<!doctype", failedArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive", failedArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("{}", failedArtifact.CodebookJson);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_queued_worker_processes_success_and_rejects_terminal_reprocessing()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));

        var queued = await exportStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(queued.IsSuccess, queued.Error.ToString());
        Assert.Equal(ExportArtifactStatuses.Queued, queued.Value.Status);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportPdf, queued.Value.ArtifactType);
        Assert.False(queued.Value.CanDownload);
        Assert.Null(queued.Value.ChecksumSha256);
        Assert.Null(queued.Value.StartedAt);
        Assert.Null(queued.Value.CompletedAt);

        var processed = await exportStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queued.Value.Id,
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());
        Assert.Equal(queued.Value.Id, processed.Value.Id);
        Assert.Equal(ExportArtifactStatuses.Succeeded, processed.Value.Status);
        Assert.Equal(ExportArtifactFormats.Pdf, processed.Value.Format);
        Assert.Equal(pdfBytes.LongLength, processed.Value.ByteSize);
        Assert.NotNull(processed.Value.ChecksumSha256);
        Assert.NotNull(processed.Value.StartedAt);
        Assert.NotNull(processed.Value.CompletedAt);
        Assert.True(processed.Value.CanDownload);

        var reprocessed = await exportStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queued.Value.Id,
            CancellationToken.None);

        Assert.True(reprocessed.IsFailure);
        Assert.Equal("export_artifact.not_queued", reprocessed.Error.Code);

        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            queued.Value.Id,
            CancellationToken.None);

        Assert.True(download.IsSuccess, download.Error.ToString());
        Assert.Equal(pdfBytes, download.Value.ContentBytes);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var stored = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == queued.Value.Id);
        Assert.Equal(ExportArtifactStatuses.Succeeded, stored.Status);
        Assert.Equal(ExportArtifactStorageKinds.ExternalObject, stored.StorageKind);
        Assert.NotNull(stored.StorageKey);
        Assert.Null(stored.Content);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_success_enqueues_terminal_outbox_event()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var outboxBuffer = new OutboxEventBuffer();

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions(
            new OutboxSaveChangesInterceptor(
                currentTenant,
                new CurrentAuditContext(),
                outboxBuffer)));
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes),
            outboxEventBuffer: outboxBuffer);

        var queued = await exportStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        Assert.True(queued.IsSuccess, queued.Error.ToString());

        var processed = await exportStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queued.Value.Id,
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(CreateMigratorOptions());
        var outboxEvent = await verifyDb.OutboxEvents.SingleAsync(outboxEvent =>
            outboxEvent.AggregateId == queued.Value.Id &&
            outboxEvent.EventType == "ReportPdfArtifactTerminalStateReached");
        Assert.Equal(tenantId, outboxEvent.TenantId);
        Assert.Equal("export_artifact", outboxEvent.AggregateType);
        Assert.Null(outboxEvent.PublishedAt);

        var payload = outboxEvent.Payload.RootElement;
        Assert.Equal(1, payload.GetProperty("schema_version").GetInt32());
        Assert.Equal(queued.Value.Id, payload.GetProperty("export_artifact_id").GetGuid());
        Assert.Equal(campaignSeriesId, payload.GetProperty("campaign_series_id").GetGuid());
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportPdf, payload.GetProperty("artifact_type").GetString());
        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, payload.GetProperty("target_kind").GetString());
        Assert.Equal(ExportArtifactFormats.Pdf, payload.GetProperty("format").GetString());
        Assert.Equal(ExportArtifactStatuses.Succeeded, payload.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("failure_reason_code").ValueKind);
        Assert.False(payload.TryGetProperty("tenant_id", out _));
        Assert.False(payload.TryGetProperty("storage_key", out _));
        Assert.False(payload.TryGetProperty("checksum_sha256", out _));
        Assert.DoesNotContain("wdr_", payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_failure_enqueues_terminal_outbox_event_with_safe_reason()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        var rendererError = Error.Conflict(
            "report_pdf.render_failed",
            "Renderer failed with wdr_sensitive_token and <!doctype html> details.");
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var outboxBuffer = new OutboxEventBuffer();

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions(
            new OutboxSaveChangesInterceptor(
                currentTenant,
                new CurrentAuditContext(),
                outboxBuffer)));
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FailingReportPdfRenderer(rendererError),
            outboxEventBuffer: outboxBuffer);

        var queued = await exportStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        Assert.True(queued.IsSuccess, queued.Error.ToString());

        var processed = await exportStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queued.Value.Id,
            CancellationToken.None);

        Assert.True(processed.IsFailure);
        Assert.Equal("report_pdf.render_failed", processed.Error.Code);

        await using var verifyDb = new ApplicationDbContext(CreateMigratorOptions());
        var outboxEvent = await verifyDb.OutboxEvents.SingleAsync(outboxEvent =>
            outboxEvent.AggregateId == queued.Value.Id &&
            outboxEvent.EventType == "ReportPdfArtifactTerminalStateReached");
        var payload = outboxEvent.Payload.RootElement;
        Assert.Equal(ExportArtifactStatuses.Failed, payload.GetProperty("status").GetString());
        Assert.Equal("report_pdf.render_failed", payload.GetProperty("failure_reason_code").GetString());
        Assert.False(payload.TryGetProperty("storage_key", out _));
        Assert.False(payload.TryGetProperty("checksum_sha256", out _));
        Assert.DoesNotContain("wdr_", payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<!doctype", payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive", payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_stale_rendering_recovery_enqueues_terminal_outbox_event()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var staleArtifactId = Guid.NewGuid();
        var recentArtifactId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var seedDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            await using var seedTransaction = await new TenantDbScope(seedDb).BeginTransactionAsync(tenantId);
            seedDb.ExportArtifacts.Add(CreateRenderingPdfArtifact(
                staleArtifactId,
                tenantId,
                campaignSeriesId,
                now.AddHours(-2)));
            seedDb.ExportArtifacts.Add(CreateRenderingPdfArtifact(
                recentArtifactId,
                tenantId,
                campaignSeriesId,
                now.AddMinutes(-2)));
            await seedDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var outboxBuffer = new OutboxEventBuffer();
        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions(
            new OutboxSaveChangesInterceptor(
                currentTenant,
                new CurrentAuditContext(),
                outboxBuffer)));
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            outboxEventBuffer: outboxBuffer);

        var run = await exportStore.FailStaleCampaignSeriesReportPdfArtifactsAsync(
            tenantId,
            staleBefore: now.AddMinutes(-30),
            maxArtifacts: 10,
            CancellationToken.None);

        Assert.True(run.IsSuccess, run.Error.ToString());
        Assert.Equal(1, run.Value.ProcessedArtifactCount);

        await using var verifyDb = new ApplicationDbContext(CreateMigratorOptions());
        var outboxEvent = await verifyDb.OutboxEvents.SingleAsync(outboxEvent =>
            outboxEvent.AggregateId == staleArtifactId &&
            outboxEvent.EventType == "ReportPdfArtifactTerminalStateReached");
        Assert.False(await verifyDb.OutboxEvents.AnyAsync(outboxEvent =>
            outboxEvent.AggregateId == recentArtifactId &&
            outboxEvent.EventType == "ReportPdfArtifactTerminalStateReached"));
        var payload = outboxEvent.Payload.RootElement;
        Assert.Equal(staleArtifactId, payload.GetProperty("export_artifact_id").GetGuid());
        Assert.Equal(campaignSeriesId, payload.GetProperty("campaign_series_id").GetGuid());
        Assert.Equal(ExportArtifactStatuses.Failed, payload.GetProperty("status").GetString());
        Assert.Equal("report_pdf.rendering_timeout", payload.GetProperty("failure_reason_code").GetString());
        Assert.False(payload.TryGetProperty("storage_key", out _));
        Assert.False(payload.TryGetProperty("checksum_sha256", out _));
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_queued_batch_processor_honors_max_artifacts_per_tenant()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));

        var firstQueued = await exportStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        var secondQueued = await exportStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(firstQueued.IsSuccess, firstQueued.Error.ToString());
        Assert.True(secondQueued.IsSuccess, secondQueued.Error.ToString());

        var run = await exportStore.ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
            tenantId,
            maxArtifacts: 1,
            CancellationToken.None);

        Assert.True(run.IsSuccess, run.Error.ToString());
        Assert.Equal(tenantId, run.Value.TenantId);
        Assert.Equal(1, run.Value.MaxArtifacts);
        Assert.Equal(1, run.Value.ProcessedArtifactCount);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var artifacts = await verifyDb.ExportArtifacts
            .Where(artifact =>
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf)
            .OrderBy(artifact => artifact.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, artifacts.Count);
        Assert.Equal(ExportArtifactStatuses.Succeeded, artifacts[0].Status);
        Assert.Equal(ExportArtifactStatuses.Queued, artifacts[1].Status);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_stale_rendering_recovery_marks_old_rendering_failed_only()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var staleArtifactId = Guid.NewGuid();
        var recentArtifactId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var seedDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            await using var seedTransaction = await new TenantDbScope(seedDb).BeginTransactionAsync(tenantId);
            seedDb.ExportArtifacts.Add(CreateRenderingPdfArtifact(
                staleArtifactId,
                tenantId,
                campaignSeriesId,
                now.AddHours(-2)));
            seedDb.ExportArtifacts.Add(CreateRenderingPdfArtifact(
                recentArtifactId,
                tenantId,
                campaignSeriesId,
                now.AddMinutes(-2)));
            await seedDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var run = await exportStore.FailStaleCampaignSeriesReportPdfArtifactsAsync(
            tenantId,
            staleBefore: now.AddMinutes(-30),
            maxArtifacts: 10,
            CancellationToken.None);

        Assert.True(run.IsSuccess, run.Error.ToString());
        Assert.Equal(1, run.Value.ProcessedArtifactCount);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var stale = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == staleArtifactId);
        var recent = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == recentArtifactId);

        Assert.Equal(ExportArtifactStatuses.Failed, stale.Status);
        Assert.Equal("report_pdf.rendering_timeout", stale.FailureReasonCode);
        Assert.Null(stale.StorageKey);
        Assert.Null(stale.Content);
        Assert.Null(stale.ChecksumSha256);
        Assert.Contains("report_pdf.rendering_timeout", stale.MetadataJson, StringComparison.Ordinal);
        Assert.Equal(ExportArtifactStatuses.Rendering, recent.Status);
        Assert.Null(recent.FailureReasonCode);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_queued_worker_marks_failures_safe_and_skips_missing_targets()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        var rendererError = Error.Conflict(
            "report_pdf.render_failed",
            "Renderer failed with wdr_sensitive_token and <!doctype html> details.");

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var rendererFailingStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FailingReportPdfRenderer(rendererError));

        var queuedRendererFailure = await rendererFailingStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        Assert.True(queuedRendererFailure.IsSuccess, queuedRendererFailure.Error.ToString());

        var rendererFailure = await rendererFailingStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queuedRendererFailure.Value.Id,
            CancellationToken.None);

        Assert.True(rendererFailure.IsFailure);
        Assert.Equal("report_pdf.render_failed", rendererFailure.Error.Code);

        var storeError = Error.Conflict(
            "export_artifact_object.store_failed",
            "Object store failed with wdr_sensitive_token and raw path details.");
        var storeFailingStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: new FailingExportArtifactObjectStore(storeError),
            reportPdfRenderer: new FakeReportPdfRenderer([0x25, 0x50, 0x44, 0x46]));

        var queuedStoreFailure = await storeFailingStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        Assert.True(queuedStoreFailure.IsSuccess, queuedStoreFailure.Error.ToString());

        var storeFailure = await storeFailingStore.ProcessCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            queuedStoreFailure.Value.Id,
            CancellationToken.None);

        Assert.True(storeFailure.IsFailure);
        Assert.Equal("export_artifact_object.store_failed", storeFailure.Error.Code);

        var missingTarget = Guid.NewGuid();
        var missingTargetQueue = await rendererFailingStore.QueueCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            missingTarget,
            CancellationToken.None);

        Assert.True(missingTargetQueue.IsFailure);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var failedArtifacts = await verifyDb.ExportArtifacts
            .Where(artifact =>
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                artifact.Status == ExportArtifactStatuses.Failed)
            .OrderBy(artifact => artifact.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, failedArtifacts.Count);
        Assert.Contains(failedArtifacts, artifact => artifact.Id == queuedRendererFailure.Value.Id);
        Assert.Contains(failedArtifacts, artifact => artifact.Id == queuedStoreFailure.Value.Id);
        Assert.All(
            failedArtifacts,
            artifact =>
            {
                AssertFailedPdfAttemptShape(artifact, tenantId, campaignSeriesId);
                Assert.DoesNotContain("wdr_", artifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("<!doctype", artifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("sensitive", artifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("raw path", artifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
            });
        Assert.False(await verifyDb.ExportArtifacts.AnyAsync(artifact => artifact.CampaignSeriesId == missingTarget));
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_failed_artifact_retry_creates_new_queued_artifact_and_preserves_failed_attempt()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        var rendererError = Error.Conflict(
            "report_pdf.render_failed",
            "Renderer failed with wdr_sensitive_token and <!doctype html> details.");

        Guid failedArtifactId;
        await using (var failingDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(failingDb);
            var failingStore = new ReportProofExportStore(
                failingDb,
                tenantDbScope,
                new ReportProofStore(failingDb, tenantDbScope),
                objectStore: objectStore,
                reportPdfRenderer: new FailingReportPdfRenderer(rendererError));
            var queued = await failingStore.QueueCampaignSeriesReportPdfArtifactAsync(
                tenantId,
                campaignSeriesId,
                CancellationToken.None);
            Assert.True(queued.IsSuccess, queued.Error.ToString());
            var failed = await failingStore.ProcessCampaignSeriesReportPdfArtifactAsync(
                tenantId,
                queued.Value.Id,
                CancellationToken.None);
            Assert.True(failed.IsFailure);
            failedArtifactId = queued.Value.Id;
        }

        await using var retryDb = new ApplicationDbContext(CreateRuntimeOptions());
        var retryDbScope = new TenantDbScope(retryDb);
        var retryStore = new ReportProofExportStore(
            retryDb,
            retryDbScope,
            new ReportProofStore(retryDb, retryDbScope));

        var retry = await retryStore.RetryCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            failedArtifactId,
            CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error.ToString());
        Assert.NotEqual(failedArtifactId, retry.Value.Id);
        Assert.Equal(ExportArtifactStatuses.Queued, retry.Value.Status);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportPdf, retry.Value.ArtifactType);
        Assert.Equal(campaignSeriesId, retry.Value.CampaignSeriesId);
        Assert.False(retry.Value.CanDownload);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        await using var verifyTransaction = await new TenantDbScope(verifyDb).BeginTransactionAsync(tenantId);
        var failedArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == failedArtifactId);
        var retryArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == retry.Value.Id);

        Assert.Equal(ExportArtifactStatuses.Failed, failedArtifact.Status);
        Assert.Equal("report_pdf.render_failed", failedArtifact.FailureReasonCode);
        Assert.Equal(ExportArtifactStatuses.Queued, retryArtifact.Status);
        Assert.Equal(failedArtifact.CampaignSeriesId, retryArtifact.CampaignSeriesId);
        Assert.Contains(failedArtifactId.ToString(), retryArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wdr_", retryArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<!doctype", retryArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive", retryArtifact.MetadataJson, StringComparison.OrdinalIgnoreCase);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_retry_rejects_unsupported_and_non_failed_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope),
            objectStore: objectStore,
            reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));
        var succeededPdf = await exportStore.CreateCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);
        var htmlArtifact = await exportStore.CreateCampaignSeriesReportHtmlArtifactAsync(
            tenantId,
            campaignSeriesId,
            CancellationToken.None);

        Assert.True(succeededPdf.IsSuccess, succeededPdf.Error.ToString());
        Assert.True(htmlArtifact.IsSuccess, htmlArtifact.Error.ToString());

        var retrySucceeded = await exportStore.RetryCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            succeededPdf.Value.Id,
            CancellationToken.None);
        var retryHtml = await exportStore.RetryCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            htmlArtifact.Value.Id,
            CancellationToken.None);
        var retryMissing = await exportStore.RetryCampaignSeriesReportPdfArtifactAsync(
            tenantId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(retrySucceeded.IsFailure);
        Assert.Equal("export_artifact.retry_not_failed", retrySucceeded.Error.Code);
        Assert.True(retryHtml.IsFailure);
        Assert.Equal("export_artifact.retry_not_supported", retryHtml.Error.Code);
        Assert.True(retryMissing.IsFailure);
        Assert.Equal("export_artifact.not_found", retryMissing.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_artifact_returns_not_found_for_cross_tenant_retrieval()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];
        Guid artifactId;

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantADbScope = new TenantDbScope(tenantADb);
            var exportStore = new ReportProofExportStore(
                tenantADb,
                tenantADbScope,
                new ReportProofStore(tenantADb, tenantADbScope),
                objectStore: objectStore,
                reportPdfRenderer: new FakeReportPdfRenderer(pdfBytes));
            var artifact = await exportStore.CreateCampaignSeriesReportPdfArtifactAsync(
                tenantA,
                scenario.Report.CampaignSeriesId.Value,
                CancellationToken.None);

            Assert.True(artifact.IsSuccess, artifact.Error.ToString());
            artifactId = artifact.Value.Id;
        }

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        var tenantBExportStore = new ReportProofExportStore(
            tenantBDb,
            tenantBDbScope,
            new ReportProofStore(tenantBDb, tenantBDbScope),
            objectStore: objectStore);

        var retrieved = await tenantBExportStore.GetExportArtifactAsync(
            tenantB,
            artifactId,
            CancellationToken.None);
        var download = await tenantBExportStore.GetExportArtifactDownloadAsync(
            tenantB,
            artifactId,
            CancellationToken.None);

        Assert.True(retrieved.IsFailure);
        Assert.True(download.IsFailure);
        Assert.Equal("export_artifact.not_found", retrieved.Error.Code);
        Assert.Equal("export_artifact.not_found", download.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_report_pdf_artifact_download_rejects_object_integrity_mismatch()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var campaignSeriesId = scenario.Report.CampaignSeriesId.Value;
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] content = [0x25, 0x50, 0x44, 0x46, 0x0A];
        var checksum = Convert
            .ToHexString(System.Security.Cryptography.SHA256.HashData(content))
            .ToLowerInvariant();
        var wrongChecksum = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        var sizeMismatchId = Guid.NewGuid();
        var checksumMismatchId = Guid.NewGuid();
        var sizeMismatchKey = $"tenants/{tenantId:N}/campaign-series/{campaignSeriesId:N}/reports/{sizeMismatchId:N}.pdf";
        var checksumMismatchKey = $"tenants/{tenantId:N}/campaign-series/{campaignSeriesId:N}/reports/{checksumMismatchId:N}.pdf";
        Assert.True((await objectStore.StoreAsync(sizeMismatchKey, content, CancellationToken.None)).IsSuccess);
        Assert.True((await objectStore.StoreAsync(checksumMismatchKey, content, CancellationToken.None)).IsSuccess);

        await using (var seedDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var seedScope = new TenantDbScope(seedDb);
            await using var transaction = await seedScope.BeginTransactionAsync(tenantId);
            seedDb.ExportArtifacts.Add(new ExportArtifact(
                sizeMismatchId,
                tenantId,
                ExportArtifactTargetKinds.CampaignSeries,
                campaignId: null,
                campaignSeriesId,
                ExportArtifactTypes.CampaignSeriesReportPdf,
                ExportArtifactStatuses.Succeeded,
                ExportArtifactFormats.Pdf,
                "size-mismatch.pdf",
                "application/pdf",
                rowCount: 1,
                byteSize: content.Length + 1,
                checksumSha256: checksum,
                metadataJson: """{"artifactType":"campaign_series_report_pdf"}""",
                content: null,
                codebookJson: "{}",
                createdAt: DateTimeOffset.Parse("2026-05-18T18:45:00+00:00"),
                completedAt: DateTimeOffset.Parse("2026-05-18T18:45:01+00:00"),
                storageKind: ExportArtifactStorageKinds.ExternalObject,
                storageKey: sizeMismatchKey));
            seedDb.ExportArtifacts.Add(new ExportArtifact(
                checksumMismatchId,
                tenantId,
                ExportArtifactTargetKinds.CampaignSeries,
                campaignId: null,
                campaignSeriesId,
                ExportArtifactTypes.CampaignSeriesReportPdf,
                ExportArtifactStatuses.Succeeded,
                ExportArtifactFormats.Pdf,
                "checksum-mismatch.pdf",
                "application/pdf",
                rowCount: 1,
                byteSize: content.Length,
                checksumSha256: wrongChecksum,
                metadataJson: """{"artifactType":"campaign_series_report_pdf"}""",
                content: null,
                codebookJson: "{}",
                createdAt: DateTimeOffset.Parse("2026-05-18T18:46:00+00:00"),
                completedAt: DateTimeOffset.Parse("2026-05-18T18:46:01+00:00"),
                storageKind: ExportArtifactStorageKinds.ExternalObject,
                storageKey: checksumMismatchKey));
            await seedDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(db);
        var exportStore = new ReportProofExportStore(
            db,
            tenantDbScope,
            new ReportProofStore(db, tenantDbScope),
            objectStore: objectStore);

        var sizeMismatch = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            sizeMismatchId,
            CancellationToken.None);
        var checksumMismatch = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            checksumMismatchId,
            CancellationToken.None);

        Assert.True(sizeMismatch.IsFailure);
        Assert.Equal("export_artifact.object_integrity_mismatch", sizeMismatch.Error.Code);
        Assert.True(checksumMismatch.IsFailure);
        Assert.Equal("export_artifact.object_integrity_mismatch", checksumMismatch.Error.Code);
    }

    [DockerFact]
    public async Task Export_artifact_store_retrieves_external_object_metadata_but_blocks_inline_download()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        var artifactId = Guid.NewGuid();

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.ExportArtifacts.Add(new ExportArtifact(
                artifactId,
                tenantId,
                ExportArtifactTargetKinds.CampaignSeries,
                campaignId: null,
                campaignSeriesId: scenario.Report.CampaignSeriesId.Value,
                ExportArtifactTypes.CampaignSeriesReportHtml,
                ExportArtifactStatuses.Succeeded,
                ExportArtifactFormats.Html,
                "external-report.html",
                "text/html; charset=utf-8",
                rowCount: 1,
                byteSize: 512,
                checksumSha256: "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
                metadataJson: """{"artifactType":"campaign_series_report_html"}""",
                content: null,
                codebookJson: "{}",
                createdAt: DateTimeOffset.Parse("2026-05-18T16:00:00+00:00"),
                completedAt: DateTimeOffset.Parse("2026-05-18T16:00:01+00:00"),
                storageKind: ExportArtifactStorageKinds.ExternalObject,
                storageKey: "tenants/tenant-a/reports/external-report.html"));
            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            artifactId,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            artifactId,
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.Equal(ExportArtifactStatuses.Succeeded, retrieved.Value.Status);
        Assert.False(retrieved.Value.CanDownload);
        Assert.Equal("", retrieved.Value.CsvContent);
        Assert.True(download.IsFailure);
        Assert.Equal("export_artifact.not_downloadable", download.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_series_report_html_artifact_returns_not_found_for_cross_tenant_retrieval()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);
        Assert.NotNull(scenario.Report.CampaignSeriesId);
        Guid artifactId;

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantADbScope = new TenantDbScope(tenantADb);
            var exportStore = new ReportProofExportStore(
                tenantADb,
                tenantADbScope,
                new ReportProofStore(tenantADb, tenantADbScope));
            var artifact = await exportStore.CreateCampaignSeriesReportHtmlArtifactAsync(
                tenantA,
                scenario.Report.CampaignSeriesId.Value,
                CancellationToken.None);

            Assert.True(artifact.IsSuccess, artifact.Error.ToString());
            artifactId = artifact.Value.Id;
        }

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        var tenantBExportStore = new ReportProofExportStore(
            tenantBDb,
            tenantBDbScope,
            new ReportProofStore(tenantBDb, tenantBDbScope));

        var retrieved = await tenantBExportStore.GetExportArtifactAsync(
            tenantB,
            artifactId,
            CancellationToken.None);
        var download = await tenantBExportStore.GetExportArtifactDownloadAsync(
            tenantB,
            artifactId,
            CancellationToken.None);

        Assert.True(retrieved.IsFailure);
        Assert.True(download.IsFailure);
        Assert.Equal("export_artifact.not_found", retrieved.Error.Code);
        Assert.Equal("export_artifact.not_found", download.Error.Code);
    }

    [DockerFact]
    public async Task Export_artifact_store_blocks_download_when_artifact_not_downloadable()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        var artifactId = Guid.NewGuid();

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.ExportArtifacts.Add(new ExportArtifact(
                artifactId,
                tenantId,
                ExportArtifactTargetKinds.Campaign,
                scenario.Report.CampaignId,
                scenario.Report.CampaignSeriesId,
                ExportArtifactTypes.ReportProofCsvCodebook,
                ExportArtifactStatuses.Queued,
                ExportArtifactFormats.CsvCodebook,
                $"campaign-{scenario.Report.CampaignId}-report-proof.csv",
                "text/csv",
                rowCount: 0,
                byteSize: 0,
                checksumSha256: null,
                metadataJson: """{"artifactType":"report_proof_csv_codebook"}""",
                content: null,
                codebookJson: "{}",
                createdAt: DateTimeOffset.Parse("2026-05-09T12:00:00+00:00"),
                completedAt: null));
            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var exportStore = new ReportProofExportStore(
            tenantDb,
            tenantDbScope,
            new ReportProofStore(tenantDb, tenantDbScope));

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            artifactId,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            artifactId,
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.Equal(ExportArtifactStatuses.Queued, retrieved.Value.Status);
        Assert.False(retrieved.Value.CanDownload);
        Assert.True(download.IsFailure);
        Assert.Equal("export_artifact.not_downloadable", download.Error.Code);
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_export_artifact_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantADbScope = new TenantDbScope(tenantADb);
            var exportStore = new ReportProofExportStore(
                tenantADb,
                tenantADbScope,
                new ReportProofStore(tenantADb, tenantADbScope));
            var artifact = await exportStore.CreateCampaignReportProofExportAsync(
                tenantA,
                scenario.Report.CampaignId,
                CancellationToken.None);

            Assert.True(artifact.IsSuccess, artifact.Error.ToString());
        }

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        await using var tenantBTransaction = await tenantBDbScope.BeginTransactionAsync(tenantB);

        Assert.Empty(await tenantBDb.ExportArtifacts.ToListAsync());

        await tenantBTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Export_artifact_store_returns_not_found_for_cross_tenant_retrieval()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);
        Guid artifactId;

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantADbScope = new TenantDbScope(tenantADb);
            var exportStore = new ReportProofExportStore(
                tenantADb,
                tenantADbScope,
                new ReportProofStore(tenantADb, tenantADbScope));
            var artifact = await exportStore.CreateCampaignReportProofExportAsync(
                tenantA,
                scenario.Report.CampaignId,
                CancellationToken.None);

            Assert.True(artifact.IsSuccess, artifact.Error.ToString());
            artifactId = artifact.Value.Id;
        }

        await using var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantBDbScope = new TenantDbScope(tenantBDb);
        var tenantBExportStore = new ReportProofExportStore(
            tenantBDb,
            tenantBDbScope,
            new ReportProofStore(tenantBDb, tenantBDbScope));

        var retrieved = await tenantBExportStore.GetExportArtifactAsync(
            tenantB,
            artifactId,
            CancellationToken.None);
        var download = await tenantBExportStore.GetExportArtifactDownloadAsync(
            tenantB,
            artifactId,
            CancellationToken.None);

        Assert.True(retrieved.IsFailure);
        Assert.True(download.IsFailure);
        Assert.Equal("export_artifact.not_found", retrieved.Error.Code);
        Assert.Equal("export_artifact.not_found", download.Error.Code);
    }

    [DockerFact]
    public async Task Export_artifact_store_downloads_external_object_bytes_from_private_store()
    {
        var tenantId = Guid.NewGuid();
        var scenario = await CreateReportProofScenarioAsync(tenantId, submittedResponseCount: 5);
        var objectStore = new LocalExportArtifactObjectStore(
            Microsoft.Extensions.Options.Options.Create(new ExportArtifactObjectStoreOptions
            {
                RootPath = Path.Combine(
                    Path.GetTempPath(),
                    "instruments-platform-tests",
                    Guid.NewGuid().ToString("N"))
            }));
        byte[] content = [0x00, 0x01, 0xFE, 0xFF];
        var storageKey = $"tenants/{tenantId:N}/reports/{Guid.NewGuid():N}.bin";
        var stored = await objectStore.StoreAsync(storageKey, content, CancellationToken.None);
        Assert.True(stored.IsSuccess, stored.Error.ToString());

        var artifactId = Guid.NewGuid();
        var checksum = Convert
            .ToHexString(System.Security.Cryptography.SHA256.HashData(content))
            .ToLowerInvariant();
        await using (var tenantDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            tenantDb.ExportArtifacts.Add(new ExportArtifact(
                artifactId,
                tenantId,
                ExportArtifactTargetKinds.CampaignSeries,
                campaignId: null,
                scenario.Report.CampaignSeriesId,
                ExportArtifactTypes.CampaignSeriesReportHtml,
                ExportArtifactStatuses.Succeeded,
                ExportArtifactFormats.Html,
                $"campaign-series-{scenario.Report.CampaignSeriesId}-report.html",
                "text/html; charset=utf-8",
                rowCount: 1,
                byteSize: content.Length,
                checksumSha256: checksum,
                metadataJson: "{}",
                content: null,
                codebookJson: "{}",
                createdAt: DateTimeOffset.Parse("2026-05-18T12:00:00+00:00", CultureInfo.InvariantCulture),
                completedAt: DateTimeOffset.Parse("2026-05-18T12:00:01+00:00", CultureInfo.InvariantCulture),
                storageKind: ExportArtifactStorageKinds.ExternalObject,
                storageKey: storageKey));
            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var downloadDb = new ApplicationDbContext(CreateRuntimeOptions());
        var downloadDbScope = new TenantDbScope(downloadDb);
        var signedUrlProvider = new RecordingExportArtifactSignedUrlProvider();
        var exportStore = new ReportProofExportStore(
            downloadDb,
            downloadDbScope,
            new ReportProofStore(downloadDb, downloadDbScope),
            objectStore: objectStore,
            signedUrlProvider: signedUrlProvider);

        var retrieved = await exportStore.GetExportArtifactAsync(
            tenantId,
            artifactId,
            CancellationToken.None);
        var download = await exportStore.GetExportArtifactDownloadAsync(
            tenantId,
            artifactId,
            CancellationToken.None);
        var signedUrl = await exportStore.GetExportArtifactSignedDownloadUrlAsync(
            tenantId,
            artifactId,
            TimeSpan.FromMinutes(15),
            CancellationToken.None);

        Assert.True(retrieved.IsSuccess, retrieved.Error.ToString());
        Assert.True(download.IsSuccess, download.Error.ToString());
        Assert.True(signedUrl.IsFailure);
        Assert.Equal("export_artifact.not_downloadable", signedUrl.Error.Code);
        Assert.Empty(signedUrlProvider.RequestedStorageKeys);
        Assert.True(retrieved.Value.CanDownload);
        Assert.Equal(content.Length, download.Value.ByteSize);
        Assert.Equal(checksum, download.Value.ChecksumSha256);
        Assert.Equal(content, download.Value.ContentBytes);
        Assert.Empty(download.Value.Content);
    }

    [DockerFact]
    public async Task Export_artifact_guard_blocks_cross_tenant_campaign_target_writes()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenarioA = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);
        var scenarioB = await CreateReportProofScenarioAsync(tenantB, submittedResponseCount: 5);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantADbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantADbScope.BeginTransactionAsync(tenantA);

        tenantADb.ExportArtifacts.Add(CreateGuardExportArtifact(
            tenantA,
            ExportArtifactTargetKinds.Campaign,
            scenarioB.Report.CampaignId,
            scenarioA.Report.CampaignSeriesId,
            ExportArtifactTypes.ReportProofCsvCodebook,
            "cross-tenant-campaign.csv"));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Export_artifact_guard_blocks_cross_tenant_campaign_series_target_writes()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _ = await CreateReportProofScenarioAsync(tenantA, submittedResponseCount: 5);
        var scenarioB = await CreateReportProofScenarioAsync(tenantB, submittedResponseCount: 5);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantADbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantADbScope.BeginTransactionAsync(tenantA);

        tenantADb.ExportArtifacts.Add(CreateGuardExportArtifact(
            tenantA,
            ExportArtifactTargetKinds.CampaignSeries,
            null,
            scenarioB.Report.CampaignSeriesId,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            "cross-tenant-series.csv"));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Campaign_series_response_export_store_persists_item_csv_codebook_and_local_trajectory_ids()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T16:30:00+00:00");
        await using var scenario = await CreateTwoWaveProofScenarioAsync(
            tenantId,
            "Response export proof",
            includeMatrixQuestion: true,
            includeDisplayLogicQuestion: true);
        await SubmitFiveLinkedScoredWaveComparisonResponsesAsync(scenario);
        await CloseScenarioCampaignAsync(
            scenario.Db,
            scenario.TenantDbScope,
            tenantId,
            scenario.Wave1.CampaignId,
            actorUserId,
            closedAt,
            "Wave 1 response export complete");

        var exportStore = new ReportProofExportStore(
            scenario.Db,
            scenario.TenantDbScope,
            new ReportProofStore(scenario.Db, scenario.TenantDbScope));

        var artifact = await exportStore.CreateCampaignSeriesResponseExportAsync(
            tenantId,
            scenario.SeriesId,
            CancellationToken.None);

        Assert.True(artifact.IsSuccess, artifact.Error.ToString());
        Assert.Equal("campaign_series_response_csv_codebook", artifact.Value.ArtifactType);
        Assert.Equal("succeeded", artifact.Value.Status);
        Assert.Equal("csv_codebook", artifact.Value.Format);
        Assert.Equal("campaign_series", artifact.Value.TargetKind);
        Assert.Equal(scenario.SeriesId, artifact.Value.TargetId);
        Assert.Null(artifact.Value.CampaignId);
        Assert.Equal(scenario.SeriesId, artifact.Value.CampaignSeriesId);
        Assert.Equal(10, artifact.Value.RowCount);
        Assert.Contains("response_row_id,trajectory_id,campaign_series_id,campaign_id,wave_label,campaign_status,campaign_closed_at,campaign_data_finality,launch_packet_schema_version,launch_packet_sections,launch_packet_source", artifact.Value.CsvContent);
        Assert.Contains("template;instrument;scoring;policies;identity;respondent_rules;launch_readiness;provenance", artifact.Value.CsvContent);
        Assert.Contains("answer_q01", artifact.Value.CsvContent);
        Assert.Contains("answer_body_discomfort_r01,answer_body_discomfort_r02", artifact.Value.CsvContent);
        Assert.Contains("answer_recovery_followup", artifact.Value.CsvContent);
        Assert.Contains(",c02,c03,", artifact.Value.CsvContent);
        Assert.Contains(
            "score_total_n_valid,score_total_n_expected,score_total_missing_policy_status",
            artifact.Value.CsvContent);
        Assert.Contains(",1,1,ok", artifact.Value.CsvContent);
        Assert.Contains("t000001", artifact.Value.CsvContent);
        Assert.Contains("anonymous_longitudinal", artifact.Value.CsvContent);
        Assert.Contains("closed_wave", artifact.Value.CsvContent);
        Assert.Contains("preliminary_live", artifact.Value.CsvContent);
        Assert.Contains(closedAt.ToString("O", CultureInfo.InvariantCulture), artifact.Value.CsvContent);
        Assert.DoesNotContain("assignment", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("response_session", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant_id", artifact.Value.CsvContent, StringComparison.OrdinalIgnoreCase);

        using var codebook = JsonDocument.Parse(artifact.Value.CodebookJson);
        Assert.Equal(
            "campaign_series_response_csv_codebook",
            codebook.RootElement.GetProperty("artifactType").GetString());
        Assert.Equal(
            "per_artifact",
            codebook.RootElement.GetProperty("trajectoryIdPolicy").GetString());
        Assert.Equal(
            5,
            codebook.RootElement.GetProperty("closedWaveResponseCount").GetInt32());
        Assert.Contains("launch_packet", artifact.Value.CodebookJson);
        Assert.Contains("launch_packet_schema_version", artifact.Value.CodebookJson);
        Assert.Contains("launch_packet_sections", artifact.Value.CodebookJson);
        Assert.Contains("launch_packet_source", artifact.Value.CodebookJson);
        Assert.Equal(
            5,
            codebook.RootElement.GetProperty("preliminaryLiveResponseCount").GetInt32());
        Assert.Contains(
            codebook.RootElement.GetProperty("excludedIdentifiers").EnumerateArray(),
            item => item.GetString() == "participant_code_id");
        Assert.Equal(
            1,
            codebook.RootElement.GetProperty("scoreMetadataDimensionCount").GetInt32());
        Assert.Contains(
            codebook.RootElement.GetProperty("columns").EnumerateArray(),
            column =>
                column.GetProperty("name").GetString() == "score_total_n_valid" &&
                column.GetProperty("source").GetString() == "score_output_metadata");
        var matrixColumn = codebook.RootElement
            .GetProperty("columns")
            .EnumerateArray()
            .Single(column => column.GetProperty("name").GetString() == "answer_body_discomfort_r01");
        Assert.Equal("answer", matrixColumn.GetProperty("source").GetString());
        Assert.Equal("matrix", matrixColumn.GetProperty("questionType").GetString());
        Assert.Equal("body_discomfort", matrixColumn.GetProperty("questionCode").GetString());
        Assert.Equal("r01", matrixColumn.GetProperty("matrixRowCode").GetString());
        Assert.Equal("Neck / shoulders", matrixColumn.GetProperty("matrixRowLabel").GetString());
        Assert.Equal("Mild", matrixColumn.GetProperty("valueLabels").GetProperty("c02").GetString());
        Assert.Equal(
            "one_column_per_matrix_row",
            matrixColumn.GetProperty("answerMetadata").GetProperty("exportShape").GetString());
        var displayLogicColumn = codebook.RootElement
            .GetProperty("columns")
            .EnumerateArray()
            .Single(column => column.GetProperty("name").GetString() == "answer_recovery_followup");
        var displayLogic = displayLogicColumn.GetProperty("displayLogic");
        Assert.Equal("show_when", displayLogic.GetProperty("mode").GetString());
        Assert.Equal("q01", displayLogic.GetProperty("sourceQuestionCode").GetString());
        Assert.Equal("equals", displayLogic.GetProperty("operatorName").GetString());
        Assert.Equal(3, displayLogic.GetProperty("value").GetInt32());
        Assert.True(displayLogic.GetProperty("requiredWhenVisible").GetBoolean());
        Assert.Equal("__skipped", displayLogic.GetProperty("hiddenAnswerTreatment").GetString());
        Assert.Equal(
            "__skipped",
            codebook.RootElement
                .GetProperty("missingTreatment")
                .GetProperty("hiddenByDisplayLogic")
                .GetString());

        await using var transaction = await scenario.TenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await scenario.Db.ExportArtifacts.SingleAsync(entity => entity.Id == artifact.Value.Id);
        Assert.Equal("campaign_series", persisted.TargetKind);
        Assert.Null(persisted.CampaignId);
        Assert.Equal(scenario.SeriesId, persisted.CampaignSeriesId);
        Assert.Equal(artifact.Value.ChecksumSha256, persisted.ChecksumSha256);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Setup_store_launches_campaign_and_writes_snapshot()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var series = await setupStore.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Launch consent study"),
            CancellationToken.None);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Launch lab wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);

        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(series.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.Equal(CampaignStatuses.Live, launched.Value.Status);
        Assert.Equal(versionId, launched.Value.TemplateVersionId);
        Assert.Equal(scoringRule.Value.Id, launched.Value.ScoringRuleId);
        Assert.Equal(ResponseIdentityModes.Anonymous, launched.Value.ResponseIdentityMode);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedCampaign = await tenantDb.Campaigns.SingleAsync(entity => entity.Id == campaign.Value.Id);
        var snapshot = await tenantDb.CampaignLaunchSnapshots.SingleAsync(entity => entity.CampaignId == campaign.Value.Id);
        var retentionPolicy = await tenantDb.RetentionPolicies.SingleAsync(entity => entity.CampaignSeriesId == series.Value.Id);
        var disclosurePolicy = await tenantDb.DisclosurePolicies.SingleAsync(entity => entity.CampaignSeriesId == series.Value.Id);

        Assert.Equal(CampaignStatuses.Live, storedCampaign.Status);
        Assert.Equal(launched.Value.LaunchSnapshotId, snapshot.Id);
        Assert.Equal(scoringRule.Value.Id, snapshot.ScoringRuleId);
        Assert.Equal(versionId, snapshot.TemplateVersionId);
        Assert.NotNull(snapshot.ConsentDocumentId);
        Assert.Equal(retentionPolicy.Id, launched.Value.RetentionPolicyId);
        Assert.Equal(disclosurePolicy.Id, launched.Value.DisclosurePolicyId);
        Assert.Equal(retentionPolicy.Id, snapshot.RetentionPolicyId);
        Assert.Equal(disclosurePolicy.Id, snapshot.DisclosurePolicyId);
        Assert.Contains("\"ready\":true", snapshot.LaunchReadiness);
        Assert.Contains("\"schema_version\":1", snapshot.LaunchPacket);
        Assert.Contains("\"source\":\"runtime_launch\"", snapshot.LaunchPacket);
        Assert.Contains("\"launch_readiness\"", snapshot.LaunchPacket);
        Assert.Contains("\"respondent_rules\"", snapshot.LaunchPacket);
        Assert.DoesNotContain("answer", snapshot.LaunchPacket, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", snapshot.LaunchPacket, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salt", snapshot.LaunchPacket, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant_id", snapshot.LaunchPacket, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", snapshot.LaunchPacket, StringComparison.OrdinalIgnoreCase);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_open_link_store_creates_hashed_token_for_launched_anonymous_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Open link consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Open link wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.Equal(campaign.Value.Id, openLink.Value.CampaignId);
        Assert.StartsWith("opn_", openLink.Value.Token, StringComparison.Ordinal);
        Assert.Equal($"/r/{openLink.Value.Token}", openLink.Value.RespondentPath);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var expectedHash = OpenLinkTokens.Hash(openLink.Value.Token);
        var token = await tenantDb.InvitationTokens.SingleAsync(entity => entity.TokenHash == expectedHash);
        var assignment = await tenantDb.Assignments.SingleAsync(entity => entity.InviteTokenId == token.Id);

        Assert.NotEqual(openLink.Value.Token, token.TokenHash);
        Assert.Equal(InvitationTokenChannels.OpenLink, token.Channel);
        Assert.Equal(campaign.Value.Id, token.CampaignId);
        Assert.True(assignment.Anonymous);
        Assert.Equal("public_respondent", assignment.Role);
        Assert.Equal(openLink.Value.AssignmentId, assignment.Id);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_identified_entry_store_creates_hashed_token_for_identified_assignment()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified entry consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified entry wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Identified Respondent",
            email: "identified-entry@example.test");
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(respondent);
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.Add(new AudienceMember(audience.Id, respondent.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        var entry = await setupStore.CreateCampaignIdentifiedEntryAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.Equal(campaign.Value.Id, entry.Value.CampaignId);
        Assert.StartsWith("idn_", entry.Value.Token, StringComparison.Ordinal);
        Assert.Equal($"/r/{entry.Value.Token}", entry.Value.RespondentPath);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var expectedHash = OpenLinkTokens.Hash(entry.Value.Token);
        var token = await tenantDb.InvitationTokens.SingleAsync(entity => entity.TokenHash == expectedHash);
        var assignment = await tenantDb.Assignments.SingleAsync(entity => entity.Id == entry.Value.AssignmentId);
        var subject = await tenantDb.Subjects.SingleAsync(entity => entity.Id == entry.Value.SubjectId);

        Assert.NotEqual(entry.Value.Token, token.TokenHash);
        Assert.Equal(InvitationTokenChannels.IdentifiedEntry, token.Channel);
        Assert.Equal(campaign.Value.Id, token.CampaignId);
        Assert.Equal(assignment.Id, token.AssignmentId);
        Assert.False(assignment.Anonymous);
        Assert.Null(assignment.InviteTokenId);
        Assert.Equal("self", assignment.Role);
        Assert.Equal(respondent.Id, assignment.RespondentSubjectId);
        Assert.Equal(respondent.Id, assignment.TargetSubjectId);
        Assert.Equal(respondent.Id, subject.Id);
        Assert.Equal(tenantId, subject.TenantId);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_identified_queue_access_store_creates_idempotent_hashed_tokens_per_respondent()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue access study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified queue access wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Queue Manager",
            email: "manager@example.test",
            externalId: "manager-external-id-must-not-leak");
        var ana = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Queue Ana",
            email: "ana@example.test",
            externalId: "ana-external-id-must-not-leak");
        var ivan = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Queue Ivan",
            email: "ivan@example.test",
            externalId: "ivan-external-id-must-not-leak");
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);

        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, ana, ivan);
            tenantDb.SubjectRelationships.AddRange(
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    ana.Id,
                    SubjectRelationshipTypes.ManagerOf),
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    ivan.Id,
                    SubjectRelationshipTypes.ManagerOf));
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.AddRange(
                new AudienceMember(audience.Id, ana.Id),
                new AudienceMember(audience.Id, ivan.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{ana.Id:D}}","{{ivan.Id:D}}"]}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        var issued = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(issued.IsSuccess, issued.Error.ToString());
        Assert.Equal(3, issued.Value.RespondentCount);
        Assert.Equal(4, issued.Value.AssignmentCount);
        Assert.Equal(3, issued.Value.CreatedAccessCount);
        Assert.Equal(0, issued.Value.ExistingAccessCount);
        Assert.Contains(
            issued.Value.Respondents,
            respondent => respondent.RespondentSubjectId == manager.Id && respondent.AssignmentCount == 2);
        Assert.All(issued.Value.Respondents, respondent =>
        {
            Assert.Equal("created", respondent.AccessStatus);
            Assert.StartsWith("idq_", respondent.Token, StringComparison.Ordinal);
            Assert.Equal($"/r/{respondent.Token}", respondent.RespondentPath);
        });

        var firstHashesBySubject = new Dictionary<Guid, string>();
        await using (var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var tokens = await tenantDb.InvitationTokens
                .Where(token =>
                    token.CampaignId == campaign.Value.Id &&
                    token.Channel == InvitationTokenChannels.IdentifiedQueue)
                .OrderBy(token => token.RespondentSubjectId)
                .ToListAsync();

            Assert.Equal(3, tokens.Count);
            foreach (var token in tokens)
            {
                Assert.NotNull(token.RespondentSubjectId);
                Assert.Null(token.AssignmentId);
                Assert.DoesNotContain(
                    token.TokenHash,
                    issued.Value.Respondents.Select(respondent => respondent.Token));
                firstHashesBySubject[token.RespondentSubjectId!.Value] = token.TokenHash;
            }

            await verificationTransaction.CommitAsync();
        }

        var idempotent = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(idempotent.IsSuccess, idempotent.Error.ToString());
        Assert.Equal(0, idempotent.Value.CreatedAccessCount);
        Assert.Equal(3, idempotent.Value.ExistingAccessCount);
        Assert.All(idempotent.Value.Respondents, respondent =>
        {
            Assert.Equal("existing", respondent.AccessStatus);
            Assert.Null(respondent.Token);
            Assert.Null(respondent.RespondentPath);
        });

        var repeatedWithLegacyRotateFlag = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            CreateIdentifiedQueueAccessRequestWithLegacyRotateFlag(),
            CancellationToken.None);

        Assert.True(repeatedWithLegacyRotateFlag.IsSuccess, repeatedWithLegacyRotateFlag.Error.ToString());
        Assert.Equal(0, repeatedWithLegacyRotateFlag.Value.CreatedAccessCount);
        Assert.Equal(3, repeatedWithLegacyRotateFlag.Value.ExistingAccessCount);
        Assert.All(repeatedWithLegacyRotateFlag.Value.Respondents, respondent =>
        {
            Assert.Equal("existing", respondent.AccessStatus);
            Assert.Null(respondent.Token);
            Assert.Null(respondent.RespondentPath);
        });

        await using var repeatedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var repeatedTokens = await tenantDb.InvitationTokens
            .Where(token =>
                token.CampaignId == campaign.Value.Id &&
                token.Channel == InvitationTokenChannels.IdentifiedQueue)
            .ToListAsync();

        Assert.Equal(3, repeatedTokens.Count);
        foreach (var token in repeatedTokens)
        {
            Assert.NotNull(token.RespondentSubjectId);
            Assert.Equal(firstHashesBySubject[token.RespondentSubjectId!.Value], token.TokenHash);
        }

        await repeatedTransaction.CommitAsync();

        static CreateCampaignIdentifiedQueueAccessRequest CreateIdentifiedQueueAccessRequestWithLegacyRotateFlag()
        {
            var legacyConstructor = typeof(CreateCampaignIdentifiedQueueAccessRequest)
                .GetConstructor([typeof(bool)]);

            return legacyConstructor is null
                ? new CreateCampaignIdentifiedQueueAccessRequest()
                : (CreateCampaignIdentifiedQueueAccessRequest)legacyConstructor.Invoke([true]);
        }
    }

    [DockerFact]
    public async Task Campaign_identified_queue_access_store_reports_existing_after_concurrent_create_race()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        var runtimeOptions = CreateRuntimeOptions();
        await using var tenantDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "queue.race.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"queue.race.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue access race study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified queue access race wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Queue Race Respondent",
            email: "queue-race@example.test");
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);

        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(respondent);
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.Add(new AudienceMember(audience.Id, respondent.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());

        var competingInsertInterceptor = new InsertCompetingIdentifiedQueueTokensInterceptor(runtimeOptions);
        await using var racingDb = new ApplicationDbContext(CreateRuntimeOptions(competingInsertInterceptor));
        var racingStore = new SetupWorkflowStore(racingDb, new TenantDbScope(racingDb));

        var result = await racingStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(0, result.Value.CreatedAccessCount);
        Assert.Equal(1, result.Value.ExistingAccessCount);
        var response = Assert.Single(result.Value.Respondents);
        Assert.Equal(respondent.Id, response.RespondentSubjectId);
        Assert.Equal("existing", response.AccessStatus);
        Assert.Null(response.Token);
        Assert.Null(response.RespondentPath);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var tokens = await tenantDb.InvitationTokens
            .Where(token =>
                token.CampaignId == campaign.Value.Id &&
                token.Channel == InvitationTokenChannels.IdentifiedQueue)
            .ToListAsync();
        var token = Assert.Single(tokens);
        Assert.Equal(respondent.Id, token.RespondentSubjectId);
        Assert.NotEqual(token.TokenHash, response.Token);
        Assert.False(token.TokenHash.StartsWith("idq_", StringComparison.Ordinal));
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_identified_queue_access_store_fails_closed_for_unsupported_or_unready_campaigns()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue fail-closed study");
        var draftCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Draft identified queue wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var anonymousCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Anonymous queue wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var liveWithoutAssignmentsCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Live identified without assignments wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(draftCampaign.IsSuccess, draftCampaign.Error.ToString());
        Assert.True(anonymousCampaign.IsSuccess, anonymousCampaign.Error.ToString());
        Assert.True(liveWithoutAssignmentsCampaign.IsSuccess, liveWithoutAssignmentsCampaign.Error.ToString());

        var draftAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            draftCampaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);
        var wrongTenantAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            otherTenantId,
            draftCampaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        var anonymousLaunch = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            anonymousCampaign.Value.Id,
            CancellationToken.None);
        var anonymousAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            anonymousCampaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Queue Removed Respondent",
            email: "removed@example.test");
        var audience = new Audience(Guid.NewGuid(), liveWithoutAssignmentsCampaign.Value.Id);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(respondent);
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.Add(new AudienceMember(audience.Id, respondent.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            liveWithoutAssignmentsCampaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var identifiedLaunch = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            liveWithoutAssignmentsCampaign.Value.Id,
            CancellationToken.None);

        await using (var cleanupTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var assignments = await tenantDb.Assignments
                .Where(assignment => assignment.CampaignId == liveWithoutAssignmentsCampaign.Value.Id)
                .ToListAsync();
            tenantDb.Assignments.RemoveRange(assignments);
            await tenantDb.SaveChangesAsync();
            await cleanupTransaction.CommitAsync();
        }

        var noAssignmentAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            liveWithoutAssignmentsCampaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(anonymousLaunch.IsSuccess, anonymousLaunch.Error.ToString());
        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(identifiedLaunch.IsSuccess, identifiedLaunch.Error.ToString());
        Assert.True(draftAccess.IsFailure);
        Assert.Equal("campaign.not_launched", draftAccess.Error.Code);
        Assert.True(wrongTenantAccess.IsFailure);
        Assert.Equal("campaign.not_found", wrongTenantAccess.Error.Code);
        Assert.True(anonymousAccess.IsFailure);
        Assert.Equal("identified_queue.identity_mode_not_supported", anonymousAccess.Error.Code);
        Assert.True(noAssignmentAccess.IsFailure);
        Assert.Equal("identified_queue.assignment_required", noAssignmentAccess.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_launch_materializes_identified_assignments_from_saved_respondent_rules()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified rule launch study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified rule launch wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);

        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Mira Manager",
            email: "mira@example.test",
            externalId: "mgr-001");
        var ana = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Ana Analyst",
            email: "ana@example.test",
            externalId: "emp-001");
        var ivan = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Ivan Intern",
            email: "ivan@example.test",
            externalId: "emp-002");
        var group = new SubjectGroup(
            Guid.NewGuid(),
            tenantId,
            SubjectGroupTypes.Team,
            "Research Team");
        var managerGroup = new SubjectGroup(
            Guid.NewGuid(),
            tenantId,
            SubjectGroupTypes.Team,
            "Managers");
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, ana, ivan);
            tenantDb.SubjectGroups.AddRange(group, managerGroup);
            tenantDb.SubjectMemberships.AddRange(
                new SubjectMembership(ana.Id, group.Id, SubjectGroupRoles.Member),
                new SubjectMembership(ivan.Id, group.Id, SubjectGroupRoles.Member),
                new SubjectMembership(manager.Id, managerGroup.Id, SubjectGroupRoles.Member));
            tenantDb.SubjectRelationships.AddRange(
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    ana.Id,
                    SubjectRelationshipTypes.ManagerOf),
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    ivan.Id,
                    SubjectRelationshipTypes.ManagerOf));
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.AddRange(
                new AudienceMember(audience.Id, ana.Id),
                new AudienceMember(audience.Id, ivan.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}"""),
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"duplicate_self"}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"selected_people","role":"selected_person","subject_ids":["{{ana.Id:D}}","{{ivan.Id:D}}"]}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{ana.Id:D}}","{{ivan.Id:D}}"]}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"reports_of_target","role":"direct_report","target_group_ids":["{{managerGroup.Id:D}}"]}"""),
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"all_in_group","role":"group_member","group_ids":["{{group.Id:D}}"]}""")
            ]),
            CancellationToken.None);

        var launch = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var roster = await setupStore.ListCampaignAssignmentsAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launch.IsSuccess, launch.Error.ToString());
        Assert.True(roster.IsSuccess, roster.Error.ToString());
        Assert.Equal(8, roster.Value.AssignmentCount);
        Assert.DoesNotContain(roster.Value.Assignments, assignment => assignment.Role == "duplicate_self");
        Assert.DoesNotContain(roster.Value.Assignments, assignment => assignment.Role == "selected_person");
        Assert.Contains(
            roster.Value.Assignments,
            assignment =>
                assignment.Role == "self" &&
                assignment.TargetSubjectId == ana.Id &&
                assignment.RespondentSubjectId == ana.Id);
        Assert.Contains(
            roster.Value.Assignments,
            assignment =>
                assignment.Role == "manager" &&
                assignment.TargetSubjectId == ana.Id &&
                assignment.RespondentSubjectId == manager.Id);
        Assert.Contains(
            roster.Value.Assignments,
            assignment =>
                assignment.Role == "manager" &&
                assignment.TargetSubjectId == ivan.Id &&
                assignment.RespondentSubjectId == manager.Id);
        Assert.Contains(
            roster.Value.Assignments,
            assignment =>
                assignment.Role == "direct_report" &&
                assignment.TargetSubjectId == manager.Id &&
                assignment.RespondentSubjectId == ivan.Id);
        Assert.Contains(
            roster.Value.Assignments,
            assignment =>
                assignment.Role == "group_member" &&
                assignment.TargetSubjectId is null &&
                assignment.RespondentSubjectId == ana.Id);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var assignments = await tenantDb.Assignments
            .Where(entity => entity.CampaignId == campaign.Value.Id)
            .OrderBy(entity => entity.Role)
            .ToListAsync();

        Assert.Equal(8, assignments.Count);
        Assert.All(assignments, assignment =>
        {
            Assert.False(assignment.Anonymous);
            Assert.Null(assignment.InviteTokenId);
            Assert.Equal(AssignmentStatuses.Pending, assignment.Status);
        });
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_invitation_batch_store_creates_email_tokens_assignments_notifications_and_outbox_intents()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var outboxBuffer = new OutboxEventBuffer();
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, outboxBuffer);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email invitation consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email invitation wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId,
                DefaultLocale: EmailTemplateLocales.Croatian),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com"),
                new InvitationRecipientRequest("bo@example.com"),
                new InvitationRecipientRequest("cy@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());
        Assert.Equal(campaign.Value.Id, batch.Value.CampaignId);
        Assert.Equal(3, batch.Value.RequestedRecipientCount);
        Assert.Equal(3, batch.Value.CreatedInvitationCount);
        Assert.All(batch.Value.Invitations, invitation =>
        {
            Assert.True(string.IsNullOrEmpty(invitation.Token));
            Assert.True(string.IsNullOrEmpty(invitation.RespondentPath));
            Assert.Equal(NotificationStatuses.Queued, invitation.Status);
        });

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var tokens = await tenantDb.InvitationTokens
            .Where(entity => entity.CampaignId == campaign.Value.Id && entity.Channel == InvitationTokenChannels.Email)
            .OrderBy(entity => entity.Recipient)
            .ToListAsync();
        var assignments = await tenantDb.Assignments
            .Where(entity => entity.CampaignId == campaign.Value.Id && entity.Role == "invited_respondent")
            .OrderBy(entity => entity.Id)
            .ToListAsync();
        var notifications = await tenantDb.Notifications
            .Where(entity => entity.CampaignId == campaign.Value.Id)
            .OrderBy(entity => entity.Recipient)
            .ToListAsync();

        Assert.Equal(["ada@example.com", "bo@example.com", "cy@example.com"], tokens.Select(token => token.Recipient!).ToArray());
        Assert.All(tokens, token =>
        {
            Assert.NotNull(token.Recipient);
            Assert.DoesNotContain("inv_", token.TokenHash, StringComparison.Ordinal);
        });
        Assert.Equal(3, assignments.Count);
        Assert.All(assignments, assignment => Assert.True(assignment.Anonymous));
        Assert.Equal(["ada@example.com", "bo@example.com", "cy@example.com"], notifications.Select(notification => notification.Recipient).ToArray());
        Assert.All(notifications, notification =>
        {
            Assert.Equal(NotificationChannels.Email, notification.Channel);
            Assert.Equal(Notification.InvitationTemplateCode, notification.TemplateCode);
            Assert.Equal(EmailTemplateLocales.Croatian, notification.Locale);
            Assert.Equal(NotificationStatuses.Queued, notification.Status);
        });

        Assert.Equal(3, outboxBuffer.PendingMessages.Count);
        Assert.All(outboxBuffer.PendingMessages, message =>
        {
            Assert.Equal("notification", message.AggregateType);
            Assert.Equal("InvitationEmailQueued", message.EventType);
            Assert.True(
                Encoding.UTF8.GetByteCount(message.Payload.RootElement.GetRawText()) <= OutboxPayload.MaxPayloadBytes,
                "Invitation outbox payload must stay below the M1 payload cap.");
        });
        var outboxPayloads = string.Join(
            Environment.NewLine,
            outboxBuffer.PendingMessages.Select(message => message.Payload.RootElement.GetRawText()));
        Assert.DoesNotContain("ada@example.com", outboxPayloads, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bo@example.com", outboxPayloads, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cy@example.com", outboxPayloads, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", outboxPayloads, StringComparison.Ordinal);
        Assert.DoesNotContain("inv_", outboxPayloads, StringComparison.Ordinal);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Campaign_launch_materializes_email_invitation_notifications_with_subject_locale()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Localized audience invitation study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Localized audience invitation wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId,
                DefaultLocale: EmailTemplateLocales.English),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Hrvoje Respondent",
            email: "hrvoje@example.test",
            locale: EmailTemplateLocales.Croatian);
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(respondent);
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.Add(new AudienceMember(audience.Id, respondent.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notification = await tenantDb.Notifications.SingleAsync(entity => entity.CampaignId == campaign.Value.Id);
        Assert.Equal("hrvoje@example.test", notification.Recipient);
        Assert.Equal(EmailTemplateLocales.Croatian, notification.Locale);
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Email_delivery_processor_sends_queued_invitations_through_local_dev_sink_and_reissues_hashes()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var deliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            new LocalDevEmailDeliveryProvider());

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email delivery proof study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email delivery proof wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com"),
                new InvitationRecipientRequest("bo@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());
        var invitationTokenIds = batch.Value.Invitations
            .Select(invitation => invitation.InvitationTokenId)
            .ToArray();
        Dictionary<Guid, string> oldHashes;
        await using (var tokenSnapshotTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            oldHashes = await tenantDb.InvitationTokens
                .Where(token => invitationTokenIds.Contains(token.Id))
                .ToDictionaryAsync(token => token.Id, token => token.TokenHash);
            await tokenSnapshotTransaction.CommitAsync();
        }

        var processed = await deliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());
        Assert.Equal(campaign.Value.Id, processed.Value.CampaignId);
        Assert.Equal(25, processed.Value.RequestedBatchSize);
        Assert.Equal(2, processed.Value.ProcessedCount);
        Assert.Equal(2, processed.Value.SentCount);
        Assert.Equal(0, processed.Value.FailedCount);
        Assert.All(processed.Value.Deliveries, delivery =>
        {
            Assert.Equal(NotificationStatuses.Sent, delivery.Status);
            Assert.Equal(EmailDeliveryProviderNames.LocalDev, delivery.Provider);
            Assert.Null(delivery.ProviderMessageId);
            Assert.StartsWith("/r/inv_", delivery.RespondentPath, StringComparison.Ordinal);
            Assert.Null(delivery.Error);
        });

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notifications = await tenantDb.Notifications
            .Where(notification => notification.CampaignId == campaign.Value.Id)
            .OrderBy(notification => notification.Recipient)
            .ToListAsync();
        var notificationIds = notifications.Select(notification => notification.Id).ToArray();
        var assignments = await tenantDb.Assignments
            .Where(assignment => assignment.CampaignId == campaign.Value.Id)
            .ToListAsync();
        var tokens = await tenantDb.InvitationTokens
            .Where(token => token.CampaignId == campaign.Value.Id && token.Channel == InvitationTokenChannels.Email)
            .ToListAsync();
        var attempts = await tenantDb.NotificationDeliveryAttempts
            .Where(attempt => notificationIds.Contains(attempt.NotificationId))
            .OrderBy(attempt => attempt.Recipient)
            .ToListAsync();

        Assert.All(notifications, notification =>
        {
            Assert.Equal(NotificationStatuses.Sent, notification.Status);
            Assert.NotNull(notification.SentAt);
            Assert.Null(notification.Error);
        });
        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt =>
        {
            Assert.Equal(NotificationStatuses.Sent, attempt.Status);
            Assert.Equal(EmailDeliveryProviderNames.LocalDev, attempt.Provider);
            Assert.Null(attempt.ProviderMessageId);
            Assert.Null(attempt.Error);
        });

        var tokensByNotification = (
            from notification in notifications
            join assignment in assignments on notification.AssignmentId equals assignment.Id
            let token = tokens
                .Where(token => token.AssignmentId == assignment.Id)
                .OrderByDescending(token => token.CreatedAt)
                .First()
            select new { notification.Id, Token = token })
            .ToDictionary(item => item.Id, item => item.Token);

        foreach (var delivery in processed.Value.Deliveries)
        {
            var rawToken = delivery.RespondentPath!["/r/".Length..];
            var token = tokensByNotification[delivery.NotificationId];

            Assert.Equal(OpenLinkTokens.Hash(rawToken), token.TokenHash);
            Assert.DoesNotContain(token.TokenHash, oldHashes.Values);
        }

        var persistedAttemptText = string.Join(
            Environment.NewLine,
            attempts.Select(attempt =>
                $"{attempt.Provider} {attempt.Status} {attempt.Recipient} {attempt.ProviderMessageId} {attempt.Error}"));
        Assert.DoesNotContain("/r/", persistedAttemptText, StringComparison.Ordinal);
        Assert.DoesNotContain("inv_", persistedAttemptText, StringComparison.Ordinal);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Email_delivery_processor_renders_custom_template_for_notification_locale()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var emailDeliveryProvider = new RecordingEmailDeliveryProvider();
        var deliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            emailDeliveryProvider);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Localized template delivery study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Localized template delivery wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId,
                DefaultLocale: EmailTemplateLocales.Croatian),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());

        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.EmailTemplates.Add(new EmailTemplate(
                Guid.NewGuid(),
                tenantId,
                EmailTemplateCodes.Invitation,
                EmailTemplateLocales.Croatian,
                "Prilagodeni poziv za {{workspace_name}}",
                """
                Ovo je prilagodeni poziv iz radnog prostora {{workspace_name}}.

                Otvorite poziv ovdje:
                {{respondent_link}}

                Ako vise ne zelite primati pozive, odjavite se ovdje:
                {{unsubscribe_link}}

                Hvala.
                """));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var processed = await deliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());
        Assert.Equal(1, processed.Value.SentCount);
        var message = Assert.Single(emailDeliveryProvider.Messages);
        Assert.StartsWith("Prilagodeni poziv za", message.Subject, StringComparison.Ordinal);
        Assert.Contains("https://", message.BodyText, StringComparison.Ordinal);
        Assert.Contains("/r/inv_", message.BodyText, StringComparison.Ordinal);
        Assert.Contains("/unsubscribe", message.BodyText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", message.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", message.BodyText, StringComparison.Ordinal);
        Assert.DoesNotContain("Localized template delivery wave", message.BodyText, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task Email_delivery_processor_fails_invalid_custom_template_before_provider_handoff()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var emailDeliveryProvider = new RecordingEmailDeliveryProvider();
        var deliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            emailDeliveryProvider);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Invalid template delivery study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Invalid template delivery wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());

        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.EmailTemplates.Add(new EmailTemplate(
                Guid.NewGuid(),
                tenantId,
                EmailTemplateCodes.Invitation,
                EmailTemplateLocales.English,
                "Broken invitation",
                """
                This invalid custom invitation is long enough to pass body length checks,
                but it intentionally omits the required respondent and unsubscribe variables.
                """));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var processed = await deliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());
        Assert.Equal(1, processed.Value.FailedCount);
        Assert.Empty(emailDeliveryProvider.Messages);
        var delivery = Assert.Single(processed.Value.Deliveries);
        Assert.Equal(NotificationStatuses.Failed, delivery.Status);
        Assert.Equal("email_template_invalid", delivery.Error);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notification = await tenantDb.Notifications.SingleAsync(entity => entity.CampaignId == campaign.Value.Id);
        var attempt = await tenantDb.NotificationDeliveryAttempts.SingleAsync(entity => entity.NotificationId == notification.Id);
        Assert.Equal(NotificationStatuses.Failed, notification.Status);
        Assert.Equal("email_template_invalid", notification.Error);
        Assert.Equal(NotificationStatuses.Failed, attempt.Status);
        Assert.Equal("email_template_invalid", attempt.Error);
        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Email_delivery_requeues_failed_invitations_for_retry_without_retrying_withdrawal_scrubbed()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var failingDeliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            new FailingEmailDeliveryProvider());
        var localDeliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            new LocalDevEmailDeliveryProvider());

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email retry proof study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email retry proof wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com"),
                new InvitationRecipientRequest("bo@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());

        var failed = await failingDeliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(failed.IsSuccess, failed.Error.ToString());
        Assert.Equal(2, failed.Value.ProcessedCount);
        Assert.Equal(0, failed.Value.SentCount);
        Assert.Equal(2, failed.Value.FailedCount);
        Assert.All(failed.Value.Deliveries, delivery =>
        {
            Assert.Equal(NotificationStatuses.Failed, delivery.Status);
            Assert.Equal(EmailDeliveryFailureClassifier.AzureCommunicationEmailUnknown, delivery.Error);
            Assert.Null(delivery.RespondentPath);
            Assert.Null(delivery.ProviderMessageId);
        });

        await using (var scrubTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var scrubbed = await tenantDb.Notifications.SingleAsync(
                notification => notification.CampaignId == campaign.Value.Id &&
                    notification.Recipient == "bo@example.com");
            scrubbed.ScrubForWithdrawal(DateTimeOffset.Parse("2026-05-18T12:00:00+00:00"));

            await tenantDb.SaveChangesAsync();
            await scrubTransaction.CommitAsync();
        }

        var requeued = await failingDeliveryStore.RequeueFailedCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new RequeueFailedCampaignEmailDeliveriesRequest(
                BatchSize: 25,
                ConfirmedAnotherEmailAppropriate: true,
                ConfirmedNoPriorDelivery: true),
            CancellationToken.None);

        Assert.True(requeued.IsSuccess, requeued.Error.ToString());
        Assert.Equal(campaign.Value.Id, requeued.Value.CampaignId);
        Assert.Equal(25, requeued.Value.RequestedBatchSize);
        Assert.Equal(1, requeued.Value.RequeuedCount);

        var delivered = await localDeliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(delivered.IsSuccess, delivered.Error.ToString());
        Assert.Equal(1, delivered.Value.ProcessedCount);
        Assert.Equal(1, delivered.Value.SentCount);
        Assert.Equal(0, delivered.Value.FailedCount);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notifications = await tenantDb.Notifications
            .Where(notification => notification.CampaignId == campaign.Value.Id)
            .OrderBy(notification => notification.Recipient)
            .ToListAsync();
        var notificationIds = notifications.Select(notification => notification.Id).ToArray();
        var attempts = await tenantDb.NotificationDeliveryAttempts
            .Where(attempt => notificationIds.Contains(attempt.NotificationId))
            .OrderBy(attempt => attempt.CreatedAt)
            .ToListAsync();

        var sent = Assert.Single(notifications, notification => notification.Recipient == "ada@example.com");
        Assert.Equal(NotificationStatuses.Sent, sent.Status);
        Assert.Null(sent.Error);
        Assert.NotNull(sent.SentAt);

        var scrubbedNotification = Assert.Single(
            notifications,
            notification => notification.Recipient == "withdrawn@example.invalid");
        Assert.Equal(NotificationStatuses.Failed, scrubbedNotification.Status);
        Assert.Equal("withdrawal_scrubbed", scrubbedNotification.Error);
        Assert.Null(scrubbedNotification.SentAt);

        Assert.Equal(3, attempts.Count);
        Assert.Equal(2, attempts.Count(attempt => attempt.Status == NotificationStatuses.Failed));
        Assert.Equal(1, attempts.Count(attempt => attempt.Status == NotificationStatuses.Sent));

        var persistedAttemptText = string.Join(
            Environment.NewLine,
            attempts.Select(attempt =>
                $"{attempt.Provider} {attempt.Status} {attempt.Recipient} {attempt.ProviderMessageId} {attempt.Error}"));
        Assert.DoesNotContain("/r/", persistedAttemptText, StringComparison.Ordinal);
        Assert.DoesNotContain("inv_", persistedAttemptText, StringComparison.Ordinal);
        Assert.DoesNotContain("acs-access-key", persistedAttemptText, StringComparison.OrdinalIgnoreCase);

        await verificationTransaction.CommitAsync();
    }

    private sealed class FailingEmailDeliveryProvider : IEmailDeliveryProvider
    {
        public string Provider => EmailDeliveryProviderNames.AzureCommunicationEmail;

        public Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryMessage message,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "validatedscale.communication.azure.com acs-access-key inv_secret /r/inv_secret ada@example.com");
        }
    }

    private sealed class RecordingEmailDeliveryProvider : IEmailDeliveryProvider
    {
        public string Provider => EmailDeliveryProviderNames.LocalDev;

        public List<EmailDeliveryMessage> Messages { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryMessage message,
            CancellationToken cancellationToken)
        {
            Messages.Add(message);

            return Task.FromResult(new EmailDeliveryResult(
                Provider,
                ProviderMessageId: null,
                DateTimeOffset.UtcNow));
        }
    }

    [DockerFact]
    public async Task Acs_provider_events_resolve_delivery_attempt_by_provider_message_id_idempotently()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var deliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            new FixedProviderMessageEmailDeliveryProvider(
                EmailDeliveryProviderNames.AzureCommunicationEmail,
                "acs-message-123"));

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "ACS provider event proof study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "ACS provider event proof wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var batch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(batch.IsSuccess, batch.Error.ToString());

        var processed = await deliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);

        Assert.True(processed.IsSuccess, processed.Error.ToString());
        var proof = Assert.Single(processed.Value.Deliveries);
        Assert.Equal(EmailDeliveryProviderNames.AzureCommunicationEmail, proof.Provider);
        Assert.Equal("acs-message-123", proof.ProviderMessageId);

        var delivered = await deliveryStore.RecordProviderDeliveryEventByProviderMessageIdAsync(
            new RecordProviderDeliveryEventByProviderMessageIdRequest(
                EmailDeliveryProviderNames.AzureCommunicationEmail,
                "acs-message-123",
                NotificationDeliveryEventTypes.Delivered,
                DateTimeOffset.UtcNow,
                "event-grid-delivered-1",
                "acs_email:delivered"),
            CancellationToken.None);
        var duplicateDelivered = await deliveryStore.RecordProviderDeliveryEventByProviderMessageIdAsync(
            new RecordProviderDeliveryEventByProviderMessageIdRequest(
                EmailDeliveryProviderNames.AzureCommunicationEmail,
                "acs-message-123",
                NotificationDeliveryEventTypes.Delivered,
                DateTimeOffset.UtcNow,
                "event-grid-delivered-1",
                "acs_email:delivered"),
            CancellationToken.None);

        Assert.True(delivered.IsSuccess, delivered.Error.ToString());
        Assert.False(delivered.Value.DuplicateEvent);
        Assert.True(duplicateDelivered.IsSuccess, duplicateDelivered.Error.ToString());
        Assert.True(duplicateDelivered.Value.DuplicateEvent);

        await using var verificationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notification = await tenantDb.Notifications.SingleAsync(entity => entity.Id == proof.NotificationId);
        var attempt = await tenantDb.NotificationDeliveryAttempts.SingleAsync(
            entity => entity.ProviderMessageId == "acs-message-123");
        var deliveryEvent = await tenantDb.NotificationDeliveryEvents.SingleAsync(
            entity => entity.ProviderMessageId == "acs-message-123");

        Assert.Equal(NotificationStatuses.Sent, notification.Status);
        Assert.Equal(NotificationStatuses.Sent, attempt.Status);
        Assert.Equal(NotificationDeliveryEventTypes.Delivered, deliveryEvent.EventType);
        Assert.Equal(EmailDeliveryProviderNames.AzureCommunicationEmail, deliveryEvent.Provider);
        Assert.NotNull(deliveryEvent.ProviderEventId);
        Assert.NotEqual("event-grid-delivered-1", deliveryEvent.ProviderEventId);

        await verificationTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Notification_delivery_attempt_provider_message_id_is_unique_per_provider_when_present()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedNotificationFixtureAsync(migratorOptions, tenantId, "acs-unique");

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            tenantId,
            fixture.NotificationId,
            EmailDeliveryProviderNames.AzureCommunicationEmail,
            "ada@example.com",
            "acs-message-unique",
            DateTimeOffset.UtcNow));
        tenantDb.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            tenantId,
            fixture.NotificationId,
            EmailDeliveryProviderNames.AzureCommunicationEmail,
            "bo@example.com",
            "acs-message-unique",
            DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantDb.SaveChangesAsync());
    }

    private sealed class FixedProviderMessageEmailDeliveryProvider(
        string provider,
        string providerMessageId) : IEmailDeliveryProvider
    {
        public string Provider => provider;

        public Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryMessage message,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailDeliveryResult(
                provider,
                providerMessageId,
                DateTimeOffset.UtcNow));
        }
    }

    [DockerFact]
    public async Task Campaign_invitation_batch_store_rejects_duplicate_recipients_and_unsupported_identity_modes()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email invitation guard study");
        var anonymousCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email invitation guard wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var anonymousLaunched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            anonymousCampaign.Value.Id,
            CancellationToken.None);
        var firstBatch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            anonymousCampaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]),
            CancellationToken.None);

        var duplicateBatch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            anonymousCampaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("  ADA@example.com  ")
            ]),
            CancellationToken.None);

        var identifiedCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email invitation identified wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(identifiedCampaign.IsSuccess, identifiedCampaign.Error.ToString());

        await using (var launchTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var savedIdentifiedCampaign = await tenantDb.Campaigns
                .SingleAsync(entity => entity.Id == identifiedCampaign.Value.Id);
            var launchedAt = DateTimeOffset.Parse("2026-05-18T09:00:00+00:00");
            savedIdentifiedCampaign.Launch(launchedAt);
            tenantDb.CampaignLaunchSnapshots.Add(new CampaignLaunchSnapshot(
                Guid.NewGuid(),
                tenantId,
                identifiedCampaign.Value.Id,
                seriesId,
                versionId,
                scoringRule.Value.Id,
                ResponseIdentityModes.Identified,
                "en",
                templateQuestionCount: 1,
                scoringRuleDocumentHash: "test-document-hash",
                launchReadiness: """{"ready":true,"blockers":[]}""",
                launchedAt: launchedAt,
                launchPacket: """{"ready":true,"blockers":[]}"""));
            await tenantDb.SaveChangesAsync();
            await launchTransaction.CommitAsync();
        }

        var unsupportedBatch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            identifiedCampaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("bo@example.com")
            ]),
            CancellationToken.None);

        Assert.True(anonymousCampaign.IsSuccess);
        Assert.True(anonymousLaunched.IsSuccess, anonymousLaunched.Error.ToString());
        Assert.True(firstBatch.IsSuccess, firstBatch.Error.ToString());
        Assert.True(duplicateBatch.IsFailure);
        Assert.Equal("invitation_batch.recipient_already_exists", duplicateBatch.Error.Code);
        Assert.True(unsupportedBatch.IsFailure);
        Assert.Equal("invitation_batch.identity_mode_not_supported", unsupportedBatch.Error.Code);
    }

    [DockerFact]
    public async Task Campaign_open_link_store_rejects_unlaunched_and_allows_anonymous_longitudinal_identity_mode()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Open link identity study");
        var draftCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Draft open link wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var longitudinalCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Longitudinal open link wave",
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            longitudinalCampaign.Value.Id,
            CancellationToken.None);

        var draftResult = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            draftCampaign.Value.Id,
            CancellationToken.None);
        var longitudinalResult = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            longitudinalCampaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(draftCampaign.IsSuccess);
        Assert.True(longitudinalCampaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(draftResult.IsFailure);
        Assert.Equal("campaign.not_launched", draftResult.Error.Code);
        Assert.True(longitudinalResult.IsSuccess, longitudinalResult.Error.ToString());
        Assert.Equal(longitudinalCampaign.Value.Id, longitudinalResult.Value.CampaignId);
        Assert.StartsWith("opn_", longitudinalResult.Value.Token, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task Open_link_allows_multiple_sessions_for_the_same_assignment()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Reusable open link consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Reusable open link wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.ResponseSessions.Add(new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            openLink.Value.AssignmentId,
            "en"));
        tenantDb.ResponseSessions.Add(new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            openLink.Value.AssignmentId,
            "en"));

        await tenantDb.SaveChangesAsync();

        Assert.Equal(
            2,
            await tenantDb.ResponseSessions.CountAsync(
                session => session.AssignmentId == openLink.Value.AssignmentId));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_longitudinal_open_link_entry_requires_participant_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Longitudinal public entry study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Longitudinal public entry wave",
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.Equal(ResponseIdentityModes.AnonymousLongitudinal, entry.Value.ResponseIdentityMode);
        Assert.True(entry.Value.RequiresParticipantCode);
    }

    [DockerFact]
    public async Task Anonymous_longitudinal_open_link_session_requires_participant_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var participantCodeStore = new ParticipantCodeStore(
            tenantDb,
            tenantDbScope,
            new DeterministicParticipantCodeHasher());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope, participantCodeStore);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Longitudinal missing code study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Longitudinal missing code wave",
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsFailure);
        Assert.Equal("participant_code.required", session.Error.Code);
    }

    [DockerFact]
    public async Task Anonymous_open_link_session_rejects_participant_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Anonymous code rejection study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Anonymous code rejection wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants,
                ParticipantCode: "alpha-001"),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsFailure);
        Assert.Equal("participant_code.not_allowed", session.Error.Code);
    }

    [DockerFact]
    public async Task Anonymous_longitudinal_open_link_session_stores_participant_code_id_without_raw_code()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var participantCodeStore = new ParticipantCodeStore(
            tenantDb,
            tenantDbScope,
            new DeterministicParticipantCodeHasher());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope, participantCodeStore);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Longitudinal participant code study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Longitudinal participant code wave",
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants,
                ParticipantCode: "  Alpha 001  "),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsSuccess, session.Error.ToString());

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Value.Id);
        Assert.NotNull(storedSession.ParticipantCodeId);
        var storedCode = await tenantDb.ParticipantCodes.SingleAsync(entity => entity.Id == storedSession.ParticipantCodeId);
        Assert.Equal(seriesId, storedCode.CampaignSeriesId);
        Assert.Equal(32, storedCode.Hash.Length);
        Assert.DoesNotContain(
            typeof(ParticipantCode).GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_longitudinal_open_link_rejects_second_submitted_response_for_same_code()
    {
        await using var scenario = await CreatePublicOpenLinkScenarioAsync(
            Guid.NewGuid(),
            ResponseIdentityModes.AnonymousLongitudinal,
            "Longitudinal duplicate session");
        var questionId = scenario.Entry.Questions.Single().Id;

        var firstSession = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry, "alpha-001"),
            CancellationToken.None);
        var firstSaved = await scenario.ResponseStore.SaveOpenLinkAnswersAsync(
            scenario.Token,
            firstSession.Value.Id,
            new SaveAnswersRequest([new SaveAnswerRequest(questionId, "4")]),
            CancellationToken.None);
        var firstSubmit = await scenario.ResponseStore.SubmitOpenLinkSessionAsync(
            scenario.Token,
            firstSession.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200),
            CancellationToken.None);

        var secondSession = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry, "alpha-001"),
            CancellationToken.None);

        Assert.True(firstSession.IsSuccess, firstSession.Error.ToString());
        Assert.True(firstSaved.IsSuccess, firstSaved.Error.ToString());
        Assert.True(firstSubmit.IsSuccess, firstSubmit.Error.ToString());

        await using (var scoreTransaction = await scenario.TenantDbScope.BeginTransactionAsync(scenario.TenantId))
        {
            Assert.Equal(1, await scenario.Db.ScoreRuns.CountAsync(run => run.ResponseSessionId == firstSession.Value.Id));
            Assert.Equal(1, await scenario.Db.Scores.CountAsync(score => score.ResponseSessionId == firstSession.Value.Id));
            await scoreTransaction.CommitAsync();
        }

        Assert.True(secondSession.IsFailure);
        Assert.Equal("participant_code.already_submitted", secondSession.Error.Code);
    }

    [DockerFact]
    public async Task Anonymous_longitudinal_submit_rechecks_duplicate_participant_code()
    {
        await using var scenario = await CreatePublicOpenLinkScenarioAsync(
            Guid.NewGuid(),
            ResponseIdentityModes.AnonymousLongitudinal,
            "Longitudinal duplicate submit");
        var questionId = scenario.Entry.Questions.Single().Id;

        var firstSession = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry, "alpha-001"),
            CancellationToken.None);
        var secondSession = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry, "alpha-001"),
            CancellationToken.None);
        var firstSaved = await scenario.ResponseStore.SaveOpenLinkAnswersAsync(
            scenario.Token,
            firstSession.Value.Id,
            new SaveAnswersRequest([new SaveAnswerRequest(questionId, "4")]),
            CancellationToken.None);
        var firstSubmit = await scenario.ResponseStore.SubmitOpenLinkSessionAsync(
            scenario.Token,
            firstSession.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200),
            CancellationToken.None);
        var secondSaved = await scenario.ResponseStore.SaveOpenLinkAnswersAsync(
            scenario.Token,
            secondSession.Value.Id,
            new SaveAnswersRequest([new SaveAnswerRequest(questionId, "5")]),
            CancellationToken.None);

        var secondSubmit = await scenario.ResponseStore.SubmitOpenLinkSessionAsync(
            scenario.Token,
            secondSession.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 1300),
            CancellationToken.None);

        Assert.True(firstSession.IsSuccess, firstSession.Error.ToString());
        Assert.True(secondSession.IsSuccess, secondSession.Error.ToString());
        Assert.True(firstSaved.IsSuccess, firstSaved.Error.ToString());
        Assert.True(firstSubmit.IsSuccess, firstSubmit.Error.ToString());
        Assert.True(secondSaved.IsSuccess, secondSaved.Error.ToString());
        Assert.True(secondSubmit.IsFailure);
        Assert.Equal("participant_code.already_submitted", secondSubmit.Error.Code);
    }

    [DockerFact]
    public async Task Two_wave_proof_store_returns_not_found_for_missing_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var migratorDb = new ApplicationDbContext(migratorOptions))
        {
            await migratorDb.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new CampaignSeriesProofStore(tenantDb, new TenantDbScope(tenantDb));

        var proof = await store.GetTwoWaveProofAsync(
            tenantId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(proof.IsFailure);
        Assert.Equal("campaign_series.not_found", proof.Error.Code);
    }

    [DockerFact]
    public async Task Two_wave_proof_store_counts_launched_anonymous_longitudinal_waves()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Two-wave launched proof");

        await using (scenario)
        {
            var proof = await scenario.ProofStore.GetTwoWaveProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            Assert.Equal("not_ready", proof.Value.ProofStatus);
            Assert.Equal(2, proof.Value.ExpectedWaveCount);
            Assert.Equal(2, proof.Value.LaunchedWaveCount);
            Assert.Equal(0, proof.Value.SubmittedWaveCount);
            Assert.Equal(0, proof.Value.LinkedTrajectoryCount);
            Assert.Equal(0, proof.Value.CompleteTrajectoryCount);
            Assert.Equal(2, proof.Value.Waves.Count);
            Assert.All(proof.Value.Waves, wave =>
                Assert.Equal(ResponseIdentityModes.AnonymousLongitudinal, wave.ResponseIdentityMode));
        }
    }

    [DockerFact]
    public async Task Two_wave_proof_store_counts_complete_trajectory_for_same_participant_code()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Same-code proof");

        await using (scenario)
        {
            await SubmitTwoWaveProofResponseAsync(scenario, scenario.Wave1, "alpha-001", "4");
            await SubmitTwoWaveProofResponseAsync(scenario, scenario.Wave2, "alpha-001", "5");

            var proof = await scenario.ProofStore.GetTwoWaveProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            Assert.Equal("ready", proof.Value.ProofStatus);
            Assert.Equal(2, proof.Value.SubmittedWaveCount);
            Assert.Equal(1, proof.Value.LinkedTrajectoryCount);
            Assert.Equal(1, proof.Value.CompleteTrajectoryCount);
            Assert.DoesNotContain("alpha-001", JsonSerializer.Serialize(proof.Value), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("participantCodeId", JsonSerializer.Serialize(proof.Value), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hash", JsonSerializer.Serialize(proof.Value), StringComparison.OrdinalIgnoreCase);
        }
    }

    [DockerFact]
    public async Task Two_wave_proof_store_does_not_count_complete_trajectory_for_different_codes()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Different-code proof");

        await using (scenario)
        {
            await SubmitTwoWaveProofResponseAsync(scenario, scenario.Wave1, "alpha-001", "4");
            await SubmitTwoWaveProofResponseAsync(scenario, scenario.Wave2, "beta-002", "5");

            var proof = await scenario.ProofStore.GetTwoWaveProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            Assert.Equal("not_ready", proof.Value.ProofStatus);
            Assert.Equal(2, proof.Value.SubmittedWaveCount);
            Assert.Equal(2, proof.Value.LinkedTrajectoryCount);
            Assert.Equal(0, proof.Value.CompleteTrajectoryCount);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_returns_not_ready_before_two_launched_waves()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var proofStore = new WaveComparisonProofStore(tenantDb, tenantDbScope);
        var series = await setupStore.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Two-wave comparison proof"),
            CancellationToken.None);
        Assert.True(series.IsSuccess, series.Error.ToString());

        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Wave 1",
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var proof = await proofStore.GetCampaignSeriesWaveComparisonProofAsync(
            tenantId,
            series.Value.Id,
            CancellationToken.None);

        Assert.True(proof.IsSuccess, proof.Error.ToString());
        Assert.Equal("not_ready", proof.Value.ProofStatus);
        Assert.Equal("not_validated_interpretation", proof.Value.InterpretationStatus);
        Assert.Null(proof.Value.BaselineWave);
        Assert.Null(proof.Value.ComparisonWave);
        Assert.Null(proof.Value.DisclosurePolicy);
        Assert.Empty(proof.Value.Scores);
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_returns_visible_aggregate_and_paired_deltas()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison deltas");

        await using (scenario)
        {
            var participantCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "echo-005" };
            var baselineValues = new[] { "3", "3", "4", "4", "5" };
            var comparisonValues = new[] { "4", "4", "5", "5", "5" };
            for (var index = 0; index < participantCodes.Length; index++)
            {
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave1,
                    participantCodes[index],
                    baselineValues[index]);
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave2,
                    participantCodes[index],
                    comparisonValues[index]);
            }

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            Assert.Equal("ready", proof.Value.ProofStatus);
            Assert.Equal("not_validated_interpretation", proof.Value.InterpretationStatus);
            Assert.NotNull(proof.Value.BaselineWave);
            Assert.NotNull(proof.Value.ComparisonWave);
            Assert.Equal(5, proof.Value.BaselineWave.SubmittedResponseCount);
            Assert.Equal(5, proof.Value.ComparisonWave.SubmittedResponseCount);
            Assert.Equal(5, proof.Value.DisclosurePolicy?.KMin);
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("total", score.DimensionCode);
            Assert.Equal("visible", score.Disclosure);
            Assert.Equal("compatible", score.CompatibilityStatus);
            Assert.Equal(5, score.BaselineSubmittedResponseCount);
            Assert.Equal(5, score.ComparisonSubmittedResponseCount);
            Assert.Equal(5, score.LinkedPairCount);
            Assert.Equal(5, score.BaselineScoreCount);
            Assert.Equal(5, score.ComparisonScoreCount);
            Assert.Equal(5, score.BaselineNValidTotal);
            Assert.Equal(5, score.BaselineNExpectedTotal);
            Assert.Equal("ok", score.BaselineMissingPolicyStatusSummary);
            Assert.Equal(5, score.ComparisonNValidTotal);
            Assert.Equal(5, score.ComparisonNExpectedTotal);
            Assert.Equal("ok", score.ComparisonMissingPolicyStatusSummary);
            Assert.Equal(3.8m, score.BaselineMean);
            Assert.Equal(4.6m, score.ComparisonMean);
            Assert.Equal(0.8m, score.AggregateDelta);
            Assert.Equal(0.8m, score.PairedDeltaMean);
            Assert.Null(score.SuppressionReason);
            Assert.Null(score.CompatibilityReason);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_returns_tenant_attested_interpretation_for_visible_scores()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison interpretation",
            produces: TenantAttestedScoreInterpretationProduces);

        await using (scenario)
        {
            var participantCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "echo-005" };
            var baselineValues = new[] { "3", "3", "4", "4", "5" };
            var comparisonValues = new[] { "4", "4", "5", "5", "5" };
            for (var index = 0; index < participantCodes.Length; index++)
            {
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave1,
                    participantCodes[index],
                    baselineValues[index]);
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave2,
                    participantCodes[index],
                    comparisonValues[index]);
            }

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("visible", score.Disclosure);
            Assert.Equal(3.8m, score.BaselineMean);
            Assert.Equal(4.6m, score.ComparisonMean);
            Assert.NotNull(score.BaselineInterpretation);
            Assert.NotNull(score.ComparisonInterpretation);
            var baselineInterpretation = score.BaselineInterpretation;
            var comparisonInterpretation = score.ComparisonInterpretation;
            Assert.Equal("higher", baselineInterpretation.BandCode);
            Assert.Equal("higher", comparisonInterpretation.BandCode);
            Assert.Equal("Tenant higher range", baselineInterpretation.Label);
            Assert.False(baselineInterpretation.IsValidated);
            Assert.False(comparisonInterpretation.IsOfficial);

            var baselineWaveJson = System.Text.Json.JsonSerializer.Serialize(
                proof.Value.BaselineWave,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            Assert.Contains("\"launchPacket\"", baselineWaveJson);
            Assert.Contains("\"schemaVersion\":1", baselineWaveJson);
            Assert.Contains("scoring", baselineWaveJson);
            Assert.Contains("policies", baselineWaveJson);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_returns_score_output_method_metadata()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison output metadata",
            produces:
            """
            {
              "scores": ["total"],
              "outputs": [
                {
                  "code": "total",
                  "label": "Recovery readiness index",
                  "calculation": "normalized_weighted_mean_0_100",
                  "calculation_label": "Normalized 0-100 weighted average",
                  "score_range": { "min": 0, "max": 100 }
                }
              ]
            }
            """);

        await using (scenario)
        {
            var participantCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "echo-005" };
            for (var index = 0; index < participantCodes.Length; index++)
            {
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave1,
                    participantCodes[index],
                    value: "3");
                await SubmitAndScoreTwoWaveProofResponseAsync(
                    scenario,
                    scenario.Wave2,
                    participantCodes[index],
                    value: "4");
            }

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("Recovery readiness index", score.DisplayLabel);
            Assert.Equal("normalized_weighted_mean_0_100", score.BaselineCalculation);
            Assert.Equal("normalized_weighted_mean_0_100", score.ComparisonCalculation);
            Assert.Equal("Normalized 0-100 weighted average", score.BaselineCalculationLabel);
            Assert.Equal("Normalized 0-100 weighted average", score.ComparisonCalculationLabel);
            Assert.Equal(0m, score.BaselineScoreRangeMin);
            Assert.Equal(100m, score.BaselineScoreRangeMax);
            Assert.Equal(0m, score.ComparisonScoreRangeMin);
            Assert.Equal(100m, score.ComparisonScoreRangeMax);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_suppresses_values_when_linked_pairs_are_below_k_min()
    {
        var scenario = await CreateTwoWaveProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison suppression",
            produces: TenantAttestedScoreInterpretationProduces);

        await using (scenario)
        {
            var baselineCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "echo-005" };
            var comparisonCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "foxtrot-006" };
            for (var index = 0; index < baselineCodes.Length; index++)
            {
                await SubmitAndScoreTwoWaveProofResponseAsync(scenario, scenario.Wave1, baselineCodes[index], "4");
                await SubmitAndScoreTwoWaveProofResponseAsync(scenario, scenario.Wave2, comparisonCodes[index], "5");
            }

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("suppressed", score.Disclosure);
            Assert.Equal(5, score.BaselineSubmittedResponseCount);
            Assert.Equal(5, score.ComparisonSubmittedResponseCount);
            Assert.Equal(4, score.LinkedPairCount);
            Assert.Null(score.BaselineScoreCount);
            Assert.Null(score.ComparisonScoreCount);
            Assert.Null(score.BaselineNValidTotal);
            Assert.Null(score.BaselineNExpectedTotal);
            Assert.Null(score.BaselineMissingPolicyStatusSummary);
            Assert.Null(score.ComparisonNValidTotal);
            Assert.Null(score.ComparisonNExpectedTotal);
            Assert.Null(score.ComparisonMissingPolicyStatusSummary);
            Assert.Null(score.BaselineMean);
            Assert.Null(score.ComparisonMean);
            Assert.Null(score.AggregateDelta);
            Assert.Null(score.PairedDeltaMean);
            Assert.Equal("insufficient_responses", score.SuppressionReason);
            Assert.Null(score.BaselineInterpretation);
            Assert.Null(score.ComparisonInterpretation);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_blocks_cross_tenant_series_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var scenario = await CreateTwoWaveProofScenarioAsync(
            tenantA,
            "Wave comparison cross tenant");

        await using (scenario)
        await using (var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantBDbScope = new TenantDbScope(tenantBDb);
            var proofStore = new WaveComparisonProofStore(tenantBDb, tenantBDbScope);

            var proof = await proofStore.GetCampaignSeriesWaveComparisonProofAsync(
                tenantB,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsFailure);
            Assert.Equal("campaign_series.not_found", proof.Error.Code);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_blocks_delta_without_mixed_version_compatibility()
    {
        var scenario = await CreateTwoWaveMixedScoringProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison missing compatibility",
            comparisonCompatibility: "{}");

        await using (scenario)
        {
            await SubmitFiveLinkedScoredWaveComparisonResponsesAsync(scenario);

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            Assert.Equal("1.0.0", proof.Value.BaselineWave?.ScoringRuleVersion);
            Assert.Equal("2.0.0", proof.Value.ComparisonWave?.ScoringRuleVersion);
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("compatibility_missing", score.CompatibilityStatus);
            Assert.Equal("visible", score.Disclosure);
            Assert.NotNull(score.BaselineMean);
            Assert.NotNull(score.ComparisonMean);
            Assert.Null(score.AggregateDelta);
            Assert.Null(score.PairedDeltaMean);
            Assert.Contains("No mixed-version", score.CompatibilityReason);
        }
    }

    [DockerFact]
    public async Task Wave_comparison_proof_store_allows_descriptive_only_delta_with_metadata()
    {
        var scenario = await CreateTwoWaveMixedScoringProofScenarioAsync(
            Guid.NewGuid(),
            "Wave comparison descriptive compatibility",
            """
            {
              "descriptive_only_with": [
                {
                  "rule_id": "burnout.total",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": ["total"],
                  "rationale": "Formula changed; display side-by-side only."
                }
              ]
            }
            """);

        await using (scenario)
        {
            await SubmitFiveLinkedScoredWaveComparisonResponsesAsync(scenario);

            var proof = await scenario.WaveComparisonProofStore.GetCampaignSeriesWaveComparisonProofAsync(
                scenario.TenantId,
                scenario.SeriesId,
                CancellationToken.None);

            Assert.True(proof.IsSuccess, proof.Error.ToString());
            var score = Assert.Single(proof.Value.Scores);
            Assert.Equal("descriptive_only", score.CompatibilityStatus);
            Assert.Equal("visible", score.Disclosure);
            Assert.Equal(0.8m, score.AggregateDelta);
            Assert.Equal(0.8m, score.PairedDeltaMean);
            Assert.Contains("Formula changed", score.CompatibilityReason);
        }
    }

    [DockerFact]
    public async Task Open_link_response_store_sets_token_tenant_context_for_audited_public_writes()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        CampaignOpenLinkResponse openLink;
        OpenLinkEntryResponse entry;

        await using (var setupDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var setupTenantDbScope = new TenantDbScope(setupDb);
            var setupStore = new SetupWorkflowStore(setupDb, setupTenantDbScope);
            var setupResponseStore = new ResponseCaptureStore(setupDb, setupTenantDbScope);

            var scoringRule = await setupStore.CreateScoringRuleAsync(
                tenantId,
                new CreateScoringRuleRequest(
                    versionId,
                    "burnout.total",
                    "1.0.0",
                    "scoring-rule/v1",
                    "engine/v1",
                    """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                    """{"scores":["total"]}"""),
                CancellationToken.None);
            var seriesId = await CreateSetupCampaignSeriesAsync(
                setupStore,
                tenantId,
                "Public audited open link study");
            var campaign = await setupStore.CreateCampaignAsync(
                tenantId,
                actorId: null,
                new CreateCampaignRequest(
                    versionId,
                    "Public audited open link wave",
                    ResponseIdentityModes.Anonymous,
                    CampaignSeriesId: seriesId),
                CancellationToken.None);
            var launched = await setupStore.LaunchCampaignAsync(
                tenantId,
                actorId: null,
                campaign.Value.Id,
                CancellationToken.None);
            var openLinkResult = await setupStore.CreateCampaignOpenLinkAsync(
                tenantId,
                campaign.Value.Id,
                CancellationToken.None);
            var entryResult = await setupResponseStore.GetOpenLinkEntryAsync(
                openLinkResult.Value.Token,
                CancellationToken.None);

            Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
            Assert.True(campaign.IsSuccess, campaign.Error.ToString());
            Assert.True(launched.IsSuccess, launched.Error.ToString());
            Assert.True(openLinkResult.IsSuccess, openLinkResult.Error.ToString());
            Assert.True(entryResult.IsSuccess, entryResult.Error.ToString());

            openLink = openLinkResult.Value;
            entry = entryResult.Value;
        }

        var currentTenant = new CurrentTenant();
        var currentAuditContext = new CurrentAuditContext();
        var outboxBuffer = new OutboxEventBuffer();
        var auditedOptions = CreateRuntimeOptions(
            new OutboxSaveChangesInterceptor(currentTenant, currentAuditContext, outboxBuffer),
            new AuditSaveChangesInterceptor(currentTenant, currentAuditContext));

        await using var auditedDb = new ApplicationDbContext(auditedOptions);
        var auditedTenantDbScope = new TenantDbScope(auditedDb);
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<ICurrentTenant>(currentTenant)
            .BuildServiceProvider();
        var responseStore = ActivatorUtilities.CreateInstance<ResponseCaptureStore>(
            serviceProvider,
            auditedDb,
            auditedTenantDbScope);

        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.ConsentDocument.Id,
                entry.ConsentDocument.RequiredGrants),
            CancellationToken.None);

        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(tenantId, currentTenant.TenantId);
        Assert.Equal("open_link_token", currentTenant.Source);

        await using var transaction = await auditedTenantDbScope.BeginTransactionAsync(tenantId);
        var auditedEntityTypes = await auditedDb.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.TenantId == tenantId)
            .Select(auditEvent => auditEvent.EntityType)
            .ToListAsync();

        Assert.Contains(nameof(ConsentRecord), auditedEntityTypes);
        Assert.Contains(nameof(ResponseSession), auditedEntityTypes);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Open_link_response_store_resolves_token_and_submits_without_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Public open link consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Public open link wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);
        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);
        var saved = await responseStore.SaveOpenLinkAnswersAsync(
            openLink.Value.Token,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(entry.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitOpenLinkSessionAsync(
            openLink.Value.Token,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        Assert.Equal(openLink.Value.AssignmentId, entry.Value.AssignmentId);
        Assert.Equal("Default participant disclosure", entry.Value.ConsentDocument.Title);
        Assert.Equal(1, saved.Value.SavedAnswerCount);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Value.Id);
        Assert.NotNull(storedSession.SubmittedAt);
        Assert.NotNull(storedSession.ConsentRecordId);
        Assert.Equal(openLink.Value.AssignmentId, storedSession.AssignmentId);
        var storedConsent = await tenantDb.ConsentRecords.SingleAsync(entity => entity.Id == storedSession.ConsentRecordId);
        Assert.Equal(entry.Value.ConsentDocument.Id, storedConsent.ConsentDocumentId);
        Assert.Contains("research_participation", storedConsent.AcceptedGrants);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Open_link_session_draft_returns_saved_answers_for_token_bound_session()
    {
        await using var scenario = await CreatePublicOpenLinkScenarioAsync(
            Guid.NewGuid(),
            ResponseIdentityModes.Anonymous,
            "Public draft resume");
        var questionId = scenario.Entry.Questions.Single().Id;

        var session = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry),
            CancellationToken.None);
        var saved = await scenario.ResponseStore.SaveOpenLinkAnswersAsync(
            scenario.Token,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(questionId, "4", Comment: "steady")
            ]),
            CancellationToken.None);

        var draft = await scenario.ResponseStore.GetOpenLinkSessionDraftAsync(
            scenario.Token,
            session.Value.Id,
            CancellationToken.None);

        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(draft.IsSuccess, draft.Error.ToString());
        Assert.Equal(session.Value.Id, draft.Value.Session.Id);
        Assert.Equal(scenario.Entry.AssignmentId, draft.Value.Session.AssignmentId);
        Assert.Null(draft.Value.Session.SubmittedAt);
        Assert.Null(draft.Value.Session.TimeTakenMs);
        Assert.Equal(1, draft.Value.SavedAnswerCount);
        var answer = Assert.Single(draft.Value.Answers);
        Assert.Equal(questionId, answer.QuestionId);
        Assert.Equal("4", answer.Value);
        Assert.Equal("steady", answer.Comment);
        Assert.False(answer.IsSkipped);
        Assert.False(answer.IsNa);
    }

    [DockerFact]
    public async Task Open_link_session_draft_rejects_session_from_another_token()
    {
        await using var scenario = await CreatePublicOpenLinkScenarioAsync(
            Guid.NewGuid(),
            ResponseIdentityModes.Anonymous,
            "Public draft stale pointer");
        var setupStore = new SetupWorkflowStore(scenario.Db, scenario.TenantDbScope);
        var otherCampaign = await setupStore.CreateCampaignAsync(
            scenario.TenantId,
            actorId: null,
            new CreateCampaignRequest(
                scenario.Entry.TemplateVersionId,
                "Public draft other wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: scenario.SeriesId),
            CancellationToken.None);
        Assert.True(otherCampaign.IsSuccess, otherCampaign.Error.ToString());

        var otherLaunched = await setupStore.LaunchCampaignAsync(
            scenario.TenantId,
            actorId: null,
            otherCampaign.Value.Id,
            CancellationToken.None);
        Assert.True(otherLaunched.IsSuccess, otherLaunched.Error.ToString());

        var otherOpenLink = await setupStore.CreateCampaignOpenLinkAsync(
            scenario.TenantId,
            otherCampaign.Value.Id,
            CancellationToken.None);
        Assert.True(otherOpenLink.IsSuccess, otherOpenLink.Error.ToString());

        var session = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry),
            CancellationToken.None);

        var draft = await scenario.ResponseStore.GetOpenLinkSessionDraftAsync(
            otherOpenLink.Value.Token,
            session.Value.Id,
            CancellationToken.None);

        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(draft.IsFailure);
        Assert.Equal("response_session.not_found", draft.Error.Code);
    }

    [DockerFact]
    public async Task Public_session_handle_stores_only_hash_and_supports_draft_save_submit()
    {
        await using var scenario = await CreatePublicOpenLinkScenarioAsync(
            Guid.NewGuid(),
            ResponseIdentityModes.Anonymous,
            "Public session handle");
        var questionId = scenario.Entry.Questions.Single().Id;

        var session = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            scenario.Token,
            CreateOpenLinkSessionRequestFor(scenario.Entry),
            CancellationToken.None);

        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.NotNull(session.Value.PublicHandle);
        Assert.StartsWith("rsh_", session.Value.PublicHandle, StringComparison.Ordinal);
        Assert.DoesNotContain(scenario.Token, session.Value.PublicHandle, StringComparison.Ordinal);

        await using (var transaction = await scenario.TenantDbScope.BeginTransactionAsync(scenario.TenantId))
        {
            var storedSession = await scenario.Db.ResponseSessions
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == session.Value.Id);
            Assert.Equal(OpenLinkSessionHandles.Hash(session.Value.PublicHandle), storedSession.PublicHandleHash);
            Assert.NotNull(storedSession.PublicHandleIssuedAt);
            Assert.DoesNotContain(
                session.Value.PublicHandle,
                JsonSerializer.Serialize(storedSession),
                StringComparison.Ordinal);
            await transaction.CommitAsync();
        }

        var saved = await scenario.ResponseStore.SavePublicSessionAnswersAsync(
            session.Value.PublicHandle,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(questionId, "4", Comment: "steady")
            ]),
            CancellationToken.None);
        var draft = await scenario.ResponseStore.GetPublicSessionDraftAsync(
            session.Value.PublicHandle,
            CancellationToken.None);
        var submitted = await scenario.ResponseStore.SubmitPublicSessionAsync(
            session.Value.PublicHandle,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(draft.IsSuccess, draft.Error.ToString());
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        Assert.Equal(1, saved.Value.SavedAnswerCount);
        Assert.NotNull(draft.Value.Entry);
        Assert.Equal(scenario.Entry.AssignmentId, draft.Value.Entry.AssignmentId);
        Assert.Equal(session.Value.Id, draft.Value.Session.Id);
        Assert.Equal(session.Value.PublicHandle, draft.Value.Session.PublicHandle);
        var answer = Assert.Single(draft.Value.Answers);
        Assert.Equal(questionId, answer.QuestionId);
        Assert.Equal("4", answer.Value);
        Assert.Equal("steady", answer.Comment);
        Assert.Equal(session.Value.Id, submitted.Value.Id);

        await using var scoreTransaction = await scenario.TenantDbScope.BeginTransactionAsync(scenario.TenantId);
        Assert.Equal(1, await scenario.Db.ScoreRuns.CountAsync(run => run.ResponseSessionId == session.Value.Id));
        Assert.Equal(1, await scenario.Db.Scores.CountAsync(score => score.ResponseSessionId == session.Value.Id));
        await scoreTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Identified_entry_response_store_links_consent_to_subject_and_supports_public_handle()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified respondent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified respondent wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var respondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            displayName: "Identified Respondent",
            email: "identified-response@example.test");
        var audience = new Audience(Guid.NewGuid(), campaign.Value.Id);
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.Add(respondent);
            tenantDb.Audiences.Add(audience);
            tenantDb.AudienceMembers.Add(new AudienceMember(audience.Id, respondent.Id));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest("""{"kind":"self","role":"self"}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var identifiedEntry = await setupStore.CreateCampaignIdentifiedEntryAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        var entry = await responseStore.GetIdentifiedEntryAsync(
            identifiedEntry.Value.Token,
            CancellationToken.None);
        var session = await responseStore.CreateIdentifiedEntrySessionAsync(
            identifiedEntry.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);
        var saved = await responseStore.SavePublicSessionAnswersAsync(
            session.Value.PublicHandle!,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(entry.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitPublicSessionAsync(
            session.Value.PublicHandle!,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(identifiedEntry.IsSuccess, identifiedEntry.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        Assert.Equal("identified", entry.Value.ResponseIdentityMode);
        Assert.Equal(identifiedEntry.Value.AssignmentId, entry.Value.AssignmentId);
        Assert.NotNull(session.Value.PublicHandle);
        Assert.StartsWith("rsh_", session.Value.PublicHandle, StringComparison.Ordinal);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Value.Id);
        var storedConsent = await tenantDb.ConsentRecords.SingleAsync(entity => entity.Id == storedSession.ConsentRecordId);

        Assert.NotNull(storedSession.SubmittedAt);
        Assert.Equal(identifiedEntry.Value.AssignmentId, storedSession.AssignmentId);
        Assert.Equal(identifiedEntry.Value.SubjectId, storedConsent.SubjectId);
        Assert.Equal(entry.Value.ConsentDocument.Id, storedConsent.ConsentDocumentId);
        Assert.Equal(1, await tenantDb.ScoreRuns.CountAsync(run => run.ResponseSessionId == session.Value.Id));
        Assert.Equal(1, await tenantDb.Scores.CountAsync(score => score.ResponseSessionId == session.Value.Id));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Identified_entry_store_returns_target_aware_assignment_context()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "leadership.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"leadership.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Target aware identified study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Target aware identified wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:miriam",
            email: "miriam@example.test",
            displayName: "Miriam Graham");
        var target = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:adele",
            email: "adele@example.test",
            displayName: "Adele Vance");
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, target);
            tenantDb.SubjectRelationships.Add(new SubjectRelationship(
                Guid.NewGuid(),
                tenantId,
                manager.Id,
                target.Id,
                SubjectRelationshipTypes.ManagerOf));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{target.Id:D}}"]}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());

        var identifiedEntry = await setupStore.CreateCampaignIdentifiedEntryAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(identifiedEntry.IsSuccess, identifiedEntry.Error.ToString());

        var entry = await responseStore.GetIdentifiedEntryAsync(
            identifiedEntry.Value.Token,
            CancellationToken.None);

        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.Equal(identifiedEntry.Value.AssignmentId, entry.Value.AssignmentId);
        Assert.Equal(manager.Id, identifiedEntry.Value.SubjectId);
        Assert.Equal("identified", entry.Value.ResponseIdentityMode);
        Assert.Equal("manager", entry.Value.AssignmentRole);
        Assert.NotNull(entry.Value.RespondentSubject);
        Assert.Equal(manager.Id, entry.Value.RespondentSubject.Id);
        Assert.Equal("Miriam Graham", entry.Value.RespondentSubject.DisplayName);
        Assert.Equal("miriam@example.test", entry.Value.RespondentSubject.Email);
        Assert.Equal("msgraph:tenant:miriam", entry.Value.RespondentSubject.ExternalId);
        Assert.NotNull(entry.Value.TargetSubject);
        Assert.Equal(target.Id, entry.Value.TargetSubject.Id);
        Assert.Equal("Adele Vance", entry.Value.TargetSubject.DisplayName);
        Assert.Equal("adele@example.test", entry.Value.TargetSubject.Email);
        Assert.Equal("msgraph:tenant:adele", entry.Value.TargetSubject.ExternalId);
    }

    [DockerFact]
    public async Task Identified_queue_store_returns_safe_assignments_for_token_respondent_only()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "leadership.queue.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"leadership.queue.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue respondent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified queue respondent wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:miriam",
            email: "miriam@example.test",
            displayName: "Miriam Graham");
        var firstTarget = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:adele",
            email: "adele@example.test",
            displayName: "Adele Vance");
        var secondTarget = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:alex",
            email: "alex@example.test",
            displayName: "Alex Wilber");
        var thirdTarget = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:priya",
            email: "priya@example.test",
            displayName: "Priya Shah");
        var otherRespondent = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:other",
            email: "other@example.test",
            displayName: "Other Respondent");
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, firstTarget, secondTarget, thirdTarget, otherRespondent);
            tenantDb.SubjectRelationships.AddRange(
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    firstTarget.Id,
                    SubjectRelationshipTypes.ManagerOf),
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    secondTarget.Id,
                    SubjectRelationshipTypes.ManagerOf),
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    thirdTarget.Id,
                    SubjectRelationshipTypes.ManagerOf));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{firstTarget.Id:D}}","{{secondTarget.Id:D}}","{{thirdTarget.Id:D}}"]}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var queueAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(queueAccess.IsSuccess, queueAccess.Error.ToString());

        var managerAccess = Assert.Single(queueAccess.Value.Respondents);
        Assert.Equal(manager.Id, managerAccess.RespondentSubjectId);
        Assert.NotNull(managerAccess.Token);

        await using (var statusTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var submittedAssignmentId = await tenantDb.Assignments
                .Where(assignment =>
                    assignment.CampaignId == campaign.Value.Id &&
                    assignment.RespondentSubjectId == manager.Id &&
                    assignment.TargetSubjectId == firstTarget.Id)
                .Select(assignment => assignment.Id)
                .SingleAsync();
            var draftAssignmentId = await tenantDb.Assignments
                .Where(assignment =>
                    assignment.CampaignId == campaign.Value.Id &&
                    assignment.RespondentSubjectId == manager.Id &&
                    assignment.TargetSubjectId == secondTarget.Id)
                .Select(assignment => assignment.Id)
                .SingleAsync();
            var submittedSession = new ResponseSession(
                Guid.NewGuid(),
                tenantId,
                submittedAssignmentId,
                "en");
            submittedSession.Submit(DateTimeOffset.Parse("2026-06-01T12:00:00+00:00"), timeTakenMs: 1200);
            tenantDb.ResponseSessions.AddRange(
                submittedSession,
                new ResponseSession(
                    Guid.NewGuid(),
                    tenantId,
                    draftAssignmentId,
                    "en"));
            tenantDb.Assignments.Add(Assignment.CreateIdentified(
                Guid.NewGuid(),
                tenantId,
                campaign.Value.Id,
                "self",
                otherRespondent.Id));
            await tenantDb.SaveChangesAsync();
            await statusTransaction.CommitAsync();
        }

        var entry = await responseStore.GetIdentifiedQueueAsync(
            managerAccess.Token!,
            CancellationToken.None);

        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.Equal(campaign.Value.Id, entry.Value.CampaignId);
        Assert.Equal("identified", entry.Value.ResponseIdentityMode);
        Assert.Equal(manager.Id, entry.Value.RespondentSubject.Id);
        Assert.Equal("Miriam Graham", entry.Value.RespondentSubject.Label);
        Assert.Equal("miriam@example.test", entry.Value.RespondentSubject.Email);
        Assert.Equal(3, entry.Value.AssignmentCount);
        Assert.Equal(2, entry.Value.StartedCount);
        Assert.Equal(1, entry.Value.SubmittedCount);
        Assert.Equal(3, entry.Value.Assignments.Count);
        Assert.Contains(entry.Value.Assignments, assignment =>
            assignment.TargetSubject?.Id == firstTarget.Id &&
            assignment.ResponseStatus == "submitted" &&
            assignment.SessionId.HasValue &&
            assignment.SubmittedAt.HasValue);
        Assert.Contains(entry.Value.Assignments, assignment =>
            assignment.TargetSubject?.Id == secondTarget.Id &&
            assignment.ResponseStatus == "draft" &&
            assignment.SessionId.HasValue &&
            !assignment.SubmittedAt.HasValue);
        Assert.Contains(entry.Value.Assignments, assignment =>
            assignment.TargetSubject?.Id == thirdTarget.Id &&
            assignment.ResponseStatus == "not_started" &&
            !assignment.SessionId.HasValue);
        Assert.DoesNotContain(entry.Value.Assignments, assignment =>
            assignment.TargetSubject?.Id == otherRespondent.Id ||
            assignment.AssignmentId == otherRespondent.Id);

        var serialized = JsonSerializer.Serialize(entry.Value);
        Assert.DoesNotContain("msgraph", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("external", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Identified_queue_store_starts_and_resumes_assignment_session_for_token_respondent()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "leadership.queue.session.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"leadership.queue.session.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue session study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified queue session wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var manager = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:miriam",
            email: "miriam@example.test",
            displayName: "Miriam Graham");
        var firstTarget = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:adele",
            email: "adele@example.test",
            displayName: "Adele Vance");
        var secondTarget = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "msgraph:tenant:alex",
            email: "alex@example.test",
            displayName: "Alex Wilber");
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, firstTarget, secondTarget);
            tenantDb.SubjectRelationships.AddRange(
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    firstTarget.Id,
                    SubjectRelationshipTypes.ManagerOf),
                new SubjectRelationship(
                    Guid.NewGuid(),
                    tenantId,
                    manager.Id,
                    secondTarget.Id,
                    SubjectRelationshipTypes.ManagerOf));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{firstTarget.Id:D}}","{{secondTarget.Id:D}}"]}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var queueAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(queueAccess.IsSuccess, queueAccess.Error.ToString());

        var managerAccess = Assert.Single(queueAccess.Value.Respondents);
        Assert.Equal(manager.Id, managerAccess.RespondentSubjectId);
        Assert.NotNull(managerAccess.Token);
        var queue = await responseStore.GetIdentifiedQueueAsync(
            managerAccess.Token!,
            CancellationToken.None);
        Assert.True(queue.IsSuccess, queue.Error.ToString());
        var assignment = Assert.Single(
            queue.Value.Assignments,
            candidate => candidate.TargetSubject?.Id == firstTarget.Id);

        var started = await responseStore.CreateIdentifiedQueueAssignmentSessionAsync(
            managerAccess.Token!,
            assignment.AssignmentId,
            new CreateOpenLinkSessionRequest(
                "en",
                queue.Value.ConsentDocument.Id,
                queue.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);

        Assert.True(started.IsSuccess, started.Error.ToString());
        Assert.Equal(assignment.AssignmentId, started.Value.Assignment.AssignmentId);
        Assert.Equal("draft", started.Value.Assignment.ResponseStatus);
        Assert.Equal(started.Value.Session.Id, started.Value.Assignment.SessionId);
        Assert.NotNull(started.Value.Session.PublicHandle);
        Assert.StartsWith("rsh_", started.Value.Session.PublicHandle, StringComparison.Ordinal);
        Assert.Equal(1, started.Value.Queue.StartedCount);
        Assert.Equal(0, started.Value.Queue.SubmittedCount);
        Assert.Empty(started.Value.Answers);
        Assert.Equal(0, started.Value.SavedAnswerCount);

        var saved = await responseStore.SavePublicSessionAnswersAsync(
            started.Value.Session.PublicHandle!,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(queue.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.Error.ToString());

        var resumed = await responseStore.CreateIdentifiedQueueAssignmentSessionAsync(
            managerAccess.Token!,
            assignment.AssignmentId,
            new CreateOpenLinkSessionRequest(
                "en",
                queue.Value.ConsentDocument.Id,
                queue.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);

        Assert.True(resumed.IsSuccess, resumed.Error.ToString());
        Assert.Equal(started.Value.Session.Id, resumed.Value.Session.Id);
        Assert.NotNull(resumed.Value.Session.PublicHandle);
        Assert.StartsWith("rsh_", resumed.Value.Session.PublicHandle, StringComparison.Ordinal);
        Assert.NotEqual(started.Value.Session.PublicHandle, resumed.Value.Session.PublicHandle);
        Assert.Equal(1, resumed.Value.SavedAnswerCount);
        var savedAnswer = Assert.Single(resumed.Value.Answers);
        Assert.Equal("4", savedAnswer.Value);

        await using var verifyTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var tokenHash = OpenLinkTokens.Hash(managerAccess.Token!);
        var token = await tenantDb.InvitationTokens.SingleAsync(entity => entity.TokenHash == tokenHash);
        var sessions = await tenantDb.ResponseSessions
            .Where(entity => entity.AssignmentId == assignment.AssignmentId)
            .ToArrayAsync();
        var consent = await tenantDb.ConsentRecords.SingleAsync(entity => entity.Id == sessions.Single().ConsentRecordId);

        Assert.Null(token.UsedAt);
        Assert.Single(sessions);
        Assert.Equal(manager.Id, consent.SubjectId);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Identified_queue_store_rejects_other_respondent_assignment_and_submitted_assignment_session()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "leadership.queue.session.boundary.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"leadership.queue.session.boundary.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Identified queue boundary study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Identified queue boundary wave",
                ResponseIdentityModes.Identified,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var manager = new Subject(Guid.NewGuid(), tenantId, email: "miriam@example.test", displayName: "Miriam Graham");
        var target = new Subject(Guid.NewGuid(), tenantId, email: "adele@example.test", displayName: "Adele Vance");
        var otherRespondent = new Subject(Guid.NewGuid(), tenantId, email: "other@example.test", displayName: "Other Respondent");
        await using (var seedTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.Subjects.AddRange(manager, target, otherRespondent);
            tenantDb.SubjectRelationships.Add(new SubjectRelationship(
                Guid.NewGuid(),
                tenantId,
                manager.Id,
                target.Id,
                SubjectRelationshipTypes.ManagerOf));
            await tenantDb.SaveChangesAsync();
            await seedTransaction.CommitAsync();
        }

        var savedRules = await setupStore.UpdateCampaignRespondentRulesAsync(
            tenantId,
            campaign.Value.Id,
            new UpdateCampaignRespondentRulesRequest(
            [
                new UpdateCampaignRespondentRuleRequest(
                    $$"""{"kind":"manager_of_target","role":"manager","target_subject_ids":["{{target.Id:D}}"]}""")
            ]),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var queueAccess = await setupStore.CreateCampaignIdentifiedQueueAccessAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignIdentifiedQueueAccessRequest(),
            CancellationToken.None);

        Assert.True(savedRules.IsSuccess, savedRules.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(queueAccess.IsSuccess, queueAccess.Error.ToString());

        var managerAccess = Assert.Single(queueAccess.Value.Respondents);
        var entry = await responseStore.GetIdentifiedQueueAsync(
            managerAccess.Token!,
            CancellationToken.None);
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        var submittedAssignment = Assert.Single(entry.Value.Assignments);
        Guid wrongRespondentAssignmentId;
        await using (var statusTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            var submittedSession = new ResponseSession(
                Guid.NewGuid(),
                tenantId,
                submittedAssignment.AssignmentId,
                "en");
            submittedSession.Submit(DateTimeOffset.UtcNow, timeTakenMs: 1200);
            wrongRespondentAssignmentId = Guid.NewGuid();
            tenantDb.ResponseSessions.Add(submittedSession);
            tenantDb.Assignments.Add(Assignment.CreateIdentified(
                wrongRespondentAssignmentId,
                tenantId,
                campaign.Value.Id,
                "peer",
                otherRespondent.Id,
                target.Id));
            await tenantDb.SaveChangesAsync();
            await statusTransaction.CommitAsync();
        }

        var wrongRespondentStart = await responseStore.CreateIdentifiedQueueAssignmentSessionAsync(
            managerAccess.Token!,
            wrongRespondentAssignmentId,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);
        var submittedStart = await responseStore.CreateIdentifiedQueueAssignmentSessionAsync(
            managerAccess.Token!,
            submittedAssignment.AssignmentId,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);

        Assert.True(wrongRespondentStart.IsFailure);
        Assert.Equal("identified_queue.assignment_not_found", wrongRespondentStart.Error.Code);
        Assert.True(submittedStart.IsFailure);
        Assert.Equal("identified_queue.assignment_submitted", submittedStart.Error.Code);
    }

    [DockerFact]
    public async Task Email_invite_response_store_resolves_token_and_submits_without_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope, new OutboxEventBuffer());
        var deliveryStore = new NotificationDeliveryStore(
            tenantDb,
            tenantDbScope,
            new LocalDevEmailDeliveryProvider());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email invite respondent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email invite respondent wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var invitationBatch = await setupStore.CreateCampaignInvitationBatchAsync(
            tenantId,
            campaign.Value.Id,
            new CreateCampaignInvitationBatchRequest(
            [
                new InvitationRecipientRequest("ada@example.com")
            ]),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(invitationBatch.IsSuccess, invitationBatch.Error.ToString());
        var invite = Assert.Single(invitationBatch.Value.Invitations);
        var delivered = await deliveryStore.ProcessCampaignEmailDeliveriesAsync(
            tenantId,
            campaign.Value.Id,
            new ProcessCampaignEmailDeliveriesRequest(BatchSize: 25),
            CancellationToken.None);
        Assert.True(delivered.IsSuccess, delivered.Error.ToString());
        var delivery = Assert.Single(delivered.Value.Deliveries);
        Assert.NotNull(delivery.RespondentPath);
        var inviteToken = delivery.RespondentPath["/r/".Length..];

        var entry = await responseStore.GetOpenLinkEntryAsync(
            inviteToken,
            CancellationToken.None);

        Assert.True(entry.IsSuccess, entry.Error.ToString());

        var session = await responseStore.CreateOpenLinkSessionAsync(
            inviteToken,
            new CreateOpenLinkSessionRequest(
                "en",
                entry.Value.ConsentDocument.Id,
                entry.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);
        var saved = await responseStore.SaveOpenLinkAnswersAsync(
            inviteToken,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(entry.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitOpenLinkSessionAsync(
            inviteToken,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(session.IsSuccess, session.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        Assert.Equal(invite.AssignmentId, entry.Value.AssignmentId);
        Assert.Equal(invite.AssignmentId, session.Value.AssignmentId);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var storedSession = await tenantDb.ResponseSessions.SingleAsync(entity => entity.Id == session.Value.Id);
        Assert.NotNull(storedSession.SubmittedAt);
        Assert.NotNull(storedSession.ConsentRecordId);
        Assert.Equal(invite.AssignmentId, storedSession.AssignmentId);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Email_invite_response_store_rejects_wrong_channel_expired_and_used_tokens_neutrally()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Email invite guard respondent study");
        var anonymousCampaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Email invite guard wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var anonymousLaunched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            anonymousCampaign.Value.Id,
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess);
        Assert.True(anonymousCampaign.IsSuccess);
        Assert.True(anonymousLaunched.IsSuccess, anonymousLaunched.Error.ToString());

        var wrongChannelToken = await CreateManualPublicInvitationTokenAsync(
            tenantDb,
            tenantDbScope,
            tenantId,
            anonymousCampaign.Value.Id,
            InvitationTokenChannels.Sms,
            "sms@example.com");
        var expiredToken = await CreateManualPublicInvitationTokenAsync(
            tenantDb,
            tenantDbScope,
            tenantId,
            anonymousCampaign.Value.Id,
            InvitationTokenChannels.Email,
            "expired@example.com",
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var usedToken = await CreateManualPublicInvitationTokenAsync(
            tenantDb,
            tenantDbScope,
            tenantId,
            anonymousCampaign.Value.Id,
            InvitationTokenChannels.Email,
            "used@example.com",
            markUsed: true);

        var results = new[]
        {
            await responseStore.GetOpenLinkEntryAsync(wrongChannelToken, CancellationToken.None),
            await responseStore.GetOpenLinkEntryAsync(expiredToken, CancellationToken.None),
            await responseStore.GetOpenLinkEntryAsync(usedToken, CancellationToken.None)
        };

        Assert.All(results, result =>
        {
            Assert.True(result.IsFailure);
            Assert.Equal("open_link.not_available", result.Error.Code);
        });
    }

    [DockerFact]
    public async Task Open_link_response_store_rejects_session_without_required_consent_grants()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Public consent required study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Public consent required wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        var session = await responseStore.CreateOpenLinkSessionAsync(
            openLink.Value.Token,
            new CreateOpenLinkSessionRequest("en", entry.Value.ConsentDocument.Id, []),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());
        Assert.True(session.IsFailure);
        Assert.Equal("consent.required_grants_missing", session.Error.Code);
    }

    [DockerFact]
    public async Task Open_link_response_store_rejects_submit_without_consent_record()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Public consent submit guard study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Public consent submit guard wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);
        var sessionId = Guid.NewGuid();

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            tenantDb.ResponseSessions.Add(new ResponseSession(
                sessionId,
                tenantId,
                openLink.Value.AssignmentId,
                "en"));
            tenantDb.Answers.Add(new Answer(
                Guid.NewGuid(),
                tenantId,
                sessionId,
                entry.Value.Questions.Single().Id,
                "4"));
            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var submitted = await responseStore.SubmitOpenLinkSessionAsync(
            openLink.Value.Token,
            sessionId,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);

        Assert.True(submitted.IsFailure);
        Assert.Equal("response.consent_required", submitted.Error.Code);
    }

    [DockerFact]
    public async Task Open_link_response_store_rejects_session_from_another_token()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Public token guard consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Public token guard wave A",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var campaignB = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Public token guard wave B",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(campaignB.IsSuccess, campaignB.Error.ToString());

        var launchedB = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaignB.Value.Id,
            CancellationToken.None);
        var openLinkA = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var openLinkB = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaignB.Value.Id,
            CancellationToken.None);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(launchedB.IsSuccess, launchedB.Error.ToString());
        Assert.True(openLinkA.IsSuccess, openLinkA.Error.ToString());
        Assert.True(openLinkB.IsSuccess, openLinkB.Error.ToString());

        var entryA = await responseStore.GetOpenLinkEntryAsync(
            openLinkA.Value.Token,
            CancellationToken.None);
        Assert.True(entryA.IsSuccess, entryA.Error.ToString());

        var sessionA = await responseStore.CreateOpenLinkSessionAsync(
            openLinkA.Value.Token,
            new CreateOpenLinkSessionRequest(
                "en",
                entryA.Value.ConsentDocument.Id,
                entryA.Value.ConsentDocument.RequiredGrants),
            CancellationToken.None);
        Assert.True(sessionA.IsSuccess, sessionA.Error.ToString());

        var rejectedSave = await responseStore.SaveOpenLinkAnswersAsync(
            openLinkB.Value.Token,
            sessionA.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(entryA.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var rejectedSubmit = await responseStore.SubmitOpenLinkSessionAsync(
            openLinkB.Value.Token,
            sessionA.Value.Id,
            new SubmitResponseSessionRequest(),
            CancellationToken.None);

        Assert.True(rejectedSave.IsFailure);
        Assert.Equal("response_session.not_found", rejectedSave.Error.Code);
        Assert.True(rejectedSubmit.IsFailure);
        Assert.Equal("response_session.not_found", rejectedSubmit.Error.Code);
    }

    [DockerFact]
    public async Task Response_capture_uses_launch_snapshot_template_when_campaign_changes()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Snapshot response consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Snapshot response wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var mutatedTemplate = await setupStore.CreateTemplateVersionAsync(
            tenantId,
            actorId: null,
            SampleSetupTemplateRequest("Mutated pulse", "q02"),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(mutatedTemplate.IsSuccess, mutatedTemplate.Error.ToString());

        await using (var mutationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            await tenantDb.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE campaign
                SET template_version_id = {mutatedTemplate.Value.TemplateVersionId},
                    updated_at = {DateTimeOffset.UtcNow}
                WHERE id = {campaign.Value.Id}
                """);
            await mutationTransaction.CommitAsync();
        }

        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(respondentCampaign.IsSuccess, respondentCampaign.Error.ToString());
        Assert.Equal(versionId, respondentCampaign.Value.TemplateVersionId);
        Assert.Equal("q01", Assert.Single(respondentCampaign.Value.Questions).Code);
    }

    [DockerFact]
    public async Task Score_computation_uses_launch_snapshot_scoring_rule_when_campaign_changes()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            "Snapshot score consent study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Snapshot score wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var assignment = await responseStore.CreateLabAssignmentAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var session = await responseStore.CreateSessionAsync(
            tenantId,
            new CreateResponseSessionRequest(assignment.Value.AssignmentId, "en"),
            CancellationToken.None);
        var saved = await responseStore.SaveAnswersAsync(
            tenantId,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(respondentCampaign.Value.Questions.Single().Id, "4")
            ]),
            CancellationToken.None);
        var submitted = await responseStore.SubmitSessionAsync(
            tenantId,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);
        var mutatedTemplate = await setupStore.CreateTemplateVersionAsync(
            tenantId,
            actorId: null,
            SampleSetupTemplateRequest("Mutated scoring pulse", "q02"),
            CancellationToken.None);
        var mutatedScoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                mutatedTemplate.Value.TemplateVersionId,
                "burnout.total",
                "2.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"2.0.0","operations":[{"op":"mean","items":["q02"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess);
        Assert.True(campaign.IsSuccess);
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(respondentCampaign.IsSuccess, respondentCampaign.Error.ToString());
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        Assert.True(mutatedTemplate.IsSuccess, mutatedTemplate.Error.ToString());
        Assert.True(mutatedScoringRule.IsSuccess, mutatedScoringRule.Error.ToString());

        await using (var mutationTransaction = await tenantDbScope.BeginTransactionAsync(tenantId))
        {
            await tenantDb.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE campaign
                SET template_version_id = {mutatedTemplate.Value.TemplateVersionId},
                    updated_at = {DateTimeOffset.UtcNow}
                WHERE id = {campaign.Value.Id}
                """);
            await mutationTransaction.CommitAsync();
        }

        var scored = await scoreStore.ComputeResponseScoresAsync(
            tenantId,
            session.Value.Id,
            CancellationToken.None);

        Assert.True(scored.IsSuccess, scored.Error.ToString());
        var score = Assert.Single(scored.Value.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4m, score.Value);
        Assert.Equal(1, score.NValid);
        Assert.Equal(1, score.NExpected);
        Assert.Equal("ok", score.MissingPolicyStatus);
    }

    [DockerFact]
    public async Task Rls_blocks_tenant_derivative_parent_from_another_tenant_derivative()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var canonical = CreateCanonicalInstrument();
        var tenantBDerivative = Instrument.CreateDerivative(
            Guid.NewGuid(),
            tenantB,
            canonical.Id,
            "tenant-b-derivative",
            "1.0.0",
            "Tenant B derivative",
            InstrumentDomains.Psychometric,
            "Tenant B private derivative");
        var tenantADerivative = Instrument.CreateDerivative(
            Guid.NewGuid(),
            tenantA,
            tenantBDerivative.Id,
            "tenant-a-bad-derivative",
            "1.0.0",
            "Tenant A bad derivative",
            InstrumentDomains.Psychometric,
            "Should not point to Tenant B derivative");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "instrument-tenant-a", "Instrument Tenant A"));
            db.Tenants.Add(new Tenant(tenantB, "instrument-tenant-b", "Instrument Tenant B"));
            db.Instruments.Add(canonical);
            db.Instruments.Add(tenantBDerivative);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        tenantADb.Instruments.Add(tenantADerivative);

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Migrations_create_template_tables_and_allow_global_template_reads()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "OLBI");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "items", "Items");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "likert_1_4",
            ScaleTypes.Likert,
            1,
            4,
            1,
            naAllowed: false,
            anchors: """[{"value":1,"label_default":"Strongly agree"},{"value":4,"label_default":"Strongly disagree"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "olbi_01",
            QuestionTypes.Likert,
            scale.Id,
            "I always find new and interesting aspects in my work.",
            required: true,
            reverseCoded: true,
            measurementLevel: MeasurementLevels.Ordinal);
        var translation = InstrumentTranslation.ForQuestion(
            Guid.NewGuid(),
            question.Id,
            "text_default",
            "hr",
            "Uvijek nalazim nove i zanimljive aspekte u svom poslu.",
            TranslationStatuses.DraftTranslation);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.TemplateSections.Add(section);
            db.QuestionScales.Add(scale);
            db.TemplateQuestions.Add(question);
            db.InstrumentTranslations.Add(translation);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var visibleVersion = await tenantDb.TemplateVersions.SingleAsync(entity => entity.Id == version.Id);
        var visibleQuestion = await tenantDb.TemplateQuestions.SingleAsync(entity => entity.Id == question.Id);
        var visibleTranslation = await tenantDb.InstrumentTranslations.SingleAsync(entity => entity.Id == translation.Id);

        Assert.True(visibleVersion.IsGlobal);
        Assert.Equal("olbi_01", visibleQuestion.Code);
        Assert.Equal("hr", visibleTranslation.Locale);
    }

    [DockerFact]
    public async Task Rls_allows_tenant_template_structure_writes()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Tenant pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "0.1.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "intro", "Intro");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "role",
            QuestionTypes.SingleChoice,
            scaleId: null,
            textDefault: "What is your role?",
            measurementLevel: MeasurementLevels.Nominal);
        var choice = new ChoiceOption(Guid.NewGuid(), question.Id, 1, "manager", "Manager");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "template-tenant", "Template Tenant"));
        tenantDb.SurveyTemplates.Add(template);
        tenantDb.TemplateVersions.Add(version);
        tenantDb.TemplateSections.Add(section);
        tenantDb.TemplateQuestions.Add(question);
        tenantDb.ChoiceOptions.Add(choice);

        await tenantDb.SaveChangesAsync();

        var savedChoice = await tenantDb.ChoiceOptions.SingleAsync(entity => entity.Id == choice.Id);

        Assert.Equal("manager", savedChoice.Value);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_tenant_runtime_from_modifying_global_template_structure()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "OLBI");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "items", "Items");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.TemplateSections.Add(section);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.TemplateQuestions.Add(new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            99,
            "bad_global_edit",
            QuestionTypes.Text,
            scaleId: null,
            textDefault: "Tenant edit should fail."));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_allows_tenant_scoring_rule_writes_for_tenant_template_versions()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Tenant pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "0.1.0", "en");
        var rule = CreateScoringRule(version.Id);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "scoring-tenant", "Scoring Tenant"));
        tenantDb.SurveyTemplates.Add(template);
        tenantDb.TemplateVersions.Add(version);
        tenantDb.ScoringRules.Add(rule);

        await tenantDb.SaveChangesAsync();

        var savedRule = await tenantDb.ScoringRules.SingleAsync(entity => entity.Id == rule.Id);

        Assert.Equal("burnout.total", savedRule.RuleKey);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_tenant_runtime_from_modifying_global_scoring_rules()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Global burnout");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.ScoringRules.Add(CreateScoringRule(version.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_allows_tenant_campaign_shell_with_global_template_version()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Global pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var series = CreateCampaignSeries(tenantId);
        var campaign = CreateCampaign(tenantId, version.Id, series.Id);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "campaign-tenant", "Campaign Tenant"));
        tenantDb.CampaignSeries.Add(series);
        tenantDb.Campaigns.Add(campaign);

        await tenantDb.SaveChangesAsync();

        var savedCampaign = await tenantDb.Campaigns.SingleAsync(entity => entity.Id == campaign.Id);

        Assert.Equal(ResponseIdentityModes.Identified, savedCampaign.ResponseIdentityMode);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_allows_tenant_campaign_shell_with_tenant_template_version()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Tenant pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "0.1.0", "en");
        var series = CreateCampaignSeries(tenantId);
        var campaign = CreateCampaign(tenantId, version.Id, series.Id);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "tenant-template", "Tenant Template"));
        tenantDb.SurveyTemplates.Add(template);
        tenantDb.TemplateVersions.Add(version);
        tenantDb.CampaignSeries.Add(series);
        tenantDb.Campaigns.Add(campaign);

        await tenantDb.SaveChangesAsync();

        var savedCampaign = await tenantDb.Campaigns.SingleAsync(entity => entity.Id == campaign.Id);

        Assert.Equal(version.Id, savedCampaign.TemplateVersionId);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Participant_code_runtime_scope_allows_same_tenant_and_blocks_cross_tenant_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var series = CreateCampaignSeries(tenantA);
        var code = new ParticipantCode(
            Guid.NewGuid(),
            tenantA,
            series.Id,
            CreateParticipantCodeHash(1),
            65_536,
            3,
            4,
            32,
            DateTimeOffset.UtcNow);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using (var tenantDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

            tenantDb.Tenants.Add(new Tenant(tenantA, "participant-code-a", "Participant Code A"));
            tenantDb.CampaignSeries.Add(series);
            tenantDb.Set<ParticipantCode>().Add(code);

            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantADb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

            Assert.Equal(1, await tenantADb.Set<ParticipantCode>().CountAsync());
        }

        await using (var tenantBDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantBDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantB);

            Assert.Equal(0, await tenantBDb.Set<ParticipantCode>().CountAsync());
        }
    }

    [DockerFact]
    public async Task Participant_code_hash_uniqueness_is_scoped_to_campaign_series()
    {
        var tenantId = Guid.NewGuid();
        var seriesA = CreateCampaignSeries(tenantId);
        var seriesB = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            "Campaign series B",
            CreateCodeSalt());
        var hash = CreateParticipantCodeHash(2);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "participant-code-scope", "Participant Code Scope"));
        tenantDb.CampaignSeries.AddRange(seriesA, seriesB);
        tenantDb.Set<ParticipantCode>().AddRange(
            new ParticipantCode(
                Guid.NewGuid(),
                tenantId,
                seriesA.Id,
                hash,
                65_536,
                3,
                4,
                32,
                DateTimeOffset.UtcNow),
            new ParticipantCode(
                Guid.NewGuid(),
                tenantId,
                seriesB.Id,
                hash,
                65_536,
                3,
                4,
                32,
                DateTimeOffset.UtcNow));

        await tenantDb.SaveChangesAsync();

        Assert.Equal(2, await tenantDb.Set<ParticipantCode>().CountAsync());

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Participant_code_store_resolves_codes_neutrally_and_persists_hashes_only()
    {
        var tenantId = Guid.NewGuid();
        var seriesA = CreateCampaignSeries(tenantId);
        var seriesB = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            "Campaign series B",
            CreateCodeSalt());
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using (var tenantDb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

            tenantDb.Tenants.Add(new Tenant(tenantId, "participant-code-store", "Participant Code Store"));
            tenantDb.CampaignSeries.AddRange(seriesA, seriesB);

            await tenantDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var storeDb = new ApplicationDbContext(CreateRuntimeOptions());
        var store = new ParticipantCodeStore(
            storeDb,
            new TenantDbScope(storeDb),
            new DeterministicParticipantCodeHasher());

        var first = await store.ResolveAsync(
            tenantId,
            seriesA.Id,
            "  Cable   Horse Battery  ",
            CancellationToken.None);
        var second = await store.ResolveAsync(
            tenantId,
            seriesA.Id,
            "cable horse battery",
            CancellationToken.None);
        var otherSeries = await store.ResolveAsync(
            tenantId,
            seriesB.Id,
            "cable horse battery",
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.True(otherSeries.IsSuccess, otherSeries.Error.ToString());
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.NotEqual(first.Value.Id, otherSeries.Value.Id);

        await using var verifyDb = new ApplicationDbContext(CreateRuntimeOptions());
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);

        var persisted = await verifyDb.ParticipantCodes
            .OrderBy(code => code.CampaignSeriesId)
            .ToListAsync();

        Assert.Equal(2, persisted.Count);
        Assert.All(persisted, code =>
        {
            Assert.Equal(32, code.Hash.Length);
            Assert.Equal(65_536, code.Argon2MemoryKiB);
            Assert.Equal(3, code.Argon2Iterations);
            Assert.Equal(4, code.Argon2Parallelism);
            Assert.Equal(32, code.Argon2OutputBytes);
        });
        Assert.DoesNotContain(
            typeof(ParticipantCodeResponse).GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase));
    }

    [DockerFact]
    public async Task Response_session_rejects_participant_code_from_another_campaign_series()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Longitudinal pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "0.1.0", "en");
        var seriesA = CreateCampaignSeries(tenantId);
        var seriesB = new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            "Other campaign series",
            CreateCodeSalt());
        var campaign = CreateCampaign(
            tenantId,
            version.Id,
            seriesA.Id,
            responseIdentityMode: ResponseIdentityModes.AnonymousLongitudinal);
        var invitationToken = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "participant-code-session-token",
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "public_respondent",
            invitationToken.Id);
        var participantCode = new ParticipantCode(
            Guid.NewGuid(),
            tenantId,
            seriesB.Id,
            CreateParticipantCodeHash(3),
            65_536,
            3,
            4,
            32,
            DateTimeOffset.UtcNow);
        var session = new ResponseSession(
            Guid.NewGuid(),
            tenantId,
            assignment.Id,
            "en",
            participantCode.Id);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "participant-code-session", "Participant Code Session"));
        tenantDb.SurveyTemplates.Add(template);
        tenantDb.TemplateVersions.Add(version);
        tenantDb.CampaignSeries.AddRange(seriesA, seriesB);
        tenantDb.Campaigns.Add(campaign);
        tenantDb.InvitationTokens.Add(invitationToken);
        tenantDb.Assignments.Add(assignment);
        tenantDb.Set<ParticipantCode>().Add(participantCode);
        tenantDb.ResponseSessions.Add(session);

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_campaign_shell_with_other_tenant_template_version()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantB, "Tenant B pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "0.1.0", "en");
        var series = CreateCampaignSeries(tenantA);
        var campaign = CreateCampaign(tenantA, version.Id, series.Id);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantB, "tenant-b-template", "Tenant B Template"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        tenantADb.Tenants.Add(new Tenant(tenantA, "tenant-a-campaign", "Tenant A Campaign"));
        tenantADb.CampaignSeries.Add(series);
        tenantADb.Campaigns.Add(campaign);

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_campaign_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Global campaign read pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaignA = CreateCampaign(tenantA, version.Id, name: "Tenant A campaign");
        var campaignB = CreateCampaign(tenantB, version.Id, name: "Tenant B campaign");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "read-tenant-a", "Read Tenant A"));
            db.Tenants.Add(new Tenant(tenantB, "read-tenant-b", "Read Tenant B"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.Campaigns.Add(campaignA);
            db.Campaigns.Add(campaignB);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleCampaigns = await tenantADb.Campaigns
            .Select(campaign => campaign.Id)
            .ToListAsync();

        Assert.Contains(campaignA.Id, visibleCampaigns);
        Assert.DoesNotContain(campaignB.Id, visibleCampaigns);
    }

    [DockerFact]
    public async Task Migrations_create_campaign_launch_snapshot_table_and_allow_snapshot_insert()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Launch snapshot pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(tenantId, version.Id, name: "Launch snapshot campaign");
        var scoringRule = CreateScoringRule(version.Id);
        var snapshot = new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            campaignSeriesId: null,
            version.Id,
            scoringRule.Id,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            templateQuestionCount: 1,
            scoringRule.DocumentHash,
            """{"ready":true,"blockers":[]}""",
            DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"));
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Tenants.Add(new Tenant(tenantId, "snapshot-tenant", "Snapshot Tenant"));
        tenantDb.SurveyTemplates.Add(template);
        tenantDb.TemplateVersions.Add(version);
        tenantDb.ScoringRules.Add(scoringRule);
        tenantDb.Campaigns.Add(campaign);
        tenantDb.CampaignLaunchSnapshots.Add(snapshot);

        await tenantDb.SaveChangesAsync();

        var savedSnapshot = await tenantDb.CampaignLaunchSnapshots.SingleAsync(entity => entity.Id == snapshot.Id);

        Assert.Equal(campaign.Id, savedSnapshot.CampaignId);
        Assert.Equal(version.Id, savedSnapshot.TemplateVersionId);
        Assert.Equal(scoringRule.Id, savedSnapshot.ScoringRuleId);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_campaign_launch_snapshot_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Global launch snapshot read pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var scoringRule = CreateScoringRule(version.Id);
        var campaignA = CreateCampaign(tenantA, version.Id, name: "Tenant A launch campaign");
        var campaignB = CreateCampaign(tenantB, version.Id, name: "Tenant B launch campaign");
        var snapshotA = new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            tenantA,
            campaignA.Id,
            campaignSeriesId: null,
            version.Id,
            scoringRule.Id,
            campaignA.ResponseIdentityMode,
            campaignA.DefaultLocale,
            templateQuestionCount: 1,
            scoringRule.DocumentHash,
            """{"ready":true,"blockers":[]}""",
            DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"));
        var snapshotB = new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            tenantB,
            campaignB.Id,
            campaignSeriesId: null,
            version.Id,
            scoringRule.Id,
            campaignB.ResponseIdentityMode,
            campaignB.DefaultLocale,
            templateQuestionCount: 1,
            scoringRule.DocumentHash,
            """{"ready":true,"blockers":[]}""",
            DateTimeOffset.Parse("2026-05-07T10:20:00+00:00"));
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "snapshot-read-a", "Snapshot Read A"));
            db.Tenants.Add(new Tenant(tenantB, "snapshot-read-b", "Snapshot Read B"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.ScoringRules.Add(scoringRule);
            db.Campaigns.Add(campaignA);
            db.Campaigns.Add(campaignB);
            db.CampaignLaunchSnapshots.Add(snapshotA);
            db.CampaignLaunchSnapshots.Add(snapshotB);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleSnapshots = await tenantADb.CampaignLaunchSnapshots
            .Select(snapshot => snapshot.Id)
            .ToListAsync();

        var visibleSnapshot = Assert.Single(visibleSnapshots);
        Assert.Equal(snapshotA.Id, visibleSnapshot);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_audience_member_subject_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Audience guard pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(tenantA, version.Id);
        var audience = new Audience(Guid.NewGuid(), campaign.Id);
        var tenantBSubject = new Subject(Guid.NewGuid(), tenantB, displayName: "Tenant B subject");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "audience-tenant-a", "Audience Tenant A"));
            db.Tenants.Add(new Tenant(tenantB, "audience-tenant-b", "Audience Tenant B"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.Campaigns.Add(campaign);
            db.Audiences.Add(audience);
            db.Subjects.Add(tenantBSubject);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        tenantADb.AudienceMembers.Add(new AudienceMember(audience.Id, tenantBSubject.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_assignment_subject_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Assignment guard pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(tenantA, version.Id);
        var tenantBSubject = new Subject(Guid.NewGuid(), tenantB, displayName: "Tenant B subject");
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "assignment-tenant-a", "Assignment Tenant A"));
            db.Tenants.Add(new Tenant(tenantB, "assignment-tenant-b", "Assignment Tenant B"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.Campaigns.Add(campaign);
            db.Subjects.Add(tenantBSubject);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        tenantADb.Assignments.Add(Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantA,
            campaign.Id,
            "self",
            tenantBSubject.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_identified_queue_invitation_token_respondent_subject_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "Queue token subject guard pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(tenantA, version.Id);
        var tenantASubject = new Subject(Guid.NewGuid(), tenantA, displayName: "Tenant A subject");
        var tenantBSubject = new Subject(Guid.NewGuid(), tenantB, displayName: "Tenant B subject");
        var sameTenantTokenId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantA, "queue-token-tenant-a", "Queue Token Tenant A"));
            db.Tenants.Add(new Tenant(tenantB, "queue-token-tenant-b", "Queue Token Tenant B"));
            db.SurveyTemplates.Add(template);
            db.TemplateVersions.Add(version);
            db.Campaigns.Add(campaign);
            db.Subjects.AddRange(tenantASubject, tenantBSubject);
            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantADb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

            tenantADb.InvitationTokens.Add(new InvitationToken(
                sameTenantTokenId,
                tenantA,
                campaign.Id,
                "same-tenant-queue-token",
                InvitationTokenChannels.IdentifiedQueue,
                respondentSubjectId: tenantASubject.Id));

            await tenantADb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantADb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

            tenantADb.InvitationTokens.Add(new InvitationToken(
                Guid.NewGuid(),
                tenantA,
                campaign.Id,
                "cross-tenant-queue-token",
                InvitationTokenChannels.IdentifiedQueue,
                respondentSubjectId: tenantBSubject.Id));

            await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
        }

        await using (var tenantADb = new ApplicationDbContext(CreateRuntimeOptions()))
        {
            var tenantDbScope = new TenantDbScope(tenantADb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

            await Assert.ThrowsAsync<PostgresException>(() => tenantADb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE invitation_token SET respondent_subject_id = {tenantBSubject.Id} WHERE id = {sameTenantTokenId}"));
        }
    }

    [DockerFact]
    public async Task Rls_allows_identified_assignment_for_identified_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "identified-assignment",
            ResponseIdentityModes.Identified,
            includeRespondent: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.RespondentId!.Value));

        await tenantDb.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_anonymous_assignment_for_identified_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "identified-anonymous-assignment",
            ResponseIdentityModes.Identified,
            includeInvitationToken: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.InviteTokenId!.Value));

        await AssertAssignmentIdentityModeBlockedAsync(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_allows_anonymous_assignment_for_anonymous_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "anonymous-assignment",
            ResponseIdentityModes.Anonymous,
            includeInvitationToken: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.InviteTokenId!.Value));

        await tenantDb.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_identified_assignment_for_anonymous_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "anonymous-identified-assignment",
            ResponseIdentityModes.Anonymous,
            includeRespondent: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.RespondentId!.Value));

        await AssertAssignmentIdentityModeBlockedAsync(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_allows_anonymous_assignment_for_anonymous_longitudinal_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "anonymous-longitudinal-assignment",
            ResponseIdentityModes.AnonymousLongitudinal,
            includeInvitationToken: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.InviteTokenId!.Value));

        await tenantDb.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Rls_blocks_identified_assignment_for_anonymous_longitudinal_campaign()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixture = await SeedAssignmentIdentityFixtureAsync(
            migratorOptions,
            tenantId,
            "anonymous-longitudinal-identified-assignment",
            ResponseIdentityModes.AnonymousLongitudinal,
            includeRespondent: true);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        tenantDb.Assignments.Add(Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.RespondentId!.Value));

        await AssertAssignmentIdentityModeBlockedAsync(() => tenantDb.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_notification_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await SeedNotificationFixtureAsync(migratorOptions, tenantA, "notification-tenant-a");
        await SeedNotificationFixtureAsync(migratorOptions, tenantB, "notification-tenant-b");

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleNotifications = await tenantADb.Notifications.ToListAsync();

        var visibleNotification = Assert.Single(visibleNotifications);
        Assert.Equal(tenantA, visibleNotification.TenantId);
    }

    [DockerFact]
    public async Task Rls_blocks_notification_for_assignment_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        var fixtureA = await SeedNotificationFixtureAsync(migratorOptions, tenantA, "notification-guard-a");
        var fixtureB = await SeedNotificationFixtureAsync(migratorOptions, tenantB, "notification-guard-b");

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        tenantADb.Notifications.Add(Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            tenantA,
            fixtureA.CampaignId,
            fixtureB.AssignmentId,
            "respondent@example.com"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => tenantADb.SaveChangesAsync());
        Assert.Contains(
            "notification campaign and assignment must belong to the same tenant campaign",
            exception.InnerException?.Message ?? exception.Message);
    }

    [DockerFact]
    public async Task Migrations_enforce_template_and_instrument_question_links()
    {
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "OLBI");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "items", "Items");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "likert_1_4",
            ScaleTypes.Likert,
            1,
            4,
            1,
            naAllowed: false,
            anchors: """[{"value":1,"label_default":"Strongly agree"},{"value":4,"label_default":"Strongly disagree"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "olbi_01",
            QuestionTypes.Likert,
            scale.Id,
            "I always find new and interesting aspects in my work.");
        var instrument = Instrument.CreateCanonical(
            Guid.NewGuid(),
            "olbi",
            "1.0.0",
            "Oldenburg Burnout Inventory",
            InstrumentDomains.Psychometric,
            "Demerouti et al. (2003)",
            InstrumentLicenseTypes.FreeAcademic,
            canonicalTemplateVersionId: version.Id);
        var migratorOptions = CreateMigratorOptions();

        await using var db = new ApplicationDbContext(migratorOptions);
        await db.Database.MigrateAsync();
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.Add(question);
        db.Instruments.Add(instrument);
        db.InstrumentItems.Add(new InstrumentItem(
            Guid.NewGuid(),
            instrument.Id,
            1,
            "OLBI_01",
            "EX",
            reverseCoded: true,
            questionId: question.Id));

        await db.SaveChangesAsync();

        db.InstrumentItems.Add(new InstrumentItem(
            Guid.NewGuid(),
            instrument.Id,
            2,
            "OLBI_02",
            "EX",
            reverseCoded: false,
            questionId: Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Audit_interceptor_writes_added_and_modified_rows_inside_tenant_scope()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var currentAuditContext = new CurrentAuditContext();
        currentAuditContext.SetActor(userId, AuditActorTypes.User);
        currentAuditContext.SetCorrelationId(Guid.NewGuid());

        var runtimeOptions = CreateRuntimeOptions(new AuditSaveChangesInterceptor(currentTenant, currentAuditContext));
        await using var dbWithAudit = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(dbWithAudit);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        dbWithAudit.Tenants.Add(new Tenant(tenantId, "acme-audit", "Acme Audit"));
        dbWithAudit.UserAccounts.Add(new UserAccount(userId, tenantId, "audited@example.com"));

        await dbWithAudit.SaveChangesAsync();

        var user = await dbWithAudit.UserAccounts.SingleAsync(entity => entity.Id == userId);
        user.ChangeLocale("hr");

        await dbWithAudit.SaveChangesAsync();

        var auditRows = await dbWithAudit.AuditEvents
            .Where(auditEvent => auditEvent.EntityType == nameof(UserAccount))
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ToListAsync();

        Assert.Contains(auditRows, auditEvent =>
            auditEvent.ChangeKind == AuditChangeKinds.Added &&
            auditEvent.EntityId == userId.ToString());

        var modified = Assert.Single(auditRows, auditEvent => auditEvent.ChangeKind == AuditChangeKinds.Modified);
        Assert.Equal(userId.ToString(), modified.EntityId);
        Assert.Equal("en", modified.Before!.RootElement.GetProperty(nameof(UserAccount.Locale)).GetString());
        Assert.Equal("hr", modified.After!.RootElement.GetProperty(nameof(UserAccount.Locale)).GetString());
        Assert.False(modified.After.RootElement.TryGetProperty(nameof(UserAccount.Email), out _));

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Audit_rows_roll_back_with_domain_changes()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var runtimeOptions = CreateRuntimeOptions(new AuditSaveChangesInterceptor(currentTenant, new CurrentAuditContext()));

        await using (var dbWithAudit = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(dbWithAudit);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

            dbWithAudit.Tenants.Add(new Tenant(tenantId, "rollback-audit", "Rollback Audit"));
            dbWithAudit.UserAccounts.Add(new UserAccount(userId, tenantId, "rollback@example.com"));

            await dbWithAudit.SaveChangesAsync();
        }

        await using var verifier = new ApplicationDbContext(migratorOptions);
        Assert.Equal(0, await verifier.AuditEvents.CountAsync(auditEvent => auditEvent.TenantId == tenantId));
        Assert.Equal(0, await verifier.UserAccounts.CountAsync(user => user.Id == userId));
    }

    [DockerFact]
    public async Task Rls_blocks_cross_tenant_audit_event_reads()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.AuditEvents.Add(new AuditEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                tenantA,
                AuditActorTypes.System,
                null,
                null,
                nameof(Tenant),
                tenantA.ToString(),
                AuditChangeKinds.Added,
                null,
                AuditJson.Create(new Dictionary<string, object?> { [nameof(Tenant.Name)] = "Tenant A" }),
                null));
            db.AuditEvents.Add(new AuditEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                tenantB,
                AuditActorTypes.System,
                null,
                null,
                nameof(Tenant),
                tenantB.ToString(),
                AuditChangeKinds.Added,
                null,
                AuditJson.Create(new Dictionary<string, object?> { [nameof(Tenant.Name)] = "Tenant B" }),
                null));

            await db.SaveChangesAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantADb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantADb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);

        var visibleRows = await tenantADb.AuditEvents.ToListAsync();

        var visibleRow = Assert.Single(visibleRows);
        Assert.Equal(tenantA, visibleRow.TenantId);
    }

    [DockerFact]
    public async Task Audit_event_rejects_update_and_delete()
    {
        var tenantId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.AuditEvents.Add(new AuditEvent(
                auditId,
                occurredAt,
                tenantId,
                AuditActorTypes.System,
                null,
                null,
                nameof(Tenant),
                tenantId.ToString(),
                AuditChangeKinds.Added,
                null,
                null,
                null));

            await db.SaveChangesAsync();
        }

        await using var verifier = new ApplicationDbContext(migratorOptions);

        await Assert.ThrowsAsync<PostgresException>(() =>
            verifier.Database.ExecuteSqlRawAsync(
                "UPDATE audit_event SET reason = 'tamper' WHERE id = {0} AND occurred_at = {1}",
                auditId,
                occurredAt));

        await Assert.ThrowsAsync<PostgresException>(() =>
            verifier.Database.ExecuteSqlRawAsync(
                "DELETE FROM audit_event WHERE id = {0} AND occurred_at = {1}",
                auditId,
                occurredAt));
    }

    [DockerFact]
    public async Task Outbox_interceptor_writes_event_inside_domain_transaction()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var outboxBuffer = new OutboxEventBuffer();

        var runtimeOptions = CreateRuntimeOptions(new OutboxSaveChangesInterceptor(
            currentTenant,
            new CurrentAuditContext(),
            outboxBuffer));
        await using var dbWithOutbox = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(dbWithOutbox);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        dbWithOutbox.Tenants.Add(new Tenant(tenantId, "outbox-audit", "Outbox Audit"));
        dbWithOutbox.UserAccounts.Add(new UserAccount(userId, tenantId, "outbox@example.com"));
        outboxBuffer.Enqueue(new OutboxMessage(
            aggregateId,
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantId,
                ["schema_version"] = 1
            })));

        await dbWithOutbox.SaveChangesAsync();

        var outboxEvent = await dbWithOutbox.OutboxEvents.SingleAsync();

        Assert.Equal(tenantId, outboxEvent.TenantId);
        Assert.Equal(aggregateId, outboxEvent.AggregateId);
        Assert.Equal("tenant", outboxEvent.AggregateType);
        Assert.Equal("TenantCreated", outboxEvent.EventType);
        Assert.Null(outboxEvent.PublishedAt);

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Outbox_events_roll_back_with_domain_changes()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var outboxBuffer = new OutboxEventBuffer();
        var runtimeOptions = CreateRuntimeOptions(new OutboxSaveChangesInterceptor(
            currentTenant,
            new CurrentAuditContext(),
            outboxBuffer));

        await using (var dbWithOutbox = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(dbWithOutbox);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

            dbWithOutbox.Tenants.Add(new Tenant(tenantId, "outbox-rollback", "Outbox Rollback"));
            dbWithOutbox.UserAccounts.Add(new UserAccount(userId, tenantId, "outbox-rollback@example.com"));
            outboxBuffer.Enqueue(new OutboxMessage(
                tenantId,
                "tenant",
                "TenantCreated",
                OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantId })));

            await dbWithOutbox.SaveChangesAsync();
        }

        await using var verifier = new ApplicationDbContext(migratorOptions);
        Assert.Equal(0, await verifier.OutboxEvents.CountAsync(outboxEvent => outboxEvent.TenantId == tenantId));
        Assert.Equal(0, await verifier.UserAccounts.CountAsync(user => user.Id == userId));
    }

    [DockerFact]
    public async Task Outbox_relay_worker_role_discovers_due_tenants_and_processes_inside_tenant_scope()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var outboxEventA = OutboxEvent.Create(
            tenantA,
            Guid.NewGuid(),
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantA }),
            null);
        var outboxEventB = OutboxEvent.Create(
            tenantB,
            Guid.NewGuid(),
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantB }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.AddRange(
                new Tenant(tenantA, "outbox-worker-a", "Outbox Worker A"),
                new Tenant(tenantB, "outbox-worker-b", "Outbox Worker B"));
            db.OutboxEvents.AddRange(outboxEventA, outboxEventB);
            await db.SaveChangesAsync();
        }

        await CreateWorkerRoleAsync(migratorOptions);

        var dispatcher = new RecordingOutboxDispatcher();
        var currentTenant = new CurrentTenant();
        await using var relayDb = new ApplicationDbContext(CreateWorkerOptions());
        var relay = new OutboxRelay(relayDb, dispatcher, new TenantDbScope(relayDb), currentTenant);

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        Assert.Single(dispatcher.DispatchedEventIds);

        var secondDispatcher = new RecordingOutboxDispatcher();
        var secondCurrentTenant = new CurrentTenant();
        await using var secondRelayDb = new ApplicationDbContext(CreateWorkerOptions());
        var secondRelay = new OutboxRelay(
            secondRelayDb,
            secondDispatcher,
            new TenantDbScope(secondRelayDb),
            secondCurrentTenant);

        var secondProcessed = await secondRelay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, secondProcessed);
        Assert.Contains(outboxEventA.Id, dispatcher.DispatchedEventIds.Concat(secondDispatcher.DispatchedEventIds));
        Assert.Contains(outboxEventB.Id, dispatcher.DispatchedEventIds.Concat(secondDispatcher.DispatchedEventIds));

        await using var verifier = new ApplicationDbContext(migratorOptions);
        Assert.All(
            await verifier.OutboxEvents
                .Where(entity => entity.Id == outboxEventA.Id || entity.Id == outboxEventB.Id)
                .ToListAsync(),
            entity => Assert.NotNull(entity.PublishedAt));
    }

    [DockerFact]
    public async Task Outbox_relay_sets_application_tenant_context_for_report_pdf_terminal_notification_handler()
    {
        var tenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        var outboxEvent = OutboxEvent.Create(
            tenantId,
            exportArtifactId,
            "export_artifact",
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["export_artifact_id"] = exportArtifactId,
                ["campaign_series_id"] = campaignSeriesId,
                ["artifact_type"] = ExportArtifactTypes.CampaignSeriesReportPdf,
                ["target_kind"] = ExportArtifactTargetKinds.CampaignSeries,
                ["format"] = ExportArtifactFormats.Pdf,
                ["status"] = ExportArtifactStatuses.Succeeded,
                ["failure_reason_code"] = null
            }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant(tenantId, "outbox-worker-notify", "Outbox Worker Notify"));
            db.OutboxEvents.Add(outboxEvent);
            await db.SaveChangesAsync();
        }

        await CreateWorkerRoleAsync(migratorOptions);

        var currentTenant = new CurrentTenant();
        await using var relayDb = new ApplicationDbContext(CreateWorkerOptions(
            new AuditSaveChangesInterceptor(currentTenant, new CurrentAuditContext())));
        var tenantDbScope = new TenantDbScope(relayDb);
        var notificationStore = new OperationalNotificationStore(relayDb, tenantDbScope);
        var dispatcher = new OutboxEventDispatcher(
        [
            new ReportPdfArtifactTerminalStateReachedOutboxHandler(notificationStore)
        ]);
        var relay = new OutboxRelay(relayDb, dispatcher, tenantDbScope, currentTenant);

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(tenantId, currentTenant.TenantId);

        await using var verifier = new ApplicationDbContext(migratorOptions);
        var notification = await verifier.OperationalNotifications.SingleAsync(
            entity => entity.SourceAggregateId == exportArtifactId);
        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(
            OperationalNotification.SourceEventTypeReportPdfArtifactTerminalStateReached,
            notification.SourceEventType);
        Assert.Equal(1, await verifier.AuditEvents.CountAsync(entity => entity.TenantId == tenantId));
    }

    [DockerFact]
    public async Task Outbox_relay_marks_dispatched_event_as_published()
    {
        var tenantId = Guid.NewGuid();
        var outboxEvent = OutboxEvent.Create(
            tenantId,
            Guid.NewGuid(),
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantId }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.OutboxEvents.Add(outboxEvent);
            await db.SaveChangesAsync();
        }

        var dispatcher = new RecordingOutboxDispatcher();
        await using var relayDb = new ApplicationDbContext(migratorOptions);
        var relay = new OutboxRelay(relayDb, dispatcher, new TenantDbScope(relayDb), new CurrentTenant());

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        Assert.Equal([outboxEvent.Id], dispatcher.DispatchedEventIds);

        await using var verifier = new ApplicationDbContext(migratorOptions);
        var saved = await verifier.OutboxEvents.SingleAsync(entity => entity.Id == outboxEvent.Id);
        Assert.NotNull(saved.PublishedAt);
        Assert.Equal(0, saved.RetryCount);
        Assert.Null(saved.NextRetryAt);
    }

    [DockerFact]
    public async Task Outbox_relay_schedules_retry_after_dispatch_failure()
    {
        var tenantId = Guid.NewGuid();
        var outboxEvent = OutboxEvent.Create(
            tenantId,
            Guid.NewGuid(),
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantId }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.OutboxEvents.Add(outboxEvent);
            await db.SaveChangesAsync();
        }

        await using var relayDb = new ApplicationDbContext(migratorOptions);
        var relay = new OutboxRelay(
            relayDb,
            new ThrowingOutboxDispatcher(),
            new TenantDbScope(relayDb),
            new CurrentTenant());

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);

        await using var verifier = new ApplicationDbContext(migratorOptions);
        var saved = await verifier.OutboxEvents.SingleAsync(entity => entity.Id == outboxEvent.Id);
        Assert.Null(saved.PublishedAt);
        Assert.Equal(1, saved.RetryCount);
        Assert.NotNull(saved.NextRetryAt);
        Assert.NotNull(saved.LastError);
        Assert.Contains("DISPATCH_FAILED", saved.LastError, StringComparison.Ordinal);
        Assert.Contains("exception_type=InvalidOperationException", saved.LastError, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task Outbox_relay_sanitizes_dispatch_failure_metadata()
    {
        var tenantId = Guid.NewGuid();
        var outboxEvent = OutboxEvent.Create(
            tenantId,
            Guid.NewGuid(),
            "tenant",
            "TenantCreated",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantId }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.OutboxEvents.Add(outboxEvent);
            await db.SaveChangesAsync();
        }

        await using var relayDb = new ApplicationDbContext(migratorOptions);
        var relay = new OutboxRelay(
            relayDb,
            new ThrowingOutboxDispatcher(SensitiveLogAssert.JoinSentinels()),
            new TenantDbScope(relayDb),
            new CurrentTenant());

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);

        await using var verifier = new ApplicationDbContext(migratorOptions);
        var saved = await verifier.OutboxEvents.SingleAsync(entity => entity.Id == outboxEvent.Id);
        Assert.Null(saved.PublishedAt);
        Assert.Equal(1, saved.RetryCount);
        Assert.NotNull(saved.NextRetryAt);
        Assert.NotNull(saved.LastError);

        var lastError = saved.LastError!;
        Assert.Contains("DISPATCH_FAILED", lastError, StringComparison.Ordinal);
        Assert.Contains("event_type=TenantCreated", lastError, StringComparison.Ordinal);
        Assert.Contains("aggregate_type=tenant", lastError, StringComparison.Ordinal);
        Assert.Contains("exception_type=InvalidOperationException", lastError, StringComparison.Ordinal);
        foreach (var sentinel in SensitiveLogAssert.DefaultSentinels)
        {
            Assert.DoesNotContain(sentinel, lastError, StringComparison.OrdinalIgnoreCase);
        }
    }

    [DockerFact]
    public async Task Outbox_relay_schedules_retry_when_dispatcher_has_no_registered_handler()
    {
        var tenantId = Guid.NewGuid();
        var outboxEvent = OutboxEvent.Create(
            tenantId,
            Guid.NewGuid(),
            "tenant",
            "UnknownEvent",
            OutboxPayload.Create(new Dictionary<string, object?> { ["tenant_id"] = tenantId }),
            null);
        var migratorOptions = CreateMigratorOptions();

        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
            db.OutboxEvents.Add(outboxEvent);
            await db.SaveChangesAsync();
        }

        await using var relayDb = new ApplicationDbContext(migratorOptions);
        var dispatcher = new OutboxEventDispatcher([new NoOpOutboxEventHandler("InvitationEmailQueued")]);
        var relay = new OutboxRelay(relayDb, dispatcher, new TenantDbScope(relayDb), new CurrentTenant());

        var processed = await relay.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);

        await using var verifier = new ApplicationDbContext(migratorOptions);
        var saved = await verifier.OutboxEvents.SingleAsync(entity => entity.Id == outboxEvent.Id);
        Assert.Null(saved.PublishedAt);
        Assert.Equal(1, saved.RetryCount);
        Assert.NotNull(saved.NextRetryAt);
        Assert.NotNull(saved.LastError);
        Assert.Contains("DISPATCH_FAILED", saved.LastError, StringComparison.Ordinal);
        Assert.Contains("event_type=UnknownEvent", saved.LastError, StringComparison.Ordinal);
        Assert.Contains(
            "exception_type=OutboxEventHandlerNotFoundException",
            saved.LastError,
            StringComparison.Ordinal);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private sealed class InsertCompetingIdentifiedQueueTokensInterceptor(
        DbContextOptions<ApplicationDbContext> runtimeOptions) : SaveChangesInterceptor
    {
        private int _inserted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _inserted, 1) != 0 || eventData.Context is null)
            {
                return result;
            }

            var queueCredentials = eventData.Context.ChangeTracker
                .Entries<InvitationToken>()
                .Where(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.Channel == InvitationTokenChannels.IdentifiedQueue &&
                    entry.Entity.RespondentSubjectId.HasValue)
                .Select(entry => new
                {
                    entry.Entity.TenantId,
                    entry.Entity.CampaignId,
                    RespondentSubjectId = entry.Entity.RespondentSubjectId!.Value
                })
                .Distinct()
                .ToArray();

            if (queueCredentials.Length == 0)
            {
                return result;
            }

            await using var db = new ApplicationDbContext(runtimeOptions);
            var tenantDbScope = new TenantDbScope(db);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(
                queueCredentials[0].TenantId,
                cancellationToken: cancellationToken);

            for (var index = 0; index < queueCredentials.Length; index++)
            {
                var credential = queueCredentials[index];
                db.InvitationTokens.Add(new InvitationToken(
                    PlatformIds.NewId(),
                    credential.TenantId,
                    credential.CampaignId,
                    $"competing-identified-queue-token-hash-{index}",
                    InvitationTokenChannels.IdentifiedQueue,
                    respondentSubjectId: credential.RespondentSubjectId));
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
    }

    private DbContextOptions<ApplicationDbContext> CreateMigratorOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private DbContextOptions<ApplicationDbContext> CreateRuntimeOptions(params IInterceptor[] interceptors)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private DbContextOptions<ApplicationDbContext> CreateWorkerOptions(params IInterceptor[] interceptors)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = WorkerUsername,
            Password = WorkerPassword
        }.ConnectionString;

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static async Task CreateRuntimeRoleAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            $$"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_roles
                    WHERE rolname = '{{RuntimeUsername}}'
                ) THEN
                    CREATE ROLE {{RuntimeUsername}} LOGIN PASSWORD '{{RuntimePassword}}';
                END IF;
            END
            $$;

            GRANT USAGE ON SCHEMA public TO {{RuntimeUsername}};
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
                tenant,
                user_account,
                external_auth_identity,
                auth_session,
                role,
                permission,
                role_permission,
                role_assignment,
                subject,
                subject_group,
                subject_membership,
                subject_relationship,
                directory_connection,
                directory_import_rule,
                directory_import_run,
                directory_import_run_item,
                instrument,
                instrument_subscale,
                instrument_item,
                instrument_norm,
                translation,
                survey_template,
                template_version,
                scoring_rule,
                score_run,
                score,
                export_artifact,
                campaign_series,
                campaign,
                campaign_launch_snapshot,
                consent_document,
                retention_policy,
                retention_due_batch,
                withdrawal_event,
                withdrawal_request_token,
                disclosure_policy,
                consent_record,
                audience,
                audience_member,
                respondent_rule,
                assignment,
                invitation_token,
                notification,
                email_template,
                notification_delivery_attempt,
                notification_delivery_event,
                email_suppression,
                operational_notification,
                participant_code,
                response_session,
                answer,
                section,
                scale,
                question,
                choice_option
            TO {{RuntimeUsername}};

            GRANT SELECT, INSERT ON TABLE
                audit_event,
                outbox_event
            TO {{RuntimeUsername}};

            GRANT SELECT ON TABLE
                worker_heartbeat
            TO {{RuntimeUsername}};
            """);
    }

    private static async Task AssertTenantScopedTableAsync(
        DbContextOptions<ApplicationDbContext> options,
        string tableName)
    {
        await using var db = new ApplicationDbContext(options);

        var exists = await db.Database.SqlQueryRaw<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = {0}
                ) AS "Value"
                """,
                tableName)
            .SingleAsync();
        var tenantScoped = await db.Database.SqlQueryRaw<bool>(
                """
                SELECT (
                    c.relrowsecurity
                    AND c.relforcerowsecurity
                    AND EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_attribute AS a
                        WHERE a.attrelid = c.oid
                          AND a.attname = 'tenant_id'
                          AND NOT a.attisdropped
                    )
                    AND EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_policy AS p
                        WHERE p.polrelid = c.oid
                    )
                ) AS "Value"
                FROM pg_catalog.pg_class AS c
                JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace
                WHERE n.nspname = 'public'
                  AND c.relname = {0}
                """,
                tableName)
            .SingleAsync();

        Assert.True(exists, $"Expected table '{tableName}' to exist.");
        Assert.True(tenantScoped, $"Expected table '{tableName}' to have tenant_id and forced row-level security policy.");
    }

    private static async Task CreateWorkerRoleAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            $$"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_roles
                    WHERE rolname = '{{WorkerUsername}}'
                ) THEN
                    CREATE ROLE {{WorkerUsername}};
                END IF;
            END
            $$;

            ALTER ROLE {{WorkerUsername}} LOGIN PASSWORD '{{WorkerPassword}}';
            GRANT USAGE ON SCHEMA public TO {{WorkerUsername}};
            GRANT SELECT, INSERT, UPDATE ON TABLE
                outbox_event,
                worker_heartbeat,
                operational_notification
            TO {{WorkerUsername}};

            GRANT SELECT, INSERT ON TABLE
                audit_event
            TO {{WorkerUsername}};
            """);
    }

    private static IConfiguration CreateOidcResolverConfiguration(int sessionMinutes)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:SessionMinutes"] =
                    sessionMinutes.ToString(CultureInfo.InvariantCulture)
            })
            .Build();
    }

    private static async Task SeedTenantGraphAsync(
        DbContextOptions<ApplicationDbContext> options,
        TenantGraph graph)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(graph.TenantId);

        db.Tenants.Add(new Tenant(graph.TenantId, graph.TenantSlug, graph.TenantName));
        db.UserAccounts.Add(new UserAccount(graph.UserId, graph.TenantId, graph.UserEmail));
        db.Roles.Add(new Role(graph.RoleId, graph.TenantId, "tenant_admin", "Tenant Admin"));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private sealed record TenantGraph(
        Guid TenantId,
        string TenantSlug,
        string TenantName,
        Guid UserId,
        string UserEmail,
        Guid RoleId)
    {
        public static TenantGraph Create(string slug, string name, string userEmail)
        {
            return new TenantGraph(
                Guid.NewGuid(),
                slug,
                name,
                Guid.NewGuid(),
                userEmail,
                Guid.NewGuid());
        }
    }

    private static Instrument CreateCanonicalInstrument()
    {
        return Instrument.CreateCanonical(
            Guid.NewGuid(),
            "olbi",
            "1.0.0",
            "Oldenburg Burnout Inventory",
            InstrumentDomains.Psychometric,
            "Demerouti et al. (2003)",
            InstrumentLicenseTypes.FreeAcademic,
            licenseTermsUrl: "https://example.com/olbi-license");
    }

    private static ScoringRule CreateScoringRule(Guid templateVersionId)
    {
        return ScoringRule.CreateDraft(
            Guid.NewGuid(),
            templateVersionId,
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            """{"rule_id":"burnout.total","version":"1.0.0"}""",
            """{"scores":["total"]}""");
    }

    private static CampaignSeries CreateCampaignSeries(Guid tenantId)
    {
        return new CampaignSeries(
            Guid.NewGuid(),
            tenantId,
            "Campaign series",
            CreateCodeSalt());
    }

    private static Campaign CreateCampaign(
        Guid tenantId,
        Guid templateVersionId,
        Guid? campaignSeriesId = null,
        string name = "Campaign",
        string responseIdentityMode = ResponseIdentityModes.Identified)
    {
        return new Campaign(
            Guid.NewGuid(),
            tenantId,
            templateVersionId,
            name,
            responseIdentityMode,
            campaignSeriesId: campaignSeriesId,
            schedule: """{"kind":"one_shot"}""");
    }

    private static CreateTemplateVersionRequest SampleSetupTemplateRequest(
        string templateName = "Private burnout pulse",
        string questionCode = "q01")
    {
        return new CreateTemplateVersionRequest(
            templateName,
            "1.0.0",
            "en",
            InstrumentId: null,
            Sections:
            [
                new CreateTemplateSectionRequest(1, "core", "Core")
            ],
            Scales:
            [
                new CreateQuestionScaleRequest(
                    "agreement",
                    ScaleTypes.Likert,
                    1,
                    5,
                    1,
                    NaAllowed: false,
                    """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""")
            ],
            Questions:
            [
                new CreateTemplateQuestionRequest(
                    1,
                    questionCode,
                    QuestionTypes.Likert,
                    "I feel depleted after work.",
                    SectionCode: "core",
                    ScaleCode: "agreement",
                    MeasurementLevel: MeasurementLevels.Ordinal)
            ]);
    }

    private static async Task<Guid> CreateSetupCampaignSeriesAsync(
        SetupWorkflowStore setupStore,
        Guid tenantId,
        string name)
    {
        var series = await setupStore.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest(name),
            CancellationToken.None);

        Assert.True(series.IsSuccess, series.Error.ToString());

        return series.Value.Id;
    }

    private async Task<PublicOpenLinkScenario> CreatePublicOpenLinkScenarioAsync(
        Guid tenantId,
        string responseIdentityMode,
        string name)
    {
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var participantCodeStore = new ParticipantCodeStore(
            tenantDb,
            tenantDbScope,
            new DeterministicParticipantCodeHasher());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope, participantCodeStore);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                """{"scores":["total"]}"""),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(
            setupStore,
            tenantId,
            $"{name} study");
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                $"{name} wave",
                responseIdentityMode,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());
        Assert.True(entry.IsSuccess, entry.Error.ToString());

        return new PublicOpenLinkScenario(
            tenantDb,
            tenantDbScope,
            responseStore,
            tenantId,
            seriesId,
            campaign.Value.Id,
            openLink.Value.Token,
            entry.Value);
    }

    private async Task<TwoWaveProofScenario> CreateTwoWaveProofScenarioAsync(
        Guid tenantId,
        string name,
        string produces = """{"scores":["total"]}""",
        bool includeMatrixQuestion = false,
        bool includeDisplayLogicQuestion = false)
    {
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(
            migratorOptions,
            tenantId,
            includeMatrixQuestion,
            includeDisplayLogicQuestion);

        await CreateRuntimeRoleAsync(migratorOptions);

        var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var participantCodeStore = new ParticipantCodeStore(
            tenantDb,
            tenantDbScope,
            new DeterministicParticipantCodeHasher());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope, participantCodeStore);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);
        var proofStore = new CampaignSeriesProofStore(tenantDb, tenantDbScope);
        var waveComparisonProofStore = new WaveComparisonProofStore(tenantDb, tenantDbScope);

        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                $"burnout.{Guid.NewGuid():N}.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                produces),
            CancellationToken.None);
        var seriesId = await CreateSetupCampaignSeriesAsync(setupStore, tenantId, $"{name} series");
        var wave1 = await CreateLaunchedLongitudinalWaveAsync(
            setupStore,
            responseStore,
            tenantId,
            versionId,
            seriesId,
            $"{name} Wave 1");
        var wave2 = await CreateLaunchedLongitudinalWaveAsync(
            setupStore,
            responseStore,
            tenantId,
            versionId,
            seriesId,
            $"{name} Wave 2");

        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());

        return new TwoWaveProofScenario(
            tenantDb,
            tenantDbScope,
            responseStore,
            scoreStore,
            proofStore,
            waveComparisonProofStore,
            tenantId,
            seriesId,
            wave1,
            wave2);
    }

    private async Task<TwoWaveProofScenario> CreateTwoWaveMixedScoringProofScenarioAsync(
        Guid tenantId,
        string name,
        string comparisonCompatibility)
    {
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var participantCodeStore = new ParticipantCodeStore(
            tenantDb,
            tenantDbScope,
            new DeterministicParticipantCodeHasher());
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope, participantCodeStore);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);
        var proofStore = new CampaignSeriesProofStore(tenantDb, tenantDbScope);
        var waveComparisonProofStore = new WaveComparisonProofStore(tenantDb, tenantDbScope);

        var baselineScoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            CreateBurnoutTotalScoringRuleRequest(versionId, "1.0.0"),
            CancellationToken.None);
        Assert.True(baselineScoringRule.IsSuccess, baselineScoringRule.Error.ToString());

        var seriesId = await CreateSetupCampaignSeriesAsync(setupStore, tenantId, $"{name} series");
        var wave1 = await CreateLaunchedLongitudinalWaveAsync(
            setupStore,
            responseStore,
            tenantId,
            versionId,
            seriesId,
            $"{name} Wave 1");

        var comparisonScoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            CreateBurnoutTotalScoringRuleRequest(versionId, "2.0.0", comparisonCompatibility),
            CancellationToken.None);
        Assert.True(comparisonScoringRule.IsSuccess, comparisonScoringRule.Error.ToString());

        var wave2 = await CreateLaunchedLongitudinalWaveAsync(
            setupStore,
            responseStore,
            tenantId,
            versionId,
            seriesId,
            $"{name} Wave 2");

        return new TwoWaveProofScenario(
            tenantDb,
            tenantDbScope,
            responseStore,
            scoreStore,
            proofStore,
            waveComparisonProofStore,
            tenantId,
            seriesId,
            wave1,
            wave2);
    }

    private static CreateScoringRuleRequest CreateBurnoutTotalScoringRuleRequest(
        Guid templateVersionId,
        string ruleVersion,
        string compatibility = "{}")
    {
        return new CreateScoringRuleRequest(
            templateVersionId,
            "burnout.total",
            ruleVersion,
            "scoring-rule/v1",
            "engine/v1",
            $$"""{"rule_id":"burnout.total","version":"{{ruleVersion}}","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
            """{"scores":["total"]}""",
            compatibility);
    }

    private static async Task<TwoWaveProofWaveScenario> CreateLaunchedLongitudinalWaveAsync(
        SetupWorkflowStore setupStore,
        ResponseCaptureStore responseStore,
        Guid tenantId,
        Guid templateVersionId,
        Guid seriesId,
        string waveName)
    {
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                templateVersionId,
                waveName,
                ResponseIdentityModes.AnonymousLongitudinal,
                CampaignSeriesId: seriesId),
            CancellationToken.None);
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());

        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(launched.IsSuccess, launched.Error.ToString());

        var openLink = await setupStore.CreateCampaignOpenLinkAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);
        Assert.True(openLink.IsSuccess, openLink.Error.ToString());

        var entry = await responseStore.GetOpenLinkEntryAsync(
            openLink.Value.Token,
            CancellationToken.None);
        Assert.True(entry.IsSuccess, entry.Error.ToString());

        return new TwoWaveProofWaveScenario(campaign.Value.Id, openLink.Value.Token, entry.Value);
    }

    private static async Task<Guid> SubmitTwoWaveProofResponseAsync(
        TwoWaveProofScenario scenario,
        TwoWaveProofWaveScenario wave,
        string participantCode,
        string value)
    {
        var questionByCode = wave.Entry.Questions.ToDictionary(question => question.Code, StringComparer.Ordinal);
        var questionId = questionByCode["q01"].Id;
        var session = await scenario.ResponseStore.CreateOpenLinkSessionAsync(
            wave.Token,
            CreateOpenLinkSessionRequestFor(wave.Entry, participantCode),
            CancellationToken.None);
        Assert.True(session.IsSuccess, session.Error.ToString());

        var saved = await scenario.ResponseStore.SaveOpenLinkAnswersAsync(
            wave.Token,
            session.Value.Id,
            new SaveAnswersRequest(CreateTwoWaveProofAnswerRequests(questionId, questionByCode, value)),
            CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.Error.ToString());

        var submitted = await scenario.ResponseStore.SubmitOpenLinkSessionAsync(
            wave.Token,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200),
            CancellationToken.None);
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());

        return session.Value.Id;
    }

    private static SaveAnswerRequest[] CreateTwoWaveProofAnswerRequests(
        Guid scoreQuestionId,
        IReadOnlyDictionary<string, RespondentQuestionResponse> questionByCode,
        string value)
    {
        var requests = new List<SaveAnswerRequest>
        {
            new(scoreQuestionId, value)
        };

        if (questionByCode.TryGetValue("body_discomfort", out var matrixQuestion))
        {
            requests.Add(new SaveAnswerRequest(
                matrixQuestion.Id,
                """{"r01":"c02","r02":"c03"}"""));
        }

        return requests.ToArray();
    }

    private static async Task SubmitAndScoreTwoWaveProofResponseAsync(
        TwoWaveProofScenario scenario,
        TwoWaveProofWaveScenario wave,
        string participantCode,
        string value)
    {
        var sessionId = await SubmitTwoWaveProofResponseAsync(
            scenario,
            wave,
            participantCode,
            value);
        var scored = await scenario.ScoreStore.ComputeResponseScoresAsync(
            scenario.TenantId,
            sessionId,
            CancellationToken.None);
        Assert.True(scored.IsSuccess, scored.Error.ToString());
    }

    private static async Task SubmitFiveLinkedScoredWaveComparisonResponsesAsync(TwoWaveProofScenario scenario)
    {
        var participantCodes = new[] { "alpha-001", "bravo-002", "charlie-003", "delta-004", "echo-005" };
        var baselineValues = new[] { "3", "3", "4", "4", "5" };
        var comparisonValues = new[] { "4", "4", "5", "5", "5" };

        for (var index = 0; index < participantCodes.Length; index++)
        {
            await SubmitAndScoreTwoWaveProofResponseAsync(
                scenario,
                scenario.Wave1,
                participantCodes[index],
                baselineValues[index]);
            await SubmitAndScoreTwoWaveProofResponseAsync(
                scenario,
                scenario.Wave2,
                participantCodes[index],
                comparisonValues[index]);
        }
    }

    private static CreateOpenLinkSessionRequest CreateOpenLinkSessionRequestFor(
        OpenLinkEntryResponse entry,
        string? participantCode = null)
    {
        return new CreateOpenLinkSessionRequest(
            "en",
            entry.ConsentDocument.Id,
            entry.ConsentDocument.RequiredGrants,
            participantCode);
    }

    private sealed class PublicOpenLinkScenario(
        ApplicationDbContext db,
        TenantDbScope tenantDbScope,
        ResponseCaptureStore responseStore,
        Guid tenantId,
        Guid seriesId,
        Guid campaignId,
        string token,
        OpenLinkEntryResponse entry) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;

        public TenantDbScope TenantDbScope { get; } = tenantDbScope;

        public ResponseCaptureStore ResponseStore { get; } = responseStore;

        public Guid TenantId { get; } = tenantId;

        public Guid SeriesId { get; } = seriesId;

        public Guid CampaignId { get; } = campaignId;

        public string Token { get; } = token;

        public OpenLinkEntryResponse Entry { get; } = entry;

        public ValueTask DisposeAsync()
        {
            return Db.DisposeAsync();
        }
    }

    private sealed class TwoWaveProofScenario(
        ApplicationDbContext db,
        TenantDbScope tenantDbScope,
        ResponseCaptureStore responseStore,
        ScoreComputationStore scoreStore,
        CampaignSeriesProofStore proofStore,
        WaveComparisonProofStore waveComparisonProofStore,
        Guid tenantId,
        Guid seriesId,
        TwoWaveProofWaveScenario wave1,
        TwoWaveProofWaveScenario wave2) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;

        public TenantDbScope TenantDbScope { get; } = tenantDbScope;

        public ResponseCaptureStore ResponseStore { get; } = responseStore;

        public ScoreComputationStore ScoreStore { get; } = scoreStore;

        public CampaignSeriesProofStore ProofStore { get; } = proofStore;

        public WaveComparisonProofStore WaveComparisonProofStore { get; } = waveComparisonProofStore;

        public Guid TenantId { get; } = tenantId;

        public Guid SeriesId { get; } = seriesId;

        public TwoWaveProofWaveScenario Wave1 { get; } = wave1;

        public TwoWaveProofWaveScenario Wave2 { get; } = wave2;

        public ValueTask DisposeAsync()
        {
            return Db.DisposeAsync();
        }
    }

    private sealed record TwoWaveProofWaveScenario(
        Guid CampaignId,
        string Token,
        OpenLinkEntryResponse Entry);

    private async Task<ReportProofScenarioResult> CreateReportProofScenarioAsync(
        Guid tenantId,
        int submittedResponseCount,
        string produces = """{"scores":["total"]}""")
    {
        var migratorOptions = CreateMigratorOptions();
        var versionId = await SeedTenantTemplateVersionAsync(migratorOptions, tenantId);

        await CreateRuntimeRoleAsync(migratorOptions);

        await using var tenantDb = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(tenantDb);
        var setupStore = new SetupWorkflowStore(tenantDb, tenantDbScope);
        var responseStore = new ResponseCaptureStore(tenantDb, tenantDbScope);
        var scoreStore = new ScoreComputationStore(tenantDb, tenantDbScope);
        var reportStore = new ReportProofStore(tenantDb, tenantDbScope);

        var series = await setupStore.CreateCampaignSeriesAsync(
            tenantId,
            new CreateCampaignSeriesRequest("Report proof series"),
            CancellationToken.None);
        var scoringRule = await setupStore.CreateScoringRuleAsync(
            tenantId,
            new CreateScoringRuleRequest(
                versionId,
                "burnout.total",
                "1.0.0",
                "scoring-rule/v1",
                "engine/v1",
                """{"rule_id":"burnout.total","version":"1.0.0","operations":[{"op":"mean","items":["q01"],"output":"total"}]}""",
                produces),
            CancellationToken.None);
        var campaign = await setupStore.CreateCampaignAsync(
            tenantId,
            actorId: null,
            new CreateCampaignRequest(
                versionId,
                "Report proof wave",
                ResponseIdentityModes.Anonymous,
                CampaignSeriesId: series.Value.Id),
            CancellationToken.None);
        var launched = await setupStore.LaunchCampaignAsync(
            tenantId,
            actorId: null,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(series.IsSuccess, series.Error.ToString());
        Assert.True(scoringRule.IsSuccess, scoringRule.Error.ToString());
        Assert.True(campaign.IsSuccess, campaign.Error.ToString());
        Assert.True(launched.IsSuccess, launched.Error.ToString());

        for (var index = 0; index < submittedResponseCount; index++)
        {
            await SubmitAndScoreReportProofResponseAsync(
                tenantDb,
                tenantDbScope,
                responseStore,
                scoreStore,
                tenantId,
                campaign.Value.Id,
                value: ((index % 5) + 1).ToString());
        }

        var report = await reportStore.GetCampaignReportProofAsync(
            tenantId,
            campaign.Value.Id,
            CancellationToken.None);

        Assert.True(report.IsSuccess, report.Error.ToString());

        return new ReportProofScenarioResult(launched.Value, report.Value);
    }

    private static async Task SubmitAndScoreReportProofResponseAsync(
        ApplicationDbContext db,
        TenantDbScope tenantDbScope,
        ResponseCaptureStore responseStore,
        ScoreComputationStore scoreStore,
        Guid tenantId,
        Guid campaignId,
        string value)
    {
        var respondentCampaign = await responseStore.GetCampaignAsync(
            tenantId,
            campaignId,
            CancellationToken.None);
        Assert.True(respondentCampaign.IsSuccess, respondentCampaign.Error.ToString());
        var question = Assert.Single(respondentCampaign.Value.Questions);
        var assignmentId = await CreateReportProofAssignmentAsync(
            db,
            tenantDbScope,
            tenantId,
            campaignId,
            CancellationToken.None);
        var session = await responseStore.CreateSessionAsync(
            tenantId,
            new CreateResponseSessionRequest(assignmentId, "en"),
            CancellationToken.None);
        Assert.True(session.IsSuccess, session.Error.ToString());
        var saved = await responseStore.SaveAnswersAsync(
            tenantId,
            session.Value.Id,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(question.Id, value)
            ]),
            CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.Error.ToString());
        var submitted = await responseStore.SubmitSessionAsync(
            tenantId,
            session.Value.Id,
            new SubmitResponseSessionRequest(TimeTakenMs: 2400),
            CancellationToken.None);
        Assert.True(submitted.IsSuccess, submitted.Error.ToString());
        var scored = await scoreStore.ComputeResponseScoresAsync(
            tenantId,
            session.Value.Id,
            CancellationToken.None);
        Assert.True(scored.IsSuccess, scored.Error.ToString());
    }

    private static async Task<Guid> CreateReportProofAssignmentAsync(
        ApplicationDbContext db,
        TenantDbScope tenantDbScope,
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var issued = OpenLinkTokens.Issue(tenantId);
        var invitationToken = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            issued.TokenHash,
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "report_proof_respondent",
            invitationToken.Id);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);
        db.InvitationTokens.Add(invitationToken);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return assignment.Id;
    }

    private async Task CloseReportProofCampaignAsync(
        Guid tenantId,
        Guid campaignId,
        Guid actorUserId,
        DateTimeOffset closedAt,
        string reason)
    {
        await using var db = new ApplicationDbContext(CreateRuntimeOptions());
        var tenantDbScope = new TenantDbScope(db);
        await CloseScenarioCampaignAsync(db, tenantDbScope, tenantId, campaignId, actorUserId, closedAt, reason);
    }

    private static async Task CloseScenarioCampaignAsync(
        ApplicationDbContext db,
        TenantDbScope tenantDbScope,
        Guid tenantId,
        Guid campaignId,
        Guid actorUserId,
        DateTimeOffset closedAt,
        string reason)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, actorUserId);
        var campaign = await db.Campaigns.SingleAsync(entity => entity.Id == campaignId);
        campaign.Close(reason, actorUserId, closedAt);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<string> CreateManualPublicInvitationTokenAsync(
        ApplicationDbContext db,
        ITenantDbScope tenantDbScope,
        Guid tenantId,
        Guid campaignId,
        string channel,
        string recipient,
        DateTimeOffset? expiresAt = null,
        bool markUsed = false)
    {
        var issued = OpenLinkTokens.IssueInvitation(tenantId);
        var invitationToken = new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            issued.TokenHash,
            channel,
            recipient,
            expiresAt);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "invited_respondent",
            invitationToken.Id);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.InvitationTokens.Add(invitationToken);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        if (markUsed)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE invitation_token SET used_at = {DateTimeOffset.UtcNow} WHERE id = {invitationToken.Id}");
        }

        await transaction.CommitAsync();

        return issued.RawToken;
    }

    private static async Task<Guid> SeedTenantTemplateVersionAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        bool includeMatrixQuestion = false,
        bool includeDisplayLogicQuestion = false)
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Seeded tenant pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "core", "Core");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "agreement",
            ScaleTypes.Likert,
            1,
            5,
            1,
            naAllowed: false,
            """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "q01",
            QuestionTypes.Likert,
            scale.Id,
            "I feel depleted after work.",
            required: true,
            measurementLevel: MeasurementLevels.Ordinal);
        var questions = new List<TemplateQuestion> { question };

        if (includeMatrixQuestion)
        {
            questions.Add(new TemplateQuestion(
                Guid.NewGuid(),
                version.Id,
                section.Id,
                2,
                "body_discomfort",
                QuestionTypes.Matrix,
                scaleId: null,
                "How much discomfort did you feel in each area?",
                required: false,
                variableLabel: "Body discomfort by area",
                measurementLevel: MeasurementLevels.Nominal,
                payload:
                    """{"matrix":{"mode":"single","rows":[{"code":"r01","label":"Neck / shoulders"},{"code":"r02","label":"Lower back"}],"columns":[{"code":"c01","label":"None"},{"code":"c02","label":"Mild"},{"code":"c03","label":"Severe"}]}}"""));
        }

        if (includeDisplayLogicQuestion)
        {
            questions.Add(new TemplateQuestion(
                Guid.NewGuid(),
                version.Id,
                section.Id,
                questions.Count + 1,
                "recovery_followup",
                QuestionTypes.Text,
                scaleId: null,
                "What recovery support would help most?",
                required: false,
                variableLabel: "Recovery support follow-up",
                measurementLevel: MeasurementLevels.Nominal,
                payload:
                    """{"text":{"multiline":true},"displayLogic":{"mode":"show_when","sourceQuestionCode":"q01","operator":"equals","value":3,"requiredWhenVisible":true}}"""));
        }

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        db.Tenants.Add(new Tenant(tenantId, $"tenant-{tenantId:N}", "Seeded Tenant"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.AddRange(questions);
        await db.SaveChangesAsync();

        return version.Id;
    }

    private static async Task<Guid> SeedTenantTemplateVersionWithThreeQuestionsAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId)
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Seeded tenant graph pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "core", "Core");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "agreement",
            ScaleTypes.Likert,
            1,
            5,
            1,
            naAllowed: false,
            """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""");
        var questions = new[]
        {
            new TemplateQuestion(
                Guid.NewGuid(),
                version.Id,
                section.Id,
                1,
                "q01",
                QuestionTypes.Likert,
                scale.Id,
                "I feel depleted after work.",
                required: true,
                measurementLevel: MeasurementLevels.Ordinal),
            new TemplateQuestion(
                Guid.NewGuid(),
                version.Id,
                section.Id,
                2,
                "q02",
                QuestionTypes.Likert,
                scale.Id,
                "Interruptions are hard to handle.",
                required: true,
                measurementLevel: MeasurementLevels.Ordinal),
            new TemplateQuestion(
                Guid.NewGuid(),
                version.Id,
                section.Id,
                3,
                "q03",
                QuestionTypes.Likert,
                scale.Id,
                "I can regain focus after a short break.",
                required: true,
                reverseCoded: true,
                measurementLevel: MeasurementLevels.Ordinal)
        };

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        db.Tenants.Add(new Tenant(tenantId, $"tenant-{tenantId:N}", "Seeded Tenant"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.AddRange(questions);
        await db.SaveChangesAsync();

        return version.Id;
    }

    private static string GraphReverseCodedScoringDocument()
    {
        return """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "burnout.total",
              "rule_version": "1.0.0",
              "scale_defaults": {
                "agreement": { "min": 1, "max": 5 }
              },
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                {
                  "id": "scored_answers",
                  "op": "reverse_code",
                  "input": "core_answers",
                  "scale": "agreement",
                  "reverse_flag_source": "explicit_list",
                  "explicit_reverse_items": ["q03"]
                },
                { "id": "total", "op": "mean", "input": "scored_answers" }
              ],
              "outputs": [
                { "code": "total", "node": "total" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """;
    }

    private static async Task<ResponseAssignmentFixture> SeedResponseAssignmentFixtureAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug,
        bool includeSubmittedResponse = false)
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Response capture pulse");
        var version = TemplateVersion.CreateTenantDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var section = new TemplateSection(Guid.NewGuid(), version.Id, 1, "core", "Core");
        var scale = new QuestionScale(
            Guid.NewGuid(),
            version.Id,
            "agreement",
            ScaleTypes.Likert,
            1,
            5,
            1,
            naAllowed: false,
            """[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]""");
        var question = new TemplateQuestion(
            Guid.NewGuid(),
            version.Id,
            section.Id,
            1,
            "q01",
            QuestionTypes.Likert,
            scale.Id,
            "I feel depleted after work.",
            required: true,
            measurementLevel: MeasurementLevels.Ordinal);
        var scoringRule = CreateScoringRule(version.Id);
        var subject = new Subject(Guid.NewGuid(), tenantId, displayName: "Respondent");
        var campaign = CreateCampaign(
            tenantId,
            version.Id,
            name: "Response capture campaign",
            responseIdentityMode: ResponseIdentityModes.Identified);
        var assignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "self",
            subject.Id);
        ResponseSession? session = null;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, "Response Capture Tenant"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.Add(question);
        db.ScoringRules.Add(scoringRule);
        db.Subjects.Add(subject);
        db.Campaigns.Add(campaign);
        db.Assignments.Add(assignment);

        if (includeSubmittedResponse)
        {
            session = new ResponseSession(
                Guid.NewGuid(),
                tenantId,
                assignment.Id,
                "en");
            session.Submit(DateTimeOffset.UtcNow, timeTakenMs: 1200);
            var answer = new Answer(
                Guid.NewGuid(),
                tenantId,
                session.Id,
                question.Id,
                "4");
            db.ResponseSessions.Add(session);
            db.Answers.Add(answer);
        }

        await db.SaveChangesAsync();

        return new ResponseAssignmentFixture(
            campaign.Id,
            assignment.Id,
            version.Id,
            question.Id,
            scoringRule.Id,
            session?.Id);
    }

    private sealed record ResponseAssignmentFixture(
        Guid CampaignId,
        Guid AssignmentId,
        Guid TemplateVersionId,
        Guid QuestionId,
        Guid ScoringRuleId,
        Guid? ResponseSessionId);

    private static InvitationToken CreateInvitationToken(Guid tenantId, Guid campaignId)
    {
        return new InvitationToken(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            $"token-{Guid.NewGuid():N}",
            InvitationTokenChannels.Email,
            recipient: "respondent@example.com");
    }

    private static async Task AssertAssignmentIdentityModeBlockedAsync(Func<Task> saveChangesAsync)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(saveChangesAsync);

        Assert.Contains(
            "assignment shape does not match campaign response identity mode",
            exception.InnerException?.Message ?? exception.Message);
    }

    private static async Task<AssignmentIdentityFixture> SeedAssignmentIdentityFixtureAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug,
        string responseIdentityMode,
        bool includeRespondent = false,
        bool includeInvitationToken = false)
    {
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), $"{responseIdentityMode} assignment pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(
            tenantId,
            version.Id,
            name: $"{responseIdentityMode} campaign",
            responseIdentityMode: responseIdentityMode);
        var respondent = includeRespondent
            ? new Subject(Guid.NewGuid(), tenantId, displayName: "Respondent")
            : null;
        var token = includeInvitationToken
            ? CreateInvitationToken(tenantId, campaign.Id)
            : null;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();

        db.Tenants.Add(new Tenant(tenantId, tenantSlug, "Assignment Identity Tenant"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.Campaigns.Add(campaign);

        if (respondent is not null)
        {
            db.Subjects.Add(respondent);
        }

        if (token is not null)
        {
            db.InvitationTokens.Add(token);
        }

        await db.SaveChangesAsync();

        return new AssignmentIdentityFixture(
            campaign.Id,
            respondent?.Id,
            token?.Id);
    }

    private static async Task<NotificationFixture> SeedNotificationFixtureAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug)
    {
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), $"{tenantSlug} notification pulse");
        var version = TemplateVersion.CreateCanonicalDraft(Guid.NewGuid(), template.Id, "1.0.0", "en");
        var campaign = CreateCampaign(
            tenantId,
            version.Id,
            name: $"{tenantSlug} campaign",
            responseIdentityMode: ResponseIdentityModes.Anonymous);
        var token = CreateInvitationToken(tenantId, campaign.Id);
        var assignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            "invited_respondent",
            token.Id);
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            tenantId,
            campaign.Id,
            assignment.Id,
            $"{tenantSlug}@example.com");

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, "Notification Tenant"));
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.Campaigns.Add(campaign);
        db.InvitationTokens.Add(token);
        db.Assignments.Add(assignment);
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return new NotificationFixture(campaign.Id, assignment.Id, notification.Id);
    }

    private static byte[] CreateCodeSalt()
    {
        var salt = new byte[32];
        salt[0] = 42;

        return salt;
    }

    private static byte[] CreateParticipantCodeHash(byte seed)
    {
        var hash = new byte[32];
        hash[0] = seed;

        return hash;
    }

    private static ExportArtifact CreateGuardExportArtifact(
        Guid tenantId,
        string targetKind,
        Guid? campaignId,
        Guid? campaignSeriesId,
        string artifactType,
        string fileName)
    {
        return new ExportArtifact(
            Guid.NewGuid(),
            tenantId,
            targetKind,
            campaignId,
            campaignSeriesId,
            artifactType,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            fileName,
            "text/csv",
            rowCount: 1,
            byteSize: 10,
            checksumSha256: "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
            metadataJson: "{}",
            content: "value\r\n1\r\n",
            codebookJson: "{}",
            createdAt: DateTimeOffset.Parse("2026-05-06T12:00:00+00:00"),
            completedAt: DateTimeOffset.Parse("2026-05-06T12:00:01+00:00"));
    }

    private sealed record ReportProofScenarioResult(
        LaunchCampaignResponse Launch,
        CampaignReportProofResponse Report);

    private sealed class DeterministicParticipantCodeHasher : IParticipantCodeHasher
    {
        public Task<ParticipantCodeHashResult> HashAsync(
            string rawCode,
            byte[] seriesSalt,
            CancellationToken cancellationToken)
        {
            var normalized = string.Join(
                ' ',
                rawCode
                    .Trim()
                    .ToLowerInvariant()
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            var input = Encoding.UTF8.GetBytes(normalized);
            var hash = new byte[32];

            for (var index = 0; index < hash.Length; index++)
            {
                var inputByte = input.Length == 0 ? (byte)0 : input[index % input.Length];
                hash[index] = (byte)(inputByte ^ seriesSalt[index % seriesSalt.Length]);
            }

            return Task.FromResult(new ParticipantCodeHashResult(
                hash,
                new ParticipantCodeHashingParameters(65_536, 3, 4, 32)));
        }
    }

    private sealed record AssignmentIdentityFixture(
        Guid CampaignId,
        Guid? RespondentId,
        Guid? InviteTokenId);

    private sealed record NotificationFixture(
        Guid CampaignId,
        Guid AssignmentId,
        Guid NotificationId);

    private static async Task SeedSubjectGraphAsync(
        DbContextOptions<ApplicationDbContext> options,
        SubjectGraph graph)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(graph.TenantId);

        db.Tenants.Add(new Tenant(graph.TenantId, graph.TenantSlug, graph.TenantName));
        db.UserAccounts.Add(new UserAccount(graph.UserId, graph.TenantId, graph.UserEmail));
        db.Subjects.Add(new Subject(
            graph.SubjectId,
            graph.TenantId,
            externalId: graph.SubjectExternalId,
            userAccountId: graph.UserId,
            email: graph.UserEmail,
            displayName: graph.SubjectName));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private sealed record SubjectGraph(
        Guid TenantId,
        string TenantSlug,
        string TenantName,
        Guid UserId,
        string UserEmail,
        Guid SubjectId,
        string SubjectExternalId,
        string SubjectName)
    {
        public static SubjectGraph Create(string slug, string name, string userEmail)
        {
            return new SubjectGraph(
                Guid.NewGuid(),
                slug,
                name,
                Guid.NewGuid(),
                userEmail,
                Guid.NewGuid(),
                $"{slug}-subject",
                $"{name} Subject");
        }
    }

    private sealed class RecordingOutboxDispatcher : IOutboxEventDispatcher
    {
        private readonly List<Guid> _dispatchedEventIds = [];

        public IReadOnlyList<Guid> DispatchedEventIds => _dispatchedEventIds;

        public Task DispatchAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            _dispatchedEventIds.Add(outboxEvent.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingOutboxDispatcher(string message = "dispatch failed") : IOutboxEventDispatcher
    {
        public Task DispatchAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class NoOpOutboxEventHandler(string eventType) : IOutboxEventHandler
    {
        public string EventType { get; } = eventType;

        public Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReportPdfRenderer(byte[] pdfBytes) : IReportPdfRenderer
    {
        public Task<Result<ReportPdfRenderResult>> RenderAsync(
            ReportPdfRenderRequest request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("campaign-series-report", request.TemplateId);
            Assert.Equal(1, request.TemplateVersion);
            Assert.Contains("<!doctype html>", request.Html, StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(Result.Success(new ReportPdfRenderResult(
                pdfBytes,
                "application/pdf",
                pdfBytes.LongLength,
                "fake-pdf",
                "test-browser",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
        }
    }

    private sealed class FailingReportPdfRenderer(Error error) : IReportPdfRenderer
    {
        public Task<Result<ReportPdfRenderResult>> RenderAsync(
            ReportPdfRenderRequest request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("campaign-series-report", request.TemplateId);
            Assert.Equal(1, request.TemplateVersion);
            Assert.Contains("<!doctype html>", request.Html, StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(Result.Failure<ReportPdfRenderResult>(error));
        }
    }

    private static ExportArtifact CreateRenderingPdfArtifact(
        Guid artifactId,
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset startedAt)
    {
        return new ExportArtifact(
            artifactId,
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: campaignSeriesId,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            ExportArtifactStatuses.Rendering,
            ExportArtifactFormats.Pdf,
            $"campaign-series-{campaignSeriesId}-report.pdf",
            "application/pdf",
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            metadataJson: """{"artifactType":"campaign_series_report_pdf","status":"rendering"}""",
            content: null,
            codebookJson: "{}",
            createdAt: startedAt,
            completedAt: null,
            startedAt: startedAt,
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: null);
    }

    private sealed class FailingExportArtifactObjectStore(Error error) : IExportArtifactObjectStore
    {
        public Task<Result<bool>> StoreAsync(
            string storageKey,
            byte[] content,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Failure<bool>(error));
        }

        public Task<Result<byte[]>> ReadAsync(
            string storageKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Failure<byte[]>(error));
        }
    }

    private sealed class RecordingExportArtifactSignedUrlProvider : IExportArtifactSignedUrlProvider
    {
        public List<string> RequestedStorageKeys { get; } = [];

        public Task<Result<ExportArtifactSignedReadUrlResponse>> CreateReadUrlAsync(
            string storageKey,
            TimeSpan expiresIn,
            CancellationToken cancellationToken)
        {
            RequestedStorageKeys.Add(storageKey);

            return Task.FromResult(Result.Success(new ExportArtifactSignedReadUrlResponse(
                "https://object-store.example.test/artifact?X-Amz-Signature=safe",
                DateTimeOffset.UtcNow.Add(expiresIn))));
        }
    }

    private static void AssertFailedPdfAttemptShape(
        ExportArtifact artifact,
        Guid tenantId,
        Guid campaignSeriesId)
    {
        Assert.Equal(tenantId, artifact.TenantId);
        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, artifact.TargetKind);
        Assert.Null(artifact.CampaignId);
        Assert.Equal(campaignSeriesId, artifact.CampaignSeriesId);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportPdf, artifact.ArtifactType);
        Assert.Equal(ExportArtifactStatuses.Failed, artifact.Status);
        Assert.Equal(ExportArtifactFormats.Pdf, artifact.Format);
        Assert.Equal("application/pdf", artifact.ContentType);
        Assert.EndsWith(".pdf", artifact.FileName, StringComparison.Ordinal);
        Assert.Equal(ExportArtifactStorageKinds.ExternalObject, artifact.StorageKind);
        Assert.Null(artifact.StorageKey);
        Assert.Null(artifact.ChecksumSha256);
        Assert.Null(artifact.Content);
        Assert.Null(artifact.CompletedAt);
        Assert.NotNull(artifact.StartedAt);
        Assert.NotNull(artifact.FailedAt);
        Assert.NotNull(artifact.FailureReasonCode);
        Assert.Equal(0, artifact.RowCount);
        Assert.Equal(0, artifact.ByteSize);
    }
}
