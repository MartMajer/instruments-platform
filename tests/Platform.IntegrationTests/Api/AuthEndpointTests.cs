using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Platform.Application.Auth;
using Platform.Api.Auth;
using Platform.IntegrationTests.Support;

namespace Platform.IntegrationTests.Api;

public sealed class AuthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AppCookieScheme = "Platform.AppCookie";
    private const string OidcScheme = "Platform.Oidc";

    [Fact]
    public async Task Interactive_oidc_authentication_uses_app_cookie_and_oidc_challenge()
    {
        using var provider = BuildInteractiveAuthServiceProvider();
        var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(AppCookieScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(AppCookieScheme, authentication.DefaultSignInScheme);
        Assert.Equal(AppCookieScheme, authentication.DefaultChallengeScheme);
        Assert.Equal(OidcScheme, authentication.DefaultSignOutScheme);

        Assert.NotNull(await schemeProvider.GetSchemeAsync(AppCookieScheme));
        Assert.NotNull(await schemeProvider.GetSchemeAsync(OidcScheme));
    }

    [Fact]
    public void Interactive_oidc_cookie_is_hardened_for_browser_session()
    {
        using var provider = BuildInteractiveAuthServiceProvider();
        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AppCookieScheme);

        Assert.Equal("__Host-instruments-platform", cookieOptions.Cookie.Name);
        Assert.Equal("/", cookieOptions.Cookie.Path);
        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/auth/login?tenantId=not-a-guid")]
    public async Task Login_endpoint_rejects_missing_or_invalid_tenant_id(string path)
    {
        using var client = CreateInteractiveOidcFactory().CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_endpoint_rejects_non_local_return_url()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory().CreateClient();

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&returnUrl=https://evil.example.test/app");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_endpoint_allows_configured_web_origin_return_url()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("auth.example.test", response.Headers.Location?.Host);
    }

    [Fact]
    public async Task Logout_endpoint_clears_app_cookie_and_redirects_without_provider_logout()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/");

        var response = await client.GetAsync($"/auth/logout?returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://app.example.test/", response.Headers.Location?.ToString());
        Assert.DoesNotContain(
            "auth.example.test",
            response.Headers.Location?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_endpoint_can_redirect_to_provider_logout_when_requested()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/register");

        var response = await client.GetAsync($"/auth/logout?provider=1&returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("auth.example.test", response.Headers.Location?.Host);
        Assert.Equal("/v2/logout", response.Headers.Location?.AbsolutePath);
        Assert.Contains("client_id=test-client-id", response.Headers.Location?.Query);
        Assert.Contains(
            "returnTo=https%3A%2F%2Fapp.example.test%2Fregister",
            response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_uses_forwarded_https_scheme_for_oidc_callback()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test",
            ["ReverseProxy:ForwardedHeaders:Enabled"] = "true"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/auth/login?tenantId={tenantId}&returnUrl={returnUrl}");
        request.Headers.Host = "api.example.test";
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "redirect_uri=https%3A%2F%2Fapi.example.test%2Fauth%2Fcallback",
            response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_forwards_allowed_prompt_to_provider_challenge()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&returnUrl={returnUrl}&prompt=login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("prompt=login", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_forwards_login_hint_to_provider_challenge()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");
        var loginHint = Uri.EscapeDataString("member@example.test");

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&returnUrl={returnUrl}&login_hint={loginHint}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("login_hint=member%40example.test", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_starts_registration_bootstrap_with_signup_hint()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/register");

        var response = await client.GetAsync(
            $"/auth/login?registration=1&returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("auth.example.test", response.Headers.Location?.Host);
        Assert.Contains("prompt=login", response.Headers.Location?.Query);
        Assert.Contains("screen_hint=signup", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_forwards_select_account_prompt_for_registration_bootstrap()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/register");

        var response = await client.GetAsync(
            $"/auth/login?registration=1&returnUrl={returnUrl}&prompt=select_account");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("auth.example.test", response.Headers.Location?.Host);
        Assert.Contains("prompt=select_account", response.Headers.Location?.Query);
        Assert.Contains("screen_hint=signup", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_rejects_unsupported_prompt()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory().CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&returnUrl=/app&prompt=create");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oidc_remote_failure_redirects_back_to_configured_web_return_url()
    {
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(
            resolver,
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.test"
            });
        var context = CreateRemoteFailureContext(
            "https://app.example.test/app",
            new Exception("access_denied"));

        await events.RemoteFailure(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            "https://app.example.test/app?auth=failed",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Oidc_remote_failure_preserves_email_unverified_reason()
    {
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(
            resolver,
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.test"
            });
        var context = CreateRemoteFailureContext(
            "https://app.example.test/register",
            new Exception("platform_login_verified_email_required"));
        Assert.NotNull(context.Properties);
        context.Properties!.Items[PlatformOidcEvents.AuthFailureReasonPropertyName] =
            PlatformOidcEvents.EmailUnverifiedFailureReason;

        await events.RemoteFailure(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            "https://app.example.test/register?auth=email_unverified",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Oidc_remote_failure_sends_unverified_registration_back_to_register()
    {
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(
            resolver,
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.test"
            });
        var context = CreateRemoteFailureContext(
            "https://app.example.test/app",
            new Exception("platform_login_verified_email_required"));
        Assert.NotNull(context.Properties);
        context.Properties!.Items[PlatformOidcEvents.AuthFailureReasonPropertyName] =
            PlatformOidcEvents.EmailUnverifiedFailureReason;
        context.Properties.Items[AuthEndpointRouteBuilderExtensions.RegistrationTokenPropertyName] =
            "registration-token";

        await events.RemoteFailure(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            "https://app.example.test/register?auth=email_unverified",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Oidc_remote_failure_without_redirect_uri_uses_configured_web_origin()
    {
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(
            resolver,
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.test"
            });
        var context = CreateRemoteFailureContext(
            null,
            new Exception("access_denied"));

        await events.RemoteFailure(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            "https://app.example.test/app?auth=failed",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Session_check_returns_unauthorized_instead_of_oidc_redirect_when_cookie_missing()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "http://127.0.0.1:5174"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
        request.Headers.Add("Origin", "http://127.0.0.1:5174");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://127.0.0.1:5174", Assert.Single(origins));
    }

    [Fact]
    public async Task Oidc_token_validation_requires_login_tenant_property()
    {
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext("researcher@example.test", emailVerified: true);

        await events.TokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
        Assert.Empty(resolver.Calls);
    }

    [Fact]
    public async Task Oidc_token_validation_rejects_unverified_email_by_default()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: false,
            tenantId);

        await events.TokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
        Assert.NotNull(context.Properties);
        Assert.Equal(
            PlatformOidcEvents.EmailUnverifiedFailureReason,
            context.Properties!.Items[PlatformOidcEvents.AuthFailureReasonPropertyName]);
        Assert.Empty(resolver.Calls);
    }

    [Fact]
    public async Task Oidc_token_validation_projects_platform_claims_from_resolved_login()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                userId,
                tenantId,
                Guid.NewGuid(),
                ["setup.manage", "campaign.launch"])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "  researcher@example.test  ",
            emailVerified: true,
            tenantId);

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Equal([(tenantId, "researcher@example.test", true, "auth0", "auth0|subject")], resolver.Calls);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.UserId && claim.Value == userId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.TenantMembership && claim.Value == tenantId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.Permission && claim.Value == "setup.manage");
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.Permission && claim.Value == "campaign.launch");
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.EmailVerified && claim.Value == "true");
    }

    [Fact]
    public async Task Oidc_token_validation_projects_normalized_platform_email_claim()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                Guid.NewGuid(),
                tenantId,
                Guid.NewGuid(),
                [PlatformPermissions.SetupManage])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "  Owner@Example.Test  ",
            emailVerified: true,
            tenantId);

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == ClaimTypes.Email && claim.Value == "owner@example.test");
    }

    [Fact]
    public async Task Oidc_token_validation_projects_team_manage_permission_from_resolved_login()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                Guid.NewGuid(),
                tenantId,
                Guid.NewGuid(),
                [PlatformPermissions.TeamManage])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "owner@example.test",
            emailVerified: true,
            tenantId);

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.Permission && claim.Value == PlatformPermissions.TeamManage);
    }

    [Fact]
    public async Task Oidc_token_validation_requires_provider_subject()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: true,
            tenantId,
            providerSubject: null);

        await events.TokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
        Assert.Empty(resolver.Calls);
    }

    [Fact]
    public async Task Oidc_token_validation_passes_provider_subject_to_resolver()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                Guid.NewGuid(),
                tenantId,
                sessionId,
                [])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: true,
            tenantId,
            providerSubject: "auth0|abc123");

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Equal([(tenantId, "researcher@example.test", true, "auth0", "auth0|abc123")], resolver.Calls);
    }

    [Fact]
    public async Task Oidc_token_validation_projects_session_id_claim_from_resolved_login()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                Guid.NewGuid(),
                tenantId,
                sessionId,
                [])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: true,
            tenantId);

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.SessionId && claim.Value == sessionId.ToString());
    }

    [Fact]
    public async Task Oidc_token_validation_projects_registration_bootstrap_claims_without_tenant_session()
    {
        var tenantResolver = new FakeOidcLoginResolver();
        var registrationResolver = new FakeRegistrationLoginResolver();
        var events = CreateOidcEvents(tenantResolver, registrationResolver: registrationResolver);
        var context = CreateTokenValidatedContext(
            "Owner@Example.Test",
            emailVerified: true,
            registrationBootstrap: true,
            providerSubject: "auth0|abc123");

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Empty(tenantResolver.Calls);
        Assert.Empty(registrationResolver.Calls);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformRegistrationClaimTypes.Pending && claim.Value == "true");
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformRegistrationClaimTypes.Email && claim.Value == "owner@example.test");
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformRegistrationClaimTypes.Provider && claim.Value == "auth0");
        Assert.DoesNotContain(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.TenantMembership);
        Assert.DoesNotContain(context.Principal.Claims, claim =>
            claim.Type == "sub" || claim.Value == "auth0|abc123");
    }

    [Fact]
    public async Task Oidc_token_validation_removes_provider_claims_from_cookie_principal()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                Guid.NewGuid(),
                tenantId,
                Guid.NewGuid(),
                [])
        };
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: true,
            tenantId,
            providerSubject: "auth0|abc123");

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.DoesNotContain(context.Principal!.Claims, claim =>
            claim.Type is "sub" or "email");
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.EmailVerified && claim.Value == "true");
    }

    [Fact]
    public async Task Oidc_token_validation_fails_when_platform_login_is_not_resolved()
    {
        var tenantId = Guid.NewGuid();
        var resolver = new FakeOidcLoginResolver();
        var events = CreateOidcEvents(resolver);
        var context = CreateTokenValidatedContext(
            "researcher@example.test",
            emailVerified: true,
            tenantId);

        await events.TokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
        Assert.DoesNotContain(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.UserId);
    }

    [Fact]
    public async Task Session_endpoint_rejects_anonymous_user()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add("X-Tenant-Id", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_endpoint_returns_current_actor_for_tenant_member()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        request.Headers.Add(TestAuthHandler.PermissionsHeader, "campaign.launch instrument.read");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.False(payload.EmailVerificationRequired);
        Assert.Contains("campaign.launch", payload.Permissions);
        Assert.Contains("instrument.read", payload.Permissions);
    }

    [Fact]
    public async Task Session_endpoint_infers_tenant_context_from_single_membership_when_header_is_missing()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        request.Headers.Add(TestAuthHandler.PermissionsHeader, "setup.manage");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Contains("setup.manage", payload.Permissions);
    }

    [Fact]
    public async Task Session_endpoint_returns_current_actor_email_when_claim_is_present()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        request.Headers.Add("X-Test-Email", "owner@example.test");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ProfileSessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal("owner@example.test", payload.Email);
    }

    [Fact]
    public async Task Session_endpoint_requires_email_verification_when_session_claim_is_unverified()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        request.Headers.Add(TestAuthHandler.PermissionsHeader, "setup.manage");
        request.Headers.Add(TestAuthHandler.EmailVerifiedHeader, "false");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.True(payload.EmailVerificationRequired);
        Assert.Contains("setup.manage", payload.Permissions);
    }

    [Fact]
    public async Task Session_endpoint_rejects_authenticated_user_without_tenant_membership()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/session");

        request.Headers.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_endpoint_rejects_anonymous_user()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");

        request.Headers.Add("X-Tenant-Id", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_endpoint_returns_token_for_tenant_member()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");

        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.CsrfToken));
    }

    [Fact]
    public async Task Csrf_endpoint_returns_token_for_tenant_member_over_local_http_in_development()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");

        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.CsrfToken));
    }

    [Fact]
    public async Task Cookie_validation_accepts_valid_local_session()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var validator = new FakePlatformSessionValidator { IsValid = true };
        var events = new PlatformSessionCookieEvents(validator);
        var context = CreateCookieValidatePrincipalContext(sessionId, userId, [tenantId]);

        await events.ValidatePrincipal(context);

        Assert.NotNull(context.Principal);
        Assert.Equal([(sessionId, userId, tenantId)], validator.Calls);
    }

    [Fact]
    public async Task Cookie_csrf_middleware_rejects_unsafe_cookie_mutation_without_token()
    {
        var antiforgery = new FakeAntiforgery { ThrowOnValidate = true };
        var nextCalled = false;
        var middleware = new CookieCsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateCsrfMiddlewareContext(
            HttpMethods.Post,
            PlatformAuthenticationSchemes.AppCookie,
            requireTenantMember: true);

        await middleware.InvokeAsync(context, antiforgery);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(1, antiforgery.ValidateCalls);
    }

    [Fact]
    public async Task Cookie_csrf_middleware_allows_unsafe_cookie_mutation_with_token()
    {
        var antiforgery = new FakeAntiforgery();
        var nextCalled = false;
        var middleware = new CookieCsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateCsrfMiddlewareContext(
            HttpMethods.Post,
            PlatformAuthenticationSchemes.AppCookie,
            requireTenantMember: true);

        context.Request.Headers["X-CSRF-TOKEN"] = "token";

        await middleware.InvokeAsync(context, antiforgery);

        Assert.True(nextCalled);
        Assert.Equal(1, antiforgery.ValidateCalls);
    }

    [Fact]
    public async Task Cookie_csrf_middleware_skips_test_auth_requests()
    {
        var antiforgery = new FakeAntiforgery { ThrowOnValidate = true };
        var nextCalled = false;
        var middleware = new CookieCsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateCsrfMiddlewareContext(
            HttpMethods.Post,
            TestAuthHandler.SchemeName,
            requireTenantMember: true);

        await middleware.InvokeAsync(context, antiforgery);

        Assert.True(nextCalled);
        Assert.Equal(0, antiforgery.ValidateCalls);
    }

    [Fact]
    public async Task Cookie_csrf_middleware_skips_public_respondent_mutations()
    {
        var antiforgery = new FakeAntiforgery { ThrowOnValidate = true };
        var nextCalled = false;
        var middleware = new CookieCsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateCsrfMiddlewareContext(
            HttpMethods.Post,
            PlatformAuthenticationSchemes.AppCookie,
            requireTenantMember: false,
            path: "/respondent/public-sessions/handle/submit");

        await middleware.InvokeAsync(context, antiforgery);

        Assert.True(nextCalled);
        Assert.Equal(0, antiforgery.ValidateCalls);
    }

    [Fact]
    public async Task Cookie_validation_rejects_missing_session_id()
    {
        var validator = new FakePlatformSessionValidator { IsValid = true };
        var events = new PlatformSessionCookieEvents(validator);
        var context = CreateCookieValidatePrincipalContext(
            sessionId: null,
            userId: Guid.NewGuid(),
            tenantIds: [Guid.NewGuid()]);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.Empty(validator.Calls);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("mismatched")]
    public async Task Cookie_validation_rejects_invalid_local_session(string _)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var validator = new FakePlatformSessionValidator { IsValid = false };
        var events = new PlatformSessionCookieEvents(validator);
        var context = CreateCookieValidatePrincipalContext(sessionId, userId, [tenantId]);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.Equal([(sessionId, userId, tenantId)], validator.Calls);
    }

    [Fact]
    public async Task Session_revoker_revokes_valid_session_claims()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var store = new FakePlatformSessionRevocationStore();
        var revoker = new PlatformSessionRevoker(store);
        var principal = CreatePlatformPrincipal(sessionId, userId, [tenantId]);

        await revoker.RevokeAsync(principal, CancellationToken.None);

        Assert.Equal([(sessionId, userId, tenantId, "logout")], store.Calls);
    }

    [Theory]
    [InlineData("missing-session")]
    [InlineData("missing-user")]
    [InlineData("missing-tenant")]
    [InlineData("multiple-tenants")]
    public async Task Session_revoker_ignores_missing_or_ambiguous_session_claims(string caseName)
    {
        var tenantId = Guid.NewGuid();
        Guid? sessionId = caseName == "missing-session" ? null : Guid.NewGuid();
        Guid? userId = caseName == "missing-user" ? null : Guid.NewGuid();
        var tenantIds = caseName switch
        {
            "missing-tenant" => [],
            "multiple-tenants" => new[] { tenantId, Guid.NewGuid() },
            _ => [tenantId]
        };
        var store = new FakePlatformSessionRevocationStore();
        var revoker = new PlatformSessionRevoker(store);
        var principal = CreatePlatformPrincipal(sessionId, userId, tenantIds);

        await revoker.RevokeAsync(principal, CancellationToken.None);

        Assert.Empty(store.Calls);
    }

    [Fact]
    public async Task Login_endpoint_forces_login_prompt_for_registration_token_without_tenant_id()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");

        var response = await client.GetAsync(
            $"/auth/login?registrationToken=registration-token&returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("auth.example.test", response.Headers.Location?.Host);
        Assert.Contains("prompt=login", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task Login_endpoint_clears_local_app_cookie_before_registration_challenge()
    {
        using var client = CreateInteractiveOidcFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.test"
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var returnUrl = Uri.EscapeDataString("https://app.example.test/app");

        var response = await client.GetAsync(
            $"/auth/login?registrationToken=registration-token&returnUrl={returnUrl}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie =>
            cookie.StartsWith("__Host-instruments-platform=;", StringComparison.Ordinal) &&
            cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_endpoint_rejects_conflicting_tenant_and_registration_context()
    {
        var tenantId = Guid.NewGuid();
        using var client = CreateInteractiveOidcFactory().CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            $"/auth/login?tenantId={tenantId}&registrationToken=registration-token&returnUrl=/app");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Oidc_token_validation_projects_platform_claims_from_registration_token()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantResolver = new FakeOidcLoginResolver();
        var registrationResolver = new FakeRegistrationLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                userId,
                tenantId,
                sessionId,
                [PlatformPermissions.SetupManage, PlatformPermissions.TeamManage])
        };
        var events = CreateOidcEvents(tenantResolver, registrationResolver: registrationResolver);
        var context = CreateTokenValidatedContext(
            "  Owner@Example.Test  ",
            emailVerified: true,
            registrationToken: "registration-token");

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Empty(tenantResolver.Calls);
        Assert.Equal([("registration-token", "owner@example.test", true, "auth0", "auth0|subject")], registrationResolver.Calls);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.UserId && claim.Value == userId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.TenantMembership && claim.Value == tenantId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.SessionId && claim.Value == sessionId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.Permission && claim.Value == PlatformPermissions.SetupManage);
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.Permission && claim.Value == PlatformPermissions.TeamManage);
    }

    [Fact]
    public async Task Oidc_token_validation_allows_unverified_email_for_registration_token()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantResolver = new FakeOidcLoginResolver();
        var registrationResolver = new FakeRegistrationLoginResolver
        {
            Resolution = new PlatformOidcLoginResolution(
                userId,
                tenantId,
                sessionId,
                [PlatformPermissions.SetupManage],
                EmailVerified: false)
        };
        var events = CreateOidcEvents(tenantResolver, registrationResolver: registrationResolver);
        var context = CreateTokenValidatedContext(
            "  Owner@Example.Test  ",
            emailVerified: false,
            registrationToken: "registration-token");

        await events.TokenValidated(context);

        Assert.Null(context.Result?.Failure);
        Assert.Empty(tenantResolver.Calls);
        Assert.Equal([("registration-token", "owner@example.test", false, "auth0", "auth0|subject")], registrationResolver.Calls);
        Assert.Contains(context.Principal!.Claims, claim =>
            claim.Type == PlatformClaimTypes.UserId && claim.Value == userId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.TenantMembership && claim.Value == tenantId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.SessionId && claim.Value == sessionId.ToString());
        Assert.Contains(context.Principal.Claims, claim =>
            claim.Type == PlatformClaimTypes.EmailVerified && claim.Value == "false");
    }

    private HttpClient CreateClient()
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });
            });
        }).CreateClient();
    }

    private WebApplicationFactory<Program> CreateInteractiveOidcFactory(
        Dictionary<string, string?>? configurationOverrides = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Oidc:InteractiveEnabled", "true");
            builder.UseSetting("Authentication:Oidc:Authority", "https://auth.example.test/");
            builder.UseSetting("Authentication:Oidc:ClientId", "test-client-id");
            builder.UseSetting("Authentication:Oidc:ClientSecret", "test-client-secret");
            builder.UseSetting("Authentication:Oidc:CallbackPath", "/auth/callback");
            builder.UseSetting("Authentication:Oidc:SignedOutCallbackPath", "/auth/signout-callback");
            if (configurationOverrides is not null)
            {
                foreach (var (key, value) in configurationOverrides)
                {
                    builder.UseSetting(key, value);
                }
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDb"] =
                        "Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used",
                    ["Authentication:Oidc:InteractiveEnabled"] = "true",
                    ["Authentication:Oidc:Authority"] = "https://auth.example.test/",
                    ["Authentication:Oidc:ClientId"] = "test-client-id",
                    ["Authentication:Oidc:ClientSecret"] = "test-client-secret",
                    ["Authentication:Oidc:CallbackPath"] = "/auth/callback",
                    ["Authentication:Oidc:SignedOutCallbackPath"] = "/auth/signout-callback"
                };
                if (configurationOverrides is not null)
                {
                    foreach (var (key, value) in configurationOverrides)
                    {
                        settings[key] = value;
                    }
                }

                configuration.AddInMemoryCollection(settings);
            });
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<OpenIdConnectOptions>(OidcScheme, options =>
                {
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                        new OpenIdConnectConfiguration
                    {
                        AuthorizationEndpoint = "https://auth.example.test/authorize"
                    });
                });
            });
        });
    }

    private static ServiceProvider BuildInteractiveAuthServiceProvider()
    {
        var configuration = CreateInteractiveOidcConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPlatformAuthentication(configuration, new TestHostEnvironment("Production"));

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateInteractiveOidcConfiguration(
        Dictionary<string, string?>? configurationOverrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Oidc:InteractiveEnabled"] = "true",
            ["Authentication:Oidc:Authority"] = "https://auth.example.test/",
            ["Authentication:Oidc:ClientId"] = "test-client-id",
            ["Authentication:Oidc:ClientSecret"] = "test-client-secret",
            ["Authentication:Oidc:CallbackPath"] = "/auth/callback",
            ["Authentication:Oidc:SignedOutCallbackPath"] = "/auth/signout-callback"
        };

        if (configurationOverrides is not null)
        {
            foreach (var (key, value) in configurationOverrides)
            {
                settings[key] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static PlatformOidcEvents CreateOidcEvents(
        FakeOidcLoginResolver resolver,
        Dictionary<string, string?>? configurationOverrides = null,
        FakeRegistrationLoginResolver? registrationResolver = null)
    {
        var configuration = CreateInteractiveOidcConfiguration(configurationOverrides);

        return new PlatformOidcEvents(
            resolver,
            registrationResolver ?? new FakeRegistrationLoginResolver(),
            new Sha256ProviderSubjectHasher(),
            configuration,
            NullLogger<PlatformOidcEvents>.Instance);
    }

    private static RemoteFailureContext CreateRemoteFailureContext(
        string? redirectUri,
        Exception failure)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return new RemoteFailureContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(
                OidcScheme,
                OidcScheme,
                typeof(TestAuthHandler)),
            new OpenIdConnectOptions(),
            failure)
        {
            Properties = properties
        };
    }

    private static TokenValidatedContext CreateTokenValidatedContext(
        string? email,
        bool emailVerified,
        Guid? tenantId = null,
        string? providerSubject = "auth0|subject",
        string? registrationToken = null,
        bool registrationBootstrap = false)
    {
        var claims = new List<Claim>
        {
            new("email_verified", emailVerified ? "true" : "false")
        };

        if (providerSubject is not null)
        {
            claims.Add(new Claim("sub", providerSubject));
        }

        if (email is not null)
        {
            claims.Add(new Claim("email", email));
        }

        var properties = new AuthenticationProperties();
        if (tenantId.HasValue)
        {
            properties.Items[AuthEndpointRouteBuilderExtensions.TenantIdPropertyName] =
                tenantId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(registrationToken))
        {
            properties.Items[AuthEndpointRouteBuilderExtensions.RegistrationTokenPropertyName] =
                registrationToken;
        }
        if (registrationBootstrap)
        {
            properties.Items[AuthEndpointRouteBuilderExtensions.RegistrationBootstrapPropertyName] =
                "true";
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, OidcScheme));

        return new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(
                OidcScheme,
                OidcScheme,
                typeof(TestAuthHandler)),
            new OpenIdConnectOptions(),
            principal,
            properties);
    }

    private static CookieValidatePrincipalContext CreateCookieValidatePrincipalContext(
        Guid? sessionId,
        Guid? userId,
        Guid[] tenantIds)
    {
        var principal = CreatePlatformPrincipal(sessionId, userId, tenantIds);
        var ticket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties(),
            AppCookieScheme);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = BuildCookieEventServiceProvider()
        };

        return new CookieValidatePrincipalContext(
            httpContext,
            new AuthenticationScheme(
                AppCookieScheme,
                AppCookieScheme,
                typeof(TestAuthHandler)),
            new CookieAuthenticationOptions(),
            ticket);
    }

    private static ClaimsPrincipal CreatePlatformPrincipal(
        Guid? sessionId,
        Guid? userId,
        Guid[] tenantIds)
    {
        var claims = new List<Claim>();

        if (sessionId.HasValue)
        {
            claims.Add(new Claim(PlatformClaimTypes.SessionId, sessionId.Value.ToString()));
        }

        if (userId.HasValue)
        {
            claims.Add(new Claim(PlatformClaimTypes.UserId, userId.Value.ToString()));
        }

        foreach (var tenantId in tenantIds)
        {
            claims.Add(new Claim(PlatformClaimTypes.TenantMembership, tenantId.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AppCookieScheme));
    }

    private static ServiceProvider BuildCookieEventServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddAuthentication(AppCookieScheme)
            .AddCookie(AppCookieScheme);

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateCsrfMiddlewareContext(
        string method,
        string authenticationType,
        bool requireTenantMember,
        string path = "/setup/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(PlatformClaimTypes.UserId, Guid.NewGuid().ToString())],
            authenticationType));

        var metadata = requireTenantMember
            ? new EndpointMetadataCollection(new AuthorizeAttribute(PlatformPolicies.TenantMember))
            : new EndpointMetadataCollection();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "csrf-test"));

        return context;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Platform.IntegrationTests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }

    private sealed class FakeOidcLoginResolver : IPlatformOidcLoginResolver
    {
        public PlatformOidcLoginResolution? Resolution { get; init; }

        public List<(Guid TenantId, string Email, bool EmailVerified, string Provider, string ProviderSubject)> Calls { get; } = [];

        public Task<PlatformOidcLoginResolution?> ResolveAsync(
            Guid tenantId,
            string email,
            bool emailVerified,
            string provider,
            string providerSubject,
            CancellationToken cancellationToken)
        {
            Calls.Add((tenantId, email, emailVerified, provider, providerSubject));
            return Task.FromResult(Resolution);
        }
    }

    private sealed class FakeRegistrationLoginResolver : IPlatformRegistrationLoginResolver
    {
        public PlatformOidcLoginResolution? Resolution { get; init; }

        public List<(string RegistrationToken, string Email, bool EmailVerified, string Provider, string ProviderSubject)> Calls { get; } = [];

        public Task<PlatformOidcLoginResolution?> ResolveAsync(
            string registrationToken,
            string email,
            bool emailVerified,
            string provider,
            string providerSubject,
            CancellationToken cancellationToken)
        {
            Calls.Add((registrationToken, email, emailVerified, provider, providerSubject));
            return Task.FromResult(Resolution);
        }
    }

    private sealed class FakePlatformSessionValidator : IPlatformSessionValidator
    {
        public bool IsValid { get; init; }

        public List<(Guid SessionId, Guid UserId, Guid TenantId)> Calls { get; } = [];

        public Task<bool> ValidateAsync(
            Guid sessionId,
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            Calls.Add((sessionId, userId, tenantId));
            return Task.FromResult(IsValid);
        }
    }

    private sealed class FakePlatformSessionRevocationStore : IPlatformSessionRevocationStore
    {
        public List<(Guid SessionId, Guid UserId, Guid TenantId, string Reason)> Calls { get; } = [];

        public Task RevokeAsync(
            Guid sessionId,
            Guid userId,
            Guid tenantId,
            string reason,
            CancellationToken cancellationToken)
        {
            Calls.Add((sessionId, userId, tenantId, reason));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public bool ThrowOnValidate { get; init; }

        public int ValidateCalls { get; private set; }

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            return new AntiforgeryTokenSet(
                "request-token",
                "cookie-token",
                "csrf",
                "X-CSRF-TOKEN");
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            return GetAndStoreTokens(httpContext);
        }

        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            return Task.FromResult(!ThrowOnValidate);
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            ValidateCalls++;

            return ThrowOnValidate
                ? Task.FromException(new AntiforgeryValidationException("invalid csrf token"))
                : Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
        }
    }

    private sealed record CsrfResponse(string CsrfToken);

    private sealed record SessionResponse(
        Guid UserId,
        Guid TenantId,
        bool EmailVerificationRequired,
        string[] Permissions);

    private sealed record ProfileSessionResponse(
        Guid UserId,
        Guid TenantId,
        string? Email,
        string[] Permissions);
}
