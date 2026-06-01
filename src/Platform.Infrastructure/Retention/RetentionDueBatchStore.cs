using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Retention;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Retention;

public sealed class RetentionDueBatchStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IRetentionDueCandidateStore dueCandidateStore)
    : IRetentionDueBatchStore
{
    public async Task<Result<RetentionDueBatchResponse>> PlanDueBatchAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var plan = await dueCandidateStore.PlanDueCandidatesAsync(
            tenantId,
            campaignSeriesId,
            asOf,
            cancellationToken);
        if (plan.IsFailure)
        {
            return Result.Failure<RetentionDueBatchResponse>(plan.Error);
        }

        var batch = plan.Value.Batches.SingleOrDefault();
        if (batch is null)
        {
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.no_candidates", "Retention due plan has no candidates."));
        }

        if (batch.Status != RetentionDueCandidateStatuses.Ready || !batch.DueBefore.HasValue)
        {
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.policy_unsupported", "Retention due plan is not executable."));
        }

        if (batch.ResponseSessionCount <= 0)
        {
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.no_candidates", "Retention due plan has no candidates."));
        }

        var idempotencyKey = RetentionDueBatch.CreateIdempotencyKey(
            batch.CampaignSeriesId,
            batch.RetentionPolicyId,
            batch.Anchor,
            batch.ActionAfter,
            plan.Value.AsOf,
            batch.DueBefore.Value);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var existing = await db.RetentionDueBatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                dueBatch => dueBatch.TenantId == tenantId && dueBatch.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result.Success(ToResponse(existing));
        }

        var dueBatch = RetentionDueBatch.Plan(
            PlatformIds.NewId(),
            tenantId,
            batch.CampaignSeriesId,
            batch.RetentionPolicyId,
            batch.Anchor,
            batch.ActionAfter,
            plan.Value.AsOf,
            batch.DueBefore.Value,
            batch.ConsentRecordCount,
            batch.ResponseSessionCount,
            batch.AnswerCount,
            batch.ScoreRunCount,
            batch.ScoreCount,
            batch.DerivedArtifactCount,
            idempotencyKey,
            DateTimeOffset.UtcNow);

        db.RetentionDueBatches.Add(dueBatch);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(dueBatch));
    }

    public async Task<Result<RetentionDueBatchDryRunResponse>> DryRunDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken)
    {
        var batchResult = await LoadBatchAsync(tenantId, dueBatchId, cancellationToken);
        if (batchResult.IsFailure)
        {
            return Result.Failure<RetentionDueBatchDryRunResponse>(batchResult.Error);
        }

        var mismatches = await BuildParityMismatchesAsync(tenantId, batchResult.Value, cancellationToken);
        return Result.Success(new RetentionDueBatchDryRunResponse(
            ToResponse(batchResult.Value),
            mismatches.Count == 0,
            mismatches));
    }

    public async Task<Result<RetentionDueBatchResponse>> ClaimDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        DateTimeOffset processingStartedAt,
        CancellationToken cancellationToken)
    {
        var dryRun = await DryRunDueBatchAsync(tenantId, dueBatchId, cancellationToken);
        if (dryRun.IsFailure)
        {
            return Result.Failure<RetentionDueBatchResponse>(dryRun.Error);
        }

        if (!dryRun.Value.ParityMatched)
        {
            await MarkFailedAsync(
                tenantId,
                dueBatchId,
                "retention_due_batch.parity_mismatch",
                "Retention due-batch parity mismatch.",
                processingStartedAt,
                cancellationToken);
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.parity_mismatch", "Retention due-batch no longer matches its planned counts."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await LoadBatchForUpdateAsync(dueBatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure<RetentionDueBatchResponse>(DueBatchNotFound());
        }

        if (batch.Status != RetentionDueBatchStatuses.Planned)
        {
            return Result.Failure<RetentionDueBatchResponse>(DueBatchNotPlanned());
        }

        batch.Claim(processingStartedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(batch));
    }

    public async Task<Result<RetentionDueBatchResponse>> CompleteDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await LoadBatchForUpdateAsync(dueBatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure<RetentionDueBatchResponse>(DueBatchNotFound());
        }

        if (batch.Status != RetentionDueBatchStatuses.Processing)
        {
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.not_processing", "Retention due-batch is not processing."));
        }

        batch.Complete(completedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(batch));
    }

    public async Task<Result<RetentionDueBatchResponse>> FailDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        string failureCode,
        string? failureDetail,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await LoadBatchForUpdateAsync(dueBatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure<RetentionDueBatchResponse>(DueBatchNotFound());
        }

        if (batch.Status is RetentionDueBatchStatuses.Completed or RetentionDueBatchStatuses.Failed)
        {
            return Result.Failure<RetentionDueBatchResponse>(
                Error.Conflict("retention_due_batch.terminal", "Retention due-batch is terminal."));
        }

        batch.Fail(failureCode, failureDetail, failedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(batch));
    }

    public async Task<Result<RetentionDueBatchExecutionResponse>> ExecuteDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken)
    {
        var dryRun = await DryRunDueBatchAsync(tenantId, dueBatchId, cancellationToken);
        if (dryRun.IsFailure)
        {
            return Result.Failure<RetentionDueBatchExecutionResponse>(dryRun.Error);
        }

        if (!dryRun.Value.ParityMatched)
        {
            await MarkFailedAsync(
                tenantId,
                dueBatchId,
                "retention_due_batch.parity_mismatch",
                "Retention due-batch parity mismatch.",
                DateTimeOffset.UtcNow,
                cancellationToken);
            return Result.Failure<RetentionDueBatchExecutionResponse>(
                Error.Conflict("retention_due_batch.parity_mismatch", "Retention due-batch no longer matches its planned counts."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await LoadBatchForUpdateAsync(dueBatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure<RetentionDueBatchExecutionResponse>(DueBatchNotFound());
        }

        if (batch.Status != RetentionDueBatchStatuses.Processing)
        {
            return Result.Failure<RetentionDueBatchExecutionResponse>(
                Error.Conflict("retention_due_batch.not_executable", "Retention due-batch cannot be executed."));
        }

        var mutationGraph = await BuildDueBatchMutationGraphAsync(batch, cancellationToken);
        if (!MatchesPlannedCounts(batch, mutationGraph))
        {
            batch.Fail(
                "retention_due_batch.parity_mismatch",
                "Retention due-batch parity mismatch.",
                DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Failure<RetentionDueBatchExecutionResponse>(
                Error.Conflict("retention_due_batch.parity_mismatch", "Retention due-batch no longer matches its planned counts."));
        }

        var processedAt = DateTimeOffset.UtcNow;
        var result = batch.ActionAfter == RetentionPolicy.Delete
            ? "deleted_graph"
            : "anonymized_graph";
        var deliveryIdentityScrubCounts = DeliveryIdentityScrubCounts.Empty;

        if (batch.ActionAfter == RetentionPolicy.Delete)
        {
            db.Scores.RemoveRange(mutationGraph.Scores);
            db.ScoreRuns.RemoveRange(mutationGraph.ScoreRuns);
            db.Answers.RemoveRange(mutationGraph.Answers);
            db.ResponseSessions.RemoveRange(mutationGraph.ResponseSessions);
            db.ConsentRecords.RemoveRange(mutationGraph.ConsentRecords);
        }
        else
        {
            deliveryIdentityScrubCounts = ScrubDeliveryIdentity(mutationGraph, processedAt);

            foreach (var assignment in mutationGraph.Assignments)
            {
                assignment.Anonymize(processedAt);
            }

            foreach (var responseSession in mutationGraph.ResponseSessions)
            {
                responseSession.Anonymize(processedAt);
            }

            foreach (var consentRecord in mutationGraph.ConsentRecords)
            {
                consentRecord.Anonymize(processedAt);
            }
        }

        var invalidatedArtifactCount = InvalidateDerivedArtifacts(
            mutationGraph.DerivedArtifacts,
            processedAt);
        batch.Complete(
            processedAt,
            result,
            invalidatedArtifactCount,
            deliveryIdentityScrubCounts.NoticeCount,
            deliveryIdentityScrubCounts.DeliveryAttemptCount,
            deliveryIdentityScrubCounts.InviteCredentialCount);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await MarkDueBatchFailedAfterMutationSaveFailureAsync(tenantId, dueBatchId, cancellationToken);
            return Result.Failure<RetentionDueBatchExecutionResponse>(
                Error.Conflict("retention_due_batch.mutation_failed", "Retention due-batch mutation failed."));
        }

        return Result.Success(ToExecutionResponse(batch, mutationGraph, result, invalidatedArtifactCount, deliveryIdentityScrubCounts));
    }

    public async Task<Result<RetentionDueBatchAutomationRunResponse>> RunDueBatchAutomationAsync(
        Guid tenantId,
        DateTimeOffset asOf,
        int maxBatches,
        CancellationToken cancellationToken)
    {
        if (maxBatches <= 0)
        {
            return Result.Failure<RetentionDueBatchAutomationRunResponse>(
                Error.Conflict("retention_due_batch_automation.invalid_max_batches", "Retention due-batch automation requires a positive max batch count."));
        }

        var cappedMaxBatches = Math.Min(maxBatches, 100);
        var campaignSeriesIds = await LoadRetentionCampaignSeriesIdsAsync(tenantId, asOf, cancellationToken);
        var items = new List<RetentionDueBatchAutomationItemResponse>();
        var dueBatchCount = 0;
        var claimedBatchCount = 0;
        var completedBatchCount = 0;
        var failedBatchCount = 0;
        var noCandidateSeriesCount = 0;
        var skippedBatchCount = 0;

        foreach (var campaignSeriesId in campaignSeriesIds)
        {
            if (completedBatchCount + failedBatchCount >= cappedMaxBatches)
            {
                skippedBatchCount++;
                items.Add(new RetentionDueBatchAutomationItemResponse(
                    campaignSeriesId,
                    DueBatchId: null,
                    Stage: "max_batches_reached",
                    Status: "skipped",
                    Result: null,
                    ErrorCode: null));
                continue;
            }

            var planned = await PlanDueBatchAsync(tenantId, campaignSeriesId, asOf, cancellationToken);
            if (planned.IsFailure)
            {
                if (planned.Error.Code == "retention_due_batch.no_candidates")
                {
                    noCandidateSeriesCount++;
                    items.Add(new RetentionDueBatchAutomationItemResponse(
                        campaignSeriesId,
                        DueBatchId: null,
                        Stage: "no_candidates",
                        Status: "skipped",
                        Result: null,
                        ErrorCode: planned.Error.Code));
                    continue;
                }

                skippedBatchCount++;
                items.Add(new RetentionDueBatchAutomationItemResponse(
                    campaignSeriesId,
                    DueBatchId: null,
                    Stage: "plan_failed",
                    Status: "skipped",
                    Result: null,
                    ErrorCode: planned.Error.Code));
                continue;
            }

            var batch = planned.Value;
            if (batch.Status == RetentionDueBatchStatuses.Planned)
            {
                dueBatchCount++;
                var claimed = await ClaimDueBatchAsync(
                    tenantId,
                    batch.Id,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
                if (claimed.IsFailure)
                {
                    failedBatchCount++;
                    items.Add(new RetentionDueBatchAutomationItemResponse(
                        campaignSeriesId,
                        batch.Id,
                        Stage: "claim_failed",
                        Status: "failed",
                        Result: null,
                        ErrorCode: claimed.Error.Code));
                    continue;
                }

                claimedBatchCount++;
                batch = claimed.Value;
            }
            else if (batch.Status == RetentionDueBatchStatuses.Processing)
            {
                dueBatchCount++;
            }
            else
            {
                skippedBatchCount++;
                items.Add(new RetentionDueBatchAutomationItemResponse(
                    campaignSeriesId,
                    batch.Id,
                    Stage: "terminal",
                    Status: "skipped",
                    Result: null,
                    ErrorCode: null));
                continue;
            }

            var executed = await ExecuteDueBatchAsync(tenantId, batch.Id, cancellationToken);
            if (executed.IsFailure)
            {
                failedBatchCount++;
                items.Add(new RetentionDueBatchAutomationItemResponse(
                    campaignSeriesId,
                    batch.Id,
                    Stage: "execute_failed",
                    Status: "failed",
                    Result: null,
                    ErrorCode: executed.Error.Code));
                continue;
            }

            completedBatchCount++;
            items.Add(new RetentionDueBatchAutomationItemResponse(
                campaignSeriesId,
                batch.Id,
                Stage: "executed",
                Status: executed.Value.Batch.Status,
                Result: executed.Value.Result,
                ErrorCode: null));
        }

        return Result.Success(new RetentionDueBatchAutomationRunResponse(
            tenantId,
            asOf,
            cappedMaxBatches,
            campaignSeriesIds.Count,
            dueBatchCount,
            claimedBatchCount,
            completedBatchCount,
            failedBatchCount,
            noCandidateSeriesCount,
            skippedBatchCount,
            items));
    }

    private async Task<Result<RetentionDueBatch>> LoadBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await db.RetentionDueBatches
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == dueBatchId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return batch is null
            ? Result.Failure<RetentionDueBatch>(DueBatchNotFound())
            : Result.Success(batch);
    }

    private async Task<RetentionDueBatch?> LoadBatchForUpdateAsync(
        Guid dueBatchId,
        CancellationToken cancellationToken)
    {
        return await db.RetentionDueBatches
            .FromSqlInterpolated($"SELECT * FROM retention_due_batch WHERE id = {dueBatchId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RetentionDueBatchParityMismatch>> BuildParityMismatchesAsync(
        Guid tenantId,
        RetentionDueBatch batch,
        CancellationToken cancellationToken)
    {
        var plan = await dueCandidateStore.PlanDueCandidatesAsync(
            tenantId,
            batch.CampaignSeriesId,
            batch.AsOf,
            cancellationToken);
        if (plan.IsFailure)
        {
            return [new RetentionDueBatchParityMismatch("plan", "ready", plan.Error.Code)];
        }

        var current = plan.Value.Batches.SingleOrDefault();
        if (current is null)
        {
            return [new RetentionDueBatchParityMismatch("batch", "present", "missing")];
        }

        var mismatches = new List<RetentionDueBatchParityMismatch>();
        AddMismatch(mismatches, "status", RetentionDueCandidateStatuses.Ready, current.Status);
        AddMismatch(mismatches, "retention_policy_id", batch.RetentionPolicyId.ToString("D"), current.RetentionPolicyId.ToString("D"));
        AddMismatch(mismatches, "anchor", batch.Anchor, current.Anchor);
        AddMismatch(mismatches, "action_after", batch.ActionAfter, current.ActionAfter);
        AddMismatch(mismatches, "due_before", NormalizeTime(batch.DueBefore), current.DueBefore.HasValue ? NormalizeTime(current.DueBefore.Value) : "missing");
        AddMismatch(mismatches, "consent_record_count", batch.ConsentRecordCount.ToString(), current.ConsentRecordCount.ToString());
        AddMismatch(mismatches, "response_session_count", batch.ResponseSessionCount.ToString(), current.ResponseSessionCount.ToString());
        AddMismatch(mismatches, "answer_count", batch.AnswerCount.ToString(), current.AnswerCount.ToString());
        AddMismatch(mismatches, "score_run_count", batch.ScoreRunCount.ToString(), current.ScoreRunCount.ToString());
        AddMismatch(mismatches, "score_count", batch.ScoreCount.ToString(), current.ScoreCount.ToString());
        AddMismatch(mismatches, "derived_artifact_count", batch.DerivedArtifactCount.ToString(), current.DerivedArtifactCount.ToString());

        return mismatches;
    }

    private async Task<IReadOnlyList<Guid>> LoadRetentionCampaignSeriesIdsAsync(
        Guid tenantId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var campaignSeriesIds = await db.RetentionPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.CreatedAt <= asOf &&
                (!policy.RetiredAt.HasValue || policy.RetiredAt.Value > asOf))
            .Select(policy => policy.CampaignSeriesId)
            .Distinct()
            .OrderBy(campaignSeriesId => campaignSeriesId)
            .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return campaignSeriesIds;
    }

    private IQueryable<Guid> DueSubmittedSessionIds(
        Guid campaignSeriesId,
        DateTimeOffset dueBefore,
        string actionAfter)
    {
        return
            from session in db.ResponseSessions
            join assignment in db.Assignments on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns on assignment.CampaignId equals campaign.Id
            where campaign.CampaignSeriesId == campaignSeriesId &&
                session.SubmittedAt.HasValue &&
                session.SubmittedAt.Value <= dueBefore &&
                (actionAfter != RetentionPolicy.Anonymize || session.AnonymizedAt == null)
            select session.Id;
    }

    private async Task<DueBatchMutationGraph> BuildDueBatchMutationGraphAsync(
        RetentionDueBatch batch,
        CancellationToken cancellationToken)
    {
        var sessionIds = DueSubmittedSessionIds(batch.CampaignSeriesId, batch.DueBefore, batch.ActionAfter);
        var responseSessions = await db.ResponseSessions
            .Where(session => sessionIds.Contains(session.Id))
            .ToListAsync(cancellationToken);
        var responseSessionIds = responseSessions.Select(session => session.Id).ToArray();
        var consentRecordIds = responseSessions
            .Where(session => session.ConsentRecordId.HasValue)
            .Select(session => session.ConsentRecordId!.Value)
            .Distinct()
            .ToArray();
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
                        artifact.CampaignSeriesId == batch.CampaignSeriesId)))
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
        var directQueueRespondentSubjectIds = assignments
            .Where(assignment => assignment.RespondentSubjectId.HasValue)
            .Select(assignment => assignment.RespondentSubjectId!.Value)
            .Distinct()
            .ToArray();
        var invitationTokens = await db.InvitationTokens
            .Where(token =>
                (token.AssignmentId.HasValue && assignmentIds.Contains(token.AssignmentId.Value)) ||
                assignmentInviteTokenIds.Contains(token.Id) ||
                (token.RespondentSubjectId.HasValue &&
                    directQueueRespondentSubjectIds.Contains(token.RespondentSubjectId.Value) &&
                    campaignIds.Contains(token.CampaignId)))
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

        return new DueBatchMutationGraph(
            consentRecords,
            responseSessions,
            answers,
            scoreRuns,
            scores,
            assignments,
            derivedArtifacts,
            notifications,
            notificationDeliveryAttempts,
            invitationTokens);
    }

    private static bool MatchesPlannedCounts(RetentionDueBatch batch, DueBatchMutationGraph mutationGraph)
    {
        return batch.ConsentRecordCount == mutationGraph.ConsentRecords.Count &&
            batch.ResponseSessionCount == mutationGraph.ResponseSessions.Count &&
            batch.AnswerCount == mutationGraph.Answers.Count &&
            batch.ScoreRunCount == mutationGraph.ScoreRuns.Count &&
            batch.ScoreCount == mutationGraph.Scores.Count &&
            batch.DerivedArtifactCount == mutationGraph.DerivedArtifacts.Count;
    }

    private async Task MarkFailedAsync(
        Guid tenantId,
        Guid dueBatchId,
        string failureCode,
        string failureDetail,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);
        var batch = await LoadBatchForUpdateAsync(dueBatchId, cancellationToken);
        if (batch is null || batch.Status is RetentionDueBatchStatuses.Completed or RetentionDueBatchStatuses.Failed)
        {
            return;
        }

        batch.Fail(failureCode, failureDetail, failedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task MarkDueBatchFailedAfterMutationSaveFailureAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await MarkFailedAsync(
            tenantId,
            dueBatchId,
            "retention_due_batch.mutation_failed",
            "Retention due-batch mutation failed.",
            DateTimeOffset.UtcNow,
            cancellationToken);
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
        DueBatchMutationGraph mutationGraph,
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

    private static RetentionDueBatchResponse ToResponse(RetentionDueBatch dueBatch)
    {
        return new RetentionDueBatchResponse(
            dueBatch.Id,
            dueBatch.CampaignSeriesId,
            dueBatch.RetentionPolicyId,
            dueBatch.Anchor,
            dueBatch.ActionAfter,
            dueBatch.Status,
            dueBatch.AsOf,
            dueBatch.DueBefore,
            dueBatch.ConsentRecordCount,
            dueBatch.ResponseSessionCount,
            dueBatch.AnswerCount,
            dueBatch.ScoreRunCount,
            dueBatch.ScoreCount,
            dueBatch.DerivedArtifactCount,
            dueBatch.IdempotencyKey,
            dueBatch.ProcessingStartedAt,
            dueBatch.CompletedAt,
            dueBatch.FailedAt,
            dueBatch.FailureCode,
            dueBatch.FailureDetail,
            dueBatch.ExecutionResult,
            dueBatch.ArtifactInvalidatedCount,
            dueBatch.NoticeScrubbedCount,
            dueBatch.DeliveryAttemptScrubbedCount,
            dueBatch.InviteCredentialScrubbedCount);
    }

    private static RetentionDueBatchExecutionResponse ToExecutionResponse(
        RetentionDueBatch batch,
        DueBatchMutationGraph mutationGraph,
        string result,
        int artifactInvalidatedCount,
        DeliveryIdentityScrubCounts deliveryIdentityScrubCounts)
    {
        return new RetentionDueBatchExecutionResponse(
            ToResponse(batch),
            result,
            mutationGraph.ConsentRecords.Count,
            mutationGraph.ResponseSessions.Count,
            mutationGraph.Answers.Count,
            mutationGraph.ScoreRuns.Count,
            mutationGraph.Scores.Count,
            mutationGraph.DerivedArtifacts.Count,
            artifactInvalidatedCount,
            deliveryIdentityScrubCounts.NoticeCount,
            deliveryIdentityScrubCounts.DeliveryAttemptCount,
            deliveryIdentityScrubCounts.InviteCredentialCount);
    }

    private static void AddMismatch(
        List<RetentionDueBatchParityMismatch> mismatches,
        string field,
        string planned,
        string current)
    {
        if (!string.Equals(planned, current, StringComparison.Ordinal))
        {
            mismatches.Add(new RetentionDueBatchParityMismatch(field, planned, current));
        }
    }

    private static string NormalizeTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O");
    }

    private static Error DueBatchNotFound()
    {
        return Error.NotFound("retention_due_batch.not_found", "Retention due-batch was not found.");
    }

    private static Error DueBatchNotPlanned()
    {
        return Error.Conflict("retention_due_batch.not_planned", "Retention due-batch is not planned.");
    }

    private sealed record DeliveryIdentityScrubCounts(
        int NoticeCount,
        int DeliveryAttemptCount,
        int InviteCredentialCount)
    {
        public static DeliveryIdentityScrubCounts Empty { get; } = new(0, 0, 0);
    }

    private sealed record DueBatchMutationGraph(
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
