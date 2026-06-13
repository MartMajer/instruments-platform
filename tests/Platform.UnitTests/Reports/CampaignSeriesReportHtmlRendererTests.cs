using System.Text.Json;
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
            ],
            ResultsAnalytics: new CampaignSeriesResultsAnalyticsResponse(
                SelectedCampaignId: null,
                SelectedCampaignName: null,
                DisclosureKMin: 5,
                DisclosureState: "visible",
                ScoreOutputs:
                [
                    new CampaignSeriesResultsScoreOutputResponse(
                        "total",
                        "visible",
                        SubmittedResponseCount: 5,
                        ScoreCount: 5,
                        Mean: 4.2m,
                        Median: 4.0m,
                        StandardDeviation: 0.4m,
                        Min: 3.8m,
                        Max: 4.8m,
                        NValidTotal: 5,
                        NExpectedTotal: 5,
                        MissingPolicyStatusSummary: "ok",
                        SuppressionReason: null)
                ],
                GroupRows:
                [
                    new CampaignSeriesResultsGroupMatrixRowResponse(
                        "department",
                        "ICU",
                        "total",
                        "visible",
                        SubmittedResponseCount: 5,
                        ScoreCount: 5,
                        Mean: 4.2m,
                        Median: 4.0m,
                        StandardDeviation: 0.4m,
                        Min: 3.8m,
                        Max: 4.8m,
                        SuppressionReason: null)
                ],
                WaveRows:
                [
                    new CampaignSeriesResultsWaveMatrixRowResponse(
                        Guid.NewGuid(),
                        "Wave 1",
                        "closed",
                        "closed_wave",
                        DateTimeOffset.Parse("2026-05-18T16:00:00+00:00"),
                        "total",
                        "visible",
                        SubmittedResponseCount: 5,
                        ScoreCount: 5,
                        Mean: 4.2m,
                        Median: 4.0m,
                        StandardDeviation: 0.4m,
                        Min: 3.8m,
                        Max: 4.8m,
                        SuppressionReason: null,
                        DeltaFromPreviousMean: 0.2m,
                        DeltaFromFirstMean: 0.2m,
                        ComparisonState: "comparable")
                ],
                Insights:
                [
                    new CampaignSeriesResultsInsightResponse(
                        "missingness",
                        "info",
                        "Missing policy satisfied",
                        "All expected score inputs were available.")
                ]));

        var result = renderer.Render(workspace, generatedAt);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; Report", result.Html, StringComparison.Ordinal);
        Assert.Contains("Instruments Platform", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-branding-source=\"platform_default\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-brand-accent=\"#2563eb\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("Result charts", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-chart-source=\"server_inline_svg\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("<svg role=\"img\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("Result outputs", result.Html, StringComparison.Ordinal);
        Assert.Contains("Group breakdowns", result.Html, StringComparison.Ordinal);
        Assert.Contains("Wave trends and finality", result.Html, StringComparison.Ordinal);
        Assert.Contains("Method and safeguards", result.Html, StringComparison.Ordinal);
        Assert.Contains("total", result.Html, StringComparison.Ordinal);
        Assert.Contains("4.2", result.Html, StringComparison.Ordinal);
        Assert.Contains("closed_wave", result.Html, StringComparison.Ordinal);
        Assert.Contains("Missing policy satisfied", result.Html, StringComparison.Ordinal);
        Assert.Contains("server_inline_svg", result.CodebookJson, StringComparison.Ordinal);
        Assert.Contains("result_charts", result.CodebookJson, StringComparison.Ordinal);
        Assert.Contains("result_outputs", result.CodebookJson, StringComparison.Ordinal);
        Assert.Contains("typed_slots_no_arbitrary_css", result.CodebookJson, StringComparison.Ordinal);
        Assert.Contains("organization_label", result.CodebookJson, StringComparison.Ordinal);
        Assert.Equal(5, result.RowCount);
        Assert.DoesNotContain("<script>", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-field=\"generated-at\"", result.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("wdr_", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant_code", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recipient", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_message", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", result.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_applies_typed_branding_slots_and_falls_back_from_unsafe_tokens()
    {
        var renderer = new CampaignSeriesReportHtmlRenderer();
        var generatedAt = DateTimeOffset.Parse("2026-05-18T15:00:00+00:00");
        var workspace = CreateMinimalWorkspace("Tenant series");

        var result = renderer.Render(
            workspace,
            generatedAt,
            new CampaignSeriesReportBranding(
                "Acme <Wellbeing>",
                "Quarterly <Risk> Report",
                "url(javascript:alert(1))",
                "custom-css",
                "explicit_input"));

        Assert.Contains("Acme &lt;Wellbeing&gt;", result.Html, StringComparison.Ordinal);
        Assert.Contains("Quarterly &lt;Risk&gt; Report", result.Html, StringComparison.Ordinal);
        Assert.Contains("Campaign series: Tenant series", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-branding-source=\"explicit_input\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-brand-accent=\"#2563eb\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("data-layout-variant=\"standard\"", result.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript", result.Html, StringComparison.OrdinalIgnoreCase);

        using var codebook = JsonDocument.Parse(result.CodebookJson);
        var branding = codebook.RootElement.GetProperty("branding");
        Assert.Equal("typed_slots_no_arbitrary_css", branding.GetProperty("boundary").GetString());
        Assert.Equal("explicit_input", branding.GetProperty("brandingSource").GetString());
        Assert.Equal("Acme <Wellbeing>", branding.GetProperty("organizationLabel").GetString());
        Assert.Equal("Quarterly <Risk> Report", branding.GetProperty("reportTitle").GetString());
        Assert.Equal("#2563eb", branding.GetProperty("accentColorHex").GetString());
        Assert.Equal("standard", branding.GetProperty("layoutVariant").GetString());
        Assert.Equal("none", branding.GetProperty("logoMode").GetString());
        Assert.Contains(
            branding.GetProperty("allowedSlots").EnumerateArray(),
            slot => slot.GetString() == "accent_color_hex");
    }

    private static CampaignSeriesReportsWorkspaceResponse CreateMinimalWorkspace(string seriesName)
    {
        return new CampaignSeriesReportsWorkspaceResponse(
            new CampaignSeriesReportsSeriesResponse(
                Guid.NewGuid(),
                seriesName,
                DateTimeOffset.Parse("2026-05-18T14:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-18T14:05:00+00:00")),
            new CampaignSeriesReportsSummaryResponse(
                CampaignCount: 0,
                LiveCampaignCount: 0,
                ReportableCampaignCount: 0,
                SubmittedResponseCount: 0,
                ScoreCount: 0,
                ExportArtifactCount: 0,
                VisibleScoreCount: 0,
                SuppressedScoreCount: 0,
                MissingPrerequisiteCount: 0,
                PreliminaryLiveReportCount: 0,
                ClosedWaveReportCount: 0),
            SelectedCampaign: null,
            MissingPrerequisites: [],
            ExportArtifacts: [],
            Campaigns: [],
            ResultsAnalytics: null);
    }
}
