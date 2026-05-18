using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Scoring;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class ScoringEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Compute_scores_endpoint_returns_score_outputs()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var scoreRunId = Guid.NewGuid();
        using var client = CreateClient(new FakeScoreComputationStore(
            sessionId,
            Result.Success(new ComputeScoresResponse(
                scoreRunId,
                sessionId,
                [
                    new ComputedScoreResponse("total", 4m, 2, 3, "ok")
                ]))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/scores",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payloadJson = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<ComputeScoresResponse>(
            payloadJson,
            JsonSerializerOptions.Web);
        Assert.NotNull(payload);
        Assert.Equal(scoreRunId, payload.ScoreRunId);
        var score = Assert.Single(payload.Scores);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4m, score.Value);
        Assert.Equal(2, score.NValid);
        Assert.Equal(3, score.NExpected);
        Assert.Equal("ok", score.MissingPolicyStatus);

        using var document = JsonDocument.Parse(payloadJson);
        var scoreJson = document.RootElement.GetProperty("scores")[0];
        Assert.Equal(2, scoreJson.GetProperty("nValid").GetInt32());
        Assert.Equal(3, scoreJson.GetProperty("nExpected").GetInt32());
        Assert.Equal("ok", scoreJson.GetProperty("missingPolicyStatus").GetString());
    }

    [Fact]
    public async Task Compute_scores_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var client = CreateClient(new FakeScoreComputationStore(
            sessionId,
            Result.Failure<ComputeScoresResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/scores",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Compute_scores_endpoint_maps_validation_errors_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var client = CreateClient(new FakeScoreComputationStore(
            sessionId,
            Result.Failure<ComputeScoresResponse>(
                Error.Validation("score.rule_missing", "No scoring rule exists."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/scores",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("score.rule_missing", payload.Title);
    }

    private HttpClient CreateClient(IScoreComputationStore store)
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

                services.AddSingleton(store);
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        string? permissions = "setup.manage")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        if (permissions is not null)
        {
            request.Headers.Add(TestAuthHandler.PermissionsHeader, permissions);
        }

        return request;
    }

    private sealed class FakeScoreComputationStore(
        Guid expectedSessionId,
        Result<ComputeScoresResponse> result) : IScoreComputationStore
    {
        public Task<Result<ComputeScoresResponse>> ComputeResponseScoresAsync(
            Guid tenantId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedSessionId, sessionId);

            return Task.FromResult(result);
        }
    }
}
