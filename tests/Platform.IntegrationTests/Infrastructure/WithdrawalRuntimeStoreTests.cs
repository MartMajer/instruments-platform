using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Platform.Application.Auditing;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Features.Retention;
using Platform.Application.Tenancy;
using Platform.Domain.Auditing;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Operations;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Interceptors;
using Platform.Infrastructure.Retention;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class WithdrawalRuntimeStoreTests : IAsyncLifetime
{
    private const string RuntimeUsername = "platform_app_runtime";
    private const string RuntimePassword = "platform_app_runtime";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task Plan_identified_withdrawal_records_retention_action_and_safe_counts()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.True(result.Value.TargetMatched);
        Assert.Equal(WithdrawalTargetKinds.IdentifiedSubject, result.Value.TargetKind);
        Assert.Equal(WithdrawalEventStatuses.Planned, result.Value.Status);
        Assert.Equal(RetentionPolicy.Anonymize, result.Value.ActionAfter);
        Assert.Equal(1, result.Value.ConsentRecordCount);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(1, result.Value.ScoreRunCount);
        Assert.Equal(1, result.Value.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(fixture.SubjectId, persisted.SubjectId);
        Assert.Null(persisted.ParticipantCodeId);
        Assert.DoesNotContain("Answer", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ParticipantCode", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_identified_withdrawal_fails_closed_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.PlanIdentifiedWithdrawalAsync(
            tenantA,
            tenantBFixture.CampaignSeriesId,
            tenantBFixture.SubjectId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);
        Assert.Empty(await verifyDb.WithdrawalEvents.ToListAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_anonymous_longitudinal_withdrawal_hashes_raw_code_and_persists_only_participant_code_id()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(runtimeOptions, tenantId, "alpha-001");
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.PlanAnonymousLongitudinalWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            "  alpha-001  ",
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.True(result.Value.TargetMatched);
        Assert.Equal(WithdrawalTargetKinds.AnonymousLongitudinalCode, result.Value.TargetKind);
        Assert.Equal(1, result.Value.ConsentRecordCount);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(1, result.Value.ScoreRunCount);
        Assert.Equal(1, result.Value.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Null(persisted.SubjectId);
        Assert.Equal(fixture.ParticipantCodeId, persisted.ParticipantCodeId);
        Assert.DoesNotContain("alpha-001", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_anonymous_longitudinal_withdrawal_records_neutral_unmatched_event()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(runtimeOptions, tenantId, "alpha-001");
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.PlanAnonymousLongitudinalWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            "missing-code",
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.False(result.Value.TargetMatched);
        Assert.Equal(WithdrawalTargetKinds.AnonymousLongitudinalUnmatched, result.Value.TargetKind);
        Assert.Equal(0, result.Value.ResponseSessionCount);
        Assert.Equal(0, result.Value.AnswerCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Null(persisted.SubjectId);
        Assert.Null(persisted.ParticipantCodeId);
        Assert.DoesNotContain("missing-code", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_creates_requested_response_session_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.False(result.Value.Idempotent);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, result.Value.TargetKind);
        Assert.Equal(fixture.ResponseSessionId, result.Value.TargetId);
        Assert.Equal(RetentionPolicy.Anonymize, result.Value.RequestedAction);
        Assert.Equal(WithdrawalEventStatuses.Requested, result.Value.Status);
        Assert.Equal(1, result.Value.ConsentRecordCount);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(1, result.Value.ScoreRunCount);
        Assert.Equal(1, result.Value.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(result.Value.RequestId, persisted.Id);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, persisted.TargetKind);
        Assert.Equal(WithdrawalEventStatuses.Requested, persisted.Status);
        Assert.Equal(fixture.ResponseSessionId, persisted.ResponseSessionId);
        Assert.Null(persisted.SubjectId);
        Assert.Null(persisted.ParticipantCodeId);
        Assert.Contains("tenant_admin", persisted.MetadataJson, StringComparison.Ordinal);
        Assert.Contains(actorUserId.ToString("D"), persisted.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("owner_requested", persisted.MetadataJson, StringComparison.Ordinal);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_rejects_wrong_tenant_response_session()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateWithdrawalRequestAsync(
            tenantA,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                tenantBFixture.ResponseSessionId,
                RetentionPolicy.Delete,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("response_session.not_found", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantA);
        Assert.Empty(await verifyDb.WithdrawalEvents.ToListAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_rejects_unsupported_target_kind()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantId, "tenant-a");
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                "consent_record",
                Guid.NewGuid(),
                RetentionPolicy.Anonymize,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_request.target_kind_unsupported", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Empty(await verifyDb.WithdrawalEvents.ToListAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_is_idempotent_for_duplicate_pending_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var command = new CreateWithdrawalRequestCommand(
            WithdrawalTargetKinds.ResponseSession,
            fixture.ResponseSessionId,
            RetentionPolicy.Delete,
            actorUserId,
            "owner_requested");

        var first = await store.CreateWithdrawalRequestAsync(tenantId, command, CancellationToken.None);
        var second = await store.CreateWithdrawalRequestAsync(tenantId, command, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.False(first.Value.Idempotent);
        Assert.True(second.Value.Idempotent);
        Assert.Equal(first.Value.RequestId, second.Value.RequestId);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_does_not_delete_or_anonymize_response_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var session = await verifyDb.ResponseSessions.SingleAsync(entity => entity.Id == fixture.ResponseSessionId);
        Assert.Null(session.AnonymizedAt);
        Assert.Equal(fixture.ConsentRecordId, session.ConsentRecordId);
        Assert.Equal("identified-ip-hash", session.IpHash);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_issue_stores_hash_only()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var issue = await store.IssueWithdrawalRequestTokenAsync(
            tenantId,
            new IssueWithdrawalRequestTokenCommand(
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                DateTimeOffset.UtcNow.AddHours(1),
                "receipt"),
            CancellationToken.None);

        Assert.True(issue.IsSuccess, issue.Error.ToString());
        Assert.StartsWith("wdr_", issue.Value.RawToken, StringComparison.Ordinal);
        Assert.Equal(fixture.ResponseSessionId, issue.Value.ResponseSessionId);
        Assert.Equal(RetentionPolicy.Delete, issue.Value.RequestedAction);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalRequestTokens.SingleAsync();
        Assert.Equal(fixture.ResponseSessionId, persisted.ResponseSessionId);
        Assert.Equal(WithdrawalRequestTokens.Hash(issue.Value.RawToken), persisted.TokenHash);
        Assert.DoesNotContain(issue.Value.RawToken, persisted.TokenHash, StringComparison.Ordinal);
        Assert.Null(persisted.ConsumedAt);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_consume_creates_requested_event_and_consumes_once()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var issue = await store.IssueWithdrawalRequestTokenAsync(
            tenantId,
            new IssueWithdrawalRequestTokenCommand(
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                DateTimeOffset.UtcNow.AddHours(1),
                "receipt"),
            CancellationToken.None);
        Assert.True(issue.IsSuccess, issue.Error.ToString());

        var result = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                issue.Value.RawToken,
                RetentionPolicy.Delete,
                "participant_request"),
            CancellationToken.None);
        var second = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                issue.Value.RawToken,
                RetentionPolicy.Delete,
                "participant_request"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Requested, result.Value.Status);
        Assert.Equal(fixture.ResponseSessionId, result.Value.TargetId);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.True(second.IsFailure);
        Assert.Equal("withdrawal_token.consumed", second.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var token = await verifyDb.WithdrawalRequestTokens.SingleAsync();
        Assert.NotNull(token.ConsumedAt);
        var withdrawal = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(WithdrawalEventStatuses.Requested, withdrawal.Status);
        Assert.Contains("public", withdrawal.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain(issue.Value.RawToken, withdrawal.MetadataJson, StringComparison.Ordinal);
        var notification = await verifyDb.OperationalNotifications.SingleAsync();
        Assert.Equal(OperationalNotification.WithdrawalRequestCreatedNotificationType, notification.NotificationType);
        Assert.Equal(OperationalNotification.SeverityWarning, notification.Severity);
        Assert.Equal(OperationalNotification.StatusUnread, notification.Status);
        Assert.Equal(withdrawal.Id, notification.SourceAggregateId);
        Assert.Equal(OperationalNotification.SourceAggregateTypeWithdrawalRequest, notification.SourceAggregateType);
        Assert.Equal(
            OperationalNotification.SourceEventTypeWithdrawalRequestCreated,
            notification.SourceEventType);
        using (var notificationPayload = JsonDocument.Parse(notification.PayloadJson))
        {
            Assert.Equal(withdrawal.Id, notificationPayload.RootElement.GetProperty("withdrawalRequestId").GetGuid());
            Assert.Equal(WithdrawalTargetKinds.ResponseSession, notificationPayload.RootElement.GetProperty("targetKind").GetString());
            Assert.Equal(RetentionPolicy.Delete, notificationPayload.RootElement.GetProperty("requestedAction").GetString());
            Assert.Equal(WithdrawalEventStatuses.Requested, notificationPayload.RootElement.GetProperty("status").GetString());
        }

        Assert.DoesNotContain(issue.Value.RawToken, notification.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("wdr_", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_consume_sets_application_tenant_context_for_audit()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Anonymize);
        await using (var issueDb = new ApplicationDbContext(runtimeOptions))
        {
            var issueStore = new WithdrawalRuntimeStore(
                issueDb,
                new TenantDbScope(issueDb),
                new DeterministicParticipantCodeHasher());
            var issue = await issueStore.IssueWithdrawalRequestTokenAsync(
                tenantId,
                new IssueWithdrawalRequestTokenCommand(
                    fixture.ResponseSessionId,
                    RetentionPolicy.Anonymize,
                    DateTimeOffset.UtcNow.AddHours(1),
                    "receipt"),
                CancellationToken.None);
            Assert.True(issue.IsSuccess, issue.Error.ToString());

            var currentTenant = new CurrentTenant();
            var auditContext = new CurrentAuditContext();
            var auditedRuntimeOptions = CreateRuntimeOptions(
                new AuditSaveChangesInterceptor(currentTenant, auditContext));
            await using var consumeDb = new ApplicationDbContext(auditedRuntimeOptions);
            var consumeStore = new WithdrawalRuntimeStore(
                consumeDb,
                new TenantDbScope(consumeDb),
                new DeterministicParticipantCodeHasher(),
                currentTenant);

            var result = await consumeStore.CreateAnonymousWithdrawalRequestAsync(
                new CreateAnonymousWithdrawalRequestCommand(
                    issue.Value.RawToken,
                    RetentionPolicy.Anonymize,
                    "participant_request"),
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error.ToString());
            Assert.True(currentTenant.HasTenant);
            Assert.Equal(tenantId, currentTenant.TenantId);
        }

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.True(await verifyDb.AuditEvents.CountAsync() > 0);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_rejects_malformed_token_without_mutation()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                "not-a-withdrawal-token",
                RetentionPolicy.Delete,
                "participant_request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_token.invalid", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_rejects_unknown_well_formed_token_without_mutation()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                $"wdr_{tenantId:N}_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ",
                RetentionPolicy.Delete,
                "participant_request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_token.invalid", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_rejects_expired_token_without_mutation()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        var issued = WithdrawalRequestTokens.Issue(tenantId);
        await using (var seedDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(seedDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            await seedDb.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO withdrawal_request_token
                    (id, tenant_id, response_session_id, token_hash, requested_action, expires_at, created_at)
                VALUES
                    ({Guid.NewGuid()}, {tenantId}, {fixture.ResponseSessionId}, {issued.TokenHash}, {RetentionPolicy.Delete}, {DateTimeOffset.UtcNow.AddMinutes(-1)}, {DateTimeOffset.UtcNow.AddMinutes(-2)})
                """);
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                issued.RawToken,
                RetentionPolicy.Delete,
                "participant_request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_token.expired", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Anonymous_withdrawal_token_rejects_action_mismatch_without_consuming()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var issue = await store.IssueWithdrawalRequestTokenAsync(
            tenantId,
            new IssueWithdrawalRequestTokenCommand(
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                DateTimeOffset.UtcNow.AddHours(1),
                "receipt"),
            CancellationToken.None);
        Assert.True(issue.IsSuccess, issue.Error.ToString());

        var result = await store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                issue.Value.RawToken,
                RetentionPolicy.Anonymize,
                "participant_request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_token.action_mismatch", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Null((await verifyDb.WithdrawalRequestTokens.SingleAsync()).ConsumedAt);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_requested_event_is_not_claimable()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var request = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(request.IsSuccess, request.Error.ToString());

        var claim = await store.ClaimWithdrawalForExecutionAsync(
            tenantId,
            request.Value.RequestId,
            CancellationToken.None);

        Assert.True(claim.IsFailure);
        Assert.Equal("withdrawal_event.not_claimable", claim.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync(entity => entity.Id == request.Value.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Requested, persisted.Status);
        Assert.Null(persisted.ProcessedAt);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_requested_event_is_not_executable_and_does_not_mutate_graph()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var request = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(request.IsSuccess, request.Error.ToString());

        var execution = await store.ExecuteWithdrawalAsync(
            tenantId,
            request.Value.RequestId,
            CancellationToken.None);

        Assert.True(execution.IsFailure);
        Assert.Equal("withdrawal_event.not_executable", execution.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync(entity => entity.Id == request.Value.RequestId);
        var session = await verifyDb.ResponseSessions.SingleAsync(entity => entity.Id == fixture.ResponseSessionId);
        Assert.Equal(WithdrawalEventStatuses.Requested, persisted.Status);
        Assert.Null(persisted.ProcessedAt);
        Assert.Null(session.AnonymizedAt);
        Assert.Equal(fixture.ConsentRecordId, session.ConsentRecordId);
        Assert.Equal("identified-ip-hash", session.IpHash);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_intake_metadata_excludes_sensitive_target_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var result = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var metadataJson = await verifyDb.WithdrawalEvents
            .Select(withdrawal => withdrawal.MetadataJson)
            .SingleAsync();
        foreach (var sensitive in new[]
        {
            "subject@example.com",
            "identified-ip-hash",
            "identified-user-agent-hash",
            new string('a', 64),
            """{"value":4}""",
            "participant",
            "token",
            "recipient",
            "provider",
            "public_handle",
            "publichandle",
            "salt",
            "raw",
            "free text"
        })
        {
            Assert.DoesNotContain(sensitive, metadataJson, StringComparison.OrdinalIgnoreCase);
        }

        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Withdrawal_request_review_lists_tenant_requests_with_safe_counts()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());

        var deleteRequest = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);
        var anonymizeRequest = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(deleteRequest.IsSuccess, deleteRequest.Error.ToString());
        Assert.True(anonymizeRequest.IsSuccess, anonymizeRequest.Error.ToString());

        var result = await store.ListWithdrawalRequestsAsync(tenantId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, request =>
            request.RequestId == deleteRequest.Value.RequestId &&
            request.TargetKind == WithdrawalTargetKinds.ResponseSession &&
            request.TargetId == fixture.ResponseSessionId &&
            request.RequestedAction == RetentionPolicy.Delete &&
            request.Status == WithdrawalEventStatuses.Requested &&
            request.ConsentRecordCount == 1 &&
            request.ResponseSessionCount == 1 &&
            request.AnswerCount == 1 &&
            request.ScoreRunCount == 1 &&
            request.ScoreCount == 1);
        Assert.Contains(result.Value, request =>
            request.RequestId == anonymizeRequest.Value.RequestId &&
            request.TargetKind == WithdrawalTargetKinds.ResponseSession &&
            request.TargetId == fixture.ResponseSessionId &&
            request.RequestedAction == RetentionPolicy.Anonymize &&
            request.Status == WithdrawalEventStatuses.Requested);
    }

    [DockerFact]
    public async Task Withdrawal_request_review_advertises_status_safe_admin_actions()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var requested = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(requested.IsSuccess, requested.Error.ToString());

        var requestedReview = await store.GetWithdrawalRequestAsync(
            tenantId,
            requested.Value.RequestId,
            CancellationToken.None);

        Assert.True(requestedReview.IsSuccess, requestedReview.Error.ToString());
        Assert.True(requestedReview.Value.CanApprove);
        Assert.True(requestedReview.Value.CanDeny);
        Assert.False(requestedReview.Value.CanExecute);

        var planned = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            requested.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(planned.IsSuccess, planned.Error.ToString());
        Assert.False(planned.Value.CanApprove);
        Assert.False(planned.Value.CanDeny);
        Assert.True(planned.Value.CanExecute);

        var requestedForDenial = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);
        Assert.True(requestedForDenial.IsSuccess, requestedForDenial.Error.ToString());

        var denied = await store.DenyWithdrawalRequestAsync(
            tenantId,
            requestedForDenial.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_denied"),
            CancellationToken.None);

        Assert.True(denied.IsSuccess, denied.Error.ToString());
        Assert.False(denied.Value.CanApprove);
        Assert.False(denied.Value.CanDeny);
        Assert.False(denied.Value.CanExecute);
    }

    [DockerFact]
    public async Task Withdrawal_request_review_gets_tenant_request_detail()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var result = await store.GetWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(created.Value.RequestId, result.Value.RequestId);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, result.Value.TargetKind);
        Assert.Equal(fixture.ResponseSessionId, result.Value.TargetId);
        Assert.Equal(RetentionPolicy.Delete, result.Value.RequestedAction);
        Assert.Equal(WithdrawalEventStatuses.Requested, result.Value.Status);
        Assert.Null(result.Value.ProcessedAt);
        Assert.Equal(1, result.Value.ConsentRecordCount);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(1, result.Value.ScoreRunCount);
        Assert.Equal(1, result.Value.ScoreCount);
    }

    [DockerFact]
    public async Task Withdrawal_request_review_get_fails_closed_for_wrong_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantB,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                tenantBFixture.ResponseSessionId,
                RetentionPolicy.Delete,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var result = await store.GetWithdrawalRequestAsync(
            tenantA,
            created.Value.RequestId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_request.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Withdrawal_request_decision_approves_requested_request_to_planned()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());
        Assert.Equal(created.Value.RequestId, approved.Value.RequestId);
        Assert.Equal(fixture.ResponseSessionId, approved.Value.TargetId);
        Assert.Equal(RetentionPolicy.Delete, approved.Value.RequestedAction);
        Assert.Equal(WithdrawalEventStatuses.Planned, approved.Value.Status);
        Assert.Null(approved.Value.ProcessedAt);
        Assert.Equal(1, approved.Value.ConsentRecordCount);
        Assert.Equal(1, approved.Value.ResponseSessionCount);
        Assert.Equal(1, approved.Value.AnswerCount);
        Assert.Equal(1, approved.Value.ScoreRunCount);
        Assert.Equal(1, approved.Value.ScoreCount);
    }

    [DockerFact]
    public async Task Withdrawal_request_decision_denied_records_terminal_operational_notification()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var denied = await store.DenyWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_denied"),
            CancellationToken.None);

        Assert.True(denied.IsSuccess, denied.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var terminal = Assert.Single(await verifyDb.OperationalNotifications
            .Where(notification =>
                notification.SourceAggregateId == created.Value.RequestId &&
                notification.SourceEventType == OperationalNotification.SourceEventTypeWithdrawalRequestTerminal)
            .ToListAsync());
        await transaction.CommitAsync();

        Assert.Equal(OperationalNotification.WithdrawalRequestTerminalNotificationType, terminal.NotificationType);
        Assert.Equal(OperationalNotification.SeverityInfo, terminal.Severity);
        Assert.Equal(OperationalNotification.StatusUnread, terminal.Status);
        Assert.Equal(OperationalNotification.SourceAggregateTypeWithdrawalRequest, terminal.SourceAggregateType);
        using var payload = JsonDocument.Parse(terminal.PayloadJson);
        Assert.Equal(1, payload.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(created.Value.RequestId, payload.RootElement.GetProperty("withdrawalRequestId").GetGuid());
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, payload.RootElement.GetProperty("targetKind").GetString());
        Assert.Equal(RetentionPolicy.Delete, payload.RootElement.GetProperty("requestedAction").GetString());
        Assert.Equal(WithdrawalEventStatuses.Denied, payload.RootElement.GetProperty("status").GetString());
        foreach (var sensitive in new[]
        {
            "rawToken",
            "rawAnswer",
            "token",
            "answer",
            "participant",
            "salt",
            "recipient",
            "provider",
            "subject",
            "storage"
        })
        {
            Assert.DoesNotContain(sensitive, terminal.PayloadJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [DockerFact]
    public async Task Withdrawal_request_decision_denies_requested_request_to_terminal_denied()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var denied = await store.DenyWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_denied"),
            CancellationToken.None);

        Assert.True(denied.IsSuccess, denied.Error.ToString());
        Assert.Equal(created.Value.RequestId, denied.Value.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Denied, denied.Value.Status);
        Assert.NotNull(denied.Value.ProcessedAt);
        Assert.True(denied.Value.ProcessedAt >= denied.Value.RequestedAt);

        var repeated = await store.DenyWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_denied"),
            CancellationToken.None);

        Assert.True(repeated.IsFailure);
        Assert.Equal("withdrawal_request.not_requested", repeated.Error.Code);
    }

    [DockerFact]
    public async Task Withdrawal_request_decision_fails_closed_for_wrong_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantB,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                tenantBFixture.ResponseSessionId,
                RetentionPolicy.Delete,
                Guid.NewGuid(),
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var result = await store.ApproveWithdrawalRequestAsync(
            tenantA,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(Guid.NewGuid(), "owner_confirmed"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("withdrawal_request.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_dry_run_returns_request_graph()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        var dryRun = await store.DryRunWithdrawalAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(dryRun.IsSuccess, dryRun.Error.ToString());
        Assert.Equal(approved.Value.RequestId, dryRun.Value.WithdrawalEventId);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, dryRun.Value.TargetKind);
        Assert.Equal(WithdrawalEventStatuses.Planned, dryRun.Value.Status);
        Assert.True(dryRun.Value.TargetMatched);
        Assert.Equal(1, dryRun.Value.ConsentRecordCount);
        Assert.Equal(1, dryRun.Value.ResponseSessionCount);
        Assert.Equal(1, dryRun.Value.AnswerCount);
        Assert.Equal(1, dryRun.Value.ScoreRunCount);
        Assert.Equal(1, dryRun.Value.ScoreCount);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ConsentRecord, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ResponseSession, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.Answer, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ScoreRun, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.Score, 1);
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_claims_approved_request()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        var claimed = await store.ClaimWithdrawalForExecutionAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(claimed.IsSuccess, claimed.Error.ToString());
        Assert.Equal(approved.Value.RequestId, claimed.Value.WithdrawalEventId);
        Assert.Equal(WithdrawalEventStatuses.Processing, claimed.Value.Status);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, claimed.Value.DryRun.TargetKind);
        Assert.True(claimed.Value.DryRun.TargetMatched);
        Assert.Equal(1, claimed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, claimed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, claimed.Value.DryRun.AnswerCount);
        Assert.Equal(1, claimed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, claimed.Value.DryRun.ScoreCount);
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_delete_removes_request_graph_and_invalidates_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, executed.Value.DryRun.TargetKind);
        Assert.Equal(1, executed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, executed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, executed.Value.DryRun.AnswerCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync(entity => entity.Id == approved.Value.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Completed, persisted.Status);
        AssertWithdrawalMetadataRecordsArtifactInvalidation(persisted.MetadataJson, "deleted_graph", 2);
        Assert.Equal(0, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(0, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        Assert.Equal(0, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(0, await verifyDb.Scores.CountAsync());
        var artifacts = await verifyDb.ExportArtifacts.OrderBy(artifact => artifact.CreatedAt).ToListAsync();
        Assert.Equal(2, artifacts.Count);
        Assert.All(artifacts, AssertInvalidatedArtifact);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_completed_records_terminal_operational_notification()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var terminal = Assert.Single(await verifyDb.OperationalNotifications
            .Where(notification =>
                notification.SourceAggregateId == approved.Value.RequestId &&
                notification.SourceEventType == OperationalNotification.SourceEventTypeWithdrawalRequestTerminal)
            .ToListAsync());
        await transaction.CommitAsync();

        Assert.Equal(OperationalNotification.WithdrawalRequestTerminalNotificationType, terminal.NotificationType);
        Assert.Equal(OperationalNotification.SeverityInfo, terminal.Severity);
        Assert.Equal(OperationalNotification.StatusUnread, terminal.Status);
        Assert.Equal(OperationalNotification.SourceAggregateTypeWithdrawalRequest, terminal.SourceAggregateType);
        using var payload = JsonDocument.Parse(terminal.PayloadJson);
        Assert.Equal(1, payload.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(approved.Value.RequestId, payload.RootElement.GetProperty("withdrawalRequestId").GetGuid());
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, payload.RootElement.GetProperty("targetKind").GetString());
        Assert.Equal(RetentionPolicy.Delete, payload.RootElement.GetProperty("requestedAction").GetString());
        Assert.Equal(WithdrawalEventStatuses.Completed, payload.RootElement.GetProperty("status").GetString());
        foreach (var sensitive in new[]
        {
            "rawToken",
            "rawAnswer",
            "token",
            "answer",
            "participant",
            "salt",
            "recipient",
            "provider",
            "subject",
            "storage"
        })
        {
            Assert.DoesNotContain(sensitive, terminal.PayloadJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_anonymize_scrubs_request_graph_and_invalidates_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Anonymize);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Anonymize,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);
        Assert.Equal(WithdrawalTargetKinds.ResponseSession, executed.Value.DryRun.TargetKind);
        Assert.Equal(1, executed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, executed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, executed.Value.DryRun.AnswerCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync(entity => entity.Id == approved.Value.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Completed, persisted.Status);
        Assert.Equal(fixture.ResponseSessionId, persisted.ResponseSessionId);
        AssertWithdrawalMetadataRecordsArtifactInvalidation(persisted.MetadataJson, "anonymized_graph", 2);

        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.Null(responseSession.ConsentRecordId);
        Assert.Null(responseSession.PublicHandleHash);
        Assert.Null(responseSession.PublicHandleIssuedAt);
        Assert.Null(responseSession.IpHash);
        Assert.Null(responseSession.UserAgentHash);
        Assert.NotNull(responseSession.AnonymizedAt);

        var consentRecord = await verifyDb.ConsentRecords.SingleAsync();
        Assert.Null(consentRecord.SubjectId);
        Assert.NotNull(consentRecord.AnonymizedAt);

        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        var artifacts = await verifyDb.ExportArtifacts.OrderBy(artifact => artifact.CreatedAt).ToListAsync();
        Assert.Equal(2, artifacts.Count);
        Assert.All(artifacts, AssertInvalidatedArtifact);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Response_session_withdrawal_execution_claim_fails_when_request_graph_changed()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var created = await store.CreateWithdrawalRequestAsync(
            tenantId,
            new CreateWithdrawalRequestCommand(
                WithdrawalTargetKinds.ResponseSession,
                fixture.ResponseSessionId,
                RetentionPolicy.Delete,
                actorUserId,
                "owner_requested"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error.ToString());

        var approved = await store.ApproveWithdrawalRequestAsync(
            tenantId,
            created.Value.RequestId,
            new WithdrawalRequestDecisionCommand(actorUserId, "owner_confirmed"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error.ToString());

        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var mutateScope = new TenantDbScope(mutateDb);
            await using var mutateTransaction = await mutateScope.BeginTransactionAsync(tenantId);
            var answer = await mutateDb.Answers.SingleAsync(entity => entity.Id == fixture.AnswerId);
            mutateDb.Answers.Remove(answer);
            await mutateDb.SaveChangesAsync();
            await mutateTransaction.CommitAsync();
        }

        var claimed = await store.ClaimWithdrawalForExecutionAsync(
            tenantId,
            approved.Value.RequestId,
            CancellationToken.None);

        Assert.True(claimed.IsFailure);
        Assert.Equal("withdrawal_event.graph_changed", claimed.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync(entity => entity.Id == approved.Value.RequestId);
        Assert.Equal(WithdrawalEventStatuses.Failed, persisted.Status);
        Assert.Contains("graph_changed", persisted.MetadataJson, StringComparison.Ordinal);
        Assert.Equal(fixture.ResponseSessionId, persisted.ResponseSessionId);
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Dry_run_identified_withdrawal_returns_safe_dependency_graph_without_mutating()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var dryRun = await store.DryRunWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(dryRun.IsSuccess, dryRun.Error.ToString());
        Assert.True(dryRun.Value.TargetMatched);
        Assert.Equal(planned.Value.Id, dryRun.Value.WithdrawalEventId);
        Assert.Equal(WithdrawalTargetKinds.IdentifiedSubject, dryRun.Value.TargetKind);
        Assert.Equal(1, dryRun.Value.ConsentRecordCount);
        Assert.Equal(1, dryRun.Value.ResponseSessionCount);
        Assert.Equal(1, dryRun.Value.AnswerCount);
        Assert.Equal(1, dryRun.Value.ScoreRunCount);
        Assert.Equal(1, dryRun.Value.ScoreCount);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ConsentRecord, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ResponseSession, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.Answer, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.ScoreRun, 1);
        AssertDependency(dryRun.Value, WithdrawalDryRunDependencyEntities.Score, 1);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Dry_run_identified_withdrawal_is_idempotent_for_unchanged_event()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var first = await store.DryRunWithdrawalAsync(tenantId, planned.Value.Id, CancellationToken.None);
        var second = await store.DryRunWithdrawalAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal(first.Value.ConsentRecordCount, second.Value.ConsentRecordCount);
        Assert.Equal(first.Value.ResponseSessionCount, second.Value.ResponseSessionCount);
        Assert.Equal(first.Value.AnswerCount, second.Value.AnswerCount);
        Assert.Equal(first.Value.ScoreRunCount, second.Value.ScoreRunCount);
        Assert.Equal(first.Value.ScoreCount, second.Value.ScoreCount);
        Assert.Equal(
            first.Value.Dependencies.Select(dependency => (dependency.Entity, dependency.Count)),
            second.Value.Dependencies.Select(dependency => (dependency.Entity, dependency.Count)));
    }

    [DockerFact]
    public async Task Dry_run_withdrawal_fails_closed_for_cross_tenant_event()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantB,
            tenantBFixture.CampaignSeriesId,
            tenantBFixture.SubjectId,
            CancellationToken.None);

        var dryRun = await store.DryRunWithdrawalAsync(
            tenantA,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(dryRun.IsFailure);
        Assert.Equal("withdrawal_event.not_found", dryRun.Error.Code);
    }

    [DockerFact]
    public async Task Dry_run_anonymous_longitudinal_unmatched_withdrawal_returns_neutral_zero_graph()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(runtimeOptions, tenantId, "alpha-001");
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanAnonymousLongitudinalWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            "missing-code",
            CancellationToken.None);

        var dryRun = await store.DryRunWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(dryRun.IsSuccess, dryRun.Error.ToString());
        Assert.False(dryRun.Value.TargetMatched);
        Assert.Equal(WithdrawalTargetKinds.AnonymousLongitudinalUnmatched, dryRun.Value.TargetKind);
        Assert.Equal(0, dryRun.Value.ConsentRecordCount);
        Assert.Equal(0, dryRun.Value.ResponseSessionCount);
        Assert.Equal(0, dryRun.Value.AnswerCount);
        Assert.Equal(0, dryRun.Value.ScoreRunCount);
        Assert.Equal(0, dryRun.Value.ScoreCount);
        Assert.All(dryRun.Value.Dependencies, dependency => Assert.Equal(0, dependency.Count));
    }

    [DockerFact]
    public async Task Dry_run_anonymous_longitudinal_withdrawal_fails_when_participant_code_no_longer_matches_event_series()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(runtimeOptions, tenantId, "alpha-001");
        await using (var planDb = new ApplicationDbContext(runtimeOptions))
        {
            var planStore = new WithdrawalRuntimeStore(
                planDb,
                new TenantDbScope(planDb),
                new DeterministicParticipantCodeHasher());
            var planned = await planStore.PlanAnonymousLongitudinalWithdrawalAsync(
                tenantId,
                fixture.CampaignSeriesId,
                "alpha-001",
                CancellationToken.None);
            Assert.True(planned.IsSuccess, planned.Error.ToString());
        }

        var otherSeriesId = Guid.NewGuid();
        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(mutateDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            mutateDb.CampaignSeries.Add(new CampaignSeries(
                otherSeriesId,
                tenantId,
                "Other withdrawal pulse",
                fixture.CodeSalt.Reverse().ToArray()));
            await mutateDb.SaveChangesAsync();
            await mutateDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE participant_code SET campaign_series_id = {otherSeriesId} WHERE id = {fixture.ParticipantCodeId}");
            await transaction.CommitAsync();
        }

        await using var dryRunDb = new ApplicationDbContext(runtimeOptions);
        var dryRunStore = new WithdrawalRuntimeStore(
            dryRunDb,
            new TenantDbScope(dryRunDb),
            new DeterministicParticipantCodeHasher());
        var eventId = await LoadOnlyWithdrawalEventIdAsync(runtimeOptions, tenantId);

        var dryRun = await dryRunStore.DryRunWithdrawalAsync(
            tenantId,
            eventId,
            CancellationToken.None);

        Assert.True(dryRun.IsFailure);
        Assert.Equal("withdrawal_event.target_mismatch", dryRun.Error.Code);
    }

    [DockerFact]
    public async Task Claim_withdrawal_for_execution_marks_event_processing_without_mutating_respondent_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var claim = await store.ClaimWithdrawalForExecutionAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(claim.IsSuccess, claim.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Processing, claim.Value.Status);
        Assert.Null(claim.Value.ProcessedAt);
        Assert.Equal(1, claim.Value.DryRun.AnswerCount);
        Assert.Equal(1, claim.Value.DryRun.ScoreRunCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(WithdrawalEventStatuses.Processing, persisted.Status);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Complete_withdrawal_execution_marks_processing_event_completed_without_mutating_respondent_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);
        var claim = await store.ClaimWithdrawalForExecutionAsync(tenantId, planned.Value.Id, CancellationToken.None);
        Assert.True(claim.IsSuccess, claim.Error.ToString());

        var completed = await store.CompleteWithdrawalExecutionAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(completed.IsSuccess, completed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, completed.Value.Status);
        Assert.NotNull(completed.Value.ProcessedAt);
        Assert.Equal(1, completed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(WithdrawalEventStatuses.Completed, (await verifyDb.WithdrawalEvents.SingleAsync()).Status);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Claim_withdrawal_for_execution_fails_closed_for_cross_tenant_event()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantB,
            tenantBFixture.CampaignSeriesId,
            tenantBFixture.SubjectId,
            CancellationToken.None);

        var claim = await store.ClaimWithdrawalForExecutionAsync(
            tenantA,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(claim.IsFailure);
        Assert.Equal("withdrawal_event.not_found", claim.Error.Code);
    }

    [DockerFact]
    public async Task Claim_withdrawal_for_execution_is_not_reentrant()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var first = await store.ClaimWithdrawalForExecutionAsync(tenantId, planned.Value.Id, CancellationToken.None);
        var second = await store.ClaimWithdrawalForExecutionAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsFailure);
        Assert.Equal("withdrawal_event.not_claimable", second.Error.Code);
    }

    [DockerFact]
    public async Task Claim_withdrawal_for_execution_marks_event_failed_when_dry_run_counts_changed()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using (var planDb = new ApplicationDbContext(runtimeOptions))
        {
            var store = new WithdrawalRuntimeStore(planDb, new TenantDbScope(planDb), new DeterministicParticipantCodeHasher());
            var planned = await store.PlanIdentifiedWithdrawalAsync(
                tenantId,
                fixture.CampaignSeriesId,
                fixture.SubjectId,
                CancellationToken.None);
            Assert.True(planned.IsSuccess, planned.Error.ToString());
        }

        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(mutateDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            mutateDb.Scores.Add(new Score(
                Guid.NewGuid(),
                tenantId,
                fixture.ScoreRunId,
                fixture.CampaignId,
                fixture.ResponseSessionId,
                "extra",
                2m,
                1));
            await mutateDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var claimDb = new ApplicationDbContext(runtimeOptions);
        var claimStore = new WithdrawalRuntimeStore(
            claimDb,
            new TenantDbScope(claimDb),
            new DeterministicParticipantCodeHasher());
        var eventId = await LoadOnlyWithdrawalEventIdAsync(runtimeOptions, tenantId);

        var claim = await claimStore.ClaimWithdrawalForExecutionAsync(
            tenantId,
            eventId,
            CancellationToken.None);

        Assert.True(claim.IsFailure);
        Assert.Equal("withdrawal_event.graph_changed", claim.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(WithdrawalEventStatuses.Failed, (await verifyDb.WithdrawalEvents.SingleAsync()).Status);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_delete_withdrawal_removes_identified_graph_and_completes_event()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);
        Assert.NotNull(executed.Value.ProcessedAt);
        Assert.Equal(1, executed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, executed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, executed.Value.DryRun.AnswerCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(WithdrawalEventStatuses.Completed, persisted.Status);
        Assert.Contains("deleted_graph", persisted.MetadataJson, StringComparison.Ordinal);
        Assert.Empty(await verifyDb.ConsentRecords.ToListAsync());
        Assert.Empty(await verifyDb.ResponseSessions.ToListAsync());
        Assert.Empty(await verifyDb.Answers.ToListAsync());
        Assert.Empty(await verifyDb.ScoreRuns.ToListAsync());
        Assert.Empty(await verifyDb.Scores.ToListAsync());
        Assert.Single(await verifyDb.Subjects.ToListAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_delete_withdrawal_removes_anonymous_longitudinal_graph_and_keeps_code_record()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(
            runtimeOptions,
            tenantId,
            "alpha-001",
            RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanAnonymousLongitudinalWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            "alpha-001",
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);
        Assert.Equal(1, executed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, executed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, executed.Value.DryRun.AnswerCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(WithdrawalEventStatuses.Completed, (await verifyDb.WithdrawalEvents.SingleAsync()).Status);
        Assert.Empty(await verifyDb.ConsentRecords.ToListAsync());
        Assert.Empty(await verifyDb.ResponseSessions.ToListAsync());
        Assert.Empty(await verifyDb.Answers.ToListAsync());
        Assert.Empty(await verifyDb.ScoreRuns.ToListAsync());
        Assert.Empty(await verifyDb.Scores.ToListAsync());
        Assert.Equal(fixture.ParticipantCodeId, (await verifyDb.ParticipantCodes.SingleAsync()).Id);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_withdrawal_anonymizes_identified_graph_without_deleting_response_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);
        Assert.Equal(1, executed.Value.DryRun.ConsentRecordCount);
        Assert.Equal(1, executed.Value.DryRun.ResponseSessionCount);
        Assert.Equal(1, executed.Value.DryRun.AnswerCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreRunCount);
        Assert.Equal(1, executed.Value.DryRun.ScoreCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(WithdrawalEventStatuses.Completed, persisted.Status);
        Assert.Contains("anonymized_graph", persisted.MetadataJson, StringComparison.Ordinal);

        var assignment = await verifyDb.Assignments.SingleAsync();
        Assert.Null(assignment.TargetSubjectId);
        Assert.Null(assignment.RespondentSubjectId);
        Assert.Null(assignment.InviteTokenId);
        Assert.NotNull(assignment.AnonymizedAt);

        var consentRecord = await verifyDb.ConsentRecords.SingleAsync();
        Assert.Null(consentRecord.SubjectId);
        Assert.NotNull(consentRecord.AnonymizedAt);

        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.Null(responseSession.ParticipantCodeId);
        Assert.Null(responseSession.ConsentRecordId);
        Assert.Null(responseSession.PublicHandleHash);
        Assert.Null(responseSession.PublicHandleIssuedAt);
        Assert.Null(responseSession.IpHash);
        Assert.Null(responseSession.UserAgentHash);
        Assert.NotNull(responseSession.AnonymizedAt);

        Assert.Equal(fixture.SubjectId, (await verifyDb.Subjects.SingleAsync()).Id);
        Assert.Equal(1, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_withdrawal_anonymizes_anonymous_longitudinal_graph_without_deleting_response_data()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedAnonymousLongitudinalResponseAsync(runtimeOptions, tenantId, "alpha-001");
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanAnonymousLongitudinalWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            "alpha-001",
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.Null(responseSession.ParticipantCodeId);
        Assert.Null(responseSession.ConsentRecordId);
        Assert.Null(responseSession.PublicHandleHash);
        Assert.Null(responseSession.PublicHandleIssuedAt);
        Assert.Null(responseSession.IpHash);
        Assert.Null(responseSession.UserAgentHash);
        Assert.NotNull(responseSession.AnonymizedAt);

        var assignment = await verifyDb.Assignments.SingleAsync();
        Assert.Null(assignment.TargetSubjectId);
        Assert.Null(assignment.RespondentSubjectId);
        Assert.Null(assignment.InviteTokenId);
        Assert.NotNull(assignment.AnonymizedAt);

        var consentRecord = await verifyDb.ConsentRecords.SingleAsync();
        Assert.Null(consentRecord.SubjectId);
        Assert.NotNull(consentRecord.AnonymizedAt);

        Assert.Equal(fixture.ParticipantCodeId, (await verifyDb.ParticipantCodes.SingleAsync()).Id);
        Assert.Equal(1, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_withdrawal_anonymize_scrubs_linked_notification_delivery_identity()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        var invitationTokenId = Guid.NewGuid();

        await using (var notificationDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(notificationDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            var notification = Notification.QueueEmailInvitation(
                notificationId,
                tenantId,
                fixture.CampaignId,
                fixture.AssignmentId,
                "subject@example.com");
            notification.MarkSent(DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"));
            notificationDb.Notifications.Add(notification);
            notificationDb.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
                deliveryAttemptId,
                tenantId,
                notificationId,
                "smtp",
                "subject@example.com",
                "provider-message-123",
                DateTimeOffset.Parse("2026-05-17T14:00:05+00:00")));
            notificationDb.InvitationTokens.Add(new InvitationToken(
                invitationTokenId,
                tenantId,
                fixture.CampaignId,
                "linked-delivery-token-hash",
                InvitationTokenChannels.Email,
                recipient: "subject@example.com",
                assignmentId: fixture.AssignmentId));
            await notificationDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(WithdrawalEventStatuses.Completed, executed.Value.Status);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.WithdrawalEvents.SingleAsync();
        Assert.Equal(WithdrawalEventStatuses.Completed, persisted.Status);
        Assert.DoesNotContain("subject@example.com", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider-message-123", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("linked-delivery-token-hash", persisted.MetadataJson, StringComparison.OrdinalIgnoreCase);
        using var metadata = JsonDocument.Parse(persisted.MetadataJson);
        Assert.Equal("anonymized_graph", metadata.RootElement.GetProperty("result").GetString());
        Assert.Equal(0, metadata.RootElement.GetProperty("artifact_invalidated_count").GetInt32());
        Assert.Equal(1, metadata.RootElement.GetProperty("notice_scrubbed_count").GetInt32());
        Assert.Equal(1, metadata.RootElement.GetProperty("delivery_attempt_scrubbed_count").GetInt32());
        Assert.Equal(1, metadata.RootElement.GetProperty("invite_credential_scrubbed_count").GetInt32());

        var assignment = await verifyDb.Assignments.SingleAsync();
        Assert.Null(assignment.RespondentSubjectId);
        Assert.Null(assignment.TargetSubjectId);
        Assert.NotNull(assignment.AnonymizedAt);

        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.Null(responseSession.ConsentRecordId);
        Assert.Null(responseSession.PublicHandleHash);
        Assert.Null(responseSession.IpHash);
        Assert.Null(responseSession.UserAgentHash);
        Assert.NotNull(responseSession.AnonymizedAt);

        var scrubbedNotification = await verifyDb.Notifications.SingleAsync(entity => entity.Id == notificationId);
        Assert.Equal("withdrawn@example.invalid", scrubbedNotification.Recipient);
        Assert.Equal(NotificationStatuses.Sent, scrubbedNotification.Status);
        Assert.Null(scrubbedNotification.Error);

        var deliveryAttempt = await verifyDb.NotificationDeliveryAttempts.SingleAsync(attempt => attempt.Id == deliveryAttemptId);
        Assert.Equal("withdrawn@example.invalid", deliveryAttempt.Recipient);
        Assert.Null(deliveryAttempt.ProviderMessageId);
        Assert.Equal(NotificationStatuses.Sent, deliveryAttempt.Status);
        Assert.Null(deliveryAttempt.Error);

        var invitationToken = await verifyDb.InvitationTokens.SingleAsync(token => token.Id == invitationTokenId);
        Assert.Null(invitationToken.AssignmentId);
        Assert.Null(invitationToken.Recipient);
        Assert.Equal($"withdrawn:{invitationTokenId:N}", invitationToken.TokenHash);
        Assert.NotNull(invitationToken.ExpiresAt);
        Assert.NotNull(invitationToken.UsedAt);

        Assert.Equal(1, await verifyDb.Notifications.CountAsync());
        Assert.Equal(1, await verifyDb.NotificationDeliveryAttempts.CountAsync());
        Assert.Equal(1, await verifyDb.InvitationTokens.CountAsync());
        Assert.Equal(1, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_identified_withdrawal_scrubs_identified_queue_token_for_subject()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        var queueTokenId = Guid.NewGuid();

        await using (var tokenDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(tokenDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            tokenDb.InvitationTokens.Add(new InvitationToken(
                queueTokenId,
                tenantId,
                fixture.CampaignId,
                "identified-queue-token-hash",
                InvitationTokenChannels.IdentifiedQueue,
                respondentSubjectId: fixture.SubjectId));
            await tokenDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(1, executed.Value.InviteCredentialScrubbedCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        var token = await verifyDb.InvitationTokens.SingleAsync(entity => entity.Id == queueTokenId);
        Assert.Equal(InvitationTokenChannels.IdentifiedQueue, token.Channel);
        Assert.Null(token.RespondentSubjectId);
        Assert.Null(token.AssignmentId);
        Assert.Null(token.Recipient);
        Assert.Equal($"withdrawn:{queueTokenId:N}", token.TokenHash);
        Assert.NotNull(token.ExpiresAt);
        Assert.NotNull(token.UsedAt);
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_delete_withdrawal_redacts_sensitive_audit_payloads()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var currentAuditContext = new CurrentAuditContext();
        currentAuditContext.SetCorrelationId(Guid.NewGuid());
        var auditedRuntimeOptions = CreateRuntimeOptions(
            new AuditSaveChangesInterceptor(currentTenant, currentAuditContext));
        await using var db = new ApplicationDbContext(auditedRuntimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var auditRows = await verifyDb.AuditEvents
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ToListAsync();
        var payloadText = AuditPayloadText(auditRows);
        Assert.DoesNotContain("""{"value":4}""", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-user-agent-hash", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(new string('a', 64), payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), payloadText, StringComparison.OrdinalIgnoreCase);

        var withdrawalPlannedAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(WithdrawalEvent) &&
                auditEvent.ChangeKind == AuditChangeKinds.Added);
        AssertAuditPropertyRedacted(withdrawalPlannedAudit, before: false, nameof(WithdrawalEvent.SubjectId));

        var answerAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(Answer) &&
                auditEvent.ChangeKind == AuditChangeKinds.Deleted);
        AssertAuditPropertyRedacted(answerAudit, before: true, nameof(Answer.Value));

        var responseSessionAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(ResponseSession) &&
                auditEvent.ChangeKind == AuditChangeKinds.Deleted);
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.ConsentRecordId));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.PublicHandleHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.IpHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.UserAgentHash));

        var consentRecordAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(ConsentRecord) &&
                auditEvent.ChangeKind == AuditChangeKinds.Deleted);
        AssertAuditPropertyRedacted(consentRecordAudit, before: true, nameof(ConsentRecord.SubjectId));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_anonymize_withdrawal_redacts_sensitive_audit_payloads()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        var invitationTokenId = Guid.NewGuid();

        await using (var notificationDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(notificationDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            var notification = Notification.QueueEmailInvitation(
                notificationId,
                tenantId,
                fixture.CampaignId,
                fixture.AssignmentId,
                "subject@example.com");
            notification.MarkSent(DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"));
            notificationDb.Notifications.Add(notification);
            notificationDb.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
                deliveryAttemptId,
                tenantId,
                notificationId,
                "smtp",
                "subject@example.com",
                "provider-message-123",
                DateTimeOffset.Parse("2026-05-17T14:00:05+00:00")));
            notificationDb.InvitationTokens.Add(new InvitationToken(
                invitationTokenId,
                tenantId,
                fixture.CampaignId,
                "linked-delivery-token-hash",
                InvitationTokenChannels.Email,
                recipient: "subject@example.com",
                assignmentId: fixture.AssignmentId));
            await notificationDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var currentAuditContext = new CurrentAuditContext();
        currentAuditContext.SetCorrelationId(Guid.NewGuid());
        var auditedRuntimeOptions = CreateRuntimeOptions(
            new AuditSaveChangesInterceptor(currentTenant, currentAuditContext));
        await using var db = new ApplicationDbContext(auditedRuntimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        var auditRows = await verifyDb.AuditEvents
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ToListAsync();
        var payloadText = AuditPayloadText(auditRows);
        Assert.DoesNotContain("subject@example.com", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider-message-123", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("linked-delivery-token-hash", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-user-agent-hash", payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(new string('a', 64), payloadText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), payloadText, StringComparison.OrdinalIgnoreCase);

        var withdrawalPlannedAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(WithdrawalEvent) &&
                auditEvent.ChangeKind == AuditChangeKinds.Added);
        AssertAuditPropertyRedacted(withdrawalPlannedAudit, before: false, nameof(WithdrawalEvent.SubjectId));

        var assignmentAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(Assignment) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(assignmentAudit, before: true, nameof(Assignment.RespondentSubjectId));
        AssertAuditPropertyRedacted(assignmentAudit, before: false, nameof(Assignment.RespondentSubjectId));

        var responseSessionAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(ResponseSession) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.ConsentRecordId));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.PublicHandleHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.IpHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: true, nameof(ResponseSession.UserAgentHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: false, nameof(ResponseSession.ConsentRecordId));
        AssertAuditPropertyRedacted(responseSessionAudit, before: false, nameof(ResponseSession.PublicHandleHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: false, nameof(ResponseSession.IpHash));
        AssertAuditPropertyRedacted(responseSessionAudit, before: false, nameof(ResponseSession.UserAgentHash));

        var consentRecordAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(ConsentRecord) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(consentRecordAudit, before: true, nameof(ConsentRecord.SubjectId));
        AssertAuditPropertyRedacted(consentRecordAudit, before: false, nameof(ConsentRecord.SubjectId));

        var notificationAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(Notification) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(notificationAudit, before: true, nameof(Notification.Recipient));
        AssertAuditPropertyRedacted(notificationAudit, before: false, nameof(Notification.Recipient));

        var deliveryAttemptAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(NotificationDeliveryAttempt) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(deliveryAttemptAudit, before: true, nameof(NotificationDeliveryAttempt.Recipient));
        AssertAuditPropertyRedacted(deliveryAttemptAudit, before: true, nameof(NotificationDeliveryAttempt.ProviderMessageId));
        AssertAuditPropertyRedacted(deliveryAttemptAudit, before: false, nameof(NotificationDeliveryAttempt.Recipient));
        AssertAuditPropertyRedacted(deliveryAttemptAudit, before: false, nameof(NotificationDeliveryAttempt.ProviderMessageId));

        var invitationTokenAudit = Assert.Single(
            auditRows,
            auditEvent => auditEvent.EntityType == nameof(InvitationToken) &&
                auditEvent.ChangeKind == AuditChangeKinds.Modified);
        AssertAuditPropertyRedacted(invitationTokenAudit, before: true, nameof(InvitationToken.AssignmentId));
        AssertAuditPropertyRedacted(invitationTokenAudit, before: true, nameof(InvitationToken.Recipient));
        AssertAuditPropertyRedacted(invitationTokenAudit, before: true, nameof(InvitationToken.TokenHash));
        AssertAuditPropertyRedacted(invitationTokenAudit, before: false, nameof(InvitationToken.AssignmentId));
        AssertAuditPropertyRedacted(invitationTokenAudit, before: false, nameof(InvitationToken.Recipient));
        AssertAuditPropertyRedacted(invitationTokenAudit, before: false, nameof(InvitationToken.TokenHash));
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_withdrawal_fails_when_graph_changed_without_mutating_graph()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using (var planDb = new ApplicationDbContext(runtimeOptions))
        {
            var store = new WithdrawalRuntimeStore(planDb, new TenantDbScope(planDb), new DeterministicParticipantCodeHasher());
            var planned = await store.PlanIdentifiedWithdrawalAsync(
                tenantId,
                fixture.CampaignSeriesId,
                fixture.SubjectId,
                CancellationToken.None);
            Assert.True(planned.IsSuccess, planned.Error.ToString());
        }

        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(mutateDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            mutateDb.ScoreRuns.Add(new ScoreRun(
                Guid.NewGuid(),
                tenantId,
                fixture.CampaignId,
                fixture.ResponseSessionId,
                fixture.ScoringRuleId));
            await mutateDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var executeDb = new ApplicationDbContext(runtimeOptions);
        var executeStore = new WithdrawalRuntimeStore(
            executeDb,
            new TenantDbScope(executeDb),
            new DeterministicParticipantCodeHasher());
        var eventId = await LoadOnlyWithdrawalEventIdAsync(runtimeOptions, tenantId);

        var executed = await executeStore.ExecuteWithdrawalAsync(
            tenantId,
            eventId,
            CancellationToken.None);

        Assert.True(executed.IsFailure);
        Assert.Equal("withdrawal_event.graph_changed", executed.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var verifyTransaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(WithdrawalEventStatuses.Failed, (await verifyDb.WithdrawalEvents.SingleAsync()).Status);
        Assert.Equal(1, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(2, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_withdrawal_fails_closed_for_cross_tenant_event()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantB,
            tenantBFixture.CampaignSeriesId,
            tenantBFixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantA,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsFailure);
        Assert.Equal("withdrawal_event.not_found", executed.Error.Code);
    }

    [DockerFact]
    public async Task Execute_withdrawal_is_not_reentrant_after_completion()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var first = await store.ExecuteWithdrawalAsync(tenantId, planned.Value.Id, CancellationToken.None);
        var second = await store.ExecuteWithdrawalAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsFailure);
        Assert.Equal("withdrawal_event.not_executable", second.Error.Code);
    }

    [DockerFact]
    public async Task Execute_delete_withdrawal_invalidates_platform_owned_derived_export_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        var artifacts = await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var campaignArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == artifacts.CampaignArtifactId);
        var seriesArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == artifacts.SeriesArtifactId);
        AssertInvalidatedArtifact(campaignArtifact);
        AssertInvalidatedArtifact(seriesArtifact);
        Assert.Equal(0, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        Assert.Equal(0, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(0, await verifyDb.Scores.CountAsync());
        var withdrawal = await verifyDb.WithdrawalEvents.SingleAsync();
        AssertWithdrawalMetadataRecordsArtifactInvalidation(withdrawal.MetadataJson, "deleted_graph", 2);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Execute_anonymize_withdrawal_invalidates_platform_owned_derived_export_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Anonymize);
        var artifacts = await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new WithdrawalRuntimeStore(db, new TenantDbScope(db), new DeterministicParticipantCodeHasher());
        var planned = await store.PlanIdentifiedWithdrawalAsync(
            tenantId,
            fixture.CampaignSeriesId,
            fixture.SubjectId,
            CancellationToken.None);

        var executed = await store.ExecuteWithdrawalAsync(
            tenantId,
            planned.Value.Id,
            CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var campaignArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == artifacts.CampaignArtifactId);
        var seriesArtifact = await verifyDb.ExportArtifacts.SingleAsync(artifact => artifact.Id == artifacts.SeriesArtifactId);
        AssertInvalidatedArtifact(campaignArtifact);
        AssertInvalidatedArtifact(seriesArtifact);
        var session = await verifyDb.ResponseSessions.SingleAsync();
        Assert.NotNull(session.AnonymizedAt);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        var withdrawal = await verifyDb.WithdrawalEvents.SingleAsync();
        AssertWithdrawalMetadataRecordsArtifactInvalidation(withdrawal.MetadataJson, "anonymized_graph", 2);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_candidates_returns_safe_counts_for_due_submitted_responses()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new RetentionDueCandidateStore(db, new TenantDbScope(db));

        var result = await store.PlanDueCandidatesAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(fixture.CampaignSeriesId, result.Value.CampaignSeriesId);
        Assert.Equal(DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"), result.Value.AsOf);
        var batch = Assert.Single(result.Value.Batches);
        Assert.Equal(fixture.RetentionPolicyId, batch.RetentionPolicyId);
        Assert.Equal(RetentionPolicy.ResponseSubmittedAt, batch.Anchor);
        Assert.Equal(RetentionPolicy.Anonymize, batch.ActionAfter);
        Assert.Equal(RetentionDueCandidateStatuses.Ready, batch.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T00:00:00+00:00"), batch.DueBefore);
        Assert.Equal(1, batch.ConsentRecordCount);
        Assert.Equal(1, batch.ResponseSessionCount);
        Assert.Equal(1, batch.AnswerCount);
        Assert.Equal(1, batch.ScoreRunCount);
        Assert.Equal(1, batch.ScoreCount);
        Assert.Equal(2, batch.DerivedArtifactCount);
        Assert.Empty(batch.Diagnostics);
        AssertDependency(batch, RetentionDueCandidateEntities.ConsentRecord, 1);
        AssertDependency(batch, RetentionDueCandidateEntities.ResponseSession, 1);
        AssertDependency(batch, RetentionDueCandidateEntities.Answer, 1);
        AssertDependency(batch, RetentionDueCandidateEntities.ScoreRun, 1);
        AssertDependency(batch, RetentionDueCandidateEntities.Score, 1);
        AssertDependency(batch, RetentionDueCandidateEntities.DerivedArtifact, 2);

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("subject@example.com", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-user-agent-hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("""{"value":4}""", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.ResponseSessionId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.AnswerId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        Assert.Equal(2, await verifyDb.ExportArtifacts.CountAsync(artifact => artifact.Status == ExportArtifactStatuses.Succeeded));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_candidates_excludes_not_yet_due_responses_without_mutation()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new RetentionDueCandidateStore(db, new TenantDbScope(db));

        var result = await store.PlanDueCandidatesAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2026-12-01T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var batch = Assert.Single(result.Value.Batches);
        Assert.Equal(RetentionDueCandidateStatuses.Ready, batch.Status);
        Assert.Equal(DateTimeOffset.Parse("2025-12-01T00:00:00+00:00"), batch.DueBefore);
        Assert.Equal(0, batch.ConsentRecordCount);
        Assert.Equal(0, batch.ResponseSessionCount);
        Assert.Equal(0, batch.AnswerCount);
        Assert.Equal(0, batch.ScoreRunCount);
        Assert.Equal(0, batch.ScoreCount);
        Assert.Equal(0, batch.DerivedArtifactCount);
        Assert.All(batch.Dependencies, dependency => Assert.Equal(0, dependency.Count));

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(verifyDb);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(2, await verifyDb.ExportArtifacts.CountAsync(artifact => artifact.Status == ExportArtifactStatuses.Succeeded));
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_candidates_fails_closed_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new RetentionDueCandidateStore(db, new TenantDbScope(db));

        var result = await store.PlanDueCandidatesAsync(
            tenantA,
            tenantBFixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);
    }

    [DockerFact]
    public async Task Plan_due_candidates_fails_closed_when_policy_missing()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var campaignSeriesId = await SeedTenantWithoutRetentionPolicyAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new RetentionDueCandidateStore(db, new TenantDbScope(db));

        var result = await store.PlanDueCandidatesAsync(
            tenantId,
            campaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("retention_policy.missing", result.Error.Code);
    }

    [DockerFact]
    public async Task Plan_due_candidates_reports_unsupported_anchor_as_safe_diagnostic()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var unsupportedScope = new TenantDbScope(mutateDb);
            await using var unsupportedTransaction = await unsupportedScope.BeginTransactionAsync(tenantId);
            mutateDb.RetentionPolicies.Add(new RetentionPolicy(
                Guid.NewGuid(),
                tenantId,
                fixture.CampaignSeriesId,
                "2.0.0",
                retainForYears: 1,
                RetentionPolicy.WaveClosedAt,
                RetentionPolicy.Anonymize,
                DateOnly.Parse("2027-05-17"),
                "{}",
                DateTimeOffset.Parse("2026-05-17T12:00:00+00:00")));
            await mutateDb.SaveChangesAsync();
            await unsupportedTransaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var store = new RetentionDueCandidateStore(db, new TenantDbScope(db));

        var result = await store.PlanDueCandidatesAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var batch = Assert.Single(result.Value.Batches);
        Assert.Equal(RetentionDueCandidateStatuses.Unsupported, batch.Status);
        Assert.Equal(RetentionPolicy.WaveClosedAt, batch.Anchor);
        Assert.Null(batch.DueBefore);
        Assert.Equal(0, batch.ResponseSessionCount);
        Assert.Equal(0, batch.AnswerCount);
        var diagnostic = Assert.Single(batch.Diagnostics);
        Assert.Equal(RetentionDueCandidateDiagnosticCodes.UnsupportedAnchor, diagnostic.Code);

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("subject@example.com", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task Plan_due_batch_persists_safe_planned_intent_from_ready_due_candidates()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var result = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(fixture.CampaignSeriesId, result.Value.CampaignSeriesId);
        Assert.Equal(fixture.RetentionPolicyId, result.Value.RetentionPolicyId);
        Assert.Equal(RetentionPolicy.ResponseSubmittedAt, result.Value.Anchor);
        Assert.Equal(RetentionPolicy.Anonymize, result.Value.ActionAfter);
        Assert.Equal(RetentionDueBatchStatuses.Planned, result.Value.Status);
        Assert.Equal(DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"), result.Value.AsOf);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T00:00:00+00:00"), result.Value.DueBefore);
        Assert.Equal(1, result.Value.ConsentRecordCount);
        Assert.Equal(1, result.Value.ResponseSessionCount);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(1, result.Value.ScoreRunCount);
        Assert.Equal(1, result.Value.ScoreCount);
        Assert.Equal(2, result.Value.DerivedArtifactCount);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.IdempotencyKey));

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("subject@example.com", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-user-agent-hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("""{"value":4}""", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.ResponseSessionId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.AnswerId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(result.Value.Id, persisted.Id);
        Assert.Equal(result.Value.IdempotencyKey, persisted.IdempotencyKey);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        Assert.Equal(2, await verifyDb.ExportArtifacts.CountAsync(artifact => artifact.Status == ExportArtifactStatuses.Succeeded));
        var persistedText = JsonSerializer.Serialize(persisted);
        Assert.DoesNotContain("subject@example.com", persistedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", persistedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.SubjectId.ToString(), persistedText, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_batch_is_idempotent_for_same_due_boundary()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var first = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        var second = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Equal(first.Value.IdempotencyKey, second.Value.IdempotencyKey);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.RetentionDueBatches.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_batch_fails_closed_for_zero_candidates()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var result = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2026-12-01T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("retention_due_batch.no_candidates", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.RetentionDueBatches.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_batch_fails_closed_for_unsupported_policy()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var unsupportedScope = new TenantDbScope(mutateDb);
            await using var unsupportedTransaction = await unsupportedScope.BeginTransactionAsync(tenantId);
            mutateDb.RetentionPolicies.Add(new RetentionPolicy(
                Guid.NewGuid(),
                tenantId,
                fixture.CampaignSeriesId,
                "2.0.0",
                retainForYears: 1,
                RetentionPolicy.WaveClosedAt,
                RetentionPolicy.Anonymize,
                DateOnly.Parse("2027-05-17"),
                "{}",
                DateTimeOffset.Parse("2026-05-17T12:00:00+00:00")));
            await mutateDb.SaveChangesAsync();
            await unsupportedTransaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var result = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("retention_due_batch.policy_unsupported", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(0, await verifyDb.RetentionDueBatches.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_batch_fails_closed_for_cross_tenant_series()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var result = await store.PlanDueBatchAsync(
            tenantA,
            tenantBFixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_series.not_found", result.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantA);
        Assert.Equal(0, await verifyDb.RetentionDueBatches.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_dry_run_reports_parity_without_mutating()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());

        var dryRun = await store.DryRunDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(dryRun.IsSuccess, dryRun.Error.ToString());
        Assert.True(dryRun.Value.ParityMatched);
        Assert.Empty(dryRun.Value.Mismatches);
        Assert.Equal(RetentionDueBatchStatuses.Planned, dryRun.Value.Batch.Status);
        Assert.Equal(1, dryRun.Value.Batch.ConsentRecordCount);
        Assert.Equal(1, dryRun.Value.Batch.ResponseSessionCount);
        Assert.Equal(1, dryRun.Value.Batch.AnswerCount);
        Assert.Equal(1, dryRun.Value.Batch.ScoreRunCount);
        Assert.Equal(1, dryRun.Value.Batch.ScoreCount);
        Assert.Equal(2, dryRun.Value.Batch.DerivedArtifactCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(RetentionDueBatchStatuses.Planned, (await verifyDb.RetentionDueBatches.SingleAsync()).Status);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_claims_parity_clean_planned_batch()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());

        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);

        Assert.True(claimed.IsSuccess, claimed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Processing, claimed.Value.Status);
        Assert.Equal(DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"), claimed.Value.ProcessingStartedAt);
        Assert.Null(claimed.Value.CompletedAt);
        Assert.Null(claimed.Value.FailedAt);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Processing, persisted.Status);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_claim_fails_and_marks_failed_for_stale_counts()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());

        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var mutateScope = new TenantDbScope(mutateDb);
            await using var mutateTransaction = await mutateScope.BeginTransactionAsync(tenantId);
            var answer = await mutateDb.Answers.SingleAsync();
            mutateDb.Answers.Remove(answer);
            await mutateDb.SaveChangesAsync();
            await mutateTransaction.CommitAsync();
        }

        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);

        Assert.True(claimed.IsFailure);
        Assert.Equal("retention_due_batch.parity_mismatch", claimed.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Failed, persisted.Status);
        Assert.Equal("retention_due_batch.parity_mismatch", persisted.FailureCode);
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_fails_closed_for_cross_tenant_batch()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantB,
            tenantBFixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());

        var dryRun = await store.DryRunDueBatchAsync(tenantA, planned.Value.Id, CancellationToken.None);
        var claim = await store.ClaimDueBatchAsync(
            tenantA,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);

        Assert.True(dryRun.IsFailure);
        Assert.Equal("retention_due_batch.not_found", dryRun.Error.Code);
        Assert.True(claim.IsFailure);
        Assert.Equal("retention_due_batch.not_found", claim.Error.Code);
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_does_not_reenter_processing_completed_or_failed_batches()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        var secondPlanned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-19T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        Assert.True(secondPlanned.IsSuccess, secondPlanned.Error.ToString());
        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        var processingClaim = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:06:00+00:00"),
            CancellationToken.None);
        var completed = await store.CompleteDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:07:00+00:00"),
            CancellationToken.None);
        var completedClaim = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:08:00+00:00"),
            CancellationToken.None);
        var failed = await store.FailDueBatchAsync(
            tenantId,
            secondPlanned.Value.Id,
            "operator_cancelled",
            "operator cancelled before scheduler execution",
            DateTimeOffset.Parse("2027-05-19T00:05:00+00:00"),
            CancellationToken.None);
        var failedClaim = await store.ClaimDueBatchAsync(
            tenantId,
            secondPlanned.Value.Id,
            DateTimeOffset.Parse("2027-05-19T00:06:00+00:00"),
            CancellationToken.None);

        Assert.True(processingClaim.IsFailure);
        Assert.Equal("retention_due_batch.not_planned", processingClaim.Error.Code);
        Assert.True(completed.IsSuccess, completed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Completed, completed.Value.Status);
        Assert.True(completedClaim.IsFailure);
        Assert.Equal("retention_due_batch.not_planned", completedClaim.Error.Code);
        Assert.True(failed.IsSuccess, failed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Failed, failed.Value.Status);
        Assert.True(failedClaim.IsFailure);
        Assert.Equal("retention_due_batch.not_planned", failedClaim.Error.Code);
    }

    [DockerFact]
    public async Task Due_batch_lifecycle_fail_records_safe_failure_metadata()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());

        var failed = await store.FailDueBatchAsync(
            tenantId,
            planned.Value.Id,
            "worker_failed",
            "subject@example.com raw token identified-ip-hash should not persist",
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);

        Assert.True(failed.IsSuccess, failed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Failed, failed.Value.Status);
        Assert.Equal("worker_failed", failed.Value.FailureCode);
        Assert.NotNull(failed.Value.FailureDetail);
        Assert.DoesNotContain("subject@example.com", failed.Value.FailureDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", failed.Value.FailureDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", failed.Value.FailureDetail, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Failed, persisted.Status);
        Assert.Equal("worker_failed", persisted.FailureCode);
        Assert.DoesNotContain("subject@example.com", persisted.FailureDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", persisted.FailureDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("identified-ip-hash", persisted.FailureDetail, StringComparison.OrdinalIgnoreCase);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_execution_delete_completes_and_deletes_due_graph()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        var executed = await store.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Completed, executed.Value.Batch.Status);
        Assert.Equal("deleted_graph", executed.Value.Result);
        Assert.Equal(1, executed.Value.ConsentRecordCount);
        Assert.Equal(1, executed.Value.ResponseSessionCount);
        Assert.Equal(1, executed.Value.AnswerCount);
        Assert.Equal(1, executed.Value.ScoreRunCount);
        Assert.Equal(1, executed.Value.ScoreCount);
        Assert.Equal(2, executed.Value.DerivedArtifactCount);
        Assert.Equal(2, executed.Value.ArtifactInvalidatedCount);
        Assert.Equal(0, executed.Value.NoticeScrubbedCount);
        Assert.Equal(0, executed.Value.DeliveryAttemptScrubbedCount);
        Assert.Equal(0, executed.Value.InviteCredentialScrubbedCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Completed, persisted.Status);
        Assert.NotNull(persisted.CompletedAt);
        Assert.Equal(0, await verifyDb.ConsentRecords.CountAsync());
        Assert.Equal(0, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        Assert.Equal(0, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(0, await verifyDb.Scores.CountAsync());
        Assert.Equal(2, await verifyDb.ExportArtifacts.CountAsync(artifact => artifact.Status == ExportArtifactStatuses.Deleted));
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_execution_anonymize_completes_scrubs_identity_and_invalidates_artifacts()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await SeedSucceededDerivedArtifactsAsync(runtimeOptions, tenantId, fixture);
        var notificationId = Guid.NewGuid();
        var deliveryAttemptId = Guid.NewGuid();
        var invitationTokenId = Guid.NewGuid();
        await using (var notificationDb = new ApplicationDbContext(runtimeOptions))
        {
            var notificationScope = new TenantDbScope(notificationDb);
            await using var notificationTransaction = await notificationScope.BeginTransactionAsync(tenantId);
            var notification = Notification.QueueEmailInvitation(
                notificationId,
                tenantId,
                fixture.CampaignId,
                fixture.AssignmentId,
                "subject@example.com");
            notification.MarkSent(DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"));
            notificationDb.Notifications.Add(notification);
            notificationDb.NotificationDeliveryAttempts.Add(NotificationDeliveryAttempt.CreateSent(
                deliveryAttemptId,
                tenantId,
                notificationId,
                "smtp",
                "subject@example.com",
                "provider-message-123",
                DateTimeOffset.Parse("2026-05-17T14:00:05+00:00")));
            notificationDb.InvitationTokens.Add(new InvitationToken(
                invitationTokenId,
                tenantId,
                fixture.CampaignId,
                "linked-delivery-token-hash",
                InvitationTokenChannels.Email,
                recipient: "subject@example.com",
                assignmentId: fixture.AssignmentId));
            await notificationDb.SaveChangesAsync();
            await notificationTransaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        var executed = await store.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(RetentionDueBatchStatuses.Completed, executed.Value.Batch.Status);
        Assert.Equal("anonymized_graph", executed.Value.Result);
        Assert.Equal(2, executed.Value.ArtifactInvalidatedCount);
        Assert.Equal(1, executed.Value.NoticeScrubbedCount);
        Assert.Equal(1, executed.Value.DeliveryAttemptScrubbedCount);
        Assert.Equal(1, executed.Value.InviteCredentialScrubbedCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var assignment = await verifyDb.Assignments.SingleAsync();
        Assert.Null(assignment.RespondentSubjectId);
        Assert.Null(assignment.TargetSubjectId);
        Assert.NotNull(assignment.AnonymizedAt);
        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.Null(responseSession.ConsentRecordId);
        Assert.Null(responseSession.IpHash);
        Assert.Null(responseSession.UserAgentHash);
        Assert.NotNull(responseSession.AnonymizedAt);
        var consentRecord = await verifyDb.ConsentRecords.SingleAsync();
        Assert.Null(consentRecord.SubjectId);
        Assert.NotNull(consentRecord.AnonymizedAt);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        Assert.Equal(2, await verifyDb.ExportArtifacts.CountAsync(artifact => artifact.Status == ExportArtifactStatuses.Deleted));
        var scrubbedNotification = await verifyDb.Notifications.SingleAsync(entity => entity.Id == notificationId);
        Assert.Equal("withdrawn@example.invalid", scrubbedNotification.Recipient);
        var deliveryAttempt = await verifyDb.NotificationDeliveryAttempts.SingleAsync(attempt => attempt.Id == deliveryAttemptId);
        Assert.Equal("withdrawn@example.invalid", deliveryAttempt.Recipient);
        Assert.Null(deliveryAttempt.ProviderMessageId);
        var invitationToken = await verifyDb.InvitationTokens.SingleAsync(token => token.Id == invitationTokenId);
        Assert.Null(invitationToken.AssignmentId);
        Assert.Null(invitationToken.Recipient);
        Assert.Equal($"withdrawn:{invitationTokenId:N}", invitationToken.TokenHash);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_execution_anonymize_scrubs_identified_queue_token_for_due_subject()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        var queueTokenId = Guid.NewGuid();

        await using (var tokenDb = new ApplicationDbContext(runtimeOptions))
        {
            var tenantDbScope = new TenantDbScope(tokenDb);
            await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
            tokenDb.InvitationTokens.Add(new InvitationToken(
                queueTokenId,
                tenantId,
                fixture.CampaignId,
                "due-identified-queue-token-hash",
                InvitationTokenChannels.IdentifiedQueue,
                respondentSubjectId: fixture.SubjectId));
            await tokenDb.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        var executed = await store.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(executed.IsSuccess, executed.Error.ToString());
        Assert.Equal(1, executed.Value.InviteCredentialScrubbedCount);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var token = await verifyDb.InvitationTokens.SingleAsync(entity => entity.Id == queueTokenId);
        Assert.Equal(InvitationTokenChannels.IdentifiedQueue, token.Channel);
        Assert.Null(token.RespondentSubjectId);
        Assert.Null(token.AssignmentId);
        Assert.Null(token.Recipient);
        Assert.Equal($"withdrawn:{queueTokenId:N}", token.TokenHash);
        Assert.NotNull(token.ExpiresAt);
        Assert.NotNull(token.UsedAt);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_execution_fails_stale_parity_without_mutating_due_graph()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await store.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        await using (var mutateDb = new ApplicationDbContext(runtimeOptions))
        {
            var mutateScope = new TenantDbScope(mutateDb);
            await using var mutateTransaction = await mutateScope.BeginTransactionAsync(tenantId);
            var answer = await mutateDb.Answers.SingleAsync();
            mutateDb.Answers.Remove(answer);
            await mutateDb.SaveChangesAsync();
            await mutateTransaction.CommitAsync();
        }

        var executed = await store.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);

        Assert.True(executed.IsFailure);
        Assert.Equal("retention_due_batch.parity_mismatch", executed.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var persisted = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Failed, persisted.Status);
        Assert.Equal("retention_due_batch.parity_mismatch", persisted.FailureCode);
        Assert.Equal(1, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_execution_rejects_wrong_tenant_non_processing_and_terminal_batches()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        await SeedTenantAsync(runtimeOptions, tenantA, "tenant-a");
        var tenantBFixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantB);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var store = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await store.PlanDueBatchAsync(
            tenantB,
            tenantBFixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var wrongTenant = await store.ExecuteDueBatchAsync(tenantA, planned.Value.Id, CancellationToken.None);
        var notProcessing = await store.ExecuteDueBatchAsync(tenantB, planned.Value.Id, CancellationToken.None);
        var claimed = await store.ClaimDueBatchAsync(
            tenantB,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());
        var completed = await store.CompleteDueBatchAsync(
            tenantB,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:06:00+00:00"),
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error.ToString());
        var completedExecute = await store.ExecuteDueBatchAsync(tenantB, planned.Value.Id, CancellationToken.None);

        Assert.True(wrongTenant.IsFailure);
        Assert.Equal("retention_due_batch.not_found", wrongTenant.Error.Code);
        Assert.True(notProcessing.IsFailure);
        Assert.Equal("retention_due_batch.not_executable", notProcessing.Error.Code);
        Assert.True(completedExecute.IsFailure);
        Assert.Equal("retention_due_batch.not_executable", completedExecute.Error.Code);
    }

    [DockerFact]
    public async Task Plan_due_candidates_excludes_already_anonymized_sessions_after_due_batch_execution()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var batchStore = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await batchStore.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await batchStore.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());
        var executed = await batchStore.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);
        Assert.True(executed.IsSuccess, executed.Error.ToString());

        var plan = await candidateStore.PlanDueCandidatesAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2028-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(plan.IsSuccess, plan.Error.ToString());
        var batch = Assert.Single(plan.Value.Batches);
        Assert.Equal(RetentionDueCandidateStatuses.Ready, batch.Status);
        Assert.Equal(0, batch.ConsentRecordCount);
        Assert.Equal(0, batch.ResponseSessionCount);
        Assert.Equal(0, batch.AnswerCount);
        Assert.Equal(0, batch.ScoreRunCount);
        Assert.Equal(0, batch.ScoreCount);
        Assert.Equal(0, batch.DerivedArtifactCount);
        Assert.All(batch.Dependencies, dependency => Assert.Equal(0, dependency.Count));

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.NotNull(responseSession.AnonymizedAt);
        Assert.Equal(1, await verifyDb.Answers.CountAsync());
        Assert.Equal(1, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(1, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Plan_due_batch_returns_no_candidates_after_anonymize_execution()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var batchStore = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await batchStore.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await batchStore.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());
        var executed = await batchStore.ExecuteDueBatchAsync(tenantId, planned.Value.Id, CancellationToken.None);
        Assert.True(executed.IsSuccess, executed.Error.ToString());

        var nextPlan = await batchStore.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2028-05-18T00:00:00+00:00"),
            CancellationToken.None);

        Assert.True(nextPlan.IsFailure);
        Assert.Equal("retention_due_batch.no_candidates", nextPlan.Error.Code);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.RetentionDueBatches.CountAsync());
        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.NotNull(responseSession.AnonymizedAt);
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_automation_plans_claims_executes_and_is_rerun_safe()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var batchStore = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);

        var run = await batchStore.RunDueBatchAutomationAsync(
            tenantId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            maxBatches: 10,
            CancellationToken.None);

        Assert.True(run.IsSuccess, run.Error.ToString());
        Assert.Equal(tenantId, run.Value.TenantId);
        Assert.Equal(DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"), run.Value.AsOf);
        Assert.Equal(1, run.Value.SeriesScannedCount);
        Assert.Equal(1, run.Value.DueBatchCount);
        Assert.Equal(1, run.Value.ClaimedBatchCount);
        Assert.Equal(1, run.Value.CompletedBatchCount);
        Assert.Equal(0, run.Value.FailedBatchCount);
        Assert.Equal(0, run.Value.NoCandidateSeriesCount);
        var item = Assert.Single(run.Value.Items);
        Assert.Equal(fixture.CampaignSeriesId, item.CampaignSeriesId);
        Assert.NotNull(item.DueBatchId);
        Assert.Equal("executed", item.Stage);
        Assert.Equal(RetentionDueBatchStatuses.Completed, item.Status);
        Assert.Equal("anonymized_graph", item.Result);
        Assert.Null(item.ErrorCode);

        var rerun = await batchStore.RunDueBatchAutomationAsync(
            tenantId,
            DateTimeOffset.Parse("2028-05-18T00:00:00+00:00"),
            maxBatches: 10,
            CancellationToken.None);

        Assert.True(rerun.IsSuccess, rerun.Error.ToString());
        Assert.Equal(1, rerun.Value.SeriesScannedCount);
        Assert.Equal(0, rerun.Value.DueBatchCount);
        Assert.Equal(0, rerun.Value.ClaimedBatchCount);
        Assert.Equal(0, rerun.Value.CompletedBatchCount);
        Assert.Equal(0, rerun.Value.FailedBatchCount);
        Assert.Equal(1, rerun.Value.NoCandidateSeriesCount);
        var rerunItem = Assert.Single(rerun.Value.Items);
        Assert.Equal("no_candidates", rerunItem.Stage);
        Assert.Equal("skipped", rerunItem.Status);
        Assert.Equal("retention_due_batch.no_candidates", rerunItem.ErrorCode);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.RetentionDueBatches.CountAsync());
        var dueBatch = await verifyDb.RetentionDueBatches.SingleAsync();
        Assert.Equal(RetentionDueBatchStatuses.Completed, dueBatch.Status);
        var responseSession = await verifyDb.ResponseSessions.SingleAsync();
        Assert.NotNull(responseSession.AnonymizedAt);
        Assert.Equal(0, await verifyDb.WithdrawalEvents.CountAsync());
        await transaction.CommitAsync();
    }

    [DockerFact]
    public async Task Due_batch_automation_executes_existing_processing_batch_without_duplicate_intent()
    {
        var tenantId = Guid.NewGuid();
        var migratorOptions = CreateMigratorOptions();
        await PrepareDatabaseAsync(migratorOptions);
        var runtimeOptions = CreateRuntimeOptions();
        var fixture = await SeedIdentifiedResponseAsync(runtimeOptions, tenantId, RetentionPolicy.Delete);
        await using var db = new ApplicationDbContext(runtimeOptions);
        var tenantDbScope = new TenantDbScope(db);
        var candidateStore = new RetentionDueCandidateStore(db, tenantDbScope);
        var batchStore = new RetentionDueBatchStore(db, tenantDbScope, candidateStore);
        var planned = await batchStore.PlanDueBatchAsync(
            tenantId,
            fixture.CampaignSeriesId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            CancellationToken.None);
        Assert.True(planned.IsSuccess, planned.Error.ToString());
        var claimed = await batchStore.ClaimDueBatchAsync(
            tenantId,
            planned.Value.Id,
            DateTimeOffset.Parse("2027-05-18T00:05:00+00:00"),
            CancellationToken.None);
        Assert.True(claimed.IsSuccess, claimed.Error.ToString());

        var run = await batchStore.RunDueBatchAutomationAsync(
            tenantId,
            DateTimeOffset.Parse("2027-05-18T00:00:00+00:00"),
            maxBatches: 10,
            CancellationToken.None);

        Assert.True(run.IsSuccess, run.Error.ToString());
        Assert.Equal(1, run.Value.SeriesScannedCount);
        Assert.Equal(1, run.Value.DueBatchCount);
        Assert.Equal(0, run.Value.ClaimedBatchCount);
        Assert.Equal(1, run.Value.CompletedBatchCount);
        Assert.Equal(0, run.Value.FailedBatchCount);
        var item = Assert.Single(run.Value.Items);
        Assert.Equal(planned.Value.Id, item.DueBatchId);
        Assert.Equal("executed", item.Stage);
        Assert.Equal(RetentionDueBatchStatuses.Completed, item.Status);
        Assert.Equal("deleted_graph", item.Result);

        await using var verifyDb = new ApplicationDbContext(runtimeOptions);
        var verifyScope = new TenantDbScope(verifyDb);
        await using var transaction = await verifyScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await verifyDb.RetentionDueBatches.CountAsync());
        Assert.Equal(0, await verifyDb.ResponseSessions.CountAsync());
        Assert.Equal(0, await verifyDb.Answers.CountAsync());
        Assert.Equal(0, await verifyDb.ScoreRuns.CountAsync());
        Assert.Equal(0, await verifyDb.Scores.CountAsync());
        await transaction.CommitAsync();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> migratorOptions)
    {
        await using var migratorDb = new ApplicationDbContext(migratorOptions);
        await migratorDb.Database.MigrateAsync();
        await CreateRuntimeRoleAsync(migratorOptions);
    }

    private DbContextOptions<ApplicationDbContext> CreateMigratorOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    private DbContextOptions<ApplicationDbContext> CreateRuntimeOptions(params IInterceptor[] interceptors)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static async Task CreateRuntimeRoleAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            $$"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_roles
                    WHERE rolname = '{{RuntimeUsername}}'
                ) THEN
                    CREATE ROLE {{RuntimeUsername}} LOGIN PASSWORD '{{RuntimePassword}}';
                END IF;
            END
            $$;

            GRANT USAGE ON SCHEMA public TO {{RuntimeUsername}};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {{RuntimeUsername}};
            """);
    }

    private static async Task<WithdrawalFixture> SeedIdentifiedResponseAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string actionAfter = RetentionPolicy.Anonymize)
    {
        var fixture = CreateFixtureIds(tenantId, ResponseIdentityModes.Identified);
        await SeedTenantAsync(options, tenantId, "identified-tenant", fixture, actionAfter);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var subject = new Subject(fixture.SubjectId, tenantId, email: "subject@example.com", displayName: "Subject");
        var assignment = Assignment.CreateIdentified(
            fixture.AssignmentId,
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.SubjectId);
        var consentRecord = new ConsentRecord(
            fixture.ConsentRecordId,
            tenantId,
            fixture.ConsentDocumentId,
            fixture.CampaignId,
            fixture.AssignmentId,
            "en",
            """["participate"]""",
            DateTimeOffset.Parse("2026-05-17T12:00:00+00:00"),
            subjectId: fixture.SubjectId);
        var session = new ResponseSession(
            fixture.ResponseSessionId,
            tenantId,
            fixture.AssignmentId,
            "en",
            consentRecordId: fixture.ConsentRecordId,
            startedAt: DateTimeOffset.Parse("2026-05-17T12:05:00+00:00"),
            publicHandleHash: new string('a', 64),
            publicHandleIssuedAt: DateTimeOffset.Parse("2026-05-17T12:05:30+00:00"),
            ipHash: "identified-ip-hash",
            userAgentHash: "identified-user-agent-hash");
        session.Submit(DateTimeOffset.Parse("2026-05-17T12:08:00+00:00"));
        var scoreRun = new ScoreRun(
            fixture.ScoreRunId,
            tenantId,
            fixture.CampaignId,
            fixture.ResponseSessionId,
            fixture.ScoringRuleId);

        db.Subjects.Add(subject);
        db.Assignments.Add(assignment);
        db.ConsentRecords.Add(consentRecord);
        db.ResponseSessions.Add(session);
        db.Answers.Add(new Answer(
            fixture.AnswerId,
            tenantId,
            fixture.ResponseSessionId,
            fixture.QuestionId,
            """{"value":4}"""));
        db.ScoreRuns.Add(scoreRun);
        db.Scores.Add(new Score(
            fixture.ScoreId,
            tenantId,
            fixture.ScoreRunId,
            fixture.CampaignId,
            fixture.ResponseSessionId,
            "total",
            4m,
            1));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return fixture;
    }

    private static async Task<WithdrawalFixture> SeedAnonymousLongitudinalResponseAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string rawCode,
        string actionAfter = RetentionPolicy.Anonymize)
    {
        var fixture = CreateFixtureIds(tenantId, ResponseIdentityModes.AnonymousLongitudinal);
        await SeedTenantAsync(options, tenantId, "anonymous-longitudinal-tenant", fixture, actionAfter);
        var hasher = new DeterministicParticipantCodeHasher();
        var hash = await hasher.HashAsync(rawCode, fixture.CodeSalt, CancellationToken.None);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var participantCode = new ParticipantCode(
            fixture.ParticipantCodeId,
            tenantId,
            fixture.CampaignSeriesId,
            hash.Hash,
            hash.Parameters.MemoryKiB,
            hash.Parameters.Iterations,
            hash.Parameters.Parallelism,
            hash.Parameters.OutputBytes,
            DateTimeOffset.Parse("2026-05-17T12:00:00+00:00"));
        var invitationToken = new InvitationToken(
            fixture.InvitationTokenId,
            tenantId,
            fixture.CampaignId,
            "withdrawal-test-token-hash",
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            fixture.AssignmentId,
            tenantId,
            fixture.CampaignId,
            "self",
            fixture.InvitationTokenId);
        var consentRecord = new ConsentRecord(
            fixture.ConsentRecordId,
            tenantId,
            fixture.ConsentDocumentId,
            fixture.CampaignId,
            fixture.AssignmentId,
            "en",
            """["participate"]""",
            DateTimeOffset.Parse("2026-05-17T12:00:00+00:00"));
        var session = new ResponseSession(
            fixture.ResponseSessionId,
            tenantId,
            fixture.AssignmentId,
            "en",
            participantCodeId: fixture.ParticipantCodeId,
            consentRecordId: fixture.ConsentRecordId,
            startedAt: DateTimeOffset.Parse("2026-05-17T12:05:00+00:00"),
            publicHandleHash: new string('b', 64),
            publicHandleIssuedAt: DateTimeOffset.Parse("2026-05-17T12:05:30+00:00"),
            ipHash: "anonymous-ip-hash",
            userAgentHash: "anonymous-user-agent-hash");
        session.Submit(DateTimeOffset.Parse("2026-05-17T12:08:00+00:00"));
        var scoreRun = new ScoreRun(
            fixture.ScoreRunId,
            tenantId,
            fixture.CampaignId,
            fixture.ResponseSessionId,
            fixture.ScoringRuleId);

        db.ParticipantCodes.Add(participantCode);
        db.InvitationTokens.Add(invitationToken);
        db.Assignments.Add(assignment);
        db.ConsentRecords.Add(consentRecord);
        db.ResponseSessions.Add(session);
        db.Answers.Add(new Answer(
            fixture.AnswerId,
            tenantId,
            fixture.ResponseSessionId,
            fixture.QuestionId,
            """{"value":4}"""));
        db.ScoreRuns.Add(scoreRun);
        db.Scores.Add(new Score(
            fixture.ScoreId,
            tenantId,
            fixture.ScoreRunId,
            fixture.CampaignId,
            fixture.ResponseSessionId,
            "total",
            4m,
            1));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return fixture;
    }

    private static async Task<Guid> SeedTenantWithoutRetentionPolicyAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId)
    {
        var fixture = CreateFixtureIds(tenantId, ResponseIdentityModes.Identified);
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        db.Tenants.Add(new Tenant(tenantId, "retention-policy-missing", "retention-policy-missing"));
        db.CampaignSeries.Add(new CampaignSeries(
            fixture.CampaignSeriesId,
            tenantId,
            "Retention policy missing",
            fixture.CodeSalt));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return fixture.CampaignSeriesId;
    }

    private static async Task SeedTenantAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        string slug,
        WithdrawalFixture? fixture = null,
        string actionAfter = RetentionPolicy.Anonymize)
    {
        fixture ??= CreateFixtureIds(tenantId, ResponseIdentityModes.Identified);
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        db.Tenants.Add(new Tenant(tenantId, slug, slug));
        db.CampaignSeries.Add(new CampaignSeries(
            fixture.CampaignSeriesId,
            tenantId,
            "Withdrawal pulse",
            fixture.CodeSalt));
        db.SurveyTemplates.Add(SurveyTemplate.CreateTenant(
            fixture.TemplateId,
            tenantId,
            "Withdrawal template"));
        var templateVersion = TemplateVersion.CreateTenantDraft(
            fixture.TemplateVersionId,
            fixture.TemplateId,
            "1.0.0",
            "en");
        templateVersion.Publish(null, DateTimeOffset.Parse("2026-05-17T11:00:00+00:00"));
        db.TemplateVersions.Add(templateVersion);
        db.TemplateSections.Add(new TemplateSection(
            fixture.SectionId,
            fixture.TemplateVersionId,
            1,
            "main",
            "Main"));
        db.TemplateQuestions.Add(new TemplateQuestion(
            fixture.QuestionId,
            fixture.TemplateVersionId,
            fixture.SectionId,
            1,
            "q1",
            QuestionTypes.Number,
            scaleId: null,
            "Question 1"));
        db.ScoringRules.Add(ScoringRule.CreateDraft(
            fixture.ScoringRuleId,
            fixture.TemplateVersionId,
            "default",
            "1.0.0",
            "1.0",
            "1.0",
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            """{"schema_version":"1.0"}""",
            """{"scores":["total"]}"""));
        db.Campaigns.Add(new Campaign(
            fixture.CampaignId,
            tenantId,
            fixture.TemplateVersionId,
            "Withdrawal wave",
            fixture.ResponseIdentityMode,
            campaignSeriesId: fixture.CampaignSeriesId,
            status: CampaignStatuses.Live,
            startAt: DateTimeOffset.Parse("2026-05-17T11:30:00+00:00")));
        db.RetentionPolicies.Add(new RetentionPolicy(
            fixture.RetentionPolicyId,
            tenantId,
            fixture.CampaignSeriesId,
            "1.0.0",
            retainForYears: 1,
            RetentionPolicy.ResponseSubmittedAt,
            actionAfter,
            DateOnly.Parse("2027-05-17"),
            "{}",
            DateTimeOffset.Parse("2026-05-17T11:00:00+00:00")));
        db.ConsentDocuments.Add(new ConsentDocument(
            fixture.ConsentDocumentId,
            tenantId,
            fixture.CampaignSeriesId,
            "en",
            "1.0.0",
            "Consent",
            "Consent body",
            """["participate"]""",
            "[]",
            DateTimeOffset.Parse("2026-05-17T11:00:00+00:00")));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static WithdrawalFixture CreateFixtureIds(Guid tenantId, string identityMode)
    {
        return new WithdrawalFixture(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Enumerable.Range(0, 32).Select(index => (byte)(index + 1)).ToArray(),
            identityMode);
    }

    private static async Task<DerivedArtifactFixture> SeedSucceededDerivedArtifactsAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId,
        WithdrawalFixture fixture)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var createdAt = DateTimeOffset.Parse("2026-05-17T12:09:00+00:00");
        var campaignArtifact = new ExportArtifact(
            Guid.NewGuid(),
            tenantId,
            ExportArtifactTargetKinds.Campaign,
            fixture.CampaignId,
            fixture.CampaignSeriesId,
            ExportArtifactTypes.ReportProofCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            "withdrawal-report-proof.csv",
            "text/csv",
            1,
            18,
            new string('c', 64),
            """{"source":"withdrawal_test"}""",
            "dimension,total\r\n4,4\r\n",
            "{}",
            createdAt,
            createdAt);
        var seriesArtifact = new ExportArtifact(
            Guid.NewGuid(),
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            fixture.CampaignSeriesId,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            "withdrawal-response-export.csv",
            "text/csv",
            1,
            17,
            new string('d', 64),
            """{"source":"withdrawal_test"}""",
            "answer,total\r\n4,4\r\n",
            "{}",
            createdAt,
            createdAt);

        db.ExportArtifacts.AddRange(campaignArtifact, seriesArtifact);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DerivedArtifactFixture(campaignArtifact.Id, seriesArtifact.Id);
    }

    private static async Task<Guid> LoadOnlyWithdrawalEventIdAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId)
    {
        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var id = await db.WithdrawalEvents.Select(withdrawal => withdrawal.Id).SingleAsync();
        await transaction.CommitAsync();

        return id;
    }

    private static void AssertDependency(
        WithdrawalDryRunResponse dryRun,
        string entity,
        int count)
    {
        var dependency = Assert.Single(dryRun.Dependencies, item => item.Entity == entity);
        Assert.Equal(count, dependency.Count);
    }

    private static void AssertDependency(
        RetentionDueCandidateBatch batch,
        string entity,
        int count)
    {
        var dependency = Assert.Single(batch.Dependencies, item => item.Entity == entity);
        Assert.Equal(count, dependency.Count);
    }

    private static void AssertInvalidatedArtifact(ExportArtifact artifact)
    {
        Assert.Equal(ExportArtifactStatuses.Deleted, artifact.Status);
        Assert.NotNull(artifact.DeletedAt);
        Assert.Null(artifact.ChecksumSha256);
        Assert.Null(artifact.Content);
        Assert.False(artifact.CanDownload);
    }

    private static void AssertWithdrawalMetadataRecordsArtifactInvalidation(
        string metadataJson,
        string expectedResult,
        int expectedInvalidatedCount)
    {
        using var document = JsonDocument.Parse(metadataJson);
        Assert.Equal(expectedResult, document.RootElement.GetProperty("result").GetString());
        Assert.Equal(
            expectedInvalidatedCount,
            document.RootElement.GetProperty("artifact_invalidated_count").GetInt32());
        Assert.DoesNotContain("answer", metadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", metadataJson, StringComparison.OrdinalIgnoreCase);
    }

    private static string AuditPayloadText(IReadOnlyCollection<AuditEvent> auditRows)
    {
        return string.Join(
            "\n",
            auditRows.SelectMany(auditEvent => new[]
            {
                auditEvent.Before?.RootElement.GetRawText(),
                auditEvent.After?.RootElement.GetRawText()
            }).OfType<string>());
    }

    private static void AssertAuditPropertyRedacted(
        AuditEvent auditEvent,
        bool before,
        string propertyName)
    {
        var document = before ? auditEvent.Before : auditEvent.After;
        Assert.NotNull(document);
        Assert.True(
            document!.RootElement.TryGetProperty(propertyName, out var property),
            $"{auditEvent.EntityType}.{propertyName} was not present in audit payload.");
        Assert.Equal("[redacted]", property.GetString());
    }

    private sealed class DeterministicParticipantCodeHasher : IParticipantCodeHasher
    {
        public Task<ParticipantCodeHashResult> HashAsync(
            string rawCode,
            byte[] salt,
            CancellationToken cancellationToken)
        {
            var normalized = rawCode.Trim().ToLowerInvariant();
            var hash = new byte[32];
            for (var index = 0; index < hash.Length; index++)
            {
                var codeByte = normalized.Length == 0 ? 0 : normalized[index % normalized.Length];
                hash[index] = (byte)(codeByte ^ salt[index % salt.Length]);
            }

            return Task.FromResult(new ParticipantCodeHashResult(
                hash,
                new ParticipantCodeHashingParameters(65_536, 3, 4, 32)));
        }
    }

    private sealed record WithdrawalFixture(
        Guid TenantId,
        Guid CampaignSeriesId,
        Guid TemplateId,
        Guid TemplateVersionId,
        Guid SectionId,
        Guid QuestionId,
        Guid ScoringRuleId,
        Guid CampaignId,
        Guid RetentionPolicyId,
        Guid ConsentDocumentId,
        Guid SubjectId,
        Guid AssignmentId,
        Guid ConsentRecordId,
        Guid ResponseSessionId,
        Guid AnswerId,
        Guid ScoreRunId,
        Guid ScoreId,
        byte[] CodeSalt,
        string ResponseIdentityMode)
    {
        public Guid ParticipantCodeId { get; } = Guid.NewGuid();

        public Guid InvitationTokenId { get; } = Guid.NewGuid();
    }

    private sealed record DerivedArtifactFixture(
        Guid CampaignArtifactId,
        Guid SeriesArtifactId);
}
