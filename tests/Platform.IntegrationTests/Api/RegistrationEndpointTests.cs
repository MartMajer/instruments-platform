using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Api.Auth;
using Platform.Api.Registration;
using Platform.Application.Auth;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class RegistrationEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string RegistrationOrCookieScheme = "RegistrationOrCookie";

    [Fact]
    public async Task Registration_intent_endpoint_returns_login_url_from_registration_service()
    {
        var expiresAt = DateTimeOffset.Parse("2026-05-18T20:00:00+00:00");
        var service = new FakeRegistrationIntentService
        {
            Result = Result.Success(new CreateRegistrationIntentResponse(
                "/auth/login?registrationToken=token&returnUrl=/app",
                expiresAt))
        };
        using var client = CreateClient(service);

        var response = await client.PostAsJsonAsync("/registration/intents", new CreateRegistrationIntentRequest(
            "Owner@Example.Test",
            "Croatian Research Lab",
            "beta-code"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateRegistrationIntentResponse>();
        Assert.NotNull(payload);
        Assert.Equal("/auth/login?registrationToken=token&returnUrl=/app", payload.LoginUrl);
        Assert.Equal(expiresAt, payload.ExpiresAt);
        Assert.Equal("Owner@Example.Test", service.Requests.Single().Email);
        Assert.Equal("Croatian Research Lab", service.Requests.Single().OrganizationName);
        Assert.Equal("beta-code", service.Requests.Single().AccessCode);
    }

    [Fact]
    public void Registration_intent_login_url_omits_signup_hint_for_entra_provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:ProviderKey"] = "entra-workforce",
                ["Authentication:Oidc:ProviderLogoutMode"] = "microsoft"
            })
            .Build();

        var loginUrl = RegistrationIntentService.BuildRegistrationLoginUrl(
            "registration-token",
            "https://app.example.test/app",
            "owner@example.test",
            configuration);

        Assert.DoesNotContain("screen_hint=", loginUrl, StringComparison.Ordinal);
        var query = QueryHelpers.ParseQuery(new Uri(new Uri("https://api.example.test"), loginUrl).Query);
        Assert.Equal("owner@example.test", query["login_hint"].Single());
    }

    [Fact]
    public async Task Registration_intent_endpoint_maps_validation_failure_to_bad_request()
    {
        var service = new FakeRegistrationIntentService
        {
            Result = Result.Failure<CreateRegistrationIntentResponse>(
                Error.Validation("registration.invalid_access_code", "Private beta access code is invalid."))
        };
        using var client = CreateClient(service);

        var response = await client.PostAsJsonAsync("/registration/intents", new CreateRegistrationIntentRequest(
            "owner@example.test",
            "Croatian Research Lab",
            "wrong"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("registration.invalid_access_code", json, StringComparison.Ordinal);
        Assert.Contains("Private beta access code is invalid.", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registration_intent_endpoint_returns_existing_workspace_sign_in_url_on_conflict()
    {
        var service = new FakeRegistrationIntentService
        {
            Result = Result.Failure<CreateRegistrationIntentResponse>(
                Error.Conflict(
                    "registration.email_exists",
                    "A workspace already exists for this email. Sign in instead.",
                    new Dictionary<string, object?>
                    {
                        ["loginUrl"] = "/auth/login?tenantId=tenant-id&login_hint=owner%40example.test"
                    }))
        };
        using var client = CreateClient(service);

        var response = await client.PostAsJsonAsync("/registration/intents", new CreateRegistrationIntentRequest(
            "owner@example.test",
            "Croatian Research Lab",
            "beta-code"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(payload);
        Assert.Equal("registration.email_exists", payload["title"].GetString());
        Assert.Equal(
            "/auth/login?tenantId=tenant-id&login_hint=owner%40example.test",
            payload["loginUrl"].GetString());
    }

    [Fact]
    public async Task Registration_workspace_endpoint_signs_in_session_that_opens_app_and_logs_out_locally()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var workspaceService = new FakeRegistrationWorkspaceService
        {
            Result = Result.Success(
                new CreateRegistrationWorkspaceResult(
                    "https://app.example.test/app",
                    new PlatformOidcLoginResolution(
                        userId,
                        tenantId,
                        sessionId,
                        [PlatformPermissions.SetupManage, PlatformPermissions.TeamManage],
                        "owner@example.test",
                        EmailVerified: false)))
        };
        var validator = new FakePlatformSessionValidator();
        var revoker = new FakePlatformSessionRevoker();
        using var client = CreateWorkspaceClient(workspaceService, validator, revoker);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/registration/workspaces")
        {
            Content = JsonContent.Create(
                new CreateRegistrationWorkspaceRequest(
                    "Owner Lab",
                    "martin-beta-2026",
                    "https://app.example.test/app"))
        };
        request.Headers.Add(RegistrationTestAuthHandler.EmailHeader, "owner@example.test");
        request.Headers.Add(RegistrationTestAuthHandler.ProviderHeader, "auth0");
        request.Headers.Add(RegistrationTestAuthHandler.ProviderSubjectHashHeader, "hashed-subject");

        var registrationResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, registrationResponse.StatusCode);
        Assert.Single(workspaceService.Requests);
        Assert.Equal("owner@example.test", workspaceService.Requests[0].Identity.Email);
        var created = await registrationResponse.Content.ReadFromJsonAsync<CreateRegistrationWorkspaceResponse>();
        Assert.NotNull(created);
        Assert.Equal(tenantId, created.TenantId);
        Assert.Equal("owner@example.test", created.Email);

        var sessionResponse = await client.GetAsync("/auth/session");

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        Assert.Contains(validator.Calls, call =>
            call.SessionId == sessionId &&
            call.UserId == userId &&
            call.TenantId == tenantId);
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(session);
        Assert.Equal(tenantId, session.TenantId);
        Assert.Equal(userId, session.UserId);
        Assert.True(session.EmailVerificationRequired);
        Assert.Contains(PlatformPermissions.SetupManage, session.Permissions);

        var logoutResponse = await client.GetAsync(
            "/auth/logout?returnUrl=https%3A%2F%2Fapp.example.test%2F");

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal("https://app.example.test/", logoutResponse.Headers.Location?.ToString());
        Assert.True(revoker.CallCount >= 1);

        var afterLogoutResponse = await client.GetAsync("/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResponse.StatusCode);
    }

    private HttpClient CreateClient(FakeRegistrationIntentService service)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IRegistrationIntentService>(service);
            });
        }).CreateClient();
    }

    private HttpClient CreateWorkspaceClient(
        FakeRegistrationWorkspaceService workspaceService,
        FakePlatformSessionValidator validator,
        FakePlatformSessionRevoker revoker)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "https://app.example.test"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = RegistrationOrCookieScheme;
                        options.DefaultChallengeScheme = RegistrationOrCookieScheme;
                        options.DefaultSignInScheme = PlatformAuthenticationSchemes.AppCookie;
                    })
                    .AddPolicyScheme(
                        RegistrationOrCookieScheme,
                        displayName: null,
                        options =>
                        {
                            options.ForwardDefaultSelector = context =>
                                context.Request.Headers.ContainsKey(RegistrationTestAuthHandler.EmailHeader)
                                    ? RegistrationTestAuthHandler.SchemeName
                                    : PlatformAuthenticationSchemes.AppCookie;
                        })
                    .AddCookie(
                        PlatformAuthenticationSchemes.AppCookie,
                        options => options.EventsType = typeof(PlatformSessionCookieEvents))
                    .AddScheme<AuthenticationSchemeOptions, RegistrationTestAuthHandler>(
                        RegistrationTestAuthHandler.SchemeName,
                        configureOptions: null);
                services.AddScoped<PlatformSessionCookieEvents>();
                services.AddSingleton<IRegistrationWorkspaceService>(workspaceService);
                services.AddSingleton<IPlatformSessionValidator>(validator);
                services.AddSingleton<IPlatformSessionRevoker>(revoker);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private sealed class FakeRegistrationIntentService : IRegistrationIntentService
    {
        public Result<CreateRegistrationIntentResponse> Result { get; init; }

        public List<CreateRegistrationIntentRequest> Requests { get; } = [];

        public Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
            CreateRegistrationIntentRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeRegistrationWorkspaceService : IRegistrationWorkspaceService
    {
        public Result<CreateRegistrationWorkspaceResult>? Result { get; init; }

        public List<(RegistrationIdentity Identity, CreateRegistrationWorkspaceRequest Request)> Requests { get; } = [];

        public Task<Result<CreateRegistrationWorkspaceResult>> CreateAsync(
            RegistrationIdentity identity,
            CreateRegistrationWorkspaceRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add((identity, request));

            var result = Result
                ?? throw new InvalidOperationException("Workspace registration result was not configured.");

            return Task.FromResult(result);
        }
    }

    private sealed class FakePlatformSessionValidator : IPlatformSessionValidator
    {
        public List<(Guid SessionId, Guid UserId, Guid TenantId)> Calls { get; } = [];

        public Task<bool> ValidateAsync(
            Guid sessionId,
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            Calls.Add((sessionId, userId, tenantId));
            return Task.FromResult(true);
        }
    }

    private sealed class FakePlatformSessionRevoker : IPlatformSessionRevoker
    {
        public int CallCount { get; private set; }

        public Task RevokeAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RegistrationTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "RegistrationTest";
        public const string EmailHeader = "X-Test-Registration-Email";
        public const string ProviderHeader = "X-Test-Registration-Provider";
        public const string ProviderSubjectHashHeader = "X-Test-Registration-Subject-Hash";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(EmailHeader, out var email) ||
                !Request.Headers.TryGetValue(ProviderHeader, out var provider) ||
                !Request.Headers.TryGetValue(ProviderSubjectHashHeader, out var providerSubjectHash))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [
                    new Claim(PlatformRegistrationClaimTypes.Pending, "true"),
                    new Claim(PlatformRegistrationClaimTypes.Email, email.ToString()),
                    new Claim(PlatformRegistrationClaimTypes.Provider, provider.ToString()),
                    new Claim(
                        PlatformRegistrationClaimTypes.ProviderSubjectHash,
                        providerSubjectHash.ToString())
                ],
                SchemeName);

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed record SessionResponse(
        Guid UserId,
        Guid TenantId,
        string Email,
        bool EmailVerificationRequired,
        IReadOnlyCollection<string> Permissions);
}
