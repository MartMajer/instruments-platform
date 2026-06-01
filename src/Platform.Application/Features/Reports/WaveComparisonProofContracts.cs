using System.Text.Json.Serialization;

namespace Platform.Application.Features.Reports;

public sealed record CampaignSeriesWaveComparisonProofResponse(
    Guid CampaignSeriesId,
    string ProofStatus,
    string InterpretationStatus,
    WaveComparisonWaveResponse? BaselineWave,
    WaveComparisonWaveResponse? ComparisonWave,
    WaveComparisonDisclosurePolicyResponse? DisclosurePolicy,
    IReadOnlyList<WaveScoreComparisonResponse> Scores);

public sealed record WaveComparisonWaveResponse
{
    [JsonConstructor]
    public WaveComparisonWaveResponse(
        Guid campaignId,
        string name,
        string status,
        string responseIdentityMode,
        DateTimeOffset launchedAt,
        Guid scoringRuleId,
        string scoringRuleKey,
        string scoringRuleVersion,
        string scoringRuleDocumentHash,
        int submittedResponseCount,
        LaunchPacketProvenanceResponse? launchPacket = null)
    {
        CampaignId = campaignId;
        Name = name;
        Status = status;
        ResponseIdentityMode = responseIdentityMode;
        LaunchedAt = launchedAt;
        ScoringRuleId = scoringRuleId;
        ScoringRuleKey = scoringRuleKey;
        ScoringRuleVersion = scoringRuleVersion;
        ScoringRuleDocumentHash = scoringRuleDocumentHash;
        SubmittedResponseCount = submittedResponseCount;
        LaunchPacket = launchPacket ?? new LaunchPacketProvenanceResponse(0, [], "missing");
    }

    public Guid CampaignId { get; init; }

    public string Name { get; init; }

    public string Status { get; init; }

    public string ResponseIdentityMode { get; init; }

    public DateTimeOffset LaunchedAt { get; init; }

    public Guid ScoringRuleId { get; init; }

    public string ScoringRuleKey { get; init; }

    public string ScoringRuleVersion { get; init; }

    public string ScoringRuleDocumentHash { get; init; }

    public int SubmittedResponseCount { get; init; }

    public LaunchPacketProvenanceResponse LaunchPacket { get; init; }
}

public sealed record WaveComparisonDisclosurePolicyResponse(
    Guid Id,
    string Version,
    int KMin,
    string SuppressionStrategy);

public sealed record WaveScoreComparisonResponse(
    string DimensionCode,
    string CompatibilityStatus,
    string Disclosure,
    int BaselineSubmittedResponseCount,
    int ComparisonSubmittedResponseCount,
    int LinkedPairCount,
    int? BaselineScoreCount,
    int? ComparisonScoreCount,
    decimal? BaselineMean,
    decimal? ComparisonMean,
    decimal? AggregateDelta,
    decimal? PairedDeltaMean,
    string? SuppressionReason,
    string? CompatibilityReason,
    ScoreInterpretationResponse? BaselineInterpretation = null,
    ScoreInterpretationResponse? ComparisonInterpretation = null,
    int? BaselineNValidTotal = null,
    int? BaselineNExpectedTotal = null,
    string? BaselineMissingPolicyStatusSummary = null,
    int? ComparisonNValidTotal = null,
    int? ComparisonNExpectedTotal = null,
    string? ComparisonMissingPolicyStatusSummary = null,
    string? DisplayLabel = null,
    string? BaselineCalculation = null,
    string? BaselineCalculationLabel = null,
    decimal? BaselineScoreRangeMin = null,
    decimal? BaselineScoreRangeMax = null,
    string? ComparisonCalculation = null,
    string? ComparisonCalculationLabel = null,
    decimal? ComparisonScoreRangeMin = null,
    decimal? ComparisonScoreRangeMax = null);
