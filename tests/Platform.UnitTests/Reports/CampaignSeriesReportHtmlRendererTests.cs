using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.Reports;

namespace Platform.UnitTests.Reports;

public sealed class CampaignSeriesReportHtmlRendererTests
{
    [Fact]
    public void Render_encodes_html_and_redacts_sensitive_sentinels()
    {
        var renderer = new CampaignSeriesReportHtmlRenderer();
        var generatedAt = DateTimeOffset.Parse("2026-05-18T15:00:00+00:00");
        var workspace = new CampaignSeriesReportsWorkspaceResponse(
            new CampaignSeriesReportsSeriesResponse(
                Guid.NewGuid(),
                "<script>alert(1)</script> Report",
                DateTimeOffset.Parse("2026-05-18T14:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-18T14:05:00+00:00")),
            new CampaignSeriesReportsSummaryResponse(
                CampaignCount: 1,
                LiveCampaignCount: 1,
                ReportableCampaignCount: 1,
                SubmittedResponseCount: 5,
                ScoreCount: 5,
                ExportArtifactCount: 0,
                VisibleScoreCount: 5,
                SuppressedScoreCount: 0,
                MissingPrerequisiteCount: 0,
                PreliminaryLiveReportCount: 1,
                ClosedWaveReportCount: 0),
            new CampaignSeriesReportsCampaignResponse(
                Guid.NewGuid(),
                "wdr_secret participant_code recipient@example.test provider_message token",
                "live",
                "anonymous",
                "en",
                LatestLaunchSnapshotId: Guid.NewGuid(),
                LatestLaunchAt: DateTimeOffset.Parse("2026-05-18T14:10:00+00:00"),
                ScoringRuleId: Guid.NewGuid(),
                ConsentDocumentId: Guid.NewGuid(),
                RetentionPolicyId: Guid.NewGuid(),
                DisclosurePolicyId: Guid.NewGuid(),
                SubmittedResponseCount: 5,
                ScoreCount: 5,
                ExportArtifactCount: 0,
                VisibleScoreCount: 5,
                SuppressedScoreCount: 0,
                DisclosureState: "visible",
                DisclosureKMin: 5,
                ReportStatus: "proof_only",
                InterpretationStatus: "not_validated_interpretation",
                LatestExportArtifactId: null,
                LatestExportArtifactFileName: null,
                LatestExportArtifactStatus: null,
                LatestExportArtifactCreatedAt: null,
                LatestExportArtifactCompletedAt: null,
                LatestExportArtifactStartedAt: null,
                LatestExportArtifactFailedAt: null,
                LatestExportArtifactExpiresAt: null,
                LatestExportArtifactDeletedAt: null,
                LatestExportArtifactFailureReasonCode: null,
                LatestExportArtifactCanDownload: false,
                DataFinality: "preliminary_live"),
            MissingPrerequisites: [],
            ExportArtifacts: [],
            Campaigns:
            [
                new CampaignSeriesReportsCampaignResponse(
                    Guid.NewGuid(),
                    "wdr_secret participant_code recipient@example.test provider_message token",
                    "live",
                    "anonymous",
                    "en",
                    LatestLaunchSnapshotId: Guid.NewGuid(),
                    LatestLaunchAt: DateTimeOffset.Parse("2026-05-18T14:10:00+00:00"),
                    ScoringRuleId: Guid.NewGuid(),
                    ConsentDocumentId: Guid.NewGuid(),
                    RetentionPolicyId: Guid.NewGuid(),
                    DisclosurePolicyId: Guid.NewGuid(),
                    SubmittedResponseCount: 5,
                    ScoreCount: 5,
                    ExportArtifactCount: 0,
                    VisibleScoreCount: 5,
                    SuppressedScoreCount: 0,
                    DisclosureState: "visible",
                    DisclosureKMin: 5,
                    ReportStatus: "proof_only",
                    InterpretationStatus: "not_validated_interpretation",
                    LatestExportArtifactId: null,
                    LatestExportArtifactFileName: null,
                    LatestExportArtifactStatus: null,
                    LatestExportArtifactCreatedAt: null,
                    LatestExportArtifactCompletedAt: null,
                    LatestExportArtifactStartedAt: null,
                    LatestExportArtifactFailedAt: null,
                    LatestExportArtifactExpiresAt: null,
                    LatestExportArtifactDeletedAt: null,
                    LatestExportArtifactFailureReasonCode: null,
                    LatestExportArtifactCanDownload: false,
                    DataFinality: "preliminary_live")
            ]);

        var result = renderer.Render(workspace, generatedAt);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; Report", result.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-field=\"generated-at\"", result.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("wdr_", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", result.Html, StringComparison.OrdinalIgnoreCase);
    }
}
