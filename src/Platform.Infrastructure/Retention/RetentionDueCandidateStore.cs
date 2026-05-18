using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Retention;
using Platform.Domain.Consent;
using Platform.Domain.Reports;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Retention;

public sealed class RetentionDueCandidateStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope)
    : IRetentionDueCandidateStore
{
    public async Task<Result<RetentionDueCandidatePlanResponse>> PlanDueCandidatesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(tenantId, cancellationToken: cancellationToken);

        var seriesExists = await db.CampaignSeries
            .AsNoTracking()
            .AnyAsync(series => series.Id == campaignSeriesId, cancellationToken);
        if (!seriesExists)
        {
            return Result.Failure<RetentionDueCandidatePlanResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var policy = await db.RetentionPolicies
            .AsNoTracking()
            .Where(candidate =>
                candidate.CampaignSeriesId == campaignSeriesId &&
                candidate.CreatedAt <= asOf &&
                (!candidate.RetiredAt.HasValue || candidate.RetiredAt.Value > asOf))
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            return Result.Failure<RetentionDueCandidatePlanResponse>(
                Error.Conflict("retention_policy.missing", "Campaign series has no usable retention policy."));
        }

        var batch = await BuildBatchAsync(policy, asOf, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new RetentionDueCandidatePlanResponse(
            campaignSeriesId,
            asOf,
            [batch]));
    }

    private async Task<RetentionDueCandidateBatch> BuildBatchAsync(
        RetentionPolicy policy,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        if (policy.RetentionStartEvent != RetentionPolicy.ResponseSubmittedAt)
        {
            return UnsupportedBatch(
                policy,
                RetentionDueCandidateDiagnosticCodes.UnsupportedAnchor,
                "Retention anchor is not supported by due-candidate planning.");
        }

        if (policy.ActionAfter is not (RetentionPolicy.Delete or RetentionPolicy.Anonymize))
        {
            return UnsupportedBatch(
                policy,
                RetentionDueCandidateDiagnosticCodes.UnsupportedAction,
                "Retention action is not supported by due-candidate planning.");
        }

        var dueBefore = asOf.AddYears(-policy.RetainForYears);
        var sessionIds = DueSubmittedSessionIds(policy.CampaignSeriesId, dueBefore, policy.ActionAfter);
        var responseSessionCount = await sessionIds.CountAsync(cancellationToken);
        var consentRecordIds = db.ResponseSessions
            .Where(session =>
                sessionIds.Contains(session.Id) &&
                session.ConsentRecordId.HasValue)
            .Select(session => session.ConsentRecordId!.Value)
            .Distinct();
        var consentRecordCount = await db.ConsentRecords
            .Where(record => consentRecordIds.Contains(record.Id))
            .CountAsync(cancellationToken);
        var answerCount = await db.Answers
            .Where(answer => sessionIds.Contains(answer.SessionId))
            .CountAsync(cancellationToken);
        var scoreRunIds = db.ScoreRuns
            .Where(scoreRun => sessionIds.Contains(scoreRun.ResponseSessionId))
            .Select(scoreRun => scoreRun.Id);
        var scoreRunCount = await scoreRunIds.CountAsync(cancellationToken);
        var scoreCount = await db.Scores
            .Where(score =>
                sessionIds.Contains(score.ResponseSessionId) ||
                scoreRunIds.Contains(score.ScoreRunId))
            .CountAsync(cancellationToken);
        var derivedArtifactCount = await CountDerivedArtifactsAsync(
            policy.CampaignSeriesId,
            sessionIds,
            cancellationToken);

        return new RetentionDueCandidateBatch(
            policy.Id,
            policy.CampaignSeriesId,
            policy.RetentionStartEvent,
            policy.ActionAfter,
            RetentionDueCandidateStatuses.Ready,
            dueBefore,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            derivedArtifactCount,
            Dependencies(
                consentRecordCount,
                responseSessionCount,
                answerCount,
                scoreRunCount,
                scoreCount,
                derivedArtifactCount),
            []);
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

    private async Task<int> CountDerivedArtifactsAsync(
        Guid campaignSeriesId,
        IQueryable<Guid> sessionIds,
        CancellationToken cancellationToken)
    {
        var campaignIds = await (
            from session in db.ResponseSessions
            join assignment in db.Assignments on session.AssignmentId equals assignment.Id
            where sessionIds.Contains(session.Id)
            select assignment.CampaignId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (campaignIds.Length == 0)
        {
            return 0;
        }

        return await db.ExportArtifacts
            .Where(artifact =>
                artifact.Status == ExportArtifactStatuses.Succeeded &&
                artifact.DeletedAt == null &&
                ((artifact.TargetKind == ExportArtifactTargetKinds.Campaign &&
                    artifact.CampaignId.HasValue &&
                    campaignIds.Contains(artifact.CampaignId.Value)) ||
                    (artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                        artifact.CampaignSeriesId == campaignSeriesId)))
            .CountAsync(cancellationToken);
    }

    private static RetentionDueCandidateBatch UnsupportedBatch(
        RetentionPolicy policy,
        string diagnosticCode,
        string message)
    {
        return new RetentionDueCandidateBatch(
            policy.Id,
            policy.CampaignSeriesId,
            policy.RetentionStartEvent,
            policy.ActionAfter,
            RetentionDueCandidateStatuses.Unsupported,
            DueBefore: null,
            ConsentRecordCount: 0,
            ResponseSessionCount: 0,
            AnswerCount: 0,
            ScoreRunCount: 0,
            ScoreCount: 0,
            DerivedArtifactCount: 0,
            Dependencies(0, 0, 0, 0, 0, 0),
            [new RetentionDueCandidateDiagnostic(diagnosticCode, message)]);
    }

    private static RetentionDueCandidateDependency[] Dependencies(
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        int derivedArtifactCount)
    {
        return
        [
            new(RetentionDueCandidateEntities.ConsentRecord, consentRecordCount),
            new(RetentionDueCandidateEntities.ResponseSession, responseSessionCount),
            new(RetentionDueCandidateEntities.Answer, answerCount),
            new(RetentionDueCandidateEntities.ScoreRun, scoreRunCount),
            new(RetentionDueCandidateEntities.Score, scoreCount),
            new(RetentionDueCandidateEntities.DerivedArtifact, derivedArtifactCount)
        ];
    }
}
