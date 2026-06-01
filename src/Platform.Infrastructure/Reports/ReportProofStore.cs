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

        var scoreOutputMetadata = ScoreOutputMetadataParser.ParseProduces(scoringRule.Produces);
        if (scoreOutputMetadata.IsFailure)
        {
            return Result.Failure<CampaignReportProofResponse>(scoreOutputMetadata.Error);
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

        var scoreRows = await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where score.CampaignId == campaignId &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                orderby score.DimensionCode, score.ResponseSessionId, score.ComputedAt descending, score.Id descending
                select score)
            .ToListAsync(cancellationToken);

        var latestScores = scoreRows
            .GroupBy(score => new { score.ResponseSessionId, score.DimensionCode })
            .Select(group => group.First())
            .ToArray();
        var scoreSummaries = latestScores
            .GroupBy(score => score.DimensionCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => CreateScoreSummary(
                group.Key,
                group.ToArray(),
                submittedResponseCount,
                snapshot.ResponseIdentityMode,
                disclosurePolicy.KMin,
                interpretationMetadata.Value,
                scoreOutputMetadata.Value))
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
        string responseIdentityMode,
        int disclosureKMin,
        ScoreInterpretationMetadata? interpretationMetadata,
        IReadOnlyDictionary<string, ScoreOutputMetadata> scoreOutputMetadata)
    {
        var metadata = FindScoreOutputMetadata(scoreOutputMetadata, dimensionCode);
        var disclosurePasses = ResultOutputDisclosurePasses(
            responseIdentityMode,
            disclosureKMin,
            scores.Count);

        if (!disclosurePasses)
        {
            return new ReportScoreSummaryResponse(
                dimensionCode,
                Suppressed,
                submittedResponseCount,
                ScoreCount: null,
                Mean: null,
                Median: null,
                StandardDeviation: null,
                Min: null,
                Max: null,
                SuppressionReason: InsufficientResponses,
                DisplayLabel: metadata?.Label,
                Calculation: metadata?.Calculation,
                CalculationLabel: metadata?.CalculationLabel,
                ScoreRangeMin: metadata?.ScoreRangeMin,
                ScoreRangeMax: metadata?.ScoreRangeMax);
        }

        var values = scores.Select(score => score.Value).ToArray();
        var mean = Math.Round(values.Average(), 4, MidpointRounding.AwayFromZero);
        var median = CalculateMedian(values);
        var standardDeviation = CalculateStandardDeviation(values);

        return new ReportScoreSummaryResponse(
            dimensionCode,
            Visible,
            submittedResponseCount,
            values.Length,
            mean,
            median,
            standardDeviation,
            values.Min(),
            values.Max(),
            SuppressionReason: null,
            ScoreInterpretationProjection.Create(interpretationMetadata, dimensionCode, mean),
            scores.Sum(score => score.NValid),
            scores.Sum(score => score.NExpected),
            SummarizeMissingPolicyStatus(scores.Select(score => score.MissingPolicyStatus)),
            metadata?.Label,
            metadata?.Calculation,
            metadata?.CalculationLabel,
            metadata?.ScoreRangeMin,
            metadata?.ScoreRangeMax);
    }

    private static ScoreOutputMetadata? FindScoreOutputMetadata(
        IReadOnlyDictionary<string, ScoreOutputMetadata> metadata,
        string dimensionCode)
    {
        var normalized = dimensionCode.Trim().ToLowerInvariant();

        return metadata.TryGetValue(normalized, out var value) ? value : null;
    }

    private static bool ResultOutputDisclosurePasses(
        string responseIdentityMode,
        int disclosureKMin,
        int scoreCount)
    {
        if (scoreCount <= 0)
        {
            return false;
        }

        return responseIdentityMode == ResponseIdentityModes.Identified ||
            scoreCount >= disclosureKMin;
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

    private static decimal CalculateMedian(IReadOnlyCollection<decimal> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            return 0;
        }

        var middle = ordered.Length / 2;
        var median = ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2;

        return Math.Round(median, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateStandardDeviation(IReadOnlyCollection<decimal> values)
    {
        if (values.Count <= 1)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values
            .Select(value => Math.Pow((double)(value - mean), 2))
            .Average();

        return Math.Round((decimal)Math.Sqrt(variance), 4, MidpointRounding.AwayFromZero);
    }
}
