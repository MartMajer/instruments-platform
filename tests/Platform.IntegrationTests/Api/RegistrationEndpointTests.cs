using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Api.Registration;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class RegistrationEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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
}