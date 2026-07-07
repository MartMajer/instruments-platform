using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Api;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests.Api;

public sealed class WorkerHeartbeatHealthEndpointRegressionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app;Timeout=1;Command Timeout=1";

    [Fact]
    public async Task Ready_health_includes_worker_heartbeat_without_operational_details()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDb"] = UnreachableConnectionString
                });
            });
            builder.ConfigureTestServices(services =>
            {
                // With minimal hosting the in-memory configuration above is applied
                // before the app's JSON providers, so a running local dev database
                // would win over the unreachable connection string.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(UnreachableConnectionString));
            });
        }).CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(payload);
        Assert.Contains(payload.Checks, check => check.Name == "worker_heartbeat");
        Assert.DoesNotContain("platform-workers", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("instance", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LastSeenAt", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StartedAt", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StaleAfter", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MachineName", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HealthResponse(
        string Service,
        string Status,
        HealthCheckResponse[] Checks);

    private sealed record HealthCheckResponse(string Name, string Status);
}
