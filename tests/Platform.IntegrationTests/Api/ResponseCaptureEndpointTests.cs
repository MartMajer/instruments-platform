using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Application.Features.Responses;
using Platform.IntegrationTests.Support;
using Platform.IntegrationTests.Support.Logging;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class ResponseCaptureEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Respondent_campaign_endpoint_returns_template_questions()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeResponseCaptureStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(HttpMethod.Get, $"/respondent/campaigns/{campaignId}", tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RespondentCampaignResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public async Task Respondent_campaign_endpoint_allows_tenant_member_without_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeResponseCaptureStore(campaignId: campaignId));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/respondent/campaigns/{campaignId}",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Response_capture_endpoints_create_assignment_session_save_answers_and_submit()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var client = CreateClient(new FakeResponseCaptureStore(
            campaignId: campaignId,
            assignmentId: assignmentId,
            sessionId: sessionId));

        var assignmentResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/campaigns/{campaignId}/lab-assignment",
            tenantId));
        var sessionResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            "/respondent/sessions",
            tenantId,
            new CreateResponseSessionRequest(assignmentId, "en")));
        var saveResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Put,
            $"/respondent/sessions/{sessionId}/answers",
            tenantId,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(Guid.NewGuid(), "4")
            ])));
        var submitResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/submit",
            tenantId,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200)));

        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitResponseSessionResponse>();
        Assert.NotNull(submitted);
        Assert.Equal(sessionId, submitted.Id);
    }

    [Fact]
    public async Task Submit_endpoint_maps_required_answer_validation_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var validation = Result.Failure<SubmitResponseSessionResponse>(
            Error.Validation(
                "response.required_answers_missing",
                "Required questions must be answered before submit."));
        using var client = CreateClient(new FakeResponseCaptureStore(
            sessionId: sessionId,
            submitResult: validation));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/submit",
            tenantId,
            new SubmitResponseSessionRequest());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("response.required_answers_missing", payload.Title);
    }

    [Fact]
    public async Task Open_link_entry_endpoint_does_not_require_tenant_auth_headers()
    {
        const string token = "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        using var client = CreateClient(new FakeResponseCaptureStore(openLinkToken: token));

        var response = await client.GetAsync($"/respondent/open-links/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssignmentId);
        Assert.Equal("anonymous", payload.ResponseIdentityMode);
        Assert.False(payload.RequiresParticipantCode);
        Assert.NotNull(payload.ConsentDocument);
        Assert.Equal("Default participant disclosure", payload.ConsentDocument.Title);
        Assert.Contains("data_processing", payload.ConsentDocument.RequiredGrants);
        Assert.Single(payload.Questions);
    }

    [Fact]
    public void Public_respondent_endpoints_require_rate_limiting_policies()
    {
        using var configuredFactory = CreateFactory(new FakeResponseCaptureStore());
        _ = configuredFactory.CreateClient();
        var endpoints = configuredFactory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .ToArray();
        var expectedPoliciesByRoute = new Dictionary<string, string>
        {
            ["/respondent/open-links/{token}"] = "public-respondent-entry",
            ["/respondent/open-links/{token}/sessions"] = "public-respondent-entry",
            ["/respondent/identified-entries/{token}"] = "public-respondent-entry",
            ["/respondent/identified-entries/{token}/sessions"] = "public-respondent-entry",
            ["/respondent/identified-queues/{token}"] = "public-respondent-entry",
            ["/respondent/identified-queues/{token}/assignments/{assignmentId:guid}/sessions"] =
                "public-respondent-entry",
            ["/respondent/open-links/{token}/sessions/{sessionId:guid}/draft"] = "public-respondent-session",
            ["/respondent/open-links/{token}/sessions/{sessionId:guid}/answers"] = "public-respondent-session",
            ["/respondent/open-links/{token}/sessions/{sessionId:guid}/submit"] = "public-respondent-submit",
            ["/respondent/public-sessions/{handle}/draft"] = "public-respondent-session",
            ["/respondent/public-sessions/{handle}/answers"] = "public-respondent-session",
            ["/respondent/public-sessions/{handle}/submit"] = "public-respondent-submit"
        };

        foreach (var (route, policyName) in expectedPoliciesByRoute)
        {
            var endpoint = Assert.Single(
                endpoints,
                candidate => candidate.RoutePattern.RawText == route);
            var rateLimitMetadata = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();

            Assert.NotNull(rateLimitMetadata);
            Assert.Equal(policyName, rateLimitMetadata.PolicyName);
        }
    }

    [Fact]
    public async Task Public_respondent_entry_rate_limit_rejects_rotated_token_probe_before_store()
    {
        const string firstToken = "opn_11111111111141118111111111111111_sensitiveOPN1";
        const string secondToken = "opn_11111111111141118111111111111111_sensitiveOPN2";
        var store = new FakeResponseCaptureStore(openLinkToken: firstToken);
        using var client = CreateClient(
            store,
            configuration: new Dictionary<string, string?>
            {
                ["PublicRespondentRateLimiting:EntryPermitLimit"] = "1",
                ["PublicRespondentRateLimiting:WindowSeconds"] = "60"
            });

        var firstResponse = await client.GetAsync($"/respondent/open-links/{firstToken}");
        var secondResponse = await client.GetAsync($"/respondent/open-links/{secondToken}");
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(1, store.OpenLinkEntryRequestCount);
        Assert.Contains("public_respondent.rate_limited", secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(firstToken, secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(secondToken, secondBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_respondent_entry_rate_limit_uses_forwarded_client_ip_in_reverse_proxy_mode()
    {
        const string firstToken = "opn_11111111111141118111111111111111_forwardedOPN1";
        const string secondToken = "opn_11111111111141118111111111111111_forwardedOPN2";
        const string thirdToken = "opn_11111111111141118111111111111111_forwardedOPN3";
        var store = new FakeResponseCaptureStore(openLinkToken: firstToken);
        using var client = CreateClient(
            store,
            configuration: new Dictionary<string, string?>
            {
                ["ReverseProxy:ForwardedHeaders:Enabled"] = "true",
                ["PublicRespondentRateLimiting:EntryPermitLimit"] = "1",
                ["PublicRespondentRateLimiting:WindowSeconds"] = "60"
            });
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, $"/respondent/open-links/{firstToken}");
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, $"/respondent/open-links/{secondToken}");
        using var thirdRequest = new HttpRequestMessage(HttpMethod.Get, $"/respondent/open-links/{thirdToken}");
        firstRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.10");
        secondRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.10");
        thirdRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.11");

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);
        var thirdResponse = await client.SendAsync(thirdRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, thirdResponse.StatusCode);
        Assert.Equal(2, store.OpenLinkEntryRequestCount);
    }

    [Fact]
    public async Task Public_respondent_session_creation_rate_limit_rejects_rotated_token_participant_code_probe_before_store()
    {
        const string firstToken = "opn_11111111111141118111111111111111_sensitiveOPN1";
        const string secondToken = "opn_11111111111141118111111111111111_sensitiveOPN2";
        const string participantCode = "alpha-raw-participant-code-2026";
        var store = new FakeResponseCaptureStore(openLinkToken: firstToken);
        using var client = CreateClient(
            store,
            configuration: new Dictionary<string, string?>
            {
                ["PublicRespondentRateLimiting:EntryPermitLimit"] = "1",
                ["PublicRespondentRateLimiting:WindowSeconds"] = "60"
            });
        var request = new CreateOpenLinkSessionRequest(
            "en",
            Guid.NewGuid(),
            ["data_processing"],
            participantCode);

        var firstResponse = await client.PostAsJsonAsync(
            $"/respondent/open-links/{firstToken}/sessions",
            request);
        var secondResponse = await client.PostAsJsonAsync(
            $"/respondent/open-links/{secondToken}/sessions",
            request);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(1, store.OpenLinkSessionRequestCount);
        Assert.Contains("public_respondent.rate_limited", secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(firstToken, secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(secondToken, secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(participantCode, secondBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_session_answer_rate_limit_rejects_repeated_handle_write_before_store()
    {
        const string publicHandle = "rsh_11111111111141118111111111111111_sensitiveRSH";
        const string rawAnswer = "raw free-text answer with identifiable detail";
        var store = new FakeResponseCaptureStore(publicSessionHandle: publicHandle);
        using var client = CreateClient(
            store,
            configuration: new Dictionary<string, string?>
            {
                ["PublicRespondentRateLimiting:SessionPermitLimit"] = "1",
                ["PublicRespondentRateLimiting:WindowSeconds"] = "60"
            });
        var request = new SaveAnswersRequest(
        [
            new SaveAnswerRequest(
                Guid.NewGuid(),
                rawAnswer)
        ]);

        var firstResponse = await client.PutAsJsonAsync(
            $"/respondent/public-sessions/{publicHandle}/answers",
            request);
        var secondResponse = await client.PutAsJsonAsync(
            $"/respondent/public-sessions/{publicHandle}/answers",
            request);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(1, store.PublicSessionSaveRequestCount);
        Assert.Contains("public_respondent.rate_limited", secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(publicHandle, secondBody, StringComparison.Ordinal);
        Assert.DoesNotContain(rawAnswer, secondBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_session_answer_rate_limit_keeps_different_handles_independent_after_entry()
    {
        const string firstPublicHandle = "rsh_11111111111141118111111111111111_sensitiveRSH1";
        const string secondPublicHandle = "rsh_11111111111141118111111111111111_sensitiveRSH2";
        var store = new FakeResponseCaptureStore(publicSessionHandle: firstPublicHandle);
        using var client = CreateClient(
            store,
            configuration: new Dictionary<string, string?>
            {
                ["PublicRespondentRateLimiting:SessionPermitLimit"] = "1",
                ["PublicRespondentRateLimiting:WindowSeconds"] = "60"
            });
        var request = new SaveAnswersRequest(
        [
            new SaveAnswerRequest(
                Guid.NewGuid(),
                "4")
        ]);

        var firstResponse = await client.PutAsJsonAsync(
            $"/respondent/public-sessions/{firstPublicHandle}/answers",
            request);
        var secondResponse = await client.PutAsJsonAsync(
            $"/respondent/public-sessions/{secondPublicHandle}/answers",
            request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, store.PublicSessionSaveRequestCount);
    }

    [Fact]
    public async Task Email_invite_entry_endpoint_does_not_require_tenant_auth_headers()
    {
        const string token = "inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        using var client = CreateClient(new FakeResponseCaptureStore(openLinkToken: token));

        var response = await client.GetAsync($"/respondent/open-links/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssignmentId);
        Assert.NotNull(payload.ConsentDocument);
        Assert.Equal("Default participant disclosure", payload.ConsentDocument.Title);
    }

    [Fact]
    public async Task Identified_entry_endpoint_does_not_require_tenant_auth_headers()
    {
        const string token = "idn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        using var client = CreateClient(new FakeResponseCaptureStore(identifiedEntryToken: token));

        var response = await client.GetAsync($"/respondent/identified-entries/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssignmentId);
        Assert.Equal("identified", payload.ResponseIdentityMode);
        Assert.False(payload.RequiresParticipantCode);
        Assert.NotNull(payload.ConsentDocument);
        Assert.Equal("Default participant disclosure", payload.ConsentDocument.Title);
    }

    [Fact]
    public async Task Identified_entry_endpoint_returns_assignment_subject_context()
    {
        const string token = "idn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var campaignId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var respondentSubjectId = Guid.NewGuid();
        var targetSubjectId = Guid.NewGuid();
        var entry = new OpenLinkEntryResponse(
            campaignId,
            assignmentId,
            Guid.NewGuid(),
            "Leadership feedback",
            "live",
            "identified",
            RequiresParticipantCode: false,
            "en",
            new ConsentDocumentResponse(
                Guid.NewGuid(),
                "en",
                "1.0.0",
                "Default participant disclosure",
                "Consent body",
                ["data_processing"],
                []),
            [
                new RespondentQuestionResponse(
                    questionId,
                    1,
                    "leadership_clarity",
                    "likert",
                    "This person gives clear direction.",
                    Required: true)
            ],
            AssignmentRole: "manager",
            RespondentSubject: new RespondentSubjectContextResponse(
                respondentSubjectId,
                "Miriam Graham",
                "miriam@example.test",
                "msgraph:tenant:miriam"),
            TargetSubject: new RespondentSubjectContextResponse(
                targetSubjectId,
                "Adele Vance",
                "adele@example.test",
                "msgraph:tenant:adele"));
        using var client = CreateClient(new FakeResponseCaptureStore(
            identifiedEntryToken: token,
            identifiedEntryResult: Result.Success(entry)));

        var response = await client.GetAsync($"/respondent/identified-entries/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("manager", payload.AssignmentRole);
        Assert.NotNull(payload.RespondentSubject);
        Assert.Equal(respondentSubjectId, payload.RespondentSubject.Id);
        Assert.Equal("Miriam Graham", payload.RespondentSubject.DisplayName);
        Assert.Equal("miriam@example.test", payload.RespondentSubject.Email);
        Assert.NotNull(payload.TargetSubject);
        Assert.Equal(targetSubjectId, payload.TargetSubject.Id);
        Assert.Equal("Adele Vance", payload.TargetSubject.DisplayName);
        Assert.Equal("msgraph:tenant:adele", payload.TargetSubject.ExternalId);
    }

    [Fact]
    public async Task Identified_queue_endpoint_returns_safe_assignment_queue_without_tenant_auth_headers()
    {
        const string token = "idq_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var campaignId = Guid.NewGuid();
        var respondentSubjectId = Guid.NewGuid();
        var targetSubjectId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        using var client = CreateClient(new FakeResponseCaptureStore(
            campaignId: campaignId,
            assignmentId: assignmentId,
            identifiedQueueToken: token,
            queueRespondentSubjectId: respondentSubjectId,
            queueTargetSubjectId: targetSubjectId));

        var response = await client.GetAsync($"/respondent/identified-queues/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IdentifiedQueueEntryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(campaignId, payload.CampaignId);
        Assert.Equal("identified", payload.ResponseIdentityMode);
        Assert.Equal(respondentSubjectId, payload.RespondentSubject.Id);
        Assert.Equal("Miriam Graham", payload.RespondentSubject.DisplayName);
        var assignment = Assert.Single(payload.Assignments);
        Assert.Equal(assignmentId, assignment.AssignmentId);
        Assert.Equal("manager", assignment.Role);
        Assert.Equal("not_started", assignment.ResponseStatus);
        Assert.NotNull(assignment.TargetSubject);
        Assert.Equal(targetSubjectId, assignment.TargetSubject.Id);
        Assert.Equal("Adele Vance", assignment.TargetSubject.DisplayName);

        var serialized = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("external", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("msgraph", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Identified_queue_assignment_session_endpoint_returns_queue_assignment_session_and_draft()
    {
        const string token = "idq_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var campaignId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const string publicHandle = "rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        using var client = CreateClient(new FakeResponseCaptureStore(
            campaignId: campaignId,
            assignmentId: assignmentId,
            sessionId: sessionId,
            identifiedQueueToken: token,
            publicSessionHandle: publicHandle));

        var response = await client.PostAsJsonAsync(
            $"/respondent/identified-queues/{token}/assignments/{assignmentId}/sessions",
            new CreateOpenLinkSessionRequest("en"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var payload = await JsonDocument.ParseAsync(stream);
        Assert.Equal(
            sessionId.ToString(),
            payload.RootElement.GetProperty("session").GetProperty("id").GetString());
        Assert.Equal(
            publicHandle,
            payload.RootElement.GetProperty("session").GetProperty("publicHandle").GetString());
        Assert.Equal(
            assignmentId.ToString(),
            payload.RootElement.GetProperty("assignment").GetProperty("assignmentId").GetString());
        Assert.Equal("draft", payload.RootElement.GetProperty("assignment").GetProperty("responseStatus").GetString());
        Assert.Equal(1, payload.RootElement.GetProperty("savedAnswerCount").GetInt32());
        Assert.Equal(
            campaignId.ToString(),
            payload.RootElement.GetProperty("queue").GetProperty("campaignId").GetString());
    }

    [Fact]
    public async Task Open_link_session_endpoint_maps_missing_required_consent_grants_to_problem_details()
    {
        const string token = "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var validation = Result.Failure<ResponseSessionResponse>(
            Error.Validation(
                "consent.required_grants_missing",
                "Required consent grants must be accepted before starting a response session."));
        using var client = CreateClient(new FakeResponseCaptureStore(
            openLinkToken: token,
            openLinkSessionResult: validation));

        var response = await client.PostAsJsonAsync(
            $"/respondent/open-links/{token}/sessions",
            new CreateOpenLinkSessionRequest("en"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("consent.required_grants_missing", payload.Title);
    }

    [Fact]
    public async Task Open_link_public_endpoints_create_session_save_answers_and_submit()
    {
        const string token = "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var sessionId = Guid.NewGuid();
        const string publicHandle = "rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        using var client = CreateClient(new FakeResponseCaptureStore(
            openLinkToken: token,
            sessionId: sessionId,
            publicSessionHandle: publicHandle));

        var entryResponse = await client.GetAsync($"/respondent/open-links/{token}");
        var entry = await entryResponse.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(entry);

        var sessionResponse = await client.PostAsJsonAsync(
            $"/respondent/open-links/{token}/sessions",
            new CreateOpenLinkSessionRequest(
                "en",
                entry.ConsentDocument.Id,
                entry.ConsentDocument.RequiredGrants));
        var saveResponse = await client.PutAsJsonAsync(
            $"/respondent/open-links/{token}/sessions/{sessionId}/answers",
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(Guid.NewGuid(), "4")
            ]));
        var submitResponse = await client.PostAsJsonAsync(
            $"/respondent/open-links/{token}/sessions/{sessionId}/submit",
            new SubmitResponseSessionRequest(TimeTakenMs: 1200));

        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<ResponseSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal(publicHandle, session.PublicHandle);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitResponseSessionResponse>();
        Assert.NotNull(submitted);
        Assert.Equal(sessionId, submitted.Id);
    }

    [Fact]
    public async Task Tenant_lab_response_write_endpoints_require_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var client = CreateClient(new FakeResponseCaptureStore(
            campaignId: campaignId,
            assignmentId: assignmentId,
            sessionId: sessionId));

        var assignmentResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/campaigns/{campaignId}/lab-assignment",
            tenantId,
            permissions: null));
        var sessionResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            "/respondent/sessions",
            tenantId,
            new CreateResponseSessionRequest(assignmentId, "en"),
            permissions: null));
        var saveResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Put,
            $"/respondent/sessions/{sessionId}/answers",
            tenantId,
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(Guid.NewGuid(), "4")
            ]),
            permissions: null));
        var submitResponse = await client.SendAsync(AuthenticatedRequest(
            HttpMethod.Post,
            $"/respondent/sessions/{sessionId}/submit",
            tenantId,
            new SubmitResponseSessionRequest(TimeTakenMs: 1200),
            permissions: null));

        Assert.Equal(HttpStatusCode.Forbidden, assignmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, submitResponse.StatusCode);
    }

    [Fact]
    public async Task Identified_entry_session_endpoint_returns_public_handle_without_tenant_auth_headers()
    {
        const string token = "idn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var sessionId = Guid.NewGuid();
        const string publicHandle = "rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var store = new FakeResponseCaptureStore(
            identifiedEntryToken: token,
            sessionId: sessionId,
            publicSessionHandle: publicHandle);
        using var client = CreateClient(store);

        var entryResponse = await client.GetAsync($"/respondent/identified-entries/{token}");
        var entry = await entryResponse.Content.ReadFromJsonAsync<OpenLinkEntryResponse>();
        Assert.NotNull(entry);

        var sessionResponse = await client.PostAsJsonAsync(
            $"/respondent/identified-entries/{token}/sessions",
            new CreateOpenLinkSessionRequest(
                "en",
                entry.ConsentDocument.Id,
                entry.ConsentDocument.RequiredGrants));

        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<ResponseSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(publicHandle, session.PublicHandle);
        Assert.Equal(entry.ConsentDocument.Id, store.LastIdentifiedEntrySessionRequest?.AcceptedConsentDocumentId);
    }

    [Fact]
    public async Task Public_session_handle_endpoints_draft_save_and_submit_without_tenant_auth_headers()
    {
        const string publicHandle = "rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var sessionId = Guid.NewGuid();
        var store = new FakeResponseCaptureStore(
            sessionId: sessionId,
            publicSessionHandle: publicHandle);
        using var client = CreateClient(store);

        var draftResponse = await client.GetAsync(
            $"/respondent/public-sessions/{publicHandle}/draft");
        var saveResponse = await client.PutAsJsonAsync(
            $"/respondent/public-sessions/{publicHandle}/answers",
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(Guid.NewGuid(), "4")
            ]));
        var submitResponse = await client.PostAsJsonAsync(
            $"/respondent/public-sessions/{publicHandle}/submit",
            new SubmitResponseSessionRequest(TimeTakenMs: 1200));

        Assert.Equal(HttpStatusCode.OK, draftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var draft = await draftResponse.Content.ReadFromJsonAsync<OpenLinkSessionDraftResponse>();
        Assert.NotNull(draft);
        Assert.NotNull(draft.Entry);
        Assert.Equal(sessionId, draft.Session.Id);
        Assert.Equal(publicHandle, store.LastPublicSessionDraftHandle);
        Assert.Equal(publicHandle, store.LastPublicSessionSaveHandle);
        Assert.Equal(publicHandle, store.LastPublicSessionSubmitHandle);
    }

    [Fact]
    public async Task Open_link_session_draft_endpoint_returns_saved_answers_without_tenant_auth_headers()
    {
        const string token = "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var sessionId = Guid.NewGuid();
        var store = new FakeResponseCaptureStore(
            openLinkToken: token,
            sessionId: sessionId);
        using var client = CreateClient(store);

        var response = await client.GetAsync(
            $"/respondent/open-links/{token}/sessions/{sessionId}/draft");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var payload = await JsonDocument.ParseAsync(stream);
        Assert.Equal(
            sessionId.ToString(),
            payload.RootElement.GetProperty("session").GetProperty("id").GetString());
        Assert.Equal(1, payload.RootElement.GetProperty("savedAnswerCount").GetInt32());
        Assert.Equal(
            "4",
            payload.RootElement.GetProperty("answers")[0].GetProperty("value").GetString());
        Assert.Equal((token, sessionId), store.LastOpenLinkDraftRequest);
    }

    [Fact]
    public async Task Open_link_session_endpoint_accepts_participant_code_payload_without_tenant_auth_headers()
    {
        const string token = "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        var sessionId = Guid.NewGuid();
        var store = new FakeResponseCaptureStore(openLinkToken: token, sessionId: sessionId);
        using var client = CreateClient(store);

        var response = await client.PostAsJsonAsync(
            $"/respondent/open-links/{token}/sessions",
            new CreateOpenLinkSessionRequest(
                "en",
                Guid.NewGuid(),
                ["data_processing"],
                ParticipantCode: "  alpha-001  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("  alpha-001  ", store.LastOpenLinkSessionRequest?.ParticipantCode);
    }

    [Fact]
    public async Task Open_link_entry_endpoint_maps_invalid_token_to_neutral_not_found()
    {
        const string token = "invalid-token";
        var result = Result.Failure<OpenLinkEntryResponse>(
            Error.NotFound(
                "open_link.not_available",
                "This link is no longer available."));
        using var client = CreateClient(new FakeResponseCaptureStore(openLinkEntryResult: result));

        var response = await client.GetAsync($"/respondent/open-links/{token}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("open_link.not_available", payload.Title);
        Assert.DoesNotContain(token, payload.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_respondent_boundary_failures_do_not_log_raw_sensitive_values()
    {
        var openLinkFailure = Result.Failure<OpenLinkEntryResponse>(
            Error.NotFound(
                "open_link.not_available",
                "This link is no longer available."));
        var identifiedEntryFailure = Result.Failure<OpenLinkEntryResponse>(
            Error.NotFound(
                "identified_entry.not_available",
                "This entry link is no longer available."));
        using var capturedLogs = new CapturedLoggerProvider();
        using var client = CreateClient(
            new FakeResponseCaptureStore(
                openLinkEntryResult: openLinkFailure,
                identifiedEntryResult: identifiedEntryFailure,
                publicSessionHandle: "rsh_11111111111141118111111111111111_sensitiveRSH"),
            capturedLogs);

        var openLinkResponse = await client.GetAsync(
            "/respondent/open-links/opn_11111111111141118111111111111111_sensitiveOPN");
        var identifiedEntryResponse = await client.GetAsync(
            "/respondent/identified-entries/idn_11111111111141118111111111111111_sensitiveIDN");
        var sessionResponse = await client.PostAsJsonAsync(
            "/respondent/open-links/opn_11111111111141118111111111111111_sensitiveOPN/sessions",
            new CreateOpenLinkSessionRequest(
                "en",
                Guid.NewGuid(),
                ["data_processing"],
                ParticipantCode: "alpha-raw-participant-code-2026"));
        var saveResponse = await client.PutAsJsonAsync(
            "/respondent/public-sessions/rsh_11111111111141118111111111111111_sensitiveRSH/answers",
            new SaveAnswersRequest(
            [
                new SaveAnswerRequest(
                    Guid.NewGuid(),
                    "raw free-text answer with identifiable detail")
            ]));

        Assert.Equal(HttpStatusCode.NotFound, openLinkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, identifiedEntryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, sessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        SensitiveLogAssert.DoesNotContain(capturedLogs, SensitiveLogAssert.DefaultSentinels);
    }

    private HttpClient CreateClient(
        IResponseCaptureStore store,
        CapturedLoggerProvider? capturedLoggerProvider = null,
        IReadOnlyDictionary<string, string?>? configuration = null)
    {
        return CreateFactory(store, capturedLoggerProvider, configuration).CreateClient();
    }

    private WebApplicationFactory<Program> CreateFactory(
        IResponseCaptureStore store,
        CapturedLoggerProvider? capturedLoggerProvider = null,
        IReadOnlyDictionary<string, string?>? configuration = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            if (configuration is not null)
            {
                foreach (var (key, value) in configuration)
                {
                    builder.UseSetting(key, value);
                }

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(configuration);
                });
            }

            if (capturedLoggerProvider is not null)
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.AddProvider(capturedLoggerProvider);
                });
            }

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
        });
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        object? body = null,
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

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private sealed class FakeResponseCaptureStore(
        Guid? campaignId = null,
        Guid? assignmentId = null,
        Guid? sessionId = null,
        Result<SubmitResponseSessionResponse>? submitResult = null,
        string? openLinkToken = null,
        Result<OpenLinkEntryResponse>? openLinkEntryResult = null,
        Result<ResponseSessionResponse>? openLinkSessionResult = null,
        Result<OpenLinkSessionDraftResponse>? openLinkDraftResult = null,
        string? identifiedEntryToken = null,
        Result<OpenLinkEntryResponse>? identifiedEntryResult = null,
        string? identifiedQueueToken = null,
        Result<IdentifiedQueueEntryResponse>? identifiedQueueResult = null,
        Guid? queueRespondentSubjectId = null,
        Guid? queueTargetSubjectId = null,
        string? publicSessionHandle = null) : IResponseCaptureStore
    {
        private readonly Guid _campaignId = campaignId ?? Guid.NewGuid();
        private readonly Guid _assignmentId = assignmentId ?? Guid.NewGuid();
        private readonly Guid _sessionId = sessionId ?? Guid.NewGuid();
        private readonly Guid _questionId = Guid.NewGuid();
        private readonly string _openLinkToken = openLinkToken ?? "opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        private readonly string _identifiedEntryToken = identifiedEntryToken ?? "idn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        private readonly string _identifiedQueueToken = identifiedQueueToken ?? "idq_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";
        private readonly Guid _queueRespondentSubjectId = queueRespondentSubjectId ?? Guid.NewGuid();
        private readonly Guid _queueTargetSubjectId = queueTargetSubjectId ?? Guid.NewGuid();
        private readonly string _publicSessionHandle = publicSessionHandle ?? "rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ";

        public CreateOpenLinkSessionRequest? LastOpenLinkSessionRequest { get; private set; }
        public CreateOpenLinkSessionRequest? LastIdentifiedEntrySessionRequest { get; private set; }
        public (string Token, Guid SessionId)? LastOpenLinkDraftRequest { get; private set; }
        public string? LastPublicSessionDraftHandle { get; private set; }
        public string? LastPublicSessionSaveHandle { get; private set; }
        public string? LastPublicSessionSubmitHandle { get; private set; }
        public int OpenLinkEntryRequestCount { get; private set; }
        public int OpenLinkSessionRequestCount { get; private set; }
        public int PublicSessionSaveRequestCount { get; private set; }

        public Task<Result<RespondentCampaignResponse>> GetCampaignAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new RespondentCampaignResponse(
                campaignId,
                Guid.NewGuid(),
                "Response capture campaign",
                "draft",
                "anonymous",
                "en",
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ])));
        }

        public Task<Result<LabAssignmentResponse>> CreateLabAssignmentAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new LabAssignmentResponse(
                _assignmentId,
                _campaignId,
                "anonymous")));
        }

        public Task<Result<ResponseSessionResponse>> CreateSessionAsync(
            Guid tenantId,
            CreateResponseSessionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ResponseSessionResponse(
                _sessionId,
                request.AssignmentId,
                request.Locale,
                DateTimeOffset.UtcNow,
                SubmittedAt: null,
                TimeTakenMs: null)));
        }

        public Task<Result<SaveAnswersResponse>> SaveAnswersAsync(
            Guid tenantId,
            Guid sessionId,
            SaveAnswersRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new SaveAnswersResponse(
                sessionId,
                request.Answers.Count)));
        }

        public Task<Result<SubmitResponseSessionResponse>> SubmitSessionAsync(
            Guid tenantId,
            Guid sessionId,
            SubmitResponseSessionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(submitResult ?? Result.Success(new SubmitResponseSessionResponse(
                sessionId,
                DateTimeOffset.UtcNow)));
        }

        public Task<Result<OpenLinkEntryResponse>> GetOpenLinkEntryAsync(
            string token,
            CancellationToken cancellationToken)
        {
            OpenLinkEntryRequestCount++;

            if (openLinkEntryResult.HasValue)
            {
                return Task.FromResult(openLinkEntryResult.Value);
            }

            return Task.FromResult(Result.Success(new OpenLinkEntryResponse(
                _campaignId,
                _assignmentId,
                Guid.NewGuid(),
                "Open link campaign",
                "live",
                "anonymous",
                RequiresParticipantCode: false,
                "en",
                SampleConsentDocument(),
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ])));
        }

        public Task<Result<EmailInvitationUnsubscribeResponse>> UnsubscribeEmailInvitationAsync(
            string token,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new EmailInvitationUnsubscribeResponse("suppressed")));
        }

        public Task<Result<OpenLinkEntryResponse>> GetIdentifiedEntryAsync(
            string token,
            CancellationToken cancellationToken)
        {
            if (identifiedEntryResult.HasValue)
            {
                return Task.FromResult(identifiedEntryResult.Value);
            }

            return Task.FromResult(Result.Success(new OpenLinkEntryResponse(
                _campaignId,
                _assignmentId,
                Guid.NewGuid(),
                "Identified campaign",
                "live",
                "identified",
                RequiresParticipantCode: false,
                "en",
                SampleConsentDocument(),
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ])));
        }

        public Task<Result<IdentifiedQueueEntryResponse>> GetIdentifiedQueueAsync(
            string token,
            CancellationToken cancellationToken)
        {
            if (identifiedQueueResult.HasValue)
            {
                return Task.FromResult(identifiedQueueResult.Value);
            }

            if (!string.Equals(token, _identifiedQueueToken, StringComparison.Ordinal))
            {
                return Task.FromResult(Result.Failure<IdentifiedQueueEntryResponse>(
                    Error.NotFound(
                        "identified_queue.not_available",
                        "This queue link is no longer available.")));
            }

            return Task.FromResult(Result.Success(new IdentifiedQueueEntryResponse(
                _campaignId,
                Guid.NewGuid(),
                "Leadership feedback",
                "live",
                "identified",
                "en",
                SampleConsentDocument(),
                new SafeRespondentSubjectContextResponse(
                    _queueRespondentSubjectId,
                    "Miriam Graham",
                    "Miriam Graham",
                    "miriam@example.test"),
                [
                    new IdentifiedQueueAssignmentResponse(
                        _assignmentId,
                        "manager",
                        "not_started",
                        new SafeRespondentSubjectContextResponse(
                            _queueTargetSubjectId,
                            "Adele Vance",
                            "Adele Vance",
                            "adele@example.test"),
                        SessionId: null,
                        StartedAt: null,
                        SubmittedAt: null)
                ],
                AssignmentCount: 1,
                StartedCount: 0,
                SubmittedCount: 0,
                Questions:
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ])));
        }

        public Task<Result<IdentifiedQueueSessionDraftResponse>> CreateIdentifiedQueueAssignmentSessionAsync(
            string token,
            Guid assignmentId,
            CreateOpenLinkSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(token, _identifiedQueueToken, StringComparison.Ordinal) ||
                assignmentId != _assignmentId)
            {
                return Task.FromResult(Result.Failure<IdentifiedQueueSessionDraftResponse>(
                    Error.NotFound(
                        "identified_queue.assignment_not_found",
                        "This queue assignment is no longer available.")));
            }

            var startedAt = DateTimeOffset.UtcNow;
            var assignment = new IdentifiedQueueAssignmentResponse(
                _assignmentId,
                "manager",
                "draft",
                new SafeRespondentSubjectContextResponse(
                    _queueTargetSubjectId,
                    "Adele Vance",
                    "Adele Vance",
                    "adele@example.test"),
                _sessionId,
                startedAt,
                SubmittedAt: null);
            var queue = new IdentifiedQueueEntryResponse(
                _campaignId,
                Guid.NewGuid(),
                "Leadership feedback",
                "live",
                "identified",
                "en",
                SampleConsentDocument(),
                new SafeRespondentSubjectContextResponse(
                    _queueRespondentSubjectId,
                    "Miriam Graham",
                    "Miriam Graham",
                    "miriam@example.test"),
                [assignment],
                AssignmentCount: 1,
                StartedCount: 1,
                SubmittedCount: 0,
                Questions:
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ]);
            var session = new ResponseSessionResponse(
                _sessionId,
                _assignmentId,
                request.Locale,
                startedAt,
                SubmittedAt: null,
                TimeTakenMs: null,
                PublicHandle: _publicSessionHandle);
            var answers = new[]
            {
                new SavedAnswerResponse(
                    _questionId,
                    "4",
                    Comment: null,
                    IsSkipped: false,
                    IsNa: false)
            };

            return Task.FromResult(Result.Success(new IdentifiedQueueSessionDraftResponse(
                queue,
                assignment,
                session,
                answers,
                answers.Length)));
        }

        public Task<Result<ResponseSessionResponse>> CreateIdentifiedEntrySessionAsync(
            string token,
            CreateOpenLinkSessionRequest request,
            CancellationToken cancellationToken)
        {
            LastIdentifiedEntrySessionRequest = request;

            return Task.FromResult(Result.Success(new ResponseSessionResponse(
                _sessionId,
                _assignmentId,
                request.Locale,
                DateTimeOffset.UtcNow,
                SubmittedAt: null,
                TimeTakenMs: null,
                PublicHandle: _publicSessionHandle)));
        }

        public Task<Result<ResponseSessionResponse>> CreateOpenLinkSessionAsync(
            string token,
            CreateOpenLinkSessionRequest request,
            CancellationToken cancellationToken)
        {
            OpenLinkSessionRequestCount++;
            LastOpenLinkSessionRequest = request;

            if (openLinkSessionResult.HasValue)
            {
                return Task.FromResult(openLinkSessionResult.Value);
            }

            return Task.FromResult(Result.Success(new ResponseSessionResponse(
                _sessionId,
                _assignmentId,
                request.Locale,
                DateTimeOffset.UtcNow,
                SubmittedAt: null,
                TimeTakenMs: null,
                PublicHandle: _publicSessionHandle)));
        }

        public Task<Result<OpenLinkSessionDraftResponse>> GetOpenLinkSessionDraftAsync(
            string token,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            LastOpenLinkDraftRequest = (token, sessionId);

            if (openLinkDraftResult.HasValue)
            {
                return Task.FromResult(openLinkDraftResult.Value);
            }

            return Task.FromResult(Result.Success(new OpenLinkSessionDraftResponse(
                new ResponseSessionResponse(
                    sessionId,
                    _assignmentId,
                    "en",
                    DateTimeOffset.UtcNow,
                    SubmittedAt: null,
                    TimeTakenMs: null),
                [
                    new SavedAnswerResponse(
                        _questionId,
                        "4",
                        Comment: null,
                        IsSkipped: false,
                        IsNa: false)
                ],
                SavedAnswerCount: 1)));
        }

        public Task<Result<OpenLinkSessionDraftResponse>> GetPublicSessionDraftAsync(
            string handle,
            CancellationToken cancellationToken)
        {
            LastPublicSessionDraftHandle = handle;

            return Task.FromResult(Result.Success(new OpenLinkSessionDraftResponse(
                new ResponseSessionResponse(
                    _sessionId,
                    _assignmentId,
                    "en",
                    DateTimeOffset.UtcNow,
                    SubmittedAt: null,
                    TimeTakenMs: null,
                    PublicHandle: handle),
                [
                    new SavedAnswerResponse(
                        _questionId,
                        "4",
                        Comment: null,
                        IsSkipped: false,
                        IsNa: false)
                ],
                SavedAnswerCount: 1,
                Entry: SampleOpenLinkEntry())));
        }

        public Task<Result<SaveAnswersResponse>> SaveOpenLinkAnswersAsync(
            string token,
            Guid sessionId,
            SaveAnswersRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new SaveAnswersResponse(
                sessionId,
                request.Answers.Count)));
        }

        public Task<Result<SaveAnswersResponse>> SavePublicSessionAnswersAsync(
            string handle,
            SaveAnswersRequest request,
            CancellationToken cancellationToken)
        {
            PublicSessionSaveRequestCount++;
            LastPublicSessionSaveHandle = handle;

            return Task.FromResult(Result.Success(new SaveAnswersResponse(
                _sessionId,
                request.Answers.Count)));
        }

        public Task<Result<SubmitResponseSessionResponse>> SubmitOpenLinkSessionAsync(
            string token,
            Guid sessionId,
            SubmitResponseSessionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new SubmitResponseSessionResponse(
                sessionId,
                DateTimeOffset.UtcNow)));
        }

        public Task<Result<SubmitResponseSessionResponse>> SubmitPublicSessionAsync(
            string handle,
            SubmitResponseSessionRequest request,
            CancellationToken cancellationToken)
        {
            LastPublicSessionSubmitHandle = handle;

            return Task.FromResult(Result.Success(new SubmitResponseSessionResponse(
                _sessionId,
                DateTimeOffset.UtcNow)));
        }

        private OpenLinkEntryResponse SampleOpenLinkEntry()
        {
            return new OpenLinkEntryResponse(
                _campaignId,
                _assignmentId,
                Guid.NewGuid(),
                "Open link campaign",
                "live",
                "anonymous",
                RequiresParticipantCode: false,
                "en",
                SampleConsentDocument(),
                [
                    new RespondentQuestionResponse(
                        _questionId,
                        1,
                        "q01",
                        "likert",
                        "I feel depleted after work.",
                        Required: true)
                ]);
        }

        private static ConsentDocumentResponse SampleConsentDocument()
        {
            return new ConsentDocumentResponse(
                Guid.NewGuid(),
                "en",
                "1.0.0",
                "Default participant disclosure",
                "Consent body",
                ["data_processing", "research_participation"],
                []);
        }
    }
}
