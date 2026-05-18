using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Reports;
using Platform.Domain.Campaigns;
using Platform.Domain.Scoring;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class ReportProofStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope) : IReportProofStore
{
    private const string ProofOnly = "proof_only";
    private const string NotValidatedInterpretation = "not_validated_interpretation";
    private const string Visible = "visible";
    private const string Suppressed = "suppressed";
    private const string InsufficientResponses = "insufficient_responses";
    private const string PreliminaryLiveDataFinality = "preliminary_live";
    private const string ClosedWaveDataFinality = "closed_wave";
    private const string NotReportableDataFinality = "not_reportable";

    public async Task<Result<CampaignReportProofResponse>> GetCampaignReportProofAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignReportProofResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CampaignId == campaignId, cancellationToken);

        if (snapshot is null)
        {
            return Result.Failure<CampaignReportProofResponse>(
                Error.Validation(
                    "report.launch_snapshot_missing",
                    "Campaign must be launched before report proof can be generated."));
        }

        if (!snapshot.DisclosurePolicyId.HasValue)
        {
            return Result.Failure<CampaignReportProofResponse>(
                Error.Validation(
                    "report.disclosure_policy_missing",
                    "Campaign launch snapshot must reference a disclosure policy."));
        }

        var disclosurePolicy = await db.DisclosurePolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == snapshot.DisclosurePolicyId.Value,
                cancellationToken);

        if (disclosurePolicy is null)
        {
            return Result.Failure<CampaignReportProofResponse>(
                Error.Validation(
                    "report.disclosure_policy_missing",
                    "Campaign launch snapshot disclosure policy was not found."));
        }

        var scoringRule = await db.ScoringRules
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == snapshot.ScoringRuleId, cancellationToken);

        if (scoringRule is null)
        {
            return Result.Failure<CampaignReportProofResponse>(
                Error.Validation("report.scoring_rule_missing", "Campaign launch snapshot scoring rule was not found."));
        }

        var interpretationMetadata = ScoreInterpretationMetadataParser.ParseProduces(scoringRule.Produces);
        if (interpretationMetadata.IsFailure)
        {
            return Result.Failure<CampaignReportProofResponse>(interpretationMetadata.Error);
        }

        var submittedResponseCount = await db.ResponseSessions
            .AsNoTracking()
            .Join(
                db.Assignments.AsNoTracking(),
                session => session.AssignmentId,
                assignment => assignment.Id,
                (session, assignment) => new { session, assignment })
            .CountAsync(
                row => row.assignment.CampaignId == campaignId &&
                    row.session.SubmittedAt.HasValue,
                cancellationToken);

        var scoreRows = await db.Scores
            .AsNoTracking()
            .Where(score => score.CampaignId == campaignId)
            .OrderBy(score => score.DimensionCode)
            .ThenBy(score => score.ResponseSessionId)
            .ThenByDescending(score => score.ComputedAt)
            .ToListAsync(cancellationToken);

        var latestScores = scoreRows
            .GroupBy(score => new { score.ResponseSessionId, score.DimensionCode })
            .Select(group => group.First())
            .ToArray();
        var disclosurePasses =
            snapshot.ResponseIdentityMode == ResponseIdentityModes.Identified ||
            submittedResponseCount >= disclosurePolicy.KMin;
        var scoreSummaries = latestScores
            .GroupBy(score => score.DimensionCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => CreateScoreSummary(
                group.Key,
                group.ToArray(),
                submittedResponseCount,
                disclosurePasses,
                interpretationMetadata.Value))
            .ToArray();

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignReportProofResponse(
            campaign.Id,
            campaign.CampaignSeriesId,
            campaign.Name,
            campaign.Status,
            ProofOnly,
            NotValidatedInterpretation,
            new ReportLaunchSnapshotResponse(
                snapshot.Id,
                snapshot.TemplateVersionId,
                snapshot.ScoringRuleId,
                snapshot.ScoringRuleDocumentHash,
                snapshot.ConsentDocumentId,
                snapshot.RetentionPolicyId,
                snapshot.DisclosurePolicyId,
                snapshot.ResponseIdentityMode,
                snapshot.LaunchedAt,
                LaunchPacketProvenanceProjection.FromJson(snapshot.LaunchPacket)),
            new ReportDisclosurePolicyResponse(
                disclosurePolicy.Id,
                disclosurePolicy.Version,
                disclosurePolicy.KMin,
                disclosurePolicy.SuppressionStrategy),
            scoreSummaries,
            campaign.ClosedAt,
            DetermineDataFinality(campaign.Status, submittedResponseCount, latestScores.Length)));
    }

    private static string DetermineDataFinality(
        string campaignStatus,
        int submittedResponseCount,
        int scoreCount)
    {
        if (submittedResponseCount == 0 || scoreCount == 0)
        {
            return NotReportableDataFinality;
        }

        return campaignStatus switch
        {
            CampaignStatuses.Closed => ClosedWaveDataFinality,
            CampaignStatuses.Live => PreliminaryLiveDataFinality,
            _ => NotReportableDataFinality
        };
    }

    private static ReportScoreSummaryResponse CreateScoreSummary(
        string dimensionCode,
        IReadOnlyList<Score> scores,
        int submittedResponseCount,
        bool disclosurePasses,
        ScoreInterpretationMetadata? interpretationMetadata)
    {
        if (!disclosurePasses)
        {
            return new ReportScoreSummaryResponse(
                dimensionCode,
                Suppressed,
                submittedResponseCount,
                ScoreCount: null,
                Mean: null,
                Min: null,
                Max: null,
                SuppressionReason: InsufficientResponses);
        }

        var values = scores.Select(score => score.Value).ToArray();
        var mean = Math.Round(values.Average(), 4, MidpointRounding.AwayFromZero);

        return new ReportScoreSummaryResponse(
            dimensionCode,
            Visible,
            submittedResponseCount,
            values.Length,
            mean,
            values.Min(),
            values.Max(),
            SuppressionReason: null,
            ScoreInterpretationProjection.Create(interpretationMetadata, dimensionCode, mean),
            scores.Sum(score => score.NValid),
            scores.Sum(score => score.NExpected),
            SummarizeMissingPolicyStatus(scores.Select(score => score.MissingPolicyStatus)));
    }

    private static string? SummarizeMissingPolicyStatus(IEnumerable<string> statuses)
    {
        var distinctStatuses = statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToArray();

        return distinctStatuses.Length switch
        {
            0 => null,
            1 => distinctStatuses[0],
            _ => "mixed"
        };
    }
}
