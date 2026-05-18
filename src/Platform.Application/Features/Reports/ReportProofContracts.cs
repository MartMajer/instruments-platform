using System.Text.Json.Serialization;

namespace Platform.Application.Features.Reports;

public sealed record CampaignReportProofResponse(
    Guid CampaignId,
    Guid? CampaignSeriesId,
    string CampaignName,
    string CampaignStatus,
    string ProofStatus,
    string InterpretationStatus,
    ReportLaunchSnapshotResponse LaunchSnapshot,
    ReportDisclosurePolicyResponse DisclosurePolicy,
    IReadOnlyList<ReportScoreSummaryResponse> Scores,
    DateTimeOffset? ClosedAt = null,
    string DataFinality = "not_reportable");

public sealed record ReportLaunchSnapshotResponse
{
    [JsonConstructor]
    public ReportLaunchSnapshotResponse(
        Guid id,
        Guid templateVersionId,
        Guid scoringRuleId,
        string scoringRuleDocumentHash,
        Guid? consentDocumentId,
        Guid? retentionPolicyId,
        Guid? disclosurePolicyId,
        string responseIdentityMode,
        DateTimeOffset launchedAt,
        LaunchPacketProvenanceResponse? launchPacket = null)
    {
        Id = id;
        TemplateVersionId = templateVersionId;
        ScoringRuleId = scoringRuleId;
        ScoringRuleDocumentHash = scoringRuleDocumentHash;
        ConsentDocumentId = consentDocumentId;
        RetentionPolicyId = retentionPolicyId;
        DisclosurePolicyId = disclosurePolicyId;
        ResponseIdentityMode = responseIdentityMode;
        LaunchedAt = launchedAt;
        LaunchPacket = launchPacket ?? new LaunchPacketProvenanceResponse(0, [], "missing");
    }

    public Guid Id { get; init; }

    public Guid TemplateVersionId { get; init; }

    public Guid ScoringRuleId { get; init; }

    public string ScoringRuleDocumentHash { get; init; }

    public Guid? ConsentDocumentId { get; init; }

    public Guid? RetentionPolicyId { get; init; }

    public Guid? DisclosurePolicyId { get; init; }

    public string ResponseIdentityMode { get; init; }

    public DateTimeOffset LaunchedAt { get; init; }

    public LaunchPacketProvenanceResponse LaunchPacket { get; init; }
}

public sealed record LaunchPacketProvenanceResponse(
    int SchemaVersion,
    IReadOnlyList<string> Sections,
    string Source = "unknown");

public sealed record ReportDisclosurePolicyResponse(
    Guid Id,
    string Version,
    int KMin,
    string SuppressionStrategy);

public sealed record ScoreInterpretationResponse(
    string Status,
    string Source,
    string BandCode,
    string Label,
    string Provenance,
    bool IsValidated,
    bool IsOfficial);

public sealed record ReportScoreSummaryResponse(
    string DimensionCode,
    string Disclosure,
    int SubmittedResponseCount,
    int? ScoreCount,
    decimal? Mean,
    decimal? Min,
    decimal? Max,
    string? SuppressionReason,
    ScoreInterpretationResponse? Interpretation = null,
    int? NValidTotal = null,
    int? NExpectedTotal = null,
    string? MissingPolicyStatusSummary = null);
