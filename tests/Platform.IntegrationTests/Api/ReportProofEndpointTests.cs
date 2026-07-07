using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Reports;
using Platform.IntegrationTests.Support;
using Platform.SharedKernel;

namespace Platform.IntegrationTests.Api;

public sealed class ReportProofEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Report_proof_endpoint_returns_projection()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var projection = new CampaignReportProofResponse(
            campaignId,
            CampaignSeriesId: Guid.NewGuid(),
            CampaignName: "Wave 1",
            CampaignStatus: "live",
            ProofStatus: "proof_only",
            InterpretationStatus: "not_validated_interpretation",
            LaunchSnapshot: new ReportLaunchSnapshotResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hash",
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "anonymous",
                DateTimeOffset.Parse("2026-05-07T12:00:00+00:00")),
            DisclosurePolicy: new ReportDisclosurePolicyResponse(
                Guid.NewGuid(),
                "1.0.0",
                5,
                "hide_cell"),
            Scores:
            [
                new ReportScoreSummaryResponse(
                    "total",
                    "visible",
                    SubmittedResponseCount: 5,
                    ScoreCount: 5,
                    Mean: 3.2m,
                    Median: 3.2m,
                    StandardDeviation: 0.8m,
                    Min: 2m,
                    Max: 4m,
                    SuppressionReason: null,
                    NValidTotal: 4,
                    NExpectedTotal: 5,
                    MissingPolicyStatusSummary: "ok")
            ]);
        using var client = CreateClient(new FakeReportProofStore(
            campaignId,
            Result.Success(projection)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/report-proof",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignReportProofResponse>();
        Assert.NotNull(payload);
        Assert.Equal("proof_only", payload.ProofStatus);
        Assert.Equal("not_validated_interpretation", payload.InterpretationStatus);
        var score = Assert.Single(payload.Scores);
        Assert.Equal("visible", score.Disclosure);
        Assert.Equal(4, score.NValidTotal);
        Assert.Equal(5, score.NExpectedTotal);
        Assert.Equal("ok", score.MissingPolicyStatusSummary);
    }

    [Fact]
    public async Task Report_proof_endpoint_allows_tenant_member_without_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var projection = new CampaignReportProofResponse(
            campaignId,
            CampaignSeriesId: Guid.NewGuid(),
            CampaignName: "Wave 1",
            CampaignStatus: "live",
            ProofStatus: "proof_only",
            InterpretationStatus: "not_validated_interpretation",
            LaunchSnapshot: new ReportLaunchSnapshotResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hash",
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "anonymous",
                DateTimeOffset.Parse("2026-05-07T12:00:00+00:00")),
            DisclosurePolicy: new ReportDisclosurePolicyResponse(
                Guid.NewGuid(),
                "1.0.0",
                5,
                "hide_cell"),
            Scores: []);
        using var client = CreateClient(new FakeReportProofStore(
            campaignId,
            Result.Success(projection)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/report-proof",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wave_comparison_proof_endpoint_returns_projection()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var baselineCampaignId = Guid.NewGuid();
        var comparisonCampaignId = Guid.NewGuid();
        var projection = new CampaignSeriesWaveComparisonProofResponse(
            seriesId,
            "ready",
            "not_validated_interpretation",
            new WaveComparisonWaveResponse(
                baselineCampaignId,
                "Wave 1",
                "live",
                "anonymous_longitudinal",
                DateTimeOffset.Parse("2026-05-08T10:00:00+00:00"),
                Guid.NewGuid(),
                "tenant-burnout.total",
                "1.0.0",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                5),
            new WaveComparisonWaveResponse(
                comparisonCampaignId,
                "Wave 2",
                "live",
                "anonymous_longitudinal",
                DateTimeOffset.Parse("2026-05-08T11:00:00+00:00"),
                Guid.NewGuid(),
                "tenant-burnout.total",
                "1.0.0",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                5),
            new WaveComparisonDisclosurePolicyResponse(Guid.NewGuid(), "1.0.0", 5, "hide_cell"),
            [
                new WaveScoreComparisonResponse(
                    "total",
                    "compatible",
                    "visible",
                    5,
                    5,
                    5,
                    5,
                    5,
                    3.2m,
                    3.8m,
                    0.6m,
                    0.4m,
                    null,
                    null,
                    BaselineNValidTotal: 4,
                    BaselineNExpectedTotal: 5,
                    BaselineMissingPolicyStatusSummary: "ok",
                    ComparisonNValidTotal: 5,
                    ComparisonNExpectedTotal: 5,
                    ComparisonMissingPolicyStatusSummary: "ok")
            ]);
        using var client = CreateClient(
            new FakeReportProofStore(
                Guid.NewGuid(),
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            waveComparisonStore: new FakeWaveComparisonProofStore(seriesId, Result.Success(projection)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaign-series/{seriesId}/wave-comparison-proof",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CampaignSeriesWaveComparisonProofResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ready", payload.ProofStatus);
        var score = Assert.Single(payload.Scores);
        Assert.Equal("compatible", score.CompatibilityStatus);
        Assert.Equal(4, score.BaselineNValidTotal);
        Assert.Equal(5, score.BaselineNExpectedTotal);
        Assert.Equal("ok", score.BaselineMissingPolicyStatusSummary);
        Assert.Equal(5, score.ComparisonNValidTotal);
        Assert.Equal(5, score.ComparisonNExpectedTotal);
        Assert.Equal("ok", score.ComparisonMissingPolicyStatusSummary);
    }

    [Fact]
    public async Task Report_proof_endpoint_maps_missing_launch_snapshot_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(new FakeReportProofStore(
            campaignId,
            Result.Failure<CampaignReportProofResponse>(
                Error.Validation(
                    "report.launch_snapshot_missing",
                    "Campaign must be launched before report proof can be generated."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/campaigns/{campaignId}/report-proof",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("report.launch_snapshot_missing", payload.Title);
    }

    [Fact]
    public async Task Report_proof_export_endpoint_returns_artifact()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: Guid.NewGuid(),
            TargetKind: "campaign",
            TargetId: campaignId,
            TargetLabel: "Campaign",
            CampaignId: campaignId,
            CampaignSeriesId: Guid.NewGuid(),
            ArtifactType: "report_proof_csv_codebook",
            Status: "succeeded",
            Format: "csv_codebook",
            FileName: $"campaign-{campaignId}-report-proof.csv",
            ContentType: "text/csv",
            RowCount: 1,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-07T12:00:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-07T12:00:00+00:00"),
            CsvContent: "campaign_id,dimension_code,disclosure\r\n",
            CodebookJson: """{"artifactType":"report_proof_csv_codebook","columns":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(campaignId, Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/report-proof/exports",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal("report_proof_csv_codebook", payload.ArtifactType);
        Assert.Equal("csv_codebook", payload.Format);
        Assert.Equal("succeeded", payload.Status);
        Assert.True(payload.CanDownload);
        Assert.Null(payload.StartedAt);
        Assert.NotNull(payload.CompletedAt);
        Assert.Null(payload.FailedAt);
        Assert.Null(payload.ExpiresAt);
        Assert.Null(payload.DeletedAt);
        Assert.Null(payload.FailureReasonCode);
        Assert.Equal(1, payload.RowCount);
        Assert.DoesNotContain("token", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Report_proof_export_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaigns/{campaignId}/report-proof/exports",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_series_report_html_artifact_endpoint_returns_metadata_without_inline_html()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: Guid.NewGuid(),
            TargetKind: "campaign_series",
            TargetId: seriesId,
            TargetLabel: "Campaign series",
            CampaignId: null,
            CampaignSeriesId: seriesId,
            ArtifactType: "campaign_series_report_html",
            Status: "succeeded",
            Format: "html",
            FileName: $"campaign-series-{seriesId}-report.html",
            ContentType: "text/html; charset=utf-8",
            RowCount: 3,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-18T14:30:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-18T14:30:01+00:00"),
            CsvContent: "",
            CodebookJson: """{"artifactType":"campaign_series_report_html","sections":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedReportHtmlSeriesId: seriesId,
                reportHtmlResult: Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/report-html-artifacts",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series_report_html", payload.ArtifactType);
        Assert.Equal("campaign_series", payload.TargetKind);
        Assert.Equal(seriesId, payload.TargetId);
        Assert.Equal("html", payload.Format);
        Assert.Equal("text/html; charset=utf-8", payload.ContentType);
        Assert.Equal("", payload.CsvContent);
        Assert.DoesNotContain("<html", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Campaign_series_report_html_artifact_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/report-html-artifacts",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_series_report_pdf_artifact_endpoint_returns_metadata_without_inline_pdf()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: Guid.NewGuid(),
            TargetKind: "campaign_series",
            TargetId: seriesId,
            TargetLabel: "Campaign series",
            CampaignId: null,
            CampaignSeriesId: seriesId,
            ArtifactType: "campaign_series_report_pdf",
            Status: "succeeded",
            Format: "pdf",
            FileName: $"campaign-series-{seriesId}-report.pdf",
            ContentType: "application/pdf",
            RowCount: 3,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-18T18:30:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-18T18:30:01+00:00"),
            CsvContent: "",
            CodebookJson: """{"artifactType":"campaign_series_report_pdf","sections":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedReportPdfSeriesId: seriesId,
                reportPdfResult: Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/report-pdf-artifacts",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series_report_pdf", payload.ArtifactType);
        Assert.Equal("campaign_series", payload.TargetKind);
        Assert.Equal(seriesId, payload.TargetId);
        Assert.Equal("pdf", payload.Format);
        Assert.Equal("application/pdf", payload.ContentType);
        Assert.Equal("", payload.CsvContent);
        Assert.True(payload.CanDownload);
    }

    [Fact]
    public async Task Campaign_series_report_pdf_artifact_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/report-pdf-artifacts",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_artifact_retry_endpoint_returns_new_queued_pdf_artifact()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var failedArtifactId = Guid.NewGuid();
        var retryArtifactId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var retryArtifact = new ReportProofExportArtifactResponse(
            Id: retryArtifactId,
            TargetKind: "campaign_series",
            TargetId: seriesId,
            TargetLabel: "Campaign series",
            CampaignId: null,
            CampaignSeriesId: seriesId,
            ArtifactType: "campaign_series_report_pdf",
            Status: "queued",
            Format: "pdf",
            FileName: $"campaign-series-{seriesId}-report.pdf",
            ContentType: "application/pdf",
            RowCount: 0,
            ByteSize: 0,
            ChecksumSha256: null,
            CreatedAt: DateTimeOffset.Parse("2026-05-18T21:30:00+00:00"),
            CompletedAt: null,
            CsvContent: "",
            CodebookJson: "{}",
            CanDownload: false);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: failedArtifactId,
                retryArtifactResult: Result.Success(retryArtifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/export-artifacts/{failedArtifactId}/retry",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal(retryArtifactId, payload.Id);
        Assert.Equal("campaign_series_report_pdf", payload.ArtifactType);
        Assert.Equal("queued", payload.Status);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.False(payload.CanDownload);
    }

    [Fact]
    public async Task Export_artifact_retry_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var failedArtifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: failedArtifactId,
                retryArtifactResult: Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/export-artifacts/{failedArtifactId}/retry",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_series_response_export_endpoint_returns_artifact()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: Guid.NewGuid(),
            TargetKind: "campaign_series",
            TargetId: seriesId,
            TargetLabel: "Campaign series",
            CampaignId: null,
            CampaignSeriesId: seriesId,
            ArtifactType: "campaign_series_response_csv_codebook",
            Status: "succeeded",
            Format: "csv_codebook",
            FileName: $"campaign-series-{seriesId}-responses.csv",
            ContentType: "text/csv",
            RowCount: 2,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-09T12:00:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-09T12:00:00+00:00"),
            CsvContent: "response_row_id,trajectory_id,campaign_series_id,answer_q01\r\nr000001,,series-id,4\r\n",
            CodebookJson: """{"artifactType":"campaign_series_response_csv_codebook","columns":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedResponseExportSeriesId: seriesId,
                responseExportResult: Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/response-exports",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series_response_csv_codebook", payload.ArtifactType);
        Assert.Equal("campaign_series", payload.TargetKind);
        Assert.Equal(seriesId, payload.TargetId);
        Assert.Null(payload.CampaignId);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.True(payload.CanDownload);
        Assert.Null(payload.FailureReasonCode);
        Assert.Equal(2, payload.RowCount);
        Assert.DoesNotContain("assignment", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Campaign_series_results_matrix_export_endpoint_returns_aggregate_artifact()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: Guid.NewGuid(),
            TargetKind: "campaign_series",
            TargetId: seriesId,
            TargetLabel: "Campaign series",
            CampaignId: null,
            CampaignSeriesId: seriesId,
            ArtifactType: "campaign_series_results_matrix_csv_codebook",
            Status: "succeeded",
            Format: "csv_codebook",
            FileName: $"campaign-series-{seriesId}-results-matrix.csv",
            ContentType: "text/csv",
            RowCount: 3,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-26T12:00:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-26T12:00:00+00:00"),
            CsvContent: "result_scope,dimension_code,mean\r\noverall,workload,4.2\r\n",
            CodebookJson: """{"artifactType":"campaign_series_results_matrix_csv_codebook","columns":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedResultsMatrixExportSeriesId: seriesId,
                resultsMatrixExportResult: Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Post,
            $"/campaign-series/{seriesId}/results-matrix-exports",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal("campaign_series_results_matrix_csv_codebook", payload.ArtifactType);
        Assert.Equal("campaign_series", payload.TargetKind);
        Assert.Equal(seriesId, payload.TargetId);
        Assert.Null(payload.CampaignId);
        Assert.Equal(seriesId, payload.CampaignSeriesId);
        Assert.True(payload.CanDownload);
        Assert.DoesNotContain("response_session", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Export_artifact_endpoint_returns_stored_artifact()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var artifact = new ReportProofExportArtifactResponse(
            Id: artifactId,
            TargetKind: "campaign",
            TargetId: campaignId,
            TargetLabel: "Campaign",
            CampaignId: campaignId,
            CampaignSeriesId: Guid.NewGuid(),
            ArtifactType: "report_proof_csv_codebook",
            Status: "succeeded",
            Format: "csv_codebook",
            FileName: $"campaign-{campaignId}-report-proof.csv",
            ContentType: "text/csv",
            RowCount: 1,
            ByteSize: 512,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CreatedAt: DateTimeOffset.Parse("2026-05-08T12:00:00+00:00"),
            CompletedAt: DateTimeOffset.Parse("2026-05-08T12:00:00+00:00"),
            CsvContent: "campaign_id,dimension_code,disclosure\r\n",
            CodebookJson: """{"artifactType":"report_proof_csv_codebook","columns":[]}""",
            CanDownload: true);
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                artifactResult: Result.Success(artifact)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReportProofExportArtifactResponse>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload.Id);
        Assert.Equal("report_proof_csv_codebook", payload.ArtifactType);
        Assert.True(payload.CanDownload);
        Assert.Null(payload.FailureReasonCode);
        Assert.Equal("campaign_id,dimension_code,disclosure\r\n", payload.CsvContent);
        Assert.DoesNotContain("token", payload.CsvContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Export_artifact_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                artifactResult: Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_artifact_download_endpoint_returns_csv_attachment()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var fileName = $"campaign-{campaignId}-report-proof.csv";
        var download = new ExportArtifactDownloadResponse(
            artifactId,
            fileName,
            "text/csv",
            ByteSize: 44,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Content: "campaign_id,dimension_code,disclosure\r\n");
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                downloadResult: Result.Success(download)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/download",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fileName, response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal("campaign_id,dimension_code,disclosure\r\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Export_artifact_download_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                downloadResult: Result.Failure<ExportArtifactDownloadResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/download",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_artifact_download_endpoint_maps_not_downloadable_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                downloadResult: Result.Failure<ExportArtifactDownloadResponse>(
                    Error.Conflict(
                        "export_artifact.not_downloadable",
                        "Export artifact is not downloadable."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/download",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("export_artifact.not_downloadable", payload.Title);
    }

    [Fact]
    public async Task Export_artifact_signed_download_url_endpoint_returns_short_lived_url_without_storage_internals()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var signedUrl = new ExportArtifactSignedDownloadUrlResponse(
            artifactId,
            $"campaign-{campaignId}-report.pdf",
            "application/pdf",
            ByteSize: 2048,
            ChecksumSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Url: "https://object-store.example.test/artifact-bucket/reports/report.pdf?X-Amz-Signature=safe-signature",
            ExpiresAt: DateTimeOffset.Parse("2026-05-18T20:15:00+00:00"));
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                signedDownloadUrlResult: Result.Success(signedUrl)));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/signed-download-url",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<ExportArtifactSignedDownloadUrlResponse>();
        Assert.NotNull(payload);
        Assert.Equal(artifactId, payload.Id);
        Assert.Equal("application/pdf", payload.ContentType);
        Assert.Equal(2048, payload.ByteSize);
        Assert.Contains("X-Amz-Signature", payload.Url, StringComparison.Ordinal);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T20:15:00+00:00"), payload.ExpiresAt);
        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-key", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Export_artifact_signed_download_url_endpoint_requires_setup_manage_permission()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                signedDownloadUrlResult: Result.Failure<ExportArtifactSignedDownloadUrlResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/signed-download-url",
            tenantId,
            permissions: null);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_artifact_signed_download_url_endpoint_maps_not_downloadable_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                signedDownloadUrlResult: Result.Failure<ExportArtifactSignedDownloadUrlResponse>(
                    Error.Conflict(
                        "export_artifact.not_downloadable",
                        "Export artifact is not downloadable."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}/signed-download-url",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("export_artifact.not_downloadable", payload.Title);
    }

    [Fact]
    public async Task Export_artifact_endpoint_maps_missing_artifact_to_problem_details()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        using var client = CreateClient(
            new FakeReportProofStore(
                campaignId,
                Result.Failure<CampaignReportProofResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found."))),
            new FakeReportProofExportStore(
                campaignId,
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign.not_found", "Campaign was not found.")),
                expectedArtifactId: artifactId,
                artifactResult: Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found."))));
        using var request = AuthenticatedRequest(
            HttpMethod.Get,
            $"/export-artifacts/{artifactId}",
            tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(payload);
        Assert.Equal("export_artifact.not_found", payload.Title);
    }

    private HttpClient CreateClient(
        IReportProofStore store,
        IReportProofExportStore? exportStore = null,
        IWaveComparisonProofStore? waveComparisonStore = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                services.AddSingleton(store);
                if (exportStore is not null)
                {
                    services.AddSingleton(exportStore);
                }

                if (waveComparisonStore is not null)
                {
                    services.AddSingleton(waveComparisonStore);
                }
            });
        }).CreateClient();
    }

    private static HttpRequestMessage AuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid tenantId,
        string? permissions = "setup.manage")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.TenantMembershipsHeader, tenantId.ToString());
        if (permissions is not null)
        {
            request.Headers.Add(TestAuthHandler.PermissionsHeader, permissions);
        }

        return request;
    }

    private sealed class FakeReportProofStore(
        Guid expectedCampaignId,
        Result<CampaignReportProofResponse> result) : IReportProofStore
    {
        public Task<Result<CampaignReportProofResponse>> GetCampaignReportProofAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCampaignId, campaignId);

            return Task.FromResult(result);
        }
    }

    private sealed class FakeWaveComparisonProofStore(
        Guid expectedCampaignSeriesId,
        Result<CampaignSeriesWaveComparisonProofResponse> result) : IWaveComparisonProofStore
    {
        public Task<Result<CampaignSeriesWaveComparisonProofResponse>> GetCampaignSeriesWaveComparisonProofAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCampaignSeriesId, campaignSeriesId);

            return Task.FromResult(result);
        }
    }

    private sealed class FakeReportProofExportStore(
        Guid expectedCampaignId,
        Result<ReportProofExportArtifactResponse> result,
        Guid? expectedArtifactId = null,
        Result<ReportProofExportArtifactResponse>? artifactResult = null,
        Result<ReportProofExportArtifactResponse>? retryArtifactResult = null,
        Result<ExportArtifactDownloadResponse>? downloadResult = null,
        Result<ExportArtifactSignedDownloadUrlResponse>? signedDownloadUrlResult = null,
        Guid? expectedResponseExportSeriesId = null,
        Result<ReportProofExportArtifactResponse>? responseExportResult = null,
        Guid? expectedResultsMatrixExportSeriesId = null,
        Result<ReportProofExportArtifactResponse>? resultsMatrixExportResult = null,
        Guid? expectedReportHtmlSeriesId = null,
        Result<ReportProofExportArtifactResponse>? reportHtmlResult = null,
        Guid? expectedReportPdfSeriesId = null,
        Result<ReportProofExportArtifactResponse>? reportPdfResult = null) : IReportProofExportStore
    {
        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignReportProofExportAsync(
            Guid tenantId,
            Guid campaignId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCampaignId, campaignId);

            return Task.FromResult(result);
        }

        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResponseExportAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedResponseExportSeriesId, campaignSeriesId);

            return Task.FromResult(
                responseExportResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResultsMatrixExportAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedResultsMatrixExportSeriesId, campaignSeriesId);

            return Task.FromResult(
                resultsMatrixExportResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportHtmlArtifactAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedReportHtmlSeriesId, campaignSeriesId);

            return Task.FromResult(
                reportHtmlResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportPdfArtifactAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedReportPdfSeriesId, campaignSeriesId);

            return Task.FromResult(
                reportPdfResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> QueueCampaignSeriesReportPdfArtifactAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedReportPdfSeriesId, campaignSeriesId);

            return Task.FromResult(
                reportPdfResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> ProcessCampaignSeriesReportPdfArtifactAsync(
            Guid tenantId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);

            return Task.FromResult(
                artifactResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }

        public Task<Result<ReportPdfArtifactWorkerRunResponse>> ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
            Guid tenantId,
            int maxArtifacts,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                ProcessedArtifactCount: 0)));
        }

        public Task<Result<ReportPdfArtifactWorkerRunResponse>> FailStaleCampaignSeriesReportPdfArtifactsAsync(
            Guid tenantId,
            DateTimeOffset staleBefore,
            int maxArtifacts,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                ProcessedArtifactCount: 0)));
        }

        public Task<Result<ReportProofExportArtifactResponse>> GetExportArtifactAsync(
            Guid tenantId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);

            return Task.FromResult(
                artifactResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }

        public Task<Result<ReportProofExportArtifactResponse>> RetryCampaignSeriesReportPdfArtifactAsync(
            Guid tenantId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);

            return Task.FromResult(
                retryArtifactResult ??
                Result.Failure<ReportProofExportArtifactResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }

        public Task<Result<ExportArtifactDownloadResponse>> GetExportArtifactDownloadAsync(
            Guid tenantId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);

            return Task.FromResult(
                downloadResult ??
                Result.Failure<ExportArtifactDownloadResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }

        public Task<Result<ExportArtifactDownloadResponse>> GetExportArtifactCodebookDownloadAsync(
            Guid tenantId,
            Guid artifactId,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);

            return Task.FromResult(
                downloadResult ??
                Result.Failure<ExportArtifactDownloadResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }

        public Task<Result<ExportArtifactSignedDownloadUrlResponse>> GetExportArtifactSignedDownloadUrlAsync(
            Guid tenantId,
            Guid artifactId,
            TimeSpan expiresIn,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedArtifactId, artifactId);
            Assert.Equal(TimeSpan.FromMinutes(15), expiresIn);

            return Task.FromResult(
                signedDownloadUrlResult ??
                Result.Failure<ExportArtifactSignedDownloadUrlResponse>(
                    Error.NotFound("export_artifact.not_found", "Export artifact was not found.")));
        }
    }
}
