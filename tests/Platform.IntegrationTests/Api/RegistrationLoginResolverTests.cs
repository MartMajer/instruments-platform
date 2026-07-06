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
            true,
            "auth0",
            "auth0|owner",
            CancellationToken.None);

        Assert.NotNull(resolution);
        Assert.True(resolution.EmailVerified);
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
            await using var transaction = await seedDb.Database.BeginTransactionAsync();
            await new TenantDbScope(seedDb).SetTenantAsync(tenantId);

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
            await transaction.CommitAsync();
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
    public async Task RegistrationIntentService_CreateAsync_omits_signup_screen_hint_for_entra_provider()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);

        await using var db = new ApplicationDbContext(options);
        var service = new RegistrationIntentService(
            CreateWorkspaceConfiguration(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:ProviderKey"] = "entra-workforce",
                ["Authentication:Oidc:ProviderLogoutMode"] = "microsoft"
            }),
            db,
            new AcceptingBetaAccessCodeVerifier(),
            new Sha256RegistrationTokenProtector(),
            TimeProvider.System);

        var result = await service.CreateAsync(
            new CreateRegistrationIntentRequest(
                "Owner@Example.Test",
                "Entra Workspace",
                "martin-beta-2026",
                "https://app.example.test/app"),
            CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.DoesNotContain("screen_hint=", result.Value.LoginUrl, StringComparison.Ordinal);
        var loginQuery = QueryHelpers.ParseQuery(
            new Uri(new Uri("https://app.example.test"), result.Value.LoginUrl).Query);
        Assert.Equal("owner@example.test", loginQuery["login_hint"].Single());
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
            false,
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


    [DockerFact]
    public async Task ResolveAsync_consumes_unverified_pending_intent_and_records_email_verification_grace()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var rawRegistrationToken = "registration-token-unverified";
        var email = "unverified-owner@example.test";
        var tokenProtector = new Sha256RegistrationTokenProtector();
        var tokenHash = tokenProtector.Hash(rawRegistrationToken);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await SeedIntentAsync(options, tokenHash, email, "Unverified Lab", "unverified-lab", createdAt);

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
            false,
            "auth0",
            "auth0|owner-unverified",
            CancellationToken.None);

        Assert.NotNull(resolution);
        Assert.False(resolution.EmailVerified);
        Assert.Equal(email, resolution.Email);
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(resolution.TenantId, currentTenant.TenantId);

        await using var verification = new ApplicationDbContext(options);
        await using var transaction = await new TenantDbScope(verification).BeginTransactionAsync(resolution.TenantId);
        var intent = await verification.RegistrationIntents.SingleAsync(intent => intent.RegistrationTokenHash == tokenHash);
        Assert.Equal(RegistrationIntentStatuses.Consumed, intent.Status);
        Assert.Equal(resolution.TenantId, intent.ConsumedTenantId);

        var binding = await verification.ExternalAuthIdentities.SingleAsync(identity =>
            identity.TenantId == resolution.TenantId &&
            identity.UserId == resolution.UserId &&
            identity.Provider == "auth0");
        Assert.Null(binding.EmailVerifiedAt);
        Assert.NotNull(binding.EmailVerificationGraceUsedAt);

        Assert.True(await verification.AuthSessions.AnyAsync(session =>
            session.Id == resolution.SessionId &&
            session.ExternalAuthIdentityId == binding.Id));
    }

    [DockerFact]
    public async Task ResolveAsync_rejects_existing_unverified_normal_binding_until_provider_email_verified()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = PlatformIds.NewId();
        var userId = PlatformIds.NewId();
        var roleId = PlatformIds.NewId();
        var bindingId = PlatformIds.NewId();
        var email = "normal-unverified@example.test";
        var hasher = new Sha256ProviderSubjectHasher();
        var providerSubject = "auth0|normal-unverified";
        var providerSubjectHash = hasher.Hash("auth0", providerSubject);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        await using (var seedDb = new ApplicationDbContext(options))
        {
            await using var transaction = await seedDb.Database.BeginTransactionAsync();
            await new TenantDbScope(seedDb).SetTenantAsync(tenantId);

            seedDb.Tenants.Add(new Tenant(tenantId, "normal-unverified", "Normal Unverified"));
            seedDb.Roles.Add(new Role(roleId, tenantId, "tenant_owner", "Tenant owner"));
            seedDb.UserAccounts.Add(new UserAccount(userId, tenantId, email));
            seedDb.RoleAssignments.Add(new RoleAssignment(
                PlatformIds.NewId(),
                tenantId,
                userId,
                roleId,
                RoleAssignmentScopes.Tenant));
            seedDb.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
                bindingId,
                tenantId,
                userId,
                "auth0",
                providerSubjectHash,
                email,
                createdAt));
            await seedDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(options);
        var currentTenant = new CurrentTenant();
        var resolver = new EfPlatformOidcLoginResolver(
            db,
            new TenantDbScope(db),
            currentTenant,
            hasher,
            CreateConfiguration());

        var unverifiedResolution = await resolver.ResolveAsync(
            tenantId,
            email,
            false,
            "auth0",
            providerSubject,
            CancellationToken.None);
        var verifiedResolution = await resolver.ResolveAsync(
            tenantId,
            email,
            true,
            "auth0",
            providerSubject,
            CancellationToken.None);

        Assert.Null(unverifiedResolution);
        Assert.NotNull(verifiedResolution);
        Assert.True(verifiedResolution.EmailVerified);
        Assert.Equal(userId, verifiedResolution.UserId);
        Assert.Equal(tenantId, verifiedResolution.TenantId);

        await using var verification = new ApplicationDbContext(options);
        await using var verifyTransaction = await new TenantDbScope(verification).BeginTransactionAsync(tenantId);
        var binding = await verification.ExternalAuthIdentities.SingleAsync(identity => identity.Id == bindingId);
        Assert.NotNull(binding.EmailVerifiedAt);
        Assert.Null(binding.EmailVerificationGraceUsedAt);
        Assert.True(await verification.AuthSessions.AnyAsync(session =>
            session.Id == verifiedResolution.SessionId &&
            session.ExternalAuthIdentityId == bindingId));
    }

    [DockerFact]
    public async Task ResolveAsync_rejects_currently_unverified_provider_email_even_when_binding_was_previously_verified()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = PlatformIds.NewId();
        var userId = PlatformIds.NewId();
        var roleId = PlatformIds.NewId();
        var bindingId = PlatformIds.NewId();
        var email = "previously-verified@example.test";
        var hasher = new Sha256ProviderSubjectHasher();
        var providerSubject = "auth0|previously-verified";
        var providerSubjectHash = hasher.Hash("auth0", providerSubject);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        await using (var seedDb = new ApplicationDbContext(options))
        {
            await using var transaction = await seedDb.Database.BeginTransactionAsync();
            await new TenantDbScope(seedDb).SetTenantAsync(tenantId);

            seedDb.Tenants.Add(new Tenant(tenantId, "previously-verified", "Previously Verified"));
            seedDb.Roles.Add(new Role(roleId, tenantId, "tenant_owner", "Tenant owner"));
            seedDb.UserAccounts.Add(new UserAccount(userId, tenantId, email));
            seedDb.RoleAssignments.Add(new RoleAssignment(
                PlatformIds.NewId(),
                tenantId,
                userId,
                roleId,
                RoleAssignmentScopes.Tenant));
            var binding = new ExternalAuthIdentity(
                bindingId,
                tenantId,
                userId,
                "auth0",
                providerSubjectHash,
                email,
                createdAt);
            binding.RecordEmailVerified(createdAt);
            seedDb.ExternalAuthIdentities.Add(binding);
            await seedDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(options);
        var resolver = new EfPlatformOidcLoginResolver(
            db,
            new TenantDbScope(db),
            new CurrentTenant(),
            hasher,
            CreateConfiguration());

        var resolution = await resolver.ResolveAsync(
            tenantId,
            email,
            false,
            "auth0",
            providerSubject,
            CancellationToken.None);

        Assert.Null(resolution);

        await using var verification = new ApplicationDbContext(options);
        await using var verifyTransaction = await new TenantDbScope(verification).BeginTransactionAsync(tenantId);
        Assert.False(await verification.AuthSessions.AnyAsync(session =>
            session.ExternalAuthIdentityId == bindingId));
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
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

    private static IConfiguration CreateWorkspaceConfiguration(
        Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Registration:Enabled"] = "true",
            ["Authentication:Oidc:SessionMinutes"] = "30",
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private sealed class AcceptingBetaAccessCodeVerifier : IBetaAccessCodeVerifier
    {
        public bool Verify(string accessCode) => true;
    }
}

