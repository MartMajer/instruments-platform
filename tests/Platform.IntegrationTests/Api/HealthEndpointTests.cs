using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Platform.IntegrationTests.Api;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_returns_platform_status()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("instruments-platform", payload.Service);
        Assert.Equal("ok", payload.Status);
    }

    [Fact]
    public async Task Live_health_endpoint_returns_platform_status_without_dependency_checks()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("instruments-platform", payload.Service);
        Assert.Equal("ok", payload.Status);
        Assert.Empty(payload.Checks ?? []);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/startup")]
    public async Task Health_probe_endpoints_ignore_malformed_tenant_header(string path)
    {
        using var client = CreateClientWithUnreachableDatabase();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        request.Headers.Add("X-Tenant-Id", "not-a-guid");

        var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("instruments-platform", payload.Service);
    }

    [Fact]
    public async Task Ready_health_returns_service_unavailable_without_sensitive_database_diagnostics()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SuperSecretPassword123", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("health_unavailable", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("unready", payload.Status);
        var database = Assert.Single(payload.Checks ?? [], check => check.Name == "database");
        Assert.Equal("unready", database.Status);
    }

    [Fact]
    public async Task Ready_health_reports_development_auth_unready_outside_development()
    {
        using var client = CreateClientWithUnreachableDatabase("Production", developmentAuthEnabled: true);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var developmentAuth = Assert.Single(payload.Checks ?? [], check => check.Name == "development_auth");
        Assert.Equal("unready", developmentAuth.Status);
    }

    [Fact]
    public async Task Ready_health_reports_oidc_configuration_unready_outside_development_when_missing()
    {
        using var client = CreateClientWithUnreachableDatabase("Production");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Authentication:Oidc", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("unready", oidc.Status);
    }

    [Fact]
    public async Task Startup_health_reports_oidc_configuration_unready_outside_development_when_missing()
    {
        using var client = CreateClientWithUnreachableDatabase("Production");

        var response = await client.GetAsync("/health/startup");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("unready", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_reports_oidc_configuration_unready_outside_development_when_https_metadata_is_disabled()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            oidcRequireHttpsMetadata: false);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("identity.example.test", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("platform-api", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("unready", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_reports_oidc_configuration_unready_outside_development_when_authority_is_not_https()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "http://identity.example.test/realms/platform",
            oidcAudience: "platform-api");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("unready", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_reports_oidc_configuration_ok_outside_development_when_configuration_is_safe()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("ok", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_reports_oidc_configuration_ok_for_interactive_auth0_configuration()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://tenant.eu.auth0.com/",
            oidcInteractiveEnabled: true,
            oidcClientId: "auth0-client",
            oidcClientSecret: "auth0-secret");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("auth0-client", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth0-secret", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("ok", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_keeps_development_oidc_configuration_permissive()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var oidc = Assert.Single(payload.Checks ?? [], check => check.Name == "oidc_configuration");
        Assert.Equal("ok", oidc.Status);
    }

    [Fact]
    public async Task Ready_health_reports_export_artifact_object_store_unready_outside_development_when_root_is_missing()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var objectStore = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "export_artifact_object_store");
        Assert.Equal("unready", objectStore.Status);
    }

    [Fact]
    public async Task Ready_health_reports_export_artifact_object_store_unready_outside_development_when_root_is_temp()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "instruments-platform-tests",
            Guid.NewGuid().ToString("N"));
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: rootPath);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rootPath, body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var objectStore = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "export_artifact_object_store");
        Assert.Equal("unready", objectStore.Status);
    }

    [Fact]
    public async Task Ready_health_reports_export_artifact_object_store_ok_outside_development_when_root_is_explicit_non_temp()
    {
        var rootPath = CreateNonTempObjectStoreRoot();
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: rootPath);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rootPath, body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var objectStore = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "export_artifact_object_store");
        Assert.Equal("ok", objectStore.Status);
    }

    [Fact]
    public async Task Ready_health_keeps_development_export_artifact_object_store_default_ok()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var objectStore = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "export_artifact_object_store");
        Assert.Equal("ok", objectStore.Status);
    }

    [Fact]
    public async Task Ready_health_keeps_development_report_pdf_renderer_default_ok()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var renderer = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "report_pdf_renderer");
        Assert.Equal("ok", renderer.Status);
    }

    [Fact]
    public async Task Ready_health_keeps_development_email_delivery_configuration_default_ok()
    {
        using var client = CreateClientWithUnreachableDatabase();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var email = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "email_delivery_configuration");
        Assert.Equal("ok", email.Status);
    }

    [Fact]
    public async Task Ready_health_reports_email_delivery_configuration_unready_without_sensitive_acs_values()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: CreateNonTempObjectStoreRoot(),
            pdfBrowserExecutablePath: Environment.ProcessPath ?? typeof(HealthEndpointTests).Assembly.Location,
            emailDeliveryProvider: "azure-communication-email",
            emailDeliverySenderDomainVerified: true,
            emailDeliveryVerifiedSenderDomain: "example.test",
            emailDeliveryFromAddress: "noreply@example.test",
            emailDeliveryPublicAppBaseUrl: "https://app.example.test",
            emailDeliveryInvitationFooterText: "Workspace invitation footer.",
            emailDeliveryAcsEndpoint: "https://validatedscale.communication.azure.com/",
            emailDeliveryAcsAccessKey: "acs-access-key-secret");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("noreply@example.test", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("acs-access-key-secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validatedscale.communication.azure.com", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EmailDelivery", body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var email = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "email_delivery_configuration");
        Assert.Equal("unready", email.Status);
    }

    [Fact]
    public async Task Ready_health_reports_report_pdf_renderer_unready_outside_development_when_browser_path_is_missing()
    {
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: CreateNonTempObjectStoreRoot());

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var renderer = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "report_pdf_renderer");
        Assert.Equal("unready", renderer.Status);
    }

    [Fact]
    public async Task Ready_health_reports_report_pdf_renderer_unready_outside_development_when_browser_path_does_not_exist()
    {
        var browserPath = Path.Combine(
            AppContext.BaseDirectory,
            "missing-browser",
            Guid.NewGuid().ToString("N"),
            "chrome.exe");
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: CreateNonTempObjectStoreRoot(),
            pdfBrowserExecutablePath: browserPath);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(browserPath, body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var renderer = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "report_pdf_renderer");
        Assert.Equal("unready", renderer.Status);
    }

    [Fact]
    public async Task Ready_health_reports_report_pdf_renderer_ok_outside_development_when_browser_path_exists()
    {
        var browserPath = Environment.ProcessPath ?? typeof(HealthEndpointTests).Assembly.Location;
        using var client = CreateClientWithUnreachableDatabase(
            "Production",
            oidcAuthority: "https://identity.example.test/realms/platform",
            oidcAudience: "platform-api",
            objectStoreRootPath: CreateNonTempObjectStoreRoot(),
            pdfBrowserExecutablePath: browserPath);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(browserPath, body, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        var renderer = Assert.Single(
            payload.Checks ?? [],
            check => check.Name == "report_pdf_renderer");
        Assert.Equal("ok", renderer.Status);
    }

    private HttpClient CreateClientWithUnreachableDatabase(
        string environment = "Development",
        bool developmentAuthEnabled = false,
        string? oidcAuthority = null,
        string? oidcAudience = null,
        bool? oidcRequireHttpsMetadata = null,
        bool oidcInteractiveEnabled = false,
        string? oidcClientId = null,
        string? oidcClientSecret = null,
        string? objectStoreRootPath = null,
        string? pdfBrowserExecutablePath = null,
        string? emailDeliveryProvider = null,
        bool? emailDeliverySenderDomainVerified = null,
        string? emailDeliveryVerifiedSenderDomain = null,
        string? emailDeliveryFromAddress = null,
        string? emailDeliveryPublicAppBaseUrl = null,
        string? emailDeliveryInvitationFooterText = null,
        string? emailDeliveryAcsEndpoint = null,
        string? emailDeliveryAcsAccessKey = null,
        string? emailDeliveryAcsEventGridWebhookSecret = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDb"] =
                        "Host=127.0.0.1;Port=1;Database=health_unavailable;Username=platform_app;Password=SuperSecretPassword123;Timeout=1;Command Timeout=1",
                    ["Authentication:Dev:Enabled"] = developmentAuthEnabled.ToString(),
                    ["Authentication:Oidc:InteractiveEnabled"] = oidcInteractiveEnabled.ToString()
                };

                if (oidcAuthority is not null)
                {
                    settings["Authentication:Oidc:Authority"] = oidcAuthority;
                }

                if (oidcAudience is not null)
                {
                    settings["Authentication:Oidc:Audience"] = oidcAudience;
                }

                if (oidcRequireHttpsMetadata is not null)
                {
                    settings["Authentication:Oidc:RequireHttpsMetadata"] =
                        oidcRequireHttpsMetadata.Value.ToString();
                }

                if (oidcClientId is not null)
                {
                    settings["Authentication:Oidc:ClientId"] = oidcClientId;
                }

                if (oidcClientSecret is not null)
                {
                    settings["Authentication:Oidc:ClientSecret"] = oidcClientSecret;
                }

                if (objectStoreRootPath is not null)
                {
                    settings["ExportArtifacts:ObjectStore:RootPath"] = objectStoreRootPath;
                }

                if (pdfBrowserExecutablePath is not null)
                {
                    settings["Reports:PdfRenderer:BrowserExecutablePath"] = pdfBrowserExecutablePath;
                }

                if (emailDeliveryProvider is not null)
                {
                    settings["EmailDelivery:Provider"] = emailDeliveryProvider;
                }

                if (emailDeliverySenderDomainVerified is not null)
                {
                    settings["EmailDelivery:SenderDomainVerified"] =
                        emailDeliverySenderDomainVerified.Value.ToString();
                }

                if (emailDeliveryVerifiedSenderDomain is not null)
                {
                    settings["EmailDelivery:VerifiedSenderDomain"] = emailDeliveryVerifiedSenderDomain;
                }

                if (emailDeliveryFromAddress is not null)
                {
                    settings["EmailDelivery:FromAddress"] = emailDeliveryFromAddress;
                }

                if (emailDeliveryPublicAppBaseUrl is not null)
                {
                    settings["EmailDelivery:PublicAppBaseUrl"] = emailDeliveryPublicAppBaseUrl;
                }

                if (emailDeliveryInvitationFooterText is not null)
                {
                    settings["EmailDelivery:InvitationFooterText"] = emailDeliveryInvitationFooterText;
                }

                if (emailDeliveryAcsEndpoint is not null)
                {
                    settings["EmailDelivery:AzureCommunicationServices:Endpoint"] = emailDeliveryAcsEndpoint;
                }

                if (emailDeliveryAcsAccessKey is not null)
                {
                    settings["EmailDelivery:AzureCommunicationServices:AccessKey"] = emailDeliveryAcsAccessKey;
                }

                if (emailDeliveryAcsEventGridWebhookSecret is not null)
                {
                    settings["EmailDelivery:AzureCommunicationServices:EventGridWebhookSecret"] =
                        emailDeliveryAcsEventGridWebhookSecret;
                }

                configuration.AddInMemoryCollection(settings);
            });
        }).CreateClient();
    }

    private static string CreateNonTempObjectStoreRoot()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "object-store-health-tests",
            Guid.NewGuid().ToString("N"));
    }

    private sealed record HealthResponse(
        string Service,
        string Status,
        HealthCheckResponse[]? Checks = null);

    private sealed record HealthCheckResponse(string Name, string Status);
}
