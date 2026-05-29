using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Platform.Application.Features.DirectoryImports;
using Platform.Domain.Auth;
using Platform.Domain.DirectoryImports;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.DirectoryImports;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class DirectoryImportStoreTests : IAsyncLifetime
{
    private const string RuntimeUsername = "platform_app_runtime";
    private const string RuntimePassword = "platform_app_runtime";
    private const string ExternalTenantId = "customer-tenant";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Save_preview_creates_preview_run_items_and_does_not_write_subjects()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "graph-preview");
        await SeedUserAccountAsync(runtimeOptions, tenantId, actorUserId, "owner@example.test");
        var rule = await SeedDirectoryRuleAsync(runtimeOptions, tenantId, "graph-preview-rule");
        await SeedSubjectAsync(
            runtimeOptions,
            tenantId,
            "Old Update",
            "old.update@example.test",
            "msgraph:customer-tenant:graph-user-update");
        await SeedSubjectAsync(
            runtimeOptions,
            tenantId,
            "Same Person",
            "same@example.test",
            "msgraph:customer-tenant:graph-user-same",
            CandidateAttributesJson);

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = CreateStore(db);
        var contextResult = await store.GetRuleExecutionContextAsync(tenantId, rule.Id, CancellationToken.None);
        Assert.True(contextResult.IsSuccess, contextResult.Error.ToString());
        var plan = DirectoryImportRulePlanner.Plan(contextResult.Value.CriteriaJson);

        var result = await store.SavePreviewAsync(
            tenantId,
            actorUserId,
            contextResult.Value,
            plan,
            [
                Candidate("graph-user-create", "Create Person", "create@example.test"),
                Candidate("graph-user-update", "Updated Person", "updated@example.test"),
                Candidate("graph-user-same", "Same Person", "same@example.test"),
                Candidate(
                    "graph-user-missing-email",
                    "Missing Email",
                    email: null,
                    warnings:
                    [
                        new GraphDirectoryCandidateWarning(
                            GraphDirectoryCandidateWarningCodes.MissingEmail,
                            "missing")
                    ])
            ],
            managers: [],
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("previewed", result.Value.Status);
        Assert.Equal(4, result.Value.Summary.MatchedUserCount);
        Assert.Equal(2, result.Value.Summary.CreateSubjectCount);
        Assert.Equal(1, result.Value.Summary.UpdateSubjectCount);
        Assert.Equal(1, result.Value.Summary.NoChangeCount);
        Assert.Equal(1, result.Value.Summary.WarningCount);
        Assert.Contains("displayName", result.Value.Summary.RetainedFields);

        await using var verificationDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verificationDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(2, await verificationDb.Subjects.CountAsync());
        var run = await verificationDb.DirectoryImportRuns.SingleAsync();
        var items = await verificationDb.DirectoryImportRunItems
            .OrderBy(item => item.Action)
            .ThenBy(item => item.IssueCode)
            .ToListAsync();

        Assert.Equal(DirectoryImportRunStatuses.Previewed, run.Status);
        Assert.Equal(DirectoryImportRunModes.Preview, run.Mode);
        Assert.Equal(actorUserId, run.CreatedByUserId);
        Assert.Contains(items, item => item.Action == DirectoryImportRunItemActions.CreateSubject);
        Assert.Contains(items, item => item.Action == DirectoryImportRunItemActions.UpdateSubject);
        Assert.Contains(items, item => item.Action == DirectoryImportRunItemActions.NoChange);
        Assert.Contains(items, item =>
            item.Action == DirectoryImportRunItemActions.Warning &&
            item.IssueCode == GraphDirectoryCandidateWarningCodes.MissingEmail);

        var persistedEvidence = JsonSerializer.Serialize(new
        {
            run.SummaryJson,
            Items = items.Select(item => new
            {
                item.SourceObjectIdHash,
                item.SafeSummaryJson
            })
        });
        Assert.DoesNotContain("client-secret", persistedEvidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access_token", persistedEvidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("create@example.test", persistedEvidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("graph-user-create", persistedEvidence, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Get_rule_execution_context_returns_credentials_and_fails_closed_for_wrong_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "graph-context-a");
        await SeedTenantAsync(runtimeOptions, tenantB, "graph-context-b");
        var rule = await SeedDirectoryRuleAsync(runtimeOptions, tenantA, "graph-context-rule");

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = CreateStore(db);

        var ownTenantResult = await store.GetRuleExecutionContextAsync(tenantA, rule.Id, CancellationToken.None);
        var wrongTenantResult = await store.GetRuleExecutionContextAsync(tenantB, rule.Id, CancellationToken.None);

        Assert.True(ownTenantResult.IsSuccess, ownTenantResult.Error.ToString());
        Assert.Equal(rule.Id, ownTenantResult.Value.RuleId);
        Assert.Equal(ExternalTenantId, ownTenantResult.Value.ExternalTenantId);
        Assert.Equal("graph-client-id", ownTenantResult.Value.Credentials.ClientId);
        Assert.Equal("client-secret", ownTenantResult.Value.Credentials.ClientSecret);
        Assert.True(wrongTenantResult.IsFailure);
        Assert.Equal("directory_import_rule.not_found", wrongTenantResult.Error.Code);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private DirectoryImportStore CreateStore(ApplicationDbContext db)
    {
        return new DirectoryImportStore(
            db,
            new TenantDbScope(db),
            Options.Create(new MicrosoftGraphDirectoryImportOptions
            {
                ClientId = "graph-client-id",
                ClientSecret = "client-secret"
            }));
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> migratorOptions)
    {
        await using (var db = new ApplicationDbContext(migratorOptions))
        {
            await db.Database.MigrateAsync();
        }

        await CreateRuntimeRoleAsync(migratorOptions);
    }

    private DbContextOptions<ApplicationDbContext> CreateMigratorOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private DbContextOptions<ApplicationDbContext> CreateRuntimeOptions()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
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
                subject,
                directory_connection,
                directory_import_rule,
                directory_import_run,
                directory_import_run_item
            TO {{RuntimeUsername}};
            """);
    }

    private static async Task SeedTenantAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string tenantSlug)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Tenants.Add(new Tenant(tenantId, tenantSlug, $"Tenant {tenantSlug}"));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedUserAccountAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        Guid userId,
        string email)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.UserAccounts.Add(new UserAccount(userId, tenantId, email));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<DirectoryImportRule> SeedDirectoryRuleAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string name)
    {
        var connection = new DirectoryConnection(
            Guid.NewGuid(),
            tenantId,
            DirectoryConnectionProviders.MicrosoftGraph,
            ExternalTenantId,
            "Customer Tenant",
            "customer.example",
            """{"scopes":["User.Read.All"]}""");
        var rule = new DirectoryImportRule(
            Guid.NewGuid(),
            tenantId,
            connection.Id,
            name,
            """{"accountEnabled":true}""",
            "{}");

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.DirectoryConnections.Add(connection);
        db.DirectoryImportRules.Add(rule);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return rule;
    }

    private const string CandidateAttributesJson = """
        {
          "department": "Psychology",
          "job_title": "Researcher",
          "employee_type": "Faculty",
          "office_location": "Zagreb",
          "msgraph_user_type": "Member"
        }
        """;

    private static async Task SeedSubjectAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string displayName,
        string email,
        string externalId,
        string attributes = "{}")
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        db.Subjects.Add(new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: externalId,
            email: email,
            displayName: displayName,
            attributes: attributes));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static GraphDirectoryUserCandidate Candidate(
        string graphUserId,
        string displayName,
        string? email,
        IReadOnlyList<GraphDirectoryCandidateWarning>? warnings = null)
    {
        return new GraphDirectoryUserCandidate(
            graphUserId,
            email,
            email,
            displayName,
            "Psychology",
            "Researcher",
            "Faculty",
            "Zagreb",
            "en",
            AccountEnabled: true,
            "Member",
            warnings ?? []);
    }
}
