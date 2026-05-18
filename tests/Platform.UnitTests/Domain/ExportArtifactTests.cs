using Platform.Domain.Reports;

namespace Platform.UnitTests.Domain;

public sealed class ExportArtifactTests
{
    private const string Sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Export_artifact_accepts_succeeded_report_proof_csv_codebook()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        var completedAt = DateTimeOffset.Parse("2026-05-07T12:00:01+00:00");

        var artifact = new ExportArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactTargetKinds.Campaign,
            campaignId: Guid.NewGuid(),
            campaignSeriesId: Guid.NewGuid(),
            ExportArtifactTypes.ReportProofCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            "  report-proof.csv  ",
            "  text/csv  ",
            rowCount: 1,
            byteSize: 128,
            Sha256,
            """{"artifactType":"report_proof_csv_codebook"}""",
            "campaign_id,dimension_code",
            """{"columns":[]}""",
            createdAt,
            completedAt);

        Assert.Equal(ExportArtifactTypes.ReportProofCsvCodebook, artifact.ArtifactType);
        Assert.Equal(ExportArtifactTargetKinds.Campaign, artifact.TargetKind);
        Assert.NotNull(artifact.CampaignId);
        Assert.Equal(ExportArtifactStatuses.Succeeded, artifact.Status);
        Assert.Equal(ExportArtifactFormats.CsvCodebook, artifact.Format);
        Assert.Equal("report-proof.csv", artifact.FileName);
        Assert.Equal("text/csv", artifact.ContentType);
        Assert.Equal(ExportArtifactStorageKinds.InlineText, artifact.StorageKind);
        Assert.Null(artifact.StorageKey);
        Assert.Equal(1, artifact.RowCount);
        Assert.Equal(128, artifact.ByteSize);
        Assert.Equal(Sha256, artifact.ChecksumSha256);
        Assert.Equal(createdAt, artifact.CreatedAt);
        Assert.Equal(completedAt, artifact.CompletedAt);
    }

    [Fact]
    public void Export_artifact_accepts_campaign_series_response_csv_codebook()
    {
        var campaignSeriesId = Guid.NewGuid();

        var artifact = ValidArtifact(
            targetKind: ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: campaignSeriesId,
            artifactType: ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            metadataJson: """{"artifactType":"campaign_series_response_csv_codebook"}""");

        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, artifact.TargetKind);
        Assert.Null(artifact.CampaignId);
        Assert.Equal(campaignSeriesId, artifact.CampaignSeriesId);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesResponseCsvCodebook, artifact.ArtifactType);
    }

    [Fact]
    public void Export_artifact_accepts_external_object_storage_metadata_without_inline_download()
    {
        var artifact = ValidArtifact(
            targetKind: ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: Guid.NewGuid(),
            artifactType: ExportArtifactTypes.CampaignSeriesReportHtml,
            format: ExportArtifactFormats.Html,
            content: null,
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: "tenants/tenant-a/reports/report.html");

        Assert.Equal(ExportArtifactStorageKinds.ExternalObject, artifact.StorageKind);
        Assert.Equal("tenants/tenant-a/reports/report.html", artifact.StorageKey);
        Assert.Null(artifact.Content);
        Assert.False(artifact.CanDownload);
        Assert.Equal(Sha256, artifact.ChecksumSha256);
    }

    [Fact]
    public void Export_artifact_rejects_invalid_external_object_storage_shape()
    {
        Assert.Throws<ArgumentException>(() => ValidArtifact(
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: null,
            content: null));
        Assert.Throws<ArgumentException>(() => ValidArtifact(
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: "tenants/tenant-a/reports/report.html",
            content: "inline content is not allowed"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(
            storageKind: "file_share",
            storageKey: "tenants/tenant-a/reports/report.html",
            content: null));
    }

    [Fact]
    public void Export_artifact_accepts_campaign_series_report_html()
    {
        var campaignSeriesId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-18T14:30:00+00:00");
        var completedAt = DateTimeOffset.Parse("2026-05-18T14:30:01+00:00");

        Assert.True(ExportArtifactTypes.IsKnown("campaign_series_report_html"));
        Assert.True(ExportArtifactFormats.IsKnown("html"));

        var artifact = new ExportArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: campaignSeriesId,
            "campaign_series_report_html",
            ExportArtifactStatuses.Succeeded,
            "html",
            "series-report.html",
            "text/html; charset=utf-8",
            rowCount: 3,
            byteSize: 512,
            Sha256,
            """{"artifactType":"campaign_series_report_html","templateId":"campaign-series-report"}""",
            "<!doctype html><html><body><h1>Report</h1></body></html>",
            """{"sections":["summary","provenance"]}""",
            createdAt,
            completedAt);

        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, artifact.TargetKind);
        Assert.Null(artifact.CampaignId);
        Assert.Equal(campaignSeriesId, artifact.CampaignSeriesId);
        Assert.Equal("campaign_series_report_html", artifact.ArtifactType);
        Assert.Equal("html", artifact.Format);
        Assert.Equal("series-report.html", artifact.FileName);
        Assert.Equal("text/html; charset=utf-8", artifact.ContentType);
        Assert.True(artifact.CanDownload);
    }

    [Fact]
    public void Export_artifact_accepts_campaign_series_report_pdf_external_object()
    {
        var campaignSeriesId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-18T18:30:00+00:00");
        var completedAt = DateTimeOffset.Parse("2026-05-18T18:30:01+00:00");

        Assert.True(ExportArtifactTypes.IsKnown(ExportArtifactTypes.CampaignSeriesReportPdf));
        Assert.True(ExportArtifactFormats.IsKnown(ExportArtifactFormats.Pdf));

        var artifact = new ExportArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: campaignSeriesId,
            ExportArtifactTypes.CampaignSeriesReportPdf,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.Pdf,
            "series-report.pdf",
            "application/pdf",
            rowCount: 3,
            byteSize: 512,
            Sha256,
            """{"artifactType":"campaign_series_report_pdf","templateId":"campaign-series-report"}""",
            content: null,
            codebookJson: """{"artifactType":"campaign_series_report_pdf","sections":["summary","provenance"]}""",
            createdAt,
            completedAt,
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: "tenants/tenant-a/campaign-series/series-a/reports/report.pdf");

        Assert.Equal(ExportArtifactTargetKinds.CampaignSeries, artifact.TargetKind);
        Assert.Null(artifact.CampaignId);
        Assert.Equal(campaignSeriesId, artifact.CampaignSeriesId);
        Assert.Equal(ExportArtifactTypes.CampaignSeriesReportPdf, artifact.ArtifactType);
        Assert.Equal(ExportArtifactFormats.Pdf, artifact.Format);
        Assert.Equal("series-report.pdf", artifact.FileName);
        Assert.Equal("application/pdf", artifact.ContentType);
        Assert.Equal(ExportArtifactStorageKinds.ExternalObject, artifact.StorageKind);
        Assert.Equal("tenants/tenant-a/campaign-series/series-a/reports/report.pdf", artifact.StorageKey);
        Assert.Null(artifact.Content);
        Assert.False(artifact.CanDownload);
    }

    [Fact]
    public void Export_artifact_rejects_unknown_status_format_type()
    {
        Assert.Throws<ArgumentException>(() => ValidArtifact(artifactType: "raw_dump"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(status: "half_done"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(format: "spreadsheet"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(targetKind: "series"));
    }

    [Fact]
    public void Export_artifact_accepts_all_lifecycle_statuses()
    {
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Queued));
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Rendering));
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Succeeded));
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Failed));
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Expired));
        Assert.True(ExportArtifactStatuses.IsKnown(ExportArtifactStatuses.Deleted));
    }

    [Fact]
    public void Queued_export_artifact_has_no_materialized_content()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-09T12:00:00+00:00");

        var artifact = ValidArtifact(
            status: ExportArtifactStatuses.Queued,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            createdAt: createdAt,
            completedAt: null);

        Assert.Equal(ExportArtifactStatuses.Queued, artifact.Status);
        Assert.False(artifact.CanDownload);
        Assert.Null(artifact.ChecksumSha256);
        Assert.Null(artifact.Content);
        Assert.Null(artifact.StartedAt);
        Assert.Null(artifact.CompletedAt);
        Assert.Null(artifact.FailedAt);
        Assert.Null(artifact.DeletedAt);
    }

    [Fact]
    public void Succeeded_export_artifact_requires_materialized_content()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-09T12:00:00+00:00");

        var artifact = ValidArtifact(
            status: ExportArtifactStatuses.Succeeded,
            createdAt: createdAt,
            completedAt: null);

        Assert.True(artifact.CanDownload);
        Assert.Equal(createdAt, artifact.CompletedAt);

        Assert.Throws<ArgumentException>(() => ValidArtifact(
            status: ExportArtifactStatuses.Succeeded,
            checksumSha256: null));
        Assert.Throws<ArgumentException>(() => ValidArtifact(
            status: ExportArtifactStatuses.Succeeded,
            content: null));
    }

    [Fact]
    public void Failed_export_artifact_requires_safe_failure_reason()
    {
        var failedAt = DateTimeOffset.Parse("2026-05-09T12:01:00+00:00");

        var artifact = ValidArtifact(
            status: ExportArtifactStatuses.Failed,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            completedAt: null,
            failedAt: failedAt,
            failureReasonCode: "browser_unavailable");

        Assert.Equal(ExportArtifactStatuses.Failed, artifact.Status);
        Assert.False(artifact.CanDownload);
        Assert.Equal(failedAt, artifact.FailedAt);
        Assert.Equal("browser_unavailable", artifact.FailureReasonCode);

        Assert.Throws<ArgumentException>(() => ValidArtifact(
            status: ExportArtifactStatuses.Failed,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            completedAt: null,
            failedAt: failedAt,
            failureReasonCode: "provider returned token abc"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(
            status: ExportArtifactStatuses.Failed,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            completedAt: null,
            failedAt: null,
            failureReasonCode: "browser_unavailable"));
    }

    [Fact]
    public void Export_artifact_downloadability_follows_lifecycle()
    {
        Assert.False(ValidArtifact(
            status: ExportArtifactStatuses.Queued,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            completedAt: null).CanDownload);
        Assert.False(ValidArtifact(
            status: ExportArtifactStatuses.Rendering,
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            content: null,
            completedAt: null,
            startedAt: DateTimeOffset.Parse("2026-05-09T12:00:30+00:00")).CanDownload);
        Assert.True(ValidArtifact(status: ExportArtifactStatuses.Succeeded).CanDownload);
        Assert.False(ValidArtifact(
            status: ExportArtifactStatuses.Expired,
            rowCount: 1,
            byteSize: 128,
            checksumSha256: null,
            content: null,
            completedAt: DateTimeOffset.Parse("2026-05-09T12:00:01+00:00"),
            expiresAt: DateTimeOffset.Parse("2026-05-10T12:00:00+00:00")).CanDownload);
        Assert.False(ValidArtifact(
            status: ExportArtifactStatuses.Deleted,
            rowCount: 1,
            byteSize: 128,
            checksumSha256: null,
            content: null,
            completedAt: DateTimeOffset.Parse("2026-05-09T12:00:01+00:00"),
            deletedAt: DateTimeOffset.Parse("2026-05-10T12:00:00+00:00")).CanDownload);
    }

    [Fact]
    public void Export_artifact_rejects_invalid_target_scope()
    {
        Assert.Throws<ArgumentException>(() => new ExportArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactTargetKinds.Campaign,
            campaignId: null,
            campaignSeriesId: Guid.NewGuid(),
            ExportArtifactTypes.ReportProofCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            "report-proof.csv",
            "text/csv",
            rowCount: 1,
            byteSize: 128,
            Sha256,
            """{"artifactType":"report_proof_csv_codebook"}""",
            "campaign_id,dimension_code",
            """{"columns":[]}"""));

        Assert.Throws<ArgumentException>(() => ValidArtifact(
            targetKind: ExportArtifactTargetKinds.CampaignSeries,
            campaignId: Guid.NewGuid()));

        Assert.Throws<ArgumentException>(() => ValidArtifact(
            targetKind: ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: null,
            defaultCampaignSeriesId: false));
    }

    [Fact]
    public void Export_artifact_rejects_invalid_json_payloads()
    {
        Assert.Throws<ArgumentException>(() => ValidArtifact(metadataJson: "[]"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(codebookJson: "[]"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(metadataJson: "{"));
        Assert.Throws<ArgumentException>(() => ValidArtifact(codebookJson: "{"));
    }

    [Fact]
    public void Export_artifact_rejects_invalid_checksum_or_negative_counts()
    {
        Assert.Throws<ArgumentException>(() => ValidArtifact(checksumSha256: "not-a-sha"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidArtifact(rowCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidArtifact(byteSize: -1));
    }

    private static ExportArtifact ValidArtifact(
        string targetKind = ExportArtifactTargetKinds.Campaign,
        Guid? campaignId = null,
        Guid? campaignSeriesId = null,
        string artifactType = ExportArtifactTypes.ReportProofCsvCodebook,
        string status = ExportArtifactStatuses.Succeeded,
        string format = ExportArtifactFormats.CsvCodebook,
        int rowCount = 1,
        long byteSize = 128,
        string? checksumSha256 = Sha256,
        string metadataJson = """{"artifactType":"report_proof_csv_codebook"}""",
        string codebookJson = """{"columns":[]}""",
        bool defaultCampaignSeriesId = true,
        string? content = "campaign_id,dimension_code",
        string storageKind = ExportArtifactStorageKinds.InlineText,
        string? storageKey = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? failedAt = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? deletedAt = null,
        string? failureReasonCode = null)
    {
        return new ExportArtifact(
            Guid.NewGuid(),
            Guid.NewGuid(),
            targetKind,
            targetKind == ExportArtifactTargetKinds.Campaign
                ? campaignId ?? Guid.NewGuid()
                : campaignId,
            campaignSeriesId ?? (defaultCampaignSeriesId ? Guid.NewGuid() : null),
            artifactType,
            status,
            format,
            "report-proof.csv",
            "text/csv",
            rowCount,
            byteSize,
            checksumSha256,
            metadataJson,
            content,
            codebookJson,
            createdAt,
            completedAt,
            startedAt,
            failedAt,
            expiresAt,
            deletedAt,
            failureReasonCode,
            storageKind,
            storageKey);
    }
}
