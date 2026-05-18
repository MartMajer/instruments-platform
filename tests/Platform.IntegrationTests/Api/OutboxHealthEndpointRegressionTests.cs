using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Platform.Api;

namespace Platform.IntegrationTests.Api;

public sealed class OutboxHealthEndpointRegressionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Ready_health_includes_outbox_operability_checks_without_snapshot_or_event_details()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app;Timeout=1;Command Timeout=1"
                });
            });
        }).CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("unready", payload.Status);
        Assert.Contains(payload.Checks, check => check.Name == "outbox_dead_letters");
        Assert.Contains(payload.Checks, check => check.Name == "outbox_due_backlog");

        Assert.DoesNotContain("DueCount", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ScheduledRetryCount", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeadLetterCount", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OldestDueCreatedAt", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DEAD_LETTER:", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Payload", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LastError", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EventType", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HealthResponse(
        string Service,
        string Status,
        HealthCheckResponse[] Checks);

    private sealed record HealthCheckResponse(string Name, string Status);
}
