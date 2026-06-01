using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Reports;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Scoring;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class WaveComparisonProofStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope) : IWaveComparisonProofStore
{
    private const string Ready = "ready";
    private const string NotReady = "not_ready";
    private const string NotValidatedInterpretation = "not_validated_interpretation";
    private const string Visible = "visible";
    private const string Suppressed = "suppressed";
    private const string InsufficientResponses = "insufficient_responses";

    public async Task<Result<CampaignSeriesWaveComparisonProofResponse>> GetCampaignSeriesWaveComparisonProofAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var seriesExists = await db.CampaignSeries
            .AsNoTracking()
            .AnyAsync(series => series.Id == campaignSeriesId, cancellationToken);
        if (!seriesExists)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var waves = await ReadLaunchedLongitudinalWavesAsync(campaignSeriesId, cancellationToken);
        if (waves.Count < 2)
        {
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new CampaignSeriesWaveComparisonProofResponse(
                campaignSeriesId,
                NotReady,
                NotValidatedInterpretation,
                BaselineWave: null,
                ComparisonWave: null,
                DisclosurePolicy: null,
                Scores: []));
        }

        var baselineWave = waves[0];
        var comparisonWave = waves[1];
        var scoringRules = await ReadScoringRulesAsync(
            baselineWave.ScoringRuleId,
            comparisonWave.ScoringRuleId,
            cancellationToken);

        if (!scoringRules.TryGetValue(baselineWave.ScoringRuleId, out var baselineScoringRule) ||
            !scoringRules.TryGetValue(comparisonWave.ScoringRuleId, out var comparisonScoringRule))
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(
                Error.Validation("report.scoring_rule_missing", "Wave comparison scoring rule was not found."));
        }

        var baselineInterpretationMetadata = ScoreInterpretationMetadataParser.ParseProduces(
            baselineScoringRule.Produces);
        if (baselineInterpretationMetadata.IsFailure)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(baselineInterpretationMetadata.Error);
        }

        var baselineOutputMetadata = ScoreOutputMetadataParser.ParseProduces(
            baselineScoringRule.Produces);
        if (baselineOutputMetadata.IsFailure)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(baselineOutputMetadata.Error);
        }

        var comparisonInterpretationMetadata = ScoreInterpretationMetadataParser.ParseProduces(
            comparisonScoringRule.Produces);
        if (comparisonInterpretationMetadata.IsFailure)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(comparisonInterpretationMetadata.Error);
        }

        var comparisonOutputMetadata = ScoreOutputMetadataParser.ParseProduces(
            comparisonScoringRule.Produces);
        if (comparisonOutputMetadata.IsFailure)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(comparisonOutputMetadata.Error);
        }

        if (!baselineWave.DisclosurePolicyId.HasValue || !comparisonWave.DisclosurePolicyId.HasValue)
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(
                Error.Validation("report.disclosure_policy_missing", "Wave comparison launch snapshots must reference disclosure policies."));
        }

        var disclosurePolicies = await ReadDisclosurePoliciesAsync(
            baselineWave.DisclosurePolicyId.Value,
            comparisonWave.DisclosurePolicyId.Value,
            cancellationToken);

        if (!disclosurePolicies.TryGetValue(baselineWave.DisclosurePolicyId.Value, out var baselineDisclosurePolicy) ||
            !disclosurePolicies.TryGetValue(comparisonWave.DisclosurePolicyId.Value, out var comparisonDisclosurePolicy))
        {
            return Result.Failure<CampaignSeriesWaveComparisonProofResponse>(
                Error.Validation("report.disclosure_policy_missing", "Wave comparison disclosure policy was not found."));
        }

        var selectedDisclosurePolicy = SelectDisclosurePolicy(
            baselineDisclosurePolicy,
            comparisonDisclosurePolicy);
        var submittedSessions = await ReadSubmittedSessionsAsync(
            baselineWave.CampaignId,
            comparisonWave.CampaignId,
            cancellationToken);
        var latestScores = await ReadLatestScoresAsync(
            submittedSessions.Select(session => session.ResponseSessionId).ToArray(),
            cancellationToken);
        var scores = CreateScoreComparisons(
            baselineWave,
            comparisonWave,
            baselineScoringRule,
            comparisonScoringRule,
            baselineInterpretationMetadata.Value,
            comparisonInterpretationMetadata.Value,
            baselineOutputMetadata.Value,
            comparisonOutputMetadata.Value,
            submittedSessions,
            latestScores,
            selectedDisclosurePolicy.KMin);

        await transaction.CommitAsync(cancellationToken);

        var baselineSubmittedResponseCount = CountSubmittedSessions(submittedSessions, baselineWave.CampaignId);
        var comparisonSubmittedResponseCount = CountSubmittedSessions(submittedSessions, comparisonWave.CampaignId);

        return Result.Success(new CampaignSeriesWaveComparisonProofResponse(
            campaignSeriesId,
            Ready,
            NotValidatedInterpretation,
            ToWaveResponse(
                baselineWave,
                baselineScoringRule,
                baselineSubmittedResponseCount),
            ToWaveResponse(
                comparisonWave,
                comparisonScoringRule,
                comparisonSubmittedResponseCount),
            new WaveComparisonDisclosurePolicyResponse(
                selectedDisclosurePolicy.Id,
                selectedDisclosurePolicy.Version,
                selectedDisclosurePolicy.KMin,
                selectedDisclosurePolicy.SuppressionStrategy),
            scores));
    }

    private async Task<IReadOnlyList<WaveSummary>> ReadLaunchedLongitudinalWavesAsync(
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return await (
            from campaign in db.Campaigns.AsNoTracking()
            join snapshot in db.CampaignLaunchSnapshots.AsNoTracking()
                on campaign.Id equals snapshot.CampaignId
            where campaign.CampaignSeriesId == campaignSeriesId &&
                snapshot.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal
            orderby snapshot.LaunchedAt, campaign.Name
            select new WaveSummary(
                campaign.Id,
                campaign.Name,
                campaign.Status,
                snapshot.ResponseIdentityMode,
                snapshot.LaunchedAt,
                snapshot.ScoringRuleId,
                snapshot.ScoringRuleDocumentHash,
                snapshot.DisclosurePolicyId,
                snapshot.LaunchPacket))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, ScoringRule>> ReadScoringRulesAsync(
        Guid baselineScoringRuleId,
        Guid comparisonScoringRuleId,
        CancellationToken cancellationToken)
    {
        var scoringRuleIds = new[] { baselineScoringRuleId, comparisonScoringRuleId };
        return await db.ScoringRules
            .AsNoTracking()
            .Where(rule => scoringRuleIds.Contains(rule.Id))
            .ToDictionaryAsync(rule => rule.Id, cancellationToken);
    }

    private async Task<Dictionary<Guid, DisclosurePolicy>> ReadDisclosurePoliciesAsync(
        Guid baselineDisclosurePolicyId,
        Guid comparisonDisclosurePolicyId,
        CancellationToken cancellationToken)
    {
        var disclosurePolicyIds = new[] { baselineDisclosurePolicyId, comparisonDisclosurePolicyId };
        return await db.DisclosurePolicies
            .AsNoTracking()
            .Where(policy => disclosurePolicyIds.Contains(policy.Id))
            .ToDictionaryAsync(policy => policy.Id, cancellationToken);
    }

    private async Task<IReadOnlyList<SubmittedSessionRow>> ReadSubmittedSessionsAsync(
        Guid baselineCampaignId,
        Guid comparisonCampaignId,
        CancellationToken cancellationToken)
    {
        var campaignIds = new[] { baselineCampaignId, comparisonCampaignId };
        return await (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking()
                on session.AssignmentId equals assignment.Id
            where campaignIds.Contains(assignment.CampaignId) &&
                session.SubmittedAt != null &&
                session.ParticipantCodeId != null
            select new SubmittedSessionRow(
                session.Id,
                assignment.CampaignId,
                session.ParticipantCodeId!.Value))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ScoreRow>> ReadLatestScoresAsync(
        IReadOnlyList<Guid> responseSessionIds,
        CancellationToken cancellationToken)
    {
        if (responseSessionIds.Count == 0)
        {
            return [];
        }

        var scoreRows = await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where responseSessionIds.Contains(score.ResponseSessionId) &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                orderby score.DimensionCode, score.ResponseSessionId, score.ComputedAt descending, score.Id descending
                select new ScoreRow(
                    score.Id,
                    score.ResponseSessionId,
                    score.CampaignId,
                    score.DimensionCode,
                    score.Value,
                    score.NValid,
                    score.NExpected,
                    score.MissingPolicyStatus,
                    score.ComputedAt))
            .ToListAsync(cancellationToken);

        return scoreRows
            .GroupBy(score => new { score.ResponseSessionId, score.DimensionCode })
            .Select(group => group
                .OrderByDescending(score => score.ComputedAt)
                .ThenByDescending(score => score.ScoreId)
                .First())
            .ToArray();
    }

    private static IReadOnlyList<WaveScoreComparisonResponse> CreateScoreComparisons(
        WaveSummary baselineWave,
        WaveSummary comparisonWave,
        ScoringRule baselineScoringRule,
        ScoringRule comparisonScoringRule,
        ScoreInterpretationMetadata? baselineInterpretationMetadata,
        ScoreInterpretationMetadata? comparisonInterpretationMetadata,
        IReadOnlyDictionary<string, ScoreOutputMetadata> baselineOutputMetadata,
        IReadOnlyDictionary<string, ScoreOutputMetadata> comparisonOutputMetadata,
        IReadOnlyList<SubmittedSessionRow> submittedSessions,
        IReadOnlyList<ScoreRow> latestScores,
        int kMin)
    {
        var submittedBySession = submittedSessions.ToDictionary(
            session => session.ResponseSessionId,
            session => session);
        var scoredRows = latestScores
            .Where(score => submittedBySession.ContainsKey(score.ResponseSessionId))
            .ToArray();
        var dimensions = scoredRows
            .Select(score => score.DimensionCode)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var baselineSubmittedResponseCount = CountSubmittedSessions(submittedSessions, baselineWave.CampaignId);
        var comparisonSubmittedResponseCount = CountSubmittedSessions(submittedSessions, comparisonWave.CampaignId);
        var baselineCompatibilityReference = ToCompatibilityReference(baselineScoringRule);
        var comparisonCompatibilityReference = ToCompatibilityReference(comparisonScoringRule);

        return dimensions
            .Select(dimension => CreateScoreComparison(
                dimension,
                baselineWave.CampaignId,
                comparisonWave.CampaignId,
                submittedBySession,
                scoredRows.Where(score => score.DimensionCode == dimension).ToArray(),
                baselineSubmittedResponseCount,
                comparisonSubmittedResponseCount,
                ScoringRuleCompatibilityResolver.Resolve(
                    baselineCompatibilityReference,
                    comparisonCompatibilityReference,
                    dimension),
                baselineInterpretationMetadata,
                comparisonInterpretationMetadata,
                baselineOutputMetadata,
                comparisonOutputMetadata,
                kMin))
            .ToArray();
    }

    private static WaveScoreComparisonResponse CreateScoreComparison(
        string dimensionCode,
        Guid baselineCampaignId,
        Guid comparisonCampaignId,
        IReadOnlyDictionary<Guid, SubmittedSessionRow> submittedBySession,
        IReadOnlyList<ScoreRow> scores,
        int baselineSubmittedResponseCount,
        int comparisonSubmittedResponseCount,
        ScoringRuleCompatibilityResolution compatibility,
        ScoreInterpretationMetadata? baselineInterpretationMetadata,
        ScoreInterpretationMetadata? comparisonInterpretationMetadata,
        IReadOnlyDictionary<string, ScoreOutputMetadata> baselineOutputMetadata,
        IReadOnlyDictionary<string, ScoreOutputMetadata> comparisonOutputMetadata,
        int kMin)
    {
        var baselineScores = ScoresForCampaign(scores, submittedBySession, baselineCampaignId);
        var comparisonScores = ScoresForCampaign(scores, submittedBySession, comparisonCampaignId);
        var baselineMetadata = FindScoreOutputMetadata(baselineOutputMetadata, dimensionCode);
        var comparisonMetadata = FindScoreOutputMetadata(comparisonOutputMetadata, dimensionCode);
        var displayLabel = comparisonMetadata?.Label ?? baselineMetadata?.Label;
        var baselineMean = MeanOrNull(baselineScores.Select(score => score.Value));
        var comparisonMean = MeanOrNull(comparisonScores.Select(score => score.Value));
        var pairedDeltas = CreatePairedDeltas(baselineScores, comparisonScores);
        var pairedDeltaMean = MeanOrNull(pairedDeltas);
        var aggregateDelta = baselineMean.HasValue && comparisonMean.HasValue
            ? Round(comparisonMean.Value - baselineMean.Value)
            : (decimal?)null;
        var deltaIsAllowed =
            compatibility.Status == ScoringRuleCompatibilityResolver.Compatible ||
            compatibility.Status == ScoringRuleCompatibilityResolver.DescriptiveOnly;
        var disclosurePasses =
            baselineSubmittedResponseCount >= kMin &&
            comparisonSubmittedResponseCount >= kMin &&
            pairedDeltas.Count >= kMin;

        if (!disclosurePasses)
        {
            return new WaveScoreComparisonResponse(
                dimensionCode,
                compatibility.Status,
                Suppressed,
                baselineSubmittedResponseCount,
                comparisonSubmittedResponseCount,
                pairedDeltas.Count,
                BaselineScoreCount: null,
                ComparisonScoreCount: null,
                BaselineMean: null,
                ComparisonMean: null,
                AggregateDelta: null,
                PairedDeltaMean: null,
                InsufficientResponses,
                compatibility.Status == ScoringRuleCompatibilityResolver.Compatible ? null : compatibility.Reason,
                DisplayLabel: displayLabel,
                BaselineCalculation: baselineMetadata?.Calculation,
                BaselineCalculationLabel: baselineMetadata?.CalculationLabel,
                BaselineScoreRangeMin: baselineMetadata?.ScoreRangeMin,
                BaselineScoreRangeMax: baselineMetadata?.ScoreRangeMax,
                ComparisonCalculation: comparisonMetadata?.Calculation,
                ComparisonCalculationLabel: comparisonMetadata?.CalculationLabel,
                ComparisonScoreRangeMin: comparisonMetadata?.ScoreRangeMin,
                ComparisonScoreRangeMax: comparisonMetadata?.ScoreRangeMax);
        }

        return new WaveScoreComparisonResponse(
            dimensionCode,
            compatibility.Status,
            Visible,
            baselineSubmittedResponseCount,
            comparisonSubmittedResponseCount,
            pairedDeltas.Count,
            baselineScores.Count,
            comparisonScores.Count,
            baselineMean,
            comparisonMean,
            deltaIsAllowed ? aggregateDelta : null,
            deltaIsAllowed ? pairedDeltaMean : null,
            SuppressionReason: null,
            compatibility.Status == ScoringRuleCompatibilityResolver.Compatible ? null : compatibility.Reason,
            ScoreInterpretationProjection.Create(baselineInterpretationMetadata, dimensionCode, baselineMean),
            ScoreInterpretationProjection.Create(comparisonInterpretationMetadata, dimensionCode, comparisonMean),
            baselineScores.Sum(score => score.NValid),
            baselineScores.Sum(score => score.NExpected),
            SummarizeMissingPolicyStatus(baselineScores.Select(score => score.MissingPolicyStatus)),
            comparisonScores.Sum(score => score.NValid),
            comparisonScores.Sum(score => score.NExpected),
            SummarizeMissingPolicyStatus(comparisonScores.Select(score => score.MissingPolicyStatus)),
            displayLabel,
            baselineMetadata?.Calculation,
            baselineMetadata?.CalculationLabel,
            baselineMetadata?.ScoreRangeMin,
            baselineMetadata?.ScoreRangeMax,
            comparisonMetadata?.Calculation,
            comparisonMetadata?.CalculationLabel,
            comparisonMetadata?.ScoreRangeMin,
            comparisonMetadata?.ScoreRangeMax);
    }

    private static ScoreOutputMetadata? FindScoreOutputMetadata(
        IReadOnlyDictionary<string, ScoreOutputMetadata> metadata,
        string dimensionCode)
    {
        if (metadata.TryGetValue(dimensionCode, out var exact))
        {
            return exact;
        }

        return metadata.FirstOrDefault(pair => string.Equals(
            pair.Key,
            dimensionCode,
            StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static IReadOnlyList<ParticipantScore> ScoresForCampaign(
        IReadOnlyList<ScoreRow> scores,
        IReadOnlyDictionary<Guid, SubmittedSessionRow> submittedBySession,
        Guid campaignId)
    {
        return scores
            .Where(score => score.CampaignId == campaignId)
            .Select(score => new ParticipantScore(
                submittedBySession[score.ResponseSessionId].ParticipantCodeId,
                score.Value,
                score.NValid,
                score.NExpected,
                score.MissingPolicyStatus))
            .ToArray();
    }

    private static IReadOnlyList<decimal> CreatePairedDeltas(
        IReadOnlyList<ParticipantScore> baselineScores,
        IReadOnlyList<ParticipantScore> comparisonScores)
    {
        var baselineByParticipant = baselineScores
            .GroupBy(score => score.ParticipantCodeId)
            .ToDictionary(group => group.Key, group => group.First().Value);
        var comparisonByParticipant = comparisonScores
            .GroupBy(score => score.ParticipantCodeId)
            .ToDictionary(group => group.Key, group => group.First().Value);

        return baselineByParticipant
            .Where(pair => comparisonByParticipant.ContainsKey(pair.Key))
            .Select(pair => Round(comparisonByParticipant[pair.Key] - pair.Value))
            .ToArray();
    }

    private static decimal? MeanOrNull(IEnumerable<decimal> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0 ? null : Round(materialized.Average());
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
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

    private static int CountSubmittedSessions(
        IReadOnlyList<SubmittedSessionRow> submittedSessions,
        Guid campaignId)
    {
        return submittedSessions.Count(session => session.CampaignId == campaignId);
    }

    private static WaveComparisonWaveResponse ToWaveResponse(
        WaveSummary wave,
        ScoringRule scoringRule,
        int submittedResponseCount)
    {
        return new WaveComparisonWaveResponse(
            wave.CampaignId,
            wave.Name,
            wave.Status,
            wave.ResponseIdentityMode,
            wave.LaunchedAt,
            scoringRule.Id,
            scoringRule.RuleKey,
            scoringRule.RuleVersion,
            scoringRule.DocumentHash,
            submittedResponseCount,
            LaunchPacketProvenanceProjection.FromJson(wave.LaunchPacket));
    }

    private static ScoringRuleCompatibilityReference ToCompatibilityReference(ScoringRule scoringRule)
    {
        return new ScoringRuleCompatibilityReference(
            scoringRule.RuleKey,
            scoringRule.RuleVersion,
            scoringRule.DocumentHash,
            scoringRule.Compatibility);
    }

    private static DisclosurePolicy SelectDisclosurePolicy(
        DisclosurePolicy baselineDisclosurePolicy,
        DisclosurePolicy comparisonDisclosurePolicy)
    {
        return comparisonDisclosurePolicy.KMin > baselineDisclosurePolicy.KMin
            ? comparisonDisclosurePolicy
            : baselineDisclosurePolicy;
    }

    private sealed record WaveSummary(
        Guid CampaignId,
        string Name,
        string Status,
        string ResponseIdentityMode,
        DateTimeOffset LaunchedAt,
        Guid ScoringRuleId,
        string ScoringRuleDocumentHash,
        Guid? DisclosurePolicyId,
        string LaunchPacket);

    private sealed record SubmittedSessionRow(
        Guid ResponseSessionId,
        Guid CampaignId,
        Guid ParticipantCodeId);

    private sealed record ScoreRow(
        Guid ScoreId,
        Guid ResponseSessionId,
        Guid CampaignId,
        string DimensionCode,
        decimal Value,
        int NValid,
        int NExpected,
        string MissingPolicyStatus,
        DateTimeOffset ComputedAt);

    private sealed record ParticipantScore(
        Guid ParticipantCodeId,
        decimal Value,
        int NValid,
        int NExpected,
        string MissingPolicyStatus);
}
