using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Platform.IntegrationTests.Api;

public sealed class DevelopmentAuthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Development_auth_allows_tenant_member_when_enabled_in_development()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient("Development", enabled: true);
        using var request = AuthenticatedDevRequest(tenantId, userId, tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Contains("setup.manage", payload.Permissions);
    }

    [Fact]
    public async Task Development_auth_rejects_when_membership_does_not_match_tenant_context()
    {
        using var client = CreateClient("Development", enabled: true);
        using var request = AuthenticatedDevRequest(
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            membershipTenantId: Guid.NewGuid());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Development_auth_is_disabled_when_configuration_is_false()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient("Development", enabled: false);
        using var request = AuthenticatedDevRequest(tenantId, Guid.NewGuid(), tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Development_auth_is_disabled_outside_development_environment()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateClient("Production", enabled: true);
        using var request = AuthenticatedDevRequest(tenantId, Guid.NewGuid(), tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Development_cors_allows_configured_local_frontend_origin()
    {
        using var client = CreateClient("Development", enabled: true);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/auth/session");
        request.Headers.Add("Origin", "http://127.0.0.1:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://127.0.0.1:5173", Assert.Single(origins));
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials));
        Assert.Equal("true", Assert.Single(credentials));
    }

    [Fact]
    public async Task Configured_cors_allows_auth0_mode_frontend_origin_outside_development_auth()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Dev:Enabled", "false");
            builder.UseSetting("Authentication:Oidc:InteractiveEnabled", "true");
            builder.UseSetting("Authentication:Oidc:Authority", "https://auth.example.test/");
            builder.UseSetting("Authentication:Oidc:ClientId", "client-id");
            builder.UseSetting("Authentication:Oidc:ClientSecret", "client-secret");
            builder.UseSetting("Cors:AllowedOrigins:0", "http://127.0.0.1:5174");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Dev:Enabled"] = "false",
                    ["Authentication:Oidc:InteractiveEnabled"] = "true",
                    ["Authentication:Oidc:Authority"] = "https://auth.example.test/",
                    ["Authentication:Oidc:ClientId"] = "client-id",
                    ["Authentication:Oidc:ClientSecret"] = "client-secret",
                    ["Cors:AllowedOrigins:0"] = "http://127.0.0.1:5174"
                });
            });
        }).CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/auth/session");
        request.Headers.Add("Origin", "http://127.0.0.1:5174");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "X-Tenant-Id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://127.0.0.1:5174", Assert.Single(origins));
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials));
        Assert.Equal("true", Assert.Single(credentials));
    }

    private HttpClient CreateClient(string environment, bool enabled)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("Authentication:Dev:Enabled", enabled.ToString());
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Dev:Enabled"] = enabled.ToString(),
                    ["Authentication:Dev:AllowedOrigins:0"] = "http://127.0.0.1:5173"
                });
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedDevRequest(
        Guid tenantId,
        Guid userId,
        Guid membershipTenantId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add("X-Dev-User-Id", userId.ToString());
        request.Headers.Add("X-Dev-Tenant-Memberships", membershipTenantId.ToString());
        request.Headers.Add("X-Dev-Permissions", "setup.manage");
        return request;
    }

    private sealed record SessionResponse(Guid UserId, Guid TenantId, string[] Permissions);
}
