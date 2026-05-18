using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application;
using Platform.Infrastructure;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class TenantDbScopeTests
{
    [Fact]
    public void Infrastructure_registration_includes_tenant_db_scope()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] =
                    "Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used"
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();

        var tenantDbScope = provider.GetRequiredService<ITenantDbScope>();

        Assert.NotNull(tenantDbScope);
    }

    [Fact]
    public async Task Tenant_db_scope_requires_active_transaction_before_setting_local_tenant()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenantDbScope.SetTenantAsync(Guid.NewGuid()));
    }
}
