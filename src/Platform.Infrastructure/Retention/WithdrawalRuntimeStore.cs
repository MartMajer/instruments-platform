using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Features.Retention;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Operations;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Retention;

public sealed class WithdrawalRuntimeStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IParticipantCodeHasher participantCodeHasher,
    ICurrentTenant? currentTenant = null)
    : IWithdrawalRuntimeStore
{
    private const int RequestReviewLimit = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<WithdrawalRequestResponse>> CreateWithdrawalRequestAsync(
        Guid tenantId,
        CreateWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        var targetKind = command.TargetKind.Trim();
        if (targetKind is not (WithdrawalTargetKinds.ResponseSession or WithdrawalTargetKinds.IdentifiedSubject))
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Validation(
                    "withdrawal_request.target_kind_unsupported",
                    "Withdrawal request target kind is not supported."));
        }

        if (command.TargetId == Guid.Empty)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Validation("withdrawal_request.target_required", "Withdrawal request target is required."));
        }

        var action = NormalizeRequestedAction(command.RequestedAction);
        if (action.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(action.Error);
        }

        var reasonCode = NormalizeReasonCode(command.ReasonCode);
        if (reasonCode.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(reasonCode.Error);
        }

        var requestedAt = DateTimeOffset.UtcNow;
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);

        if (targetKind == WithdrawalTargetKinds.IdentifiedSubject)
        {
            return await CreateIdentifiedSubjectWithdrawalRequestAsync(
                tenantId,
                command,
                action.Value,
                reasonCode.Value,
                requestedAt,
                transaction,
                cancellationToken);
        }

        var target = await LoadResponseSessionRequestContextAsync(command.TargetId, cancellationToken);
        if (target is null)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        var context = await LoadSeriesContextAsync(target.CampaignSeriesId, requestedAt, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(context.Error);
        }

        var existing = await db.WithdrawalEvents
            .AsNoTracking()
            .Where(withdrawal =>
                withdrawal.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                withdrawal.ResponseSessionId == command.TargetId &&
                withdrawal.ActionAfter == action.Value &&
                (withdrawal.Status == WithdrawalEventStatuses.Requested ||
                    withdrawal.Status == WithdrawalEventStatuses.Planned ||
                    withdrawal.Status == WithdrawalEventStatuses.Processing))
            .OrderBy(withdrawal => withdrawal.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result.Success(ToRequestResponse(existing, command.TargetId, idempotent: true));
        }

        var consentRecordCount = target.ConsentRecordId.HasValue
            ? await db.ConsentRecords.CountAsync(record => record.Id == target.ConsentRecordId.Value, cancellationToken)
            : 0;
        var answerCount = await db.Answers
            .Where(answer => answer.SessionId == command.TargetId)
            .CountAsync(cancellationToken);
        var scoreRunCount = await db.ScoreRuns
            .Where(scoreRun => scoreRun.ResponseSessionId == command.TargetId)
            .CountAsync(cancellationToken);
        var scoreCount = await db.Scores
            .Where(score => score.ResponseSessionId == command.TargetId)
            .CountAsync(cancellationToken);
        var withdrawal = WithdrawalEvent.RequestResponseSession(
            PlatformIds.NewId(),
            tenantId,
            target.CampaignSeriesId,
            context.Value.RetentionPolicyId,
            command.TargetId,
            action.Value,
            requestedAt,
            consentRecordCount,
            responseSessionCount: 1,
            answerCount,
            scoreRunCount,
            scoreCount,
            BuildRequestMetadata(command.ActorUserId, reasonCode.Value));

        db.WithdrawalEvents.Add(withdrawal);
        AddWithdrawalRequestCreatedNotification(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToRequestResponse(withdrawal, command.TargetId, idempotent: false));
    }

    /// <summary>
    /// A person-level request (GDPR erasure/anonymization for an identified
    /// respondent) becomes one reviewable event per study where the person has
    /// identified sessions, because retention policies bind per series.
    /// </summary>
    private async Task<Result<WithdrawalRequestResponse>> CreateIdentifiedSubjectWithdrawalRequestAsync(
        Guid tenantId,
        CreateWithdrawalRequestCommand command,
        string action,
        string? reasonCode,
        DateTimeOffset requestedAt,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var subjectExists = await db.Subjects
            .AsNoTracking()
            .AnyAsync(subject => subject.Id == command.TargetId, cancellationToken);
        if (!subjectExists)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        var seriesIds = await (
                from session in db.ResponseSessions
                join assignment in db.Assignments on session.AssignmentId equals assignment.Id
                join campaign in db.Campaigns on assignment.CampaignId equals campaign.Id
                where assignment.RespondentSubjectId == command.TargetId &&
                    campaign.CampaignSeriesId != null
                select campaign.CampaignSeriesId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);
        if (seriesIds.Count == 0)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Validation(
                    "withdrawal_request.no_identified_data",
                    "This person has no identified response data to withdraw."));
        }

        WithdrawalEvent? first = null;
        var firstIdempotent = false;
        foreach (var campaignSeriesId in seriesIds)
        {
            var existing = await db.WithdrawalEvents
                .AsNoTracking()
                .Where(withdrawal =>
                    withdrawal.TargetKind == WithdrawalTargetKinds.IdentifiedSubject &&
                    withdrawal.SubjectId == command.TargetId &&
                    withdrawal.CampaignSeriesId == campaignSeriesId &&
                    withdrawal.ActionAfter == action &&
                    (withdrawal.Status == WithdrawalEventStatuses.Requested ||
                        withdrawal.Status == WithdrawalEventStatuses.Planned ||
                        withdrawal.Status == WithdrawalEventStatuses.Processing))
                .OrderBy(withdrawal => withdrawal.RequestedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null)
            {
                if (first is null)
                {
                    first = existing;
                    firstIdempotent = true;
                }

                continue;
            }

            var context = await LoadSeriesContextAsync(campaignSeriesId, requestedAt, cancellationToken);
            if (context.IsFailure)
            {
                return Result.Failure<WithdrawalRequestResponse>(context.Error);
            }

            var sessionIds = MatchingIdentifiedSessionIds(campaignSeriesId, command.TargetId);
            var consentRecordCount = await db.ConsentRecords
                .Where(record =>
                    record.SubjectId == command.TargetId &&
                    db.Campaigns.Any(campaign =>
                        campaign.Id == record.CampaignId &&
                        campaign.CampaignSeriesId == campaignSeriesId))
                .CountAsync(cancellationToken);
            var withdrawal = WithdrawalEvent.RequestIdentifiedSubject(
                PlatformIds.NewId(),
                tenantId,
                campaignSeriesId,
                context.Value.RetentionPolicyId,
                command.TargetId,
                action,
                requestedAt,
                consentRecordCount,
                await sessionIds.CountAsync(cancellationToken),
                await db.Answers.Where(answer => sessionIds.Contains(answer.SessionId)).CountAsync(cancellationToken),
                await db.ScoreRuns.Where(scoreRun => sessionIds.Contains(scoreRun.ResponseSessionId)).CountAsync(cancellationToken),
                await db.Scores.Where(score => sessionIds.Contains(score.ResponseSessionId)).CountAsync(cancellationToken),
                BuildRequestMetadata(command.ActorUserId, reasonCode));

            db.WithdrawalEvents.Add(withdrawal);
            AddWithdrawalRequestCreatedNotification(withdrawal);
            if (first is null)
            {
                first = withdrawal;
                firstIdempotent = false;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToRequestResponse(first!, command.TargetId, firstIdempotent));
    }

    public async Task<Result<WithdrawalRequestTokenIssueResponse>> IssueWithdrawalRequestTokenAsync(
        Guid tenantId,
        IssueWithdrawalRequestTokenCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ResponseSessionId == Guid.Empty)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(
                Error.Validation("withdrawal_token.target_required", "Withdrawal token target is required."));
        }

        var action = NormalizeRequestedAction(command.RequestedAction);
        if (action.IsFailure)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(action.Error);
        }

        var reasonCode = NormalizeReasonCode(command.ReasonCode);
        if (reasonCode.IsFailure)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(reasonCode.Error);
        }

        var now = DateTimeOffset.UtcNow;
        if (command.ExpiresAt <= now)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(
                Error.Validation("withdrawal_token.expiry_invalid", "Withdrawal token expiry must be in the future."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var target = await LoadResponseSessionRequestContextAsync(command.ResponseSessionId, cancellationToken);
        if (target is null)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        var context = await LoadSeriesContextAsync(target.CampaignSeriesId, now, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<WithdrawalRequestTokenIssueResponse>(context.Error);
        }

        var issued = WithdrawalRequestTokens.Issue(tenantId);
        var token = new WithdrawalRequestToken(
            PlatformIds.NewId(),
            tenantId,
            command.ResponseSessionId,
            issued.TokenHash,
            action.Value,
            command.ExpiresAt,
            reasonCode.Value);

        db.WithdrawalRequestTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new WithdrawalRequestTokenIssueResponse(
            token.Id,
            token.ResponseSessionId,
            token.RequestedAction,
            token.ExpiresAt,
            issued.RawToken));
    }

    public async Task<Result<WithdrawalRequestResponse>> CreateAnonymousWithdrawalRequestAsync(
        CreateAnonymousWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        var parsed = WithdrawalRequestTokens.ParseTenant(command.Token);
        if (parsed.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(parsed.Error);
        }

        var action = NormalizeRequestedAction(command.RequestedAction);
        if (action.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(action.Error);
        }

        var reasonCode = NormalizeReasonCode(command.ReasonCode);
        if (reasonCode.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(reasonCode.Error);
        }

        var tenantContextError = EnsureApplicationTenantContext(parsed.Value.TenantId);
        if (tenantContextError != Error.None)
        {
            return Result.Failure<WithdrawalRequestResponse>(tenantContextError);
        }

        var requestedAt = DateTimeOffset.UtcNow;
        var tokenHash = WithdrawalRequestTokens.Hash(command.Token);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);
        var token = await db.WithdrawalRequestTokens
            .SingleOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken);
        if (token is null)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Validation("withdrawal_token.invalid", "Withdrawal token is invalid."));
        }

        if (token.ConsumedAt.HasValue)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Conflict("withdrawal_token.consumed", "Withdrawal token has already been consumed."));
        }

        if (token.ExpiresAt <= requestedAt)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Conflict("withdrawal_token.expired", "Withdrawal token has expired."));
        }

        if (token.RequestedAction != action.Value)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.Validation("withdrawal_token.action_mismatch", "Withdrawal token action does not match the request."));
        }

        var target = await LoadResponseSessionRequestContextAsync(token.ResponseSessionId, cancellationToken);
        if (target is null)
        {
            return Result.Failure<WithdrawalRequestResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        var context = await LoadSeriesContextAsync(target.CampaignSeriesId, requestedAt, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<WithdrawalRequestResponse>(context.Error);
        }

        var existing = await db.WithdrawalEvents
            .AsNoTracking()
            .Where(withdrawal =>
                withdrawal.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                withdrawal.ResponseSessionId == token.ResponseSessionId &&
                withdrawal.ActionAfter == action.Value &&
                (withdrawal.Status == WithdrawalEventStatuses.Requested ||
                    withdrawal.Status == WithdrawalEventStatuses.Planned ||
                    withdrawal.Status == WithdrawalEventStatuses.Processing))
            .OrderBy(withdrawal => withdrawal.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            token.MarkConsumed(requestedAt);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success(ToRequestResponse(existing, token.ResponseSessionId, idempotent: true));
        }

        var consentRecordCount = target.ConsentRecordId.HasValue
            ? await db.ConsentRecords.CountAsync(record => record.Id == target.ConsentRecordId.Value, cancellationToken)
            : 0;
        var answerCount = await db.Answers
            .Where(answer => answer.SessionId == token.ResponseSessionId)
            .CountAsync(cancellationToken);
        var scoreRunCount = await db.ScoreRuns
            .Where(scoreRun => scoreRun.ResponseSessionId == token.ResponseSessionId)
            .CountAsync(cancellationToken);
        var scoreCount = await db.Scores
            .Where(score => score.ResponseSessionId == token.ResponseSessionId)
            .CountAsync(cancellationToken);
        var withdrawal = WithdrawalEvent.RequestResponseSession(
            PlatformIds.NewId(),
            parsed.Value.TenantId,
            target.CampaignSeriesId,
            context.Value.RetentionPolicyId,
            token.ResponseSessionId,
            action.Value,
            requestedAt,
            consentRecordCount,
            responseSessionCount: 1,
            answerCount,
            scoreRunCount,
            scoreCount,
            BuildPublicRequestMetadata(reasonCode.Value));

        db.WithdrawalEvents.Add(withdrawal);
        AddWithdrawalRequestCreatedNotification(withdrawal);
        token.MarkConsumed(requestedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToRequestResponse(withdrawal, token.ResponseSessionId, idempotent: false));
    }

    public async Task<Result<IReadOnlyList<WithdrawalRequestReviewResponse>>> ListWithdrawalRequestsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawals = await db.WithdrawalEvents
            .AsNoTracking()
            .Where(withdrawal =>
                (withdrawal.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                    withdrawal.ResponseSessionId != null) ||
                (withdrawal.TargetKind == WithdrawalTargetKinds.IdentifiedSubject &&
                    withdrawal.SubjectId != null))
            .OrderByDescending(withdrawal => withdrawal.RequestedAt)
            .ThenBy(withdrawal => withdrawal.Id)
            .Take(RequestReviewLimit)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = withdrawals
            .Select(ToReviewResponse)
            .ToArray();

        return Result.Success<IReadOnlyList<WithdrawalRequestReviewResponse>>(response);
    }

    public async Task<Result<WithdrawalRequestReviewResponse>> GetWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == requestId &&
                    ((entity.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                        entity.ResponseSessionId != null) ||
                    (entity.TargetKind == WithdrawalTargetKinds.IdentifiedSubject &&
                        entity.SubjectId != null)),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.NotFound("withdrawal_request.not_found", "Withdrawal request was not found."));
        }

        return Result.Success(ToReviewResponse(withdrawal));
    }

    public async Task<Result<WithdrawalRequestReviewResponse>> ApproveWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        WithdrawalRequestDecisionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Validation("withdrawal_request.actor_required", "Withdrawal request decision actor is required."));
        }

        var reasonCode = NormalizeReasonCode(command.ReasonCode);
        if (reasonCode.IsFailure)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(reasonCode.Error);
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == requestId &&
                    ((entity.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                        entity.ResponseSessionId != null) ||
                    (entity.TargetKind == WithdrawalTargetKinds.IdentifiedSubject &&
                        entity.SubjectId != null)),
                cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.NotFound("withdrawal_request.not_found", "Withdrawal request was not found."));
        }

        if (withdrawal.Status != WithdrawalEventStatuses.Requested)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Conflict("withdrawal_request.not_requested", "Withdrawal request is not pending decision."));
        }

        withdrawal.ApproveRequest(BuildDecisionMetadata(command.ActorUserId, "approved", reasonCode.Value));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToReviewResponse(withdrawal));
    }

    public async Task<Result<WithdrawalRequestReviewResponse>> DenyWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        WithdrawalRequestDecisionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Validation("withdrawal_request.actor_required", "Withdrawal request decision actor is required."));
        }

        var reasonCode = NormalizeReasonCode(command.ReasonCode);
        if (reasonCode.IsFailure)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(reasonCode.Error);
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == requestId &&
                    ((entity.TargetKind == WithdrawalTargetKinds.ResponseSession &&
                        entity.ResponseSessionId != null) ||
                    (entity.TargetKind == WithdrawalTargetKinds.IdentifiedSubject &&
                        entity.SubjectId != null)),
                cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.NotFound("withdrawal_request.not_found", "Withdrawal request was not found."));
        }

        if (withdrawal.Status != WithdrawalEventStatuses.Requested)
        {
            return Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Conflict("withdrawal_request.not_requested", "Withdrawal request is not pending decision."));
        }

        withdrawal.DenyRequest(
            DateTimeOffset.UtcNow,
            BuildDecisionMetadata(command.ActorUserId, "denied", reasonCode.Value));
        AddWithdrawalRequestTerminalNotification(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToReviewResponse(withdrawal));
    }

    public async Task<Result<WithdrawalEventResponse>> PlanIdentifiedWithdrawalAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var requestedAt = DateTimeOffset.UtcNow;
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var context = await LoadSeriesContextAsync(campaignSeriesId, requestedAt, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<WithdrawalEventResponse>(context.Error);
        }

        var subjectExists = await db.Subjects
            .AsNoTracking()
            .AnyAsync(subject => subject.Id == subjectId, cancellationToken);
        if (!subjectExists)
        {
            return Result.Failure<WithdrawalEventResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        var sessionIds = MatchingIdentifiedSessionIds(campaignSeriesId, subjectId);
        var consentRecordCount = await db.ConsentRecords
            .Where(record =>
                record.SubjectId == subjectId &&
                db.Campaigns.Any(campaign =>
                    campaign.Id == record.CampaignId &&
                    campaign.CampaignSeriesId == campaignSeriesId))
            .CountAsync(cancellationToken);
        var responseSessionCount = await sessionIds.CountAsync(cancellationToken);
        var answerCount = await db.Answers
            .Where(answer => sessionIds.Contains(answer.SessionId))
            .CountAsync(cancellationToken);
        var scoreRunCount = await db.ScoreRuns
            .Where(scoreRun => sessionIds.Contains(scoreRun.ResponseSessionId))
            .CountAsync(cancellationToken);
        var scoreCount = await db.Scores
            .Where(score => sessionIds.Contains(score.ResponseSessionId))
            .CountAsync(cancellationToken);
        var withdrawal = WithdrawalEvent.PlanIdentified(
            PlatformIds.NewId(),
            tenantId,
            campaignSeriesId,
            context.Value.RetentionPolicyId,
            subjectId,
            context.Value.ActionAfter,
            requestedAt,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount);

        db.WithdrawalEvents.Add(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(withdrawal, targetMatched: true));
    }

    public async Task<Result<WithdrawalEventResponse>> PlanAnonymousLongitudinalWithdrawalAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        string rawParticipantCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawParticipantCode))
        {
            return Result.Failure<WithdrawalEventResponse>(
                Error.Validation("participant_code.required", "Participant code is required."));
        }

        var requestedAt = DateTimeOffset.UtcNow;
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var context = await LoadSeriesContextAsync(campaignSeriesId, requestedAt, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<WithdrawalEventResponse>(context.Error);
        }

        var hash = await participantCodeHasher.HashAsync(
            rawParticipantCode,
            context.Value.CodeSalt,
            cancellationToken);
        var participantCode = await db.ParticipantCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                code => code.CampaignSeriesId == campaignSeriesId && code.Hash == hash.Hash,
                cancellationToken);

        WithdrawalEvent withdrawal;
        var targetMatched = participantCode is not null;
        if (participantCode is null)
        {
            withdrawal = WithdrawalEvent.PlanAnonymousLongitudinalUnmatched(
                PlatformIds.NewId(),
                tenantId,
                campaignSeriesId,
                context.Value.RetentionPolicyId,
                context.Value.ActionAfter,
                requestedAt);
        }
        else
        {
            var sessionIds = MatchingParticipantCodeSessionIds(campaignSeriesId, participantCode.Id);
            var consentRecordCount = await db.ConsentRecords
                .Where(record =>
                    db.ResponseSessions.Any(session =>
                        session.ConsentRecordId == record.Id &&
                        session.ParticipantCodeId == participantCode.Id))
                .CountAsync(cancellationToken);
            var responseSessionCount = await sessionIds.CountAsync(cancellationToken);
            var answerCount = await db.Answers
                .Where(answer => sessionIds.Contains(answer.SessionId))
                .CountAsync(cancellationToken);
            var scoreRunCount = await db.ScoreRuns
                .Where(scoreRun => sessionIds.Contains(scoreRun.ResponseSessionId))
                .CountAsync(cancellationToken);
            var scoreCount = await db.Scores
                .Where(score => sessionIds.Contains(score.ResponseSessionId))
                .CountAsync(cancellationToken);
            withdrawal = WithdrawalEvent.PlanAnonymousLongitudinal(
                PlatformIds.NewId(),
                tenantId,
                campaignSeriesId,
                context.Value.RetentionPolicyId,
                participantCode.Id,
                context.Value.ActionAfter,
                requestedAt,
                consentRecordCount,
                responseSessionCount,
                answerCount,
                scoreRunCount,
                scoreCount);
        }

        db.WithdrawalEvents.Add(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(withdrawal, targetMatched));
    }

    public async Task<Result<WithdrawalDryRunResponse>> DryRunWithdrawalAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalDryRunResponse>(
                Error.NotFound("withdrawal_event.not_found", "Withdrawal event was not found."));
        }

        if (withdrawal.Status != WithdrawalEventStatuses.Planned)
        {
            return Result.Failure<WithdrawalDryRunResponse>(
                Error.Conflict("withdrawal_event.not_planned", "Withdrawal event is not planned."));
        }

        var dryRun = await BuildDryRunGraphAsync(withdrawal, cancellationToken);
        if (dryRun.IsFailure)
        {
            return Result.Failure<WithdrawalDryRunResponse>(dryRun.Error);
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToDryRunResponse(withdrawal, dryRun.Value));
    }

    public async Task<Result<WithdrawalExecutionStateResponse>> ClaimWithdrawalForExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.NotFound("withdrawal_event.not_found", "Withdrawal event was not found."));
        }

        if (withdrawal.Status != WithdrawalEventStatuses.Planned)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.not_claimable", "Withdrawal event cannot be claimed."));
        }

        var dryRun = await BuildDryRunGraphAsync(withdrawal, cancellationToken);
        if (dryRun.IsFailure)
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"target_mismatch"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(dryRun.Error);
        }

        if (!MatchesPlannedCounts(withdrawal, dryRun.Value))
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"graph_changed"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.graph_changed", "Withdrawal event graph changed."));
        }

        withdrawal.MarkProcessing();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToExecutionStateResponse(withdrawal, dryRun.Value));
    }

    public async Task<Result<WithdrawalExecutionStateResponse>> CompleteWithdrawalExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.NotFound("withdrawal_event.not_found", "Withdrawal event was not found."));
        }

        if (withdrawal.Status != WithdrawalEventStatuses.Processing)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.not_completable", "Withdrawal event cannot be completed."));
        }

        var dryRun = await BuildDryRunGraphAsync(withdrawal, cancellationToken);
        if (dryRun.IsFailure)
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"target_mismatch"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(dryRun.Error);
        }

        if (!MatchesPlannedCounts(withdrawal, dryRun.Value))
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"graph_changed"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.graph_changed", "Withdrawal event graph changed."));
        }

        withdrawal.MarkCompleted(DateTimeOffset.UtcNow, """{"result":"dry_run_guarded_noop"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToExecutionStateResponse(withdrawal, dryRun.Value));
    }

    public async Task<Result<WithdrawalExecutionStateResponse>> FailWithdrawalExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.NotFound("withdrawal_event.not_found", "Withdrawal event was not found."));
        }

        if (withdrawal.Status is WithdrawalEventStatuses.Completed or WithdrawalEventStatuses.Failed)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.not_failable", "Withdrawal event cannot be failed."));
        }

        var dryRun = await BuildDryRunGraphAsync(withdrawal, cancellationToken);
        if (dryRun.IsFailure)
        {
            dryRun = Result.Success(new DryRunGraph(TargetMatched: false, 0, 0, 0, 0, 0));
        }

        withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"execution_failed"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToExecutionStateResponse(withdrawal, dryRun.Value));
    }

    public async Task<Result<WithdrawalExecutionStateResponse>> ExecuteWithdrawalAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.NotFound("withdrawal_event.not_found", "Withdrawal event was not found."));
        }

        if (withdrawal.Status is WithdrawalEventStatuses.Completed or WithdrawalEventStatuses.Failed ||
            withdrawal.Status is not (WithdrawalEventStatuses.Planned or WithdrawalEventStatuses.Processing))
        {
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.not_executable", "Withdrawal event cannot be executed."));
        }

        var mutationGraph = await BuildMutationGraphAsync(withdrawal, cancellationToken);
        if (mutationGraph.IsFailure)
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"target_mismatch"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(mutationGraph.Error);
        }

        var dryRun = mutationGraph.Value.DryRun;
        if (!MatchesPlannedCounts(withdrawal, dryRun))
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"graph_changed"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.graph_changed", "Withdrawal event graph changed."));
        }

        if (withdrawal.ActionAfter is not (RetentionPolicy.Delete or RetentionPolicy.Anonymize))
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"action_unsupported"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.action_unsupported", "Withdrawal action is not executable."));
        }

        if (withdrawal.Status == WithdrawalEventStatuses.Planned)
        {
            withdrawal.MarkProcessing();
        }

        var processedAt = DateTimeOffset.UtcNow;
        var completionResult = "deleted_graph";
        var deliveryIdentityScrubCounts = DeliveryIdentityScrubCounts.Empty;
        if (withdrawal.ActionAfter == RetentionPolicy.Delete)
        {
            db.Scores.RemoveRange(mutationGraph.Value.Scores);
            db.ScoreRuns.RemoveRange(mutationGraph.Value.ScoreRuns);
            db.Answers.RemoveRange(mutationGraph.Value.Answers);
            db.ResponseSessions.RemoveRange(mutationGraph.Value.ResponseSessions);
            db.ConsentRecords.RemoveRange(mutationGraph.Value.ConsentRecords);
            if (withdrawal.TargetKind == WithdrawalTargetKinds.ResponseSession)
            {
                withdrawal.DetachDeletedResponseSessionTarget();
            }
        }
        else
        {
            completionResult = "anonymized_graph";
            deliveryIdentityScrubCounts = ScrubDeliveryIdentity(mutationGraph.Value, processedAt);

            foreach (var assignment in mutationGraph.Value.Assignments)
            {
                assignment.Anonymize(processedAt);
            }

            foreach (var responseSession in mutationGraph.Value.ResponseSessions)
            {
                responseSession.Anonymize(processedAt);
            }

            foreach (var consentRecord in mutationGraph.Value.ConsentRecords)
            {
                consentRecord.Anonymize(processedAt);
            }
        }

        var invalidatedArtifactCount = InvalidateDerivedArtifacts(
            mutationGraph.Value.DerivedArtifacts,
            processedAt);
        withdrawal.MarkCompleted(
            processedAt,
            BuildCompletionMetadata(
                completionResult,
                invalidatedArtifactCount,
                deliveryIdentityScrubCounts));
        AddWithdrawalRequestTerminalNotification(withdrawal);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await MarkExecutionFailedAfterMutationSaveFailureAsync(
                tenantId,
                withdrawalEventId,
                cancellationToken);
            return Result.Failure<WithdrawalExecutionStateResponse>(
                Error.Conflict("withdrawal_event.mutation_failed", "Withdrawal mutation failed."));
        }

        return Result.Success(ToExecutionStateResponse(withdrawal, dryRun));
    }

    private async Task<Result<SeriesContext>> LoadSeriesContextAsync(
        Guid campaignSeriesId,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken)
    {
        var series = await db.CampaignSeries
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);
        if (series is null)
        {
            return Result.Failure<SeriesContext>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var retentionPolicy = await db.RetentionPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.CampaignSeriesId == campaignSeriesId &&
                policy.CreatedAt <= requestedAt &&
                (!policy.RetiredAt.HasValue || policy.RetiredAt.Value > requestedAt))
            .OrderByDescending(policy => policy.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (retentionPolicy is null)
        {
            return Result.Failure<SeriesContext>(
                Error.Conflict("retention_policy.missing", "Campaign series has no usable retention policy."));
        }

        return Result.Success(new SeriesContext(
            series.CodeSalt,
            retentionPolicy.Id,
            retentionPolicy.ActionAfter));
    }

    private async Task<ResponseSessionRequestContext?> LoadResponseSessionRequestContextAsync(
        Guid responseSessionId,
        CancellationToken cancellationToken)
    {
        return await (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking() on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns.AsNoTracking() on assignment.CampaignId equals campaign.Id
            where session.Id == responseSessionId && campaign.CampaignSeriesId.HasValue
            select new ResponseSessionRequestContext(campaign.CampaignSeriesId.GetValueOrDefault(), session.ConsentRecordId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Guid> MatchingIdentifiedSessionIds(Guid campaignSeriesId, Guid subjectId)
    {
        return
            from session in db.ResponseSessions
            join assignment in db.Assignments on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns on assignment.CampaignId equals campaign.Id
            where campaign.CampaignSeriesId == campaignSeriesId &&
                assignment.RespondentSubjectId == subjectId
            select session.Id;
    }

    private IQueryable<Guid> MatchingParticipantCodeSessionIds(Guid campaignSeriesId, Guid participantCodeId)
    {
        return
            from session in db.ResponseSessions
            join assignment in db.Assignments on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns on assignment.CampaignId equals campaign.Id
            where campaign.CampaignSeriesId == campaignSeriesId &&
                session.ParticipantCodeId == participantCodeId
            select session.Id;
    }

    private async Task<Result<DryRunGraph>> DryRunIdentifiedAsync(
        WithdrawalEvent withdrawal,
        CancellationToken cancellationToken)
    {
        if (!withdrawal.SubjectId.HasValue)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var subjectExists = await db.Subjects
            .AsNoTracking()
            .AnyAsync(subject => subject.Id == withdrawal.SubjectId.Value, cancellationToken);
        if (!subjectExists)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var sessionIds = MatchingIdentifiedSessionIds(withdrawal.CampaignSeriesId, withdrawal.SubjectId.Value);
        var consentRecordCount = await db.ConsentRecords
            .Where(record =>
                record.SubjectId == withdrawal.SubjectId.Value &&
                db.Campaigns.Any(campaign =>
                    campaign.Id == record.CampaignId &&
                    campaign.CampaignSeriesId == withdrawal.CampaignSeriesId))
            .CountAsync(cancellationToken);

        return Result.Success(await CountGraphAsync(
            sessionIds,
            consentRecordCount,
            targetMatched: true,
            cancellationToken));
    }

    private async Task<Result<DryRunGraph>> BuildDryRunGraphAsync(
        WithdrawalEvent withdrawal,
        CancellationToken cancellationToken)
    {
        return withdrawal.TargetKind switch
        {
            WithdrawalTargetKinds.IdentifiedSubject => await DryRunIdentifiedAsync(withdrawal, cancellationToken),
            WithdrawalTargetKinds.AnonymousLongitudinalCode => await DryRunAnonymousLongitudinalAsync(withdrawal, cancellationToken),
            WithdrawalTargetKinds.AnonymousLongitudinalUnmatched => Result.Success(new DryRunGraph(TargetMatched: false, 0, 0, 0, 0, 0)),
            WithdrawalTargetKinds.ResponseSession => await DryRunResponseSessionAsync(withdrawal, cancellationToken),
            _ => Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."))
        };
    }

    private async Task<Result<MutationGraph>> BuildMutationGraphAsync(
        WithdrawalEvent withdrawal,
        CancellationToken cancellationToken)
    {
        var dryRun = await BuildDryRunGraphAsync(withdrawal, cancellationToken);
        if (dryRun.IsFailure)
        {
            return Result.Failure<MutationGraph>(dryRun.Error);
        }

        IQueryable<Guid> sessionIds = withdrawal.TargetKind switch
        {
            WithdrawalTargetKinds.IdentifiedSubject when withdrawal.SubjectId.HasValue =>
                MatchingIdentifiedSessionIds(withdrawal.CampaignSeriesId, withdrawal.SubjectId.Value),
            WithdrawalTargetKinds.AnonymousLongitudinalCode when withdrawal.ParticipantCodeId.HasValue =>
                MatchingParticipantCodeSessionIds(withdrawal.CampaignSeriesId, withdrawal.ParticipantCodeId.Value),
            WithdrawalTargetKinds.ResponseSession when withdrawal.ResponseSessionId.HasValue =>
                db.ResponseSessions
                    .Where(session => session.Id == withdrawal.ResponseSessionId.Value)
                    .Select(session => session.Id),
            WithdrawalTargetKinds.AnonymousLongitudinalUnmatched =>
                db.ResponseSessions.Where(_ => false).Select(session => session.Id),
            _ => db.ResponseSessions.Where(_ => false).Select(session => session.Id)
        };

        var responseSessions = await db.ResponseSessions
            .Where(session => sessionIds.Contains(session.Id))
            .ToListAsync(cancellationToken);
        var responseSessionIds = responseSessions.Select(session => session.Id).ToArray();

        var consentRecordIds = withdrawal.TargetKind switch
        {
            WithdrawalTargetKinds.IdentifiedSubject when withdrawal.SubjectId.HasValue =>
                await db.ConsentRecords
                    .Where(record =>
                        record.SubjectId == withdrawal.SubjectId.Value &&
                        db.Campaigns.Any(campaign =>
                            campaign.Id == record.CampaignId &&
                            campaign.CampaignSeriesId == withdrawal.CampaignSeriesId))
                    .Select(record => record.Id)
                    .ToListAsync(cancellationToken),
            WithdrawalTargetKinds.AnonymousLongitudinalCode =>
                responseSessions
                    .Where(session => session.ConsentRecordId.HasValue)
                    .Select(session => session.ConsentRecordId!.Value)
                    .Distinct()
                    .ToList(),
            WithdrawalTargetKinds.ResponseSession =>
                responseSessions
                    .Where(session => session.ConsentRecordId.HasValue)
                    .Select(session => session.ConsentRecordId!.Value)
                    .Distinct()
                    .ToList(),
            _ => []
        };

        var consentRecords = await db.ConsentRecords
            .Where(record => consentRecordIds.Contains(record.Id))
            .ToListAsync(cancellationToken);
        var assignmentIds = responseSessions.Select(session => session.AssignmentId)
            .Concat(consentRecords.Select(record => record.AssignmentId))
            .Distinct()
            .ToArray();
        var assignments = await db.Assignments
            .Where(assignment => assignmentIds.Contains(assignment.Id))
            .ToListAsync(cancellationToken);
        var campaignIds = assignments
            .Select(assignment => assignment.CampaignId)
            .Distinct()
            .ToArray();
        var derivedArtifacts = await db.ExportArtifacts
            .Where(artifact =>
                campaignIds.Length > 0 &&
                artifact.Status == ExportArtifactStatuses.Succeeded &&
                artifact.DeletedAt == null &&
                ((artifact.TargetKind == ExportArtifactTargetKinds.Campaign &&
                    artifact.CampaignId.HasValue &&
                    campaignIds.Contains(artifact.CampaignId.Value)) ||
                    (artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                        artifact.CampaignSeriesId == withdrawal.CampaignSeriesId)))
            .ToListAsync(cancellationToken);
        var notifications = await db.Notifications
            .Where(notification => assignmentIds.Contains(notification.AssignmentId))
            .ToListAsync(cancellationToken);
        var notificationIds = notifications
            .Select(notification => notification.Id)
            .ToArray();
        var notificationDeliveryAttempts = await db.NotificationDeliveryAttempts
            .Where(attempt => notificationIds.Contains(attempt.NotificationId))
            .ToListAsync(cancellationToken);
        var assignmentInviteTokenIds = assignments
            .Where(assignment => assignment.InviteTokenId.HasValue)
            .Select(assignment => assignment.InviteTokenId!.Value)
            .Distinct()
            .ToArray();
        var invitationTokens = await db.InvitationTokens
            .Where(token =>
                (token.AssignmentId.HasValue && assignmentIds.Contains(token.AssignmentId.Value)) ||
                assignmentInviteTokenIds.Contains(token.Id))
            .ToListAsync(cancellationToken);
        var answers = await db.Answers
            .Where(answer => responseSessionIds.Contains(answer.SessionId))
            .ToListAsync(cancellationToken);
        var scoreRuns = await db.ScoreRuns
            .Where(scoreRun => responseSessionIds.Contains(scoreRun.ResponseSessionId))
            .ToListAsync(cancellationToken);
        var scoreRunIds = scoreRuns.Select(scoreRun => scoreRun.Id).ToArray();
        var scores = await db.Scores
            .Where(score =>
                responseSessionIds.Contains(score.ResponseSessionId) ||
                scoreRunIds.Contains(score.ScoreRunId))
            .ToListAsync(cancellationToken);

        return Result.Success(new MutationGraph(
            dryRun.Value,
            consentRecords,
            responseSessions,
            answers,
            scoreRuns,
            scores,
            assignments,
            derivedArtifacts,
            notifications,
            notificationDeliveryAttempts,
            invitationTokens));
    }

    private async Task<Result<DryRunGraph>> DryRunAnonymousLongitudinalAsync(
        WithdrawalEvent withdrawal,
        CancellationToken cancellationToken)
    {
        if (!withdrawal.ParticipantCodeId.HasValue)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var participantCode = await db.ParticipantCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(code => code.Id == withdrawal.ParticipantCodeId.Value, cancellationToken);
        if (participantCode is null || participantCode.CampaignSeriesId != withdrawal.CampaignSeriesId)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var sessionIds = MatchingParticipantCodeSessionIds(
            withdrawal.CampaignSeriesId,
            withdrawal.ParticipantCodeId.Value);
        var consentRecordCount = await db.ConsentRecords
            .Where(record =>
                db.ResponseSessions.Any(session =>
                    session.ConsentRecordId == record.Id &&
                    session.ParticipantCodeId == withdrawal.ParticipantCodeId.Value))
            .CountAsync(cancellationToken);

        return Result.Success(await CountGraphAsync(
            sessionIds,
            consentRecordCount,
            targetMatched: true,
            cancellationToken));
    }

    private async Task<Result<DryRunGraph>> DryRunResponseSessionAsync(
        WithdrawalEvent withdrawal,
        CancellationToken cancellationToken)
    {
        if (!withdrawal.ResponseSessionId.HasValue)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var target = await (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking() on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns.AsNoTracking() on assignment.CampaignId equals campaign.Id
            where session.Id == withdrawal.ResponseSessionId.Value &&
                campaign.CampaignSeriesId == withdrawal.CampaignSeriesId
            select new
            {
                session.Id,
                session.ConsentRecordId
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return Result.Failure<DryRunGraph>(
                Error.Conflict("withdrawal_event.target_mismatch", "Withdrawal event target is invalid."));
        }

        var sessionIds = db.ResponseSessions
            .Where(session => session.Id == target.Id)
            .Select(session => session.Id);
        var consentRecordCount = target.ConsentRecordId.HasValue
            ? await db.ConsentRecords.CountAsync(
                record => record.Id == target.ConsentRecordId.Value,
                cancellationToken)
            : 0;

        return Result.Success(await CountGraphAsync(
            sessionIds,
            consentRecordCount,
            targetMatched: true,
            cancellationToken));
    }

    private async Task<DryRunGraph> CountGraphAsync(
        IQueryable<Guid> sessionIds,
        int consentRecordCount,
        bool targetMatched,
        CancellationToken cancellationToken)
    {
        var responseSessionCount = await sessionIds.CountAsync(cancellationToken);
        var answerCount = await db.Answers
            .Where(answer => sessionIds.Contains(answer.SessionId))
            .CountAsync(cancellationToken);
        var scoreRunCount = await db.ScoreRuns
            .Where(scoreRun => sessionIds.Contains(scoreRun.ResponseSessionId))
            .CountAsync(cancellationToken);
        var scoreCount = await db.Scores
            .Where(score => sessionIds.Contains(score.ResponseSessionId))
            .CountAsync(cancellationToken);

        return new DryRunGraph(
            targetMatched,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount);
    }

    private static WithdrawalEventResponse ToResponse(WithdrawalEvent withdrawal, bool targetMatched)
    {
        return new WithdrawalEventResponse(
            withdrawal.Id,
            withdrawal.CampaignSeriesId,
            withdrawal.TargetKind,
            withdrawal.Scope,
            withdrawal.ActionAfter,
            withdrawal.Status,
            targetMatched,
            withdrawal.RequestedAt,
            withdrawal.ConsentRecordCount,
            withdrawal.ResponseSessionCount,
            withdrawal.AnswerCount,
            withdrawal.ScoreRunCount,
            withdrawal.ScoreCount);
    }

    private static WithdrawalRequestResponse ToRequestResponse(
        WithdrawalEvent withdrawal,
        Guid targetId,
        bool idempotent)
    {
        return new WithdrawalRequestResponse(
            withdrawal.Id,
            withdrawal.TargetKind,
            targetId,
            withdrawal.ActionAfter,
            withdrawal.Status,
            withdrawal.RequestedAt,
            idempotent,
            withdrawal.ConsentRecordCount,
            withdrawal.ResponseSessionCount,
            withdrawal.AnswerCount,
            withdrawal.ScoreRunCount,
            withdrawal.ScoreCount);
    }

    private void AddWithdrawalRequestCreatedNotification(WithdrawalEvent withdrawal)
    {
        var payloadJson = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                withdrawalRequestId = withdrawal.Id,
                targetKind = withdrawal.TargetKind,
                requestedAction = withdrawal.ActionAfter,
                status = withdrawal.Status
            },
            JsonOptions);

        db.OperationalNotifications.Add(OperationalNotification.CreateWithdrawalRequestCreated(
            PlatformIds.NewId(),
            withdrawal.TenantId,
            withdrawal.Id,
            payloadJson,
            withdrawal.RequestedAt));
    }

    private void AddWithdrawalRequestTerminalNotification(WithdrawalEvent withdrawal)
    {
        if (withdrawal.Status is not (
            WithdrawalEventStatuses.Denied or
            WithdrawalEventStatuses.Completed or
            WithdrawalEventStatuses.Failed))
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                withdrawalRequestId = withdrawal.Id,
                targetKind = withdrawal.TargetKind,
                requestedAction = withdrawal.ActionAfter,
                status = withdrawal.Status
            },
            JsonOptions);

        db.OperationalNotifications.Add(OperationalNotification.CreateWithdrawalRequestTerminal(
            PlatformIds.NewId(),
            withdrawal.TenantId,
            withdrawal.Id,
            withdrawal.Status,
            payloadJson,
            withdrawal.ProcessedAt ?? DateTimeOffset.UtcNow));
    }

    private static WithdrawalRequestReviewResponse ToReviewResponse(WithdrawalEvent withdrawal)
    {
        var canDecide = withdrawal.Status == WithdrawalEventStatuses.Requested;
        var canExecute = withdrawal.Status is WithdrawalEventStatuses.Planned or WithdrawalEventStatuses.Processing;

        return new WithdrawalRequestReviewResponse(
            withdrawal.Id,
            withdrawal.TargetKind,
            withdrawal.TargetKind == WithdrawalTargetKinds.IdentifiedSubject
                ? withdrawal.SubjectId!.Value
                : withdrawal.ResponseSessionId!.Value,
            withdrawal.ActionAfter,
            withdrawal.Status,
            withdrawal.RequestedAt,
            withdrawal.ProcessedAt,
            withdrawal.ConsentRecordCount,
            withdrawal.ResponseSessionCount,
            withdrawal.AnswerCount,
            withdrawal.ScoreRunCount,
            withdrawal.ScoreCount,
            CanApprove: canDecide,
            CanDeny: canDecide,
            CanExecute: canExecute);
    }

    private static WithdrawalDryRunResponse ToDryRunResponse(
        WithdrawalEvent withdrawal,
        DryRunGraph dryRun)
    {
        var dependencies = new WithdrawalDryRunDependency[]
        {
            new(WithdrawalDryRunDependencyEntities.ConsentRecord, dryRun.ConsentRecordCount),
            new(WithdrawalDryRunDependencyEntities.ResponseSession, dryRun.ResponseSessionCount),
            new(WithdrawalDryRunDependencyEntities.Answer, dryRun.AnswerCount),
            new(WithdrawalDryRunDependencyEntities.ScoreRun, dryRun.ScoreRunCount),
            new(WithdrawalDryRunDependencyEntities.Score, dryRun.ScoreCount)
        };

        return new WithdrawalDryRunResponse(
            withdrawal.Id,
            withdrawal.CampaignSeriesId,
            withdrawal.TargetKind,
            withdrawal.Scope,
            withdrawal.ActionAfter,
            withdrawal.Status,
            dryRun.TargetMatched,
            dryRun.ConsentRecordCount,
            dryRun.ResponseSessionCount,
            dryRun.AnswerCount,
            dryRun.ScoreRunCount,
            dryRun.ScoreCount,
            dependencies);
    }

    private static WithdrawalExecutionStateResponse ToExecutionStateResponse(
        WithdrawalEvent withdrawal,
        DryRunGraph dryRun)
    {
        return new WithdrawalExecutionStateResponse(
            withdrawal.Id,
            withdrawal.Status,
            withdrawal.ProcessedAt,
            ToDryRunResponse(withdrawal, dryRun));
    }

    private static bool MatchesPlannedCounts(WithdrawalEvent withdrawal, DryRunGraph dryRun)
    {
        return withdrawal.ConsentRecordCount == dryRun.ConsentRecordCount &&
            withdrawal.ResponseSessionCount == dryRun.ResponseSessionCount &&
            withdrawal.AnswerCount == dryRun.AnswerCount &&
            withdrawal.ScoreRunCount == dryRun.ScoreRunCount &&
            withdrawal.ScoreCount == dryRun.ScoreCount;
    }

    private async Task MarkExecutionFailedAfterMutationSaveFailureAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var withdrawal = await db.WithdrawalEvents
            .SingleOrDefaultAsync(entity => entity.Id == withdrawalEventId, cancellationToken);
        if (withdrawal is null ||
            withdrawal.Status is WithdrawalEventStatuses.Completed or WithdrawalEventStatuses.Failed or WithdrawalEventStatuses.Denied)
        {
            return;
        }

        withdrawal.MarkFailed(DateTimeOffset.UtcNow, """{"failure_code":"mutation_failed"}""");
        AddWithdrawalRequestTerminalNotification(withdrawal);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static int InvalidateDerivedArtifacts(
        IReadOnlyList<ExportArtifact> artifacts,
        DateTimeOffset processedAt)
    {
        var invalidatedCount = 0;
        foreach (var artifact in artifacts)
        {
            if (artifact.InvalidateForWithdrawal(processedAt))
            {
                invalidatedCount++;
            }
        }

        return invalidatedCount;
    }

    private static DeliveryIdentityScrubCounts ScrubDeliveryIdentity(
        MutationGraph mutationGraph,
        DateTimeOffset processedAt)
    {
        foreach (var notification in mutationGraph.Notifications)
        {
            notification.ScrubForWithdrawal(processedAt);
        }

        foreach (var deliveryAttempt in mutationGraph.NotificationDeliveryAttempts)
        {
            deliveryAttempt.ScrubForWithdrawal();
        }

        foreach (var invitationToken in mutationGraph.InvitationTokens)
        {
            invitationToken.ScrubForWithdrawal(processedAt);
        }

        return new DeliveryIdentityScrubCounts(
            mutationGraph.Notifications.Count,
            mutationGraph.NotificationDeliveryAttempts.Count,
            mutationGraph.InvitationTokens.Count);
    }

    private static string BuildCompletionMetadata(
        string result,
        int invalidatedArtifactCount,
        DeliveryIdentityScrubCounts deliveryIdentityScrubCounts)
    {
        return $$"""{"result":"{{result}}","artifact_invalidated_count":{{invalidatedArtifactCount}},"notice_scrubbed_count":{{deliveryIdentityScrubCounts.NoticeCount}},"delivery_attempt_scrubbed_count":{{deliveryIdentityScrubCounts.DeliveryAttemptCount}},"invite_credential_scrubbed_count":{{deliveryIdentityScrubCounts.InviteCredentialCount}}}""";
    }

    private static Result<string> NormalizeRequestedAction(string value)
    {
        var normalized = value.Trim();
        if (normalized is not (RetentionPolicy.Delete or RetentionPolicy.Anonymize))
        {
            return Result.Failure<string>(
                Error.Validation(
                    "withdrawal_request.action_unsupported",
                    "Withdrawal request action is not supported."));
        }

        return Result.Success(normalized);
    }

    private Error EnsureApplicationTenantContext(Guid tenantId)
    {
        if (currentTenant is null)
        {
            return Error.None;
        }

        if (currentTenant.HasTenant)
        {
            return currentTenant.TenantId == tenantId
                ? Error.None
                : Error.Forbidden("tenant.mismatch", "Tenant context does not match withdrawal token tenant.");
        }

        currentTenant.SetTenant(tenantId, "withdrawal_token");
        return Error.None;
    }

    private static Result<string?> NormalizeReasonCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(null);
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 64 || normalized.Any(character => !IsSafeReasonCodeCharacter(character)))
        {
            return Result.Failure<string?>(
                Error.Validation(
                    "withdrawal_request.reason_code_invalid",
                    "Withdrawal request reason code is invalid."));
        }

        return Result.Success<string?>(normalized);
    }

    private static bool IsSafeReasonCodeCharacter(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.';
    }

    private static string BuildRequestMetadata(Guid? actorUserId, string? reasonCode)
    {
        var metadata = "{\"source\":\"tenant_admin\",\"intake\":\"request\"";
        if (actorUserId.HasValue)
        {
            metadata += $",\"actor_user_id\":\"{actorUserId.Value:D}\"";
        }

        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            metadata += $",\"reason_code\":\"{reasonCode}\"";
        }

        return metadata + "}";
    }

    private static string BuildPublicRequestMetadata(string? reasonCode)
    {
        var metadata = "{\"source\":\"public\",\"intake\":\"request\"";
        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            metadata += $",\"reason_code\":\"{reasonCode}\"";
        }

        return metadata + "}";
    }

    private static string BuildDecisionMetadata(Guid actorUserId, string decision, string? reasonCode)
    {
        var metadata = $"{{\"source\":\"tenant_admin\",\"decision\":\"{decision}\",\"actor_user_id\":\"{actorUserId:D}\"";
        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            metadata += $",\"reason_code\":\"{reasonCode}\"";
        }

        return metadata + "}";
    }

    private sealed record SeriesContext(
        byte[] CodeSalt,
        Guid RetentionPolicyId,
        string ActionAfter);

    private sealed record ResponseSessionRequestContext(
        Guid CampaignSeriesId,
        Guid? ConsentRecordId);

    private sealed record DryRunGraph(
        bool TargetMatched,
        int ConsentRecordCount,
        int ResponseSessionCount,
        int AnswerCount,
        int ScoreRunCount,
        int ScoreCount);

    private sealed record DeliveryIdentityScrubCounts(
        int NoticeCount,
        int DeliveryAttemptCount,
        int InviteCredentialCount)
    {
        public static DeliveryIdentityScrubCounts Empty { get; } = new(0, 0, 0);
    }

    private sealed record MutationGraph(
        DryRunGraph DryRun,
        IReadOnlyList<ConsentRecord> ConsentRecords,
        IReadOnlyList<ResponseSession> ResponseSessions,
        IReadOnlyList<Answer> Answers,
        IReadOnlyList<ScoreRun> ScoreRuns,
        IReadOnlyList<Score> Scores,
        IReadOnlyList<Assignment> Assignments,
        IReadOnlyList<ExportArtifact> DerivedArtifacts,
        IReadOnlyList<Notification> Notifications,
        IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts,
        IReadOnlyList<InvitationToken> InvitationTokens);
}
