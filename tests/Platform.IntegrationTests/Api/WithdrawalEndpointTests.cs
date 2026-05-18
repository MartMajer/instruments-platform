using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Retention;
using Platform.Domain.Consent;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class WithdrawalEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task WithdrawalEndpoint_creates_tenant_admin_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            Result.Success(SampleResponse(requestId, targetId, idempotent: false)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests",
            tenantId,
            new CreateWithdrawalRequestRequest(
                WithdrawalTargetKinds.ResponseSession,
                targetId,
                RetentionPolicy.Anonymize,
                "owner_requested"),
            userId: actorUserId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Requested, payload.Status);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, store.Command?.TargetKind);
        Assert.Equal(targetId, store.Command?.TargetId);
        Assert.Equal(RetentionPolicy.Anonymize, store.Command?.RequestedAction);
        Assert.Equal(actorUserId, store.Command?.ActorUserId);
    }

    [Fact]
    public async Task WithdrawalEndpoint_requires_tenant_context()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests",
            tenantId,
            new CreateWithdrawalRequestRequest(
                WithdrawalTargetKinds.ResponseSession,
                Guid.NewGuid(),
                RetentionPolicy.Delete),
            includeTenantHeader: false);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.CreateCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_rejects_read_only_member_without_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests",
            tenantId,
            new CreateWithdrawalRequestRequest(
                WithdrawalTargetKinds.ResponseSession,
                Guid.NewGuid(),
                RetentionPolicy.Delete),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.CreateCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_issues_anonymous_withdrawal_token_for_response_session()
    {
        var tenantId = Guid.NewGuid();
        var responseSessionId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var rawToken = $"wdr_{tenantId:N}_secret-token";
        var store = new FakeWithdrawalRuntimeStore(
            issueTokenResult: Result.Success(new WithdrawalRequestTokenIssueResponse(
                tokenId,
                responseSessionId,
                RetentionPolicy.Delete,
                expiresAt,
                rawToken)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests/tokens",
            tenantId,
            new IssueWithdrawalRequestTokenRequest(
                responseSessionId,
                RetentionPolicy.Delete,
                expiresAt,
                "participant_request"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestTokenIssueResponse>();
        Assert.NotNull(payload);
        Assert.Equal(tokenId, payload.TokenId);
        Assert.Equal(responseSessionId, payload.ResponseSessionId);
        Assert.Equal(RetentionPolicy.Delete, payload.RequestedAction);
        Assert.Equal(expiresAt, payload.ExpiresAt);
        Assert.Equal(rawToken, payload.RawToken);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(1, store.IssueTokenCallCount);
        Assert.Equal(responseSessionId, store.IssueTokenCommand?.ResponseSessionId);
        Assert.Equal(RetentionPolicy.Delete, store.IssueTokenCommand?.RequestedAction);
        Assert.Equal(expiresAt, store.IssueTokenCommand?.ExpiresAt);
        Assert.Equal("participant_request", store.IssueTokenCommand?.ReasonCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(rawToken, body, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WithdrawalEndpoint_issue_token_rejects_read_only_member_without_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests/tokens",
            tenantId,
            new IssueWithdrawalRequestTokenRequest(
                Guid.NewGuid(),
                RetentionPolicy.Delete,
                DateTimeOffset.UtcNow.AddHours(2),
                "participant_request"),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.IssueTokenCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_response_excludes_sensitive_target_data()
    {
        var tenantId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            Result.Success(SampleResponse(Guid.NewGuid(), targetId, idempotent: false)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            "/withdrawal-requests",
            tenantId,
            new CreateWithdrawalRequestRequest(
                WithdrawalTargetKinds.ResponseSession,
                targetId,
                RetentionPolicy.Delete,
                "owner_requested"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        foreach (var sensitive in new[]
        {
            "subject@example.com",
            "identified-ip-hash",
            "identified-user-agent-hash",
            "participant",
            "token",
            "recipient",
            "provider",
            "public_handle",
            "publichandle",
            "salt",
            "rawToken",
            "rawAnswer"
        })
        {
            Assert.DoesNotContain(sensitive, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task WithdrawalEndpoint_creates_public_anonymous_token_request_without_tenant_auth()
    {
        var rawToken = $"wdr_{Guid.NewGuid():N}_secret";
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            anonymousCreateResult: Result.Success(SampleResponse(requestId, targetId, idempotent: false)));
        using var client = CreateClient(store);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/withdrawal-requests/anonymous")
        {
            Content = JsonContent.Create(new CreateAnonymousWithdrawalRequestRequest(
                rawToken,
                RetentionPolicy.Delete,
                "participant_request"))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.RequestId);
        Assert.Equal(1, store.AnonymousCreateCallCount);
        Assert.Equal(rawToken, store.AnonymousCommand?.Token);
        Assert.Equal(RetentionPolicy.Delete, store.AnonymousCommand?.RequestedAction);
        Assert.Equal("participant_request", store.AnonymousCommand?.ReasonCode);
        Assert.Equal(0, store.CreateCallCount);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rawToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithdrawalEndpoint_anonymous_token_validation_failure_maps_to_bad_request_without_echo()
    {
        var rawToken = $"wdr_{Guid.NewGuid():N}_bad";
        var store = new FakeWithdrawalRuntimeStore(
            anonymousCreateResult: Result.Failure<WithdrawalRequestResponse>(
                Error.Validation("withdrawal_token.invalid", "Withdrawal token is invalid.")));
        using var client = CreateClient(store);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/withdrawal-requests/anonymous")
        {
            Content = JsonContent.Create(new CreateAnonymousWithdrawalRequestRequest(
                rawToken,
                RetentionPolicy.Delete,
                "participant_request"))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, store.AnonymousCreateCallCount);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rawToken, body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("withdrawal_token.expired")]
    [InlineData("withdrawal_token.consumed")]
    public async Task WithdrawalEndpoint_anonymous_token_conflict_failures_map_to_conflict_without_echo(
        string code)
    {
        var rawToken = $"wdr_{Guid.NewGuid():N}_conflict";
        var store = new FakeWithdrawalRuntimeStore(
            anonymousCreateResult: Result.Failure<WithdrawalRequestResponse>(
                Error.Conflict(code, "Withdrawal token cannot be used.")));
        using var client = CreateClient(store);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/withdrawal-requests/anonymous")
        {
            Content = JsonContent.Create(new CreateAnonymousWithdrawalRequestRequest(
                rawToken,
                RetentionPolicy.Delete,
                "participant_request"))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, store.AnonymousCreateCallCount);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rawToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithdrawalEndpoint_lists_tenant_admin_requests()
    {
        var tenantId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var review = SampleReviewResponse(requestId, targetId);
        var store = new FakeWithdrawalRuntimeStore(
            listResult: Result.Success<IReadOnlyList<WithdrawalRequestReviewResponse>>(new[] { review }));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/withdrawal-requests",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestReviewResponse[]>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload);
        Assert.Equal(requestId, item.RequestId);
        Assert.Equal(targetId, item.TargetId);
        Assert.True(item.CanApprove);
        Assert.True(item.CanDeny);
        Assert.False(item.CanExecute);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(1, store.ListCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_list_rejects_read_only_member_without_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            "/withdrawal-requests",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.ListCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_gets_tenant_admin_request()
    {
        var tenantId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            getResult: Result.Success(SampleReviewResponse(requestId, targetId)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/withdrawal-requests/{requestId}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestReviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.RequestId);
        Assert.Equal(targetId, payload.TargetId);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(requestId, store.GetRequestId);
        Assert.Equal(1, store.GetCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_get_maps_missing_request_to_not_found()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            getResult: Result.Failure<WithdrawalRequestReviewResponse>(
                Error.NotFound("withdrawal_request.not_found", "Withdrawal request was not found.")));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/withdrawal-requests/{requestId}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, store.GetCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_get_response_excludes_sensitive_target_data()
    {
        var tenantId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            getResult: Result.Success(SampleReviewResponse(Guid.NewGuid(), targetId)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/withdrawal-requests/{Guid.NewGuid()}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        foreach (var sensitive in new[]
        {
            "subject@example.com",
            "identified-ip-hash",
            "identified-user-agent-hash",
            "participant",
            "token",
            "recipient",
            "provider",
            "public_handle",
            "publichandle",
            "salt",
            "rawToken",
            "rawAnswer"
        })
        {
            Assert.DoesNotContain(sensitive, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task WithdrawalEndpoint_approves_tenant_admin_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            approveResult: Result.Success(SampleReviewResponse(
                requestId,
                targetId,
                WithdrawalEventStatuses.Planned)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/withdrawal-requests/{requestId}/approve",
            tenantId,
            new WithdrawalRequestDecisionRequest("owner_confirmed"),
            userId: actorUserId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestReviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Planned, payload.Status);
        Assert.False(payload.CanApprove);
        Assert.False(payload.CanDeny);
        Assert.True(payload.CanExecute);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(requestId, store.ApproveRequestId);
        Assert.Equal(actorUserId, store.ApproveCommand?.ActorUserId);
        Assert.Equal("owner_confirmed", store.ApproveCommand?.ReasonCode);
        Assert.Equal(1, store.ApproveCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_denies_tenant_admin_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            denyResult: Result.Success(SampleReviewResponse(
                requestId,
                targetId,
                WithdrawalEventStatuses.Denied,
                DateTimeOffset.Parse("2026-05-18T09:03:00+00:00"))));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/withdrawal-requests/{requestId}/deny",
            tenantId,
            new WithdrawalRequestDecisionRequest("owner_denied"),
            userId: actorUserId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalRequestReviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Denied, payload.Status);
        Assert.NotNull(payload.ProcessedAt);
        Assert.False(payload.CanApprove);
        Assert.False(payload.CanDeny);
        Assert.False(payload.CanExecute);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(requestId, store.DenyRequestId);
        Assert.Equal(actorUserId, store.DenyCommand?.ActorUserId);
        Assert.Equal("owner_denied", store.DenyCommand?.ReasonCode);
        Assert.Equal(1, store.DenyCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_approve_rejects_read_only_member_without_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/withdrawal-requests/{Guid.NewGuid()}/approve",
            tenantId,
            new WithdrawalRequestDecisionRequest("owner_confirmed"),
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.ApproveCallCount);
    }

    [Fact]
    public async Task WithdrawalEndpoint_executes_tenant_admin_request()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore(
            executeResult: Result.Success(SampleExecutionResponse(
                requestId,
                WithdrawalEventStatuses.Completed)));
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/withdrawal-requests/{requestId}/execute",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<WithdrawalExecutionStateResponse>();
        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.WithdrawalEventId);
        Assert.Equal(WithdrawalEventStatuses.Completed, payload.Status);
        Assert.NotNull(payload.ProcessedAt);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(requestId, store.ExecuteRequestId);
        Assert.Equal(1, store.ExecuteCallCount);
        var body = await response.Content.ReadAsStringAsync();
        foreach (var sensitive in new[]
        {
            "subject@example.com",
            "identified-ip-hash",
            "identified-user-agent-hash",
            "participant",
            "token",
            "recipient",
            "provider",
            "public_handle",
            "publichandle",
            "salt",
            "rawToken",
            "rawAnswer"
        })
        {
            Assert.DoesNotContain(sensitive, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task WithdrawalEndpoint_execute_rejects_read_only_member_without_setup_manage()
    {
        var tenantId = Guid.NewGuid();
        var store = new FakeWithdrawalRuntimeStore();
        using var client = CreateClient(store);
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/withdrawal-requests/{Guid.NewGuid()}/execute",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, store.ExecuteCallCount);
    }

    private HttpClient CreateClient(FakeWithdrawalRuntimeStore store)
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

                services.AddSingleton<IWithdrawalRuntimeStore>(store);
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        object? body = null,
        string? permissions = "setup.manage",
        Guid? userId = null,
        bool includeTenantHeader = true)
    {
        var request = new HttpRequestMessage(method, url);
        if (includeTenantHeader)
        {
            request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        }

        request.Headers.Add(TestAuthHandler.UserIdHeader, (userId ?? Guid.NewGuid()).ToString());
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

    private static WithdrawalRequestResponse SampleResponse(
        Guid requestId,
        Guid targetId,
        bool idempotent)
    {
        return new WithdrawalRequestResponse(
            requestId,
            WithdrawalTargetKinds.ResponseSession,
            targetId,
            RetentionPolicy.Anonymize,
            WithdrawalEventStatuses.Requested,
            DateTimeOffset.Parse("2026-05-18T09:00:00+00:00"),
            idempotent,
            ConsentRecordCount: 1,
            ResponseSessionCount: 1,
            AnswerCount: 1,
            ScoreRunCount: 1,
            ScoreCount: 1);
    }

    private static WithdrawalRequestReviewResponse SampleReviewResponse(
        Guid requestId,
        Guid targetId,
        string status = WithdrawalEventStatuses.Requested,
        DateTimeOffset? processedAt = null)
    {
        var canDecide = status == WithdrawalEventStatuses.Requested;
        var canExecute = status is WithdrawalEventStatuses.Planned or WithdrawalEventStatuses.Processing;

        return new WithdrawalRequestReviewResponse(
            requestId,
            WithdrawalTargetKinds.ResponseSession,
            targetId,
            RetentionPolicy.Anonymize,
            status,
            DateTimeOffset.Parse("2026-05-18T09:00:00+00:00"),
            processedAt,
            ConsentRecordCount: 1,
            ResponseSessionCount: 1,
            AnswerCount: 1,
            ScoreRunCount: 1,
            ScoreCount: 1,
            CanApprove: canDecide,
            CanDeny: canDecide,
            CanExecute: canExecute);
    }

    private static WithdrawalExecutionStateResponse SampleExecutionResponse(
        Guid withdrawalEventId,
        string status)
    {
        return new WithdrawalExecutionStateResponse(
            withdrawalEventId,
            status,
            DateTimeOffset.Parse("2026-05-18T09:04:00+00:00"),
            new WithdrawalDryRunResponse(
                withdrawalEventId,
                Guid.NewGuid(),
                WithdrawalTargetKinds.ResponseSession,
                WithdrawalScopes.CampaignSeries,
                RetentionPolicy.Anonymize,
                status,
                TargetMatched: true,
                ConsentRecordCount: 1,
                ResponseSessionCount: 1,
                AnswerCount: 1,
                ScoreRunCount: 1,
                ScoreCount: 1,
                Array.Empty<WithdrawalDryRunDependency>()));
    }

    private sealed class FakeWithdrawalRuntimeStore(
        Result<WithdrawalRequestResponse>? createResult = null,
        Result<WithdrawalRequestTokenIssueResponse>? issueTokenResult = null,
        Result<WithdrawalRequestResponse>? anonymousCreateResult = null,
        Result<IReadOnlyList<WithdrawalRequestReviewResponse>>? listResult = null,
        Result<WithdrawalRequestReviewResponse>? getResult = null,
        Result<WithdrawalRequestReviewResponse>? approveResult = null,
        Result<WithdrawalRequestReviewResponse>? denyResult = null,
        Result<WithdrawalExecutionStateResponse>? executeResult = null) : IWithdrawalRuntimeStore
    {
        public int CreateCallCount { get; private set; }

        public int IssueTokenCallCount { get; private set; }

        public int AnonymousCreateCallCount { get; private set; }

        public int ListCallCount { get; private set; }

        public int GetCallCount { get; private set; }

        public int ApproveCallCount { get; private set; }

        public int DenyCallCount { get; private set; }

        public int ExecuteCallCount { get; private set; }

        public Guid TenantId { get; private set; }

        public Guid GetRequestId { get; private set; }

        public Guid ApproveRequestId { get; private set; }

        public Guid DenyRequestId { get; private set; }

        public Guid ExecuteRequestId { get; private set; }

        public CreateWithdrawalRequestCommand? Command { get; private set; }

        public IssueWithdrawalRequestTokenCommand? IssueTokenCommand { get; private set; }

        public CreateAnonymousWithdrawalRequestCommand? AnonymousCommand { get; private set; }

        public WithdrawalRequestDecisionCommand? ApproveCommand { get; private set; }

        public WithdrawalRequestDecisionCommand? DenyCommand { get; private set; }

        public Task<Result<WithdrawalRequestResponse>> CreateWithdrawalRequestAsync(
            Guid tenantId,
            CreateWithdrawalRequestCommand command,
            CancellationToken cancellationToken)
        {
            CreateCallCount++;
            TenantId = tenantId;
            Command = command;
            return Task.FromResult(createResult ??
                Result.Success(SampleResponse(Guid.NewGuid(), command.TargetId, idempotent: false)));
        }

        public Task<Result<WithdrawalRequestTokenIssueResponse>> IssueWithdrawalRequestTokenAsync(
            Guid tenantId,
            IssueWithdrawalRequestTokenCommand command,
            CancellationToken cancellationToken)
        {
            IssueTokenCallCount++;
            TenantId = tenantId;
            IssueTokenCommand = command;
            return Task.FromResult(issueTokenResult ??
                Result.Success(new WithdrawalRequestTokenIssueResponse(
                    Guid.NewGuid(),
                    command.ResponseSessionId,
                    command.RequestedAction,
                    command.ExpiresAt,
                    $"wdr_{tenantId:N}_secret-token")));
        }

        public Task<Result<WithdrawalRequestResponse>> CreateAnonymousWithdrawalRequestAsync(
            CreateAnonymousWithdrawalRequestCommand command,
            CancellationToken cancellationToken)
        {
            AnonymousCreateCallCount++;
            AnonymousCommand = command;
            return Task.FromResult(anonymousCreateResult ??
                Result.Success(SampleResponse(Guid.NewGuid(), Guid.NewGuid(), idempotent: false)));
        }

        public Task<Result<IReadOnlyList<WithdrawalRequestReviewResponse>>> ListWithdrawalRequestsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            ListCallCount++;
            TenantId = tenantId;
            return Task.FromResult(listResult ??
                Result.Success<IReadOnlyList<WithdrawalRequestReviewResponse>>(
                    Array.Empty<WithdrawalRequestReviewResponse>()));
        }

        public Task<Result<WithdrawalRequestReviewResponse>> GetWithdrawalRequestAsync(
            Guid tenantId,
            Guid requestId,
            CancellationToken cancellationToken)
        {
            GetCallCount++;
            TenantId = tenantId;
            GetRequestId = requestId;
            return Task.FromResult(getResult ??
                Result.Failure<WithdrawalRequestReviewResponse>(
                    Error.NotFound("withdrawal_request.not_found", "Withdrawal request was not found.")));
        }

        public Task<Result<WithdrawalRequestReviewResponse>> ApproveWithdrawalRequestAsync(
            Guid tenantId,
            Guid requestId,
            WithdrawalRequestDecisionCommand command,
            CancellationToken cancellationToken)
        {
            ApproveCallCount++;
            TenantId = tenantId;
            ApproveRequestId = requestId;
            ApproveCommand = command;
            return Task.FromResult(approveResult ??
                Result.Success(SampleReviewResponse(
                    requestId,
                    Guid.NewGuid(),
                    WithdrawalEventStatuses.Planned)));
        }

        public Task<Result<WithdrawalRequestReviewResponse>> DenyWithdrawalRequestAsync(
            Guid tenantId,
            Guid requestId,
            WithdrawalRequestDecisionCommand command,
            CancellationToken cancellationToken)
        {
            DenyCallCount++;
            TenantId = tenantId;
            DenyRequestId = requestId;
            DenyCommand = command;
            return Task.FromResult(denyResult ??
                Result.Success(SampleReviewResponse(
                    requestId,
                    Guid.NewGuid(),
                    WithdrawalEventStatuses.Denied,
                    DateTimeOffset.Parse("2026-05-18T09:03:00+00:00"))));
        }

        public Task<Result<WithdrawalEventResponse>> PlanIdentifiedWithdrawalAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            Guid subjectId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalEventResponse>> PlanAnonymousLongitudinalWithdrawalAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            string rawParticipantCode,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalDryRunResponse>> DryRunWithdrawalAsync(
            Guid tenantId,
            Guid withdrawalEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalExecutionStateResponse>> ClaimWithdrawalForExecutionAsync(
            Guid tenantId,
            Guid withdrawalEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalExecutionStateResponse>> CompleteWithdrawalExecutionAsync(
            Guid tenantId,
            Guid withdrawalEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalExecutionStateResponse>> FailWithdrawalExecutionAsync(
            Guid tenantId,
            Guid withdrawalEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<WithdrawalExecutionStateResponse>> ExecuteWithdrawalAsync(
            Guid tenantId,
            Guid withdrawalEventId,
            CancellationToken cancellationToken)
        {
            ExecuteCallCount++;
            TenantId = tenantId;
            ExecuteRequestId = withdrawalEventId;
            return Task.FromResult(executeResult ??
                Result.Success(SampleExecutionResponse(
                    withdrawalEventId,
                    WithdrawalEventStatuses.Completed)));
        }
    }
}
