using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Api.Auth;
using Platform.Api.Registration;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Auth;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Api;

public sealed class RegistrationLoginResolverTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [DockerFact]
    public async Task ResolveAsync_consumes_pending_intent_and_bootstraps_tenant_owner()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var rawRegistrationToken = "registration-token";
        var email = "owner@example.test";
        var tokenProtector = new Sha256RegistrationTokenProtector();
        var tokenHash = tokenProtector.Hash(rawRegistrationToken);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await SeedIntentAsync(options, tokenHash, email, "Example Lab", "example-lab", createdAt);

        await using var db = new ApplicationDbContext(options);
        var currentTenant = new CurrentTenant();
        var resolver = new EfPlatformRegistrationLoginResolver(
            db,
            new TenantDbScope(db),
            currentTenant,
            new Sha256ProviderSubjectHasher(),
            tokenProtector,
            CreateConfiguration());

        var resolution = await resolver.ResolveAsync(
            rawRegistrationToken,
            email,
            "auth0",
            "auth0|owner",
            CancellationToken.None);

        Assert.NotNull(resolution);
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(resolution.TenantId, currentTenant.TenantId);
        Assert.Equal(email, resolution.Email);
        Assert.Equal(
            [PlatformPermissions.ExportRead, PlatformPermissions.SetupManage, PlatformPermissions.TeamManage],
            resolution.Permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToArray());

        await using var verification = new ApplicationDbContext(options);
        var tenantScope = new TenantDbScope(verification);
        await using var transaction = await tenantScope.BeginTransactionAsync(resolution.TenantId);
        var intent = await verification.RegistrationIntents.SingleAsync(intent => intent.RegistrationTokenHash == tokenHash);
        Assert.Equal(RegistrationIntentStatuses.Consumed, intent.Status);
        Assert.Equal(resolution.TenantId, intent.ConsumedTenantId);
        Assert.NotNull(intent.ConsumedAt);

        var tenant = await verification.Tenants.SingleAsync(tenant => tenant.Id == resolution.TenantId);
        Assert.Equal("example-lab", tenant.Slug);
        Assert.Equal("Example Lab", tenant.Name);

        var user = await verification.UserAccounts.SingleAsync(user => user.Id == resolution.UserId);
        Assert.Equal(email, user.Email);
        Assert.Equal(resolution.TenantId, user.TenantId);

        var ownerRole = await verification.Roles.SingleAsync(role =>
            role.TenantId == resolution.TenantId &&
            role.Code == "tenant_owner");
        Assert.Equal("Tenant owner", ownerRole.Name);

        var ownerAssignment = await verification.RoleAssignments.SingleAsync(assignment =>
            assignment.TenantId == resolution.TenantId &&
            assignment.UserId == resolution.UserId &&
            assignment.RoleId == ownerRole.Id &&
            assignment.ScopeType == RoleAssignmentScopes.Tenant);
        Assert.Null(ownerAssignment.ScopeId);

        var storedPermissions = await (
                from rolePermission in verification.RolePermissions
                join permission in verification.Permissions on rolePermission.PermissionId equals permission.Id
                where rolePermission.RoleId == ownerRole.Id
                select permission.Code)
            .OrderBy(code => code)
            .ToArrayAsync();
        Assert.Equal(
            [PlatformPermissions.ExportRead, PlatformPermissions.SetupManage, PlatformPermissions.TeamManage],
            storedPermissions);

        var binding = await verification.ExternalAuthIdentities.SingleAsync(identity =>
            identity.TenantId == resolution.TenantId &&
            identity.UserId == resolution.UserId &&
            identity.Provider == "auth0");
        Assert.Equal(email, binding.EmailAtBinding);
        Assert.NotEqual("auth0|owner", binding.ProviderSubjectHash);

        var session = await verification.AuthSessions.SingleAsync(session =>
            session.Id == resolution.SessionId &&
            session.TenantId == resolution.TenantId &&
            session.UserId == resolution.UserId &&
            session.ExternalAuthIdentityId == binding.Id);
        Assert.True(session.ExpiresAt > session.CreatedAt);
    }

    [DockerFact]
    public async Task RegistrationWorkspaceService_CreateAsync_bootstraps_owner_without_preseeded_permissions()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);

        await using var db = new ApplicationDbContext(options);
        var currentTenant = new CurrentTenant();
        var service = new RegistrationWorkspaceService(
            CreateWorkspaceConfiguration(),
            db,
            new TenantDbScope(db),
            currentTenant,
            new AcceptingBetaAccessCodeVerifier());

        var result = await service.CreateAsync(
            new RegistrationIdentity("owner@example.test", "auth0", "hashed-subject"),
            new CreateRegistrationWorkspaceRequest(
                "Owner Lab",
                "martin-beta-2026",
                "https://app.example.test/app"),
            CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Equal("owner@example.test", result.Value.Resolution.Email);
        Assert.Contains(PlatformPermissions.SetupManage, result.Value.Resolution.Permissions);
        Assert.NotEqual(Guid.Empty, result.Value.Resolution.SessionId);

        await using var verificationDb = new ApplicationDbContext(options);
        await using var _ = await new TenantDbScope(verificationDb)
            .BeginTransactionAsync(result.Value.Resolution.TenantId);
        Assert.True(await verificationDb.AuthSessions.AnyAsync(session =>
            session.Id == result.Value.Resolution.SessionId));
        Assert.True(await verificationDb.RolePermissions.AnyAsync());
        Assert.DoesNotContain(
            await verificationDb.AuditEvents.Select(audit => audit.EntityType).ToListAsync(),
            entityType => entityType == "Permission");
    }

    [DockerFact]
    public async Task RegistrationIntentService_CreateAsync_rejects_email_that_already_belongs_to_workspace()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = PlatformIds.NewId();
        var userId = PlatformIds.NewId();
        var roleId = PlatformIds.NewId();
        await using (var seedDb = new ApplicationDbContext(options))
        {
            seedDb.Tenants.Add(new Tenant(tenantId, "existing-workspace", "Existing Workspace"));
            seedDb.Roles.Add(new Role(roleId, tenantId, "tenant_owner", "Tenant owner"));
            seedDb.UserAccounts.Add(new UserAccount(userId, tenantId, "owner@example.test"));
            seedDb.RoleAssignments.Add(new RoleAssignment(
                PlatformIds.NewId(),
                tenantId,
                userId,
                roleId,
                RoleAssignmentScopes.Tenant));
            await seedDb.SaveChangesAsync();
        }

        await using var db = new ApplicationDbContext(options);
        var service = new RegistrationIntentService(
            CreateWorkspaceConfiguration(),
            db,
            new AcceptingBetaAccessCodeVerifier(),
            new Sha256RegistrationTokenProtector(),
            TimeProvider.System);

        var result = await service.CreateAsync(
            new CreateRegistrationIntentRequest(
                "Owner@Example.Test",
                "Duplicate Workspace",
                "martin-beta-2026",
                "https://app.example.test/app"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("registration.email_exists", result.Error.Code);
        Assert.True(result.Error.Extensions.TryGetValue("loginUrl", out var loginUrlValue));
        var loginUrl = Assert.IsType<string>(loginUrlValue);
        var query = QueryHelpers.ParseQuery(new Uri(new Uri("https://app.example.test"), loginUrl).Query);
        Assert.Equal(tenantId.ToString(), query["tenantId"].Single());
        Assert.Equal("owner@example.test", query["login_hint"].Single());
        Assert.DoesNotContain("registrationToken", loginUrl, StringComparison.Ordinal);

        await using var verificationDb = new ApplicationDbContext(options);
        Assert.Empty(await verificationDb.RegistrationIntents.ToListAsync());
    }

    [DockerFact]
    public async Task ResolveAsync_rejects_mismatched_provider_email_without_consuming_intent()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var rawRegistrationToken = "registration-token";
        var tokenProtector = new Sha256RegistrationTokenProtector();
        var tokenHash = tokenProtector.Hash(rawRegistrationToken);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await SeedIntentAsync(options, tokenHash, "owner@example.test", "Example Lab", "example-lab", createdAt);

        await using var db = new ApplicationDbContext(options);
        var resolver = new EfPlatformRegistrationLoginResolver(
            db,
            new TenantDbScope(db),
            new CurrentTenant(),
            new Sha256ProviderSubjectHasher(),
            tokenProtector,
            CreateConfiguration());

        var resolution = await resolver.ResolveAsync(
            rawRegistrationToken,
            "other@example.test",
            "auth0",
            "auth0|owner",
            CancellationToken.None);

        Assert.Null(resolution);

        await using var verification = new ApplicationDbContext(options);
        var intent = await verification.RegistrationIntents.SingleAsync(intent => intent.RegistrationTokenHash == tokenHash);
        Assert.Equal(RegistrationIntentStatuses.Pending, intent.Status);
        Assert.Null(intent.ConsumedTenantId);
        Assert.Empty(verification.Tenants);
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
    }

    private static async Task SeedIntentAsync(
        DbContextOptions<ApplicationDbContext> options,
        string tokenHash,
        string email,
        string organizationName,
        string slug,
        DateTimeOffset createdAt)
    {
        await using var db = new ApplicationDbContext(options);
        db.RegistrationIntents.Add(new RegistrationIntent(
            PlatformIds.NewId(),
            tokenHash,
            email,
            organizationName,
            slug,
            createdAt,
            createdAt.AddMinutes(15)));
        await db.SaveChangesAsync();
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:SessionMinutes"] = "30"
            })
            .Build();
    }

    private static IConfiguration CreateWorkspaceConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:Enabled"] = "true",
                ["Authentication:Oidc:SessionMinutes"] = "30",
                ["Cors:AllowedOrigins:0"] = "https://app.example.test"
            })
            .Build();
    }

    private sealed class AcceptingBetaAccessCodeVerifier : IBetaAccessCodeVerifier
    {
        public bool Verify(string accessCode) => true;
    }
}

