using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.Reports;
using Platform.Application.Outbox;
using Platform.Domain.Campaigns;
using Platform.Domain.Outbox;
using Platform.Domain.Reports;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.ProductSurfaces;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class ReportProofExportStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IReportProofStore reportProofStore,
    IProductSurfaceReadStore? productSurfaceReadStore = null,
    ICampaignSeriesReportHtmlRenderer? reportHtmlRenderer = null,
    IExportArtifactObjectStore? objectStore = null,
    IReportPdfRenderer? reportPdfRenderer = null,
    IOutboxEventBuffer? outboxEventBuffer = null,
    IExportArtifactSignedUrlProvider? signedUrlProvider = null) : IReportProofExportStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private const string PreliminaryLiveDataFinality = "preliminary_live";
    private const string ClosedWaveDataFinality = "closed_wave";
    private const string NotReportableDataFinality = "not_reportable";
    private const string ExportArtifactAggregateType = "export_artifact";
    private const string ReportPdfArtifactTerminalStateReachedEventType = "ReportPdfArtifactTerminalStateReached";
    private readonly IProductSurfaceReadStore _productSurfaceReadStore =
        productSurfaceReadStore ?? new ProductSurfaceReadStore(db, tenantDbScope);
    private readonly ICampaignSeriesReportHtmlRenderer _reportHtmlRenderer =
        reportHtmlRenderer ?? new CampaignSeriesReportHtmlRenderer();
    private readonly IExportArtifactObjectStore? _objectStore = objectStore;
    private readonly IReportPdfRenderer? _reportPdfRenderer = reportPdfRenderer;
    private readonly IOutboxEventBuffer? _outboxEventBuffer = outboxEventBuffer;
    private readonly IExportArtifactSignedUrlProvider? _signedUrlProvider = signedUrlProvider;

    private static readonly string[] CsvColumns =
    [
        "campaign_id",
        "campaign_series_id",
        "campaign_name",
        "campaign_status",
        "campaign_closed_at",
        "campaign_data_finality",
        "proof_status",
        "interpretation_status",
        "launch_snapshot_id",
        "launch_packet_schema_version",
        "launch_packet_sections",
        "launch_packet_source",
        "template_version_id",
        "scoring_rule_id",
        "scoring_rule_document_hash",
        "consent_document_id",
        "retention_policy_id",
        "disclosure_policy_id",
        "response_identity_mode",
        "launched_at",
        "disclosure_policy_version",
        "disclosure_k_min",
        "suppression_strategy",
        "dimension_code",
        "disclosure",
        "submitted_response_count",
        "score_count",
        "n_valid_total",
        "n_expected_total",
        "missing_policy_status_summary",
        "mean",
        "min",
        "max",
        "suppression_reason",
        "interpretation_band_code",
        "interpretation_label",
        "interpretation_status",
        "interpretation_source",
        "interpretation_provenance",
        "interpretation_validated",
        "interpretation_official"
    ];

    private static readonly string[] ResponseExportBaseColumns =
    [
        "response_row_id",
        "trajectory_id",
        "campaign_series_id",
        "campaign_id",
        "wave_label",
        "campaign_status",
        "campaign_closed_at",
        "campaign_data_finality",
        "launch_packet_schema_version",
        "launch_packet_sections",
        "launch_packet_source",
        "response_identity_mode",
        "template_version_id",
        "scoring_rule_id",
        "scoring_rule_document_hash",
        "consent_document_id",
        "consent_accepted_at",
        "consent_grants",
        "retention_policy_id",
        "disclosure_policy_id",
        "locale",
        "submitted_at",
        "time_taken_ms",
        "trajectory_disclosure"
    ];

    private static readonly string[] ReportProofExcludedIdentifiers =
    [
        "tenant_id",
        "launch_packet_raw_json",
        "launch_packet.provenance.campaign_id",
        "launch_packet.provenance.campaign_series_id",
        "launch_packet.provenance.launched_by"
    ];

    private static readonly string[] ResponseExportExcludedIdentifiers =
    [
        "tenant_id",
        "assignment_id",
        "response_session_id",
        "invitation_token_id",
        "invitation_token_hash",
        "delivery_recipient",
        "participant_code_id",
        "participant_code_hash",
        "raw_participant_code",
        "subject_id",
        "target_subject_id",
        "ip_hash",
        "user_agent_hash",
        "launch_packet_raw_json",
        "launch_packet.provenance.campaign_id",
        "launch_packet.provenance.campaign_series_id",
        "launch_packet.provenance.launched_by"
    ];

    public async Task<Result<ReportProofExportArtifactResponse>> CreateCampaignReportProofExportAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var report = await reportProofStore.GetCampaignReportProofAsync(
            tenantId,
            campaignId,
            cancellationToken);

        if (report.IsFailure)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(report.Error);
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var csvContent = BuildCsv(report.Value);
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var checksum = Convert.ToHexString(SHA256.HashData(csvBytes)).ToLowerInvariant();
        var codebookJson = BuildCodebookJson(report.Value, generatedAt, csvBytes.Length, checksum);
        var metadataJson = BuildMetadataJson(report.Value, generatedAt, csvBytes.Length, checksum);
        var artifact = new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            ExportArtifactTargetKinds.Campaign,
            report.Value.CampaignId,
            report.Value.CampaignSeriesId,
            ExportArtifactTypes.ReportProofCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            $"campaign-{report.Value.CampaignId}-report-proof.csv",
            "text/csv",
            report.Value.Scores.Count,
            csvBytes.Length,
            checksum,
            metadataJson,
            csvContent,
            codebookJson,
            generatedAt,
            generatedAt);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        db.ExportArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(artifact));
    }

    public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResponseExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return CreateCampaignSeriesResponseExportCoreAsync(tenantId, campaignSeriesId, cancellationToken);
    }

    public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportHtmlArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return CreateCampaignSeriesReportHtmlArtifactCoreAsync(tenantId, campaignSeriesId, cancellationToken);
    }

    public Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return CreateCampaignSeriesReportPdfArtifactCoreAsync(tenantId, campaignSeriesId, cancellationToken);
    }

    public Task<Result<ReportProofExportArtifactResponse>> QueueCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return QueueCampaignSeriesReportPdfArtifactCoreAsync(tenantId, campaignSeriesId, cancellationToken);
    }

    public Task<Result<ReportProofExportArtifactResponse>> ProcessCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        return ProcessCampaignSeriesReportPdfArtifactCoreAsync(tenantId, artifactId, cancellationToken);
    }

    public Task<Result<ReportProofExportArtifactResponse>> RetryCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        return RetryCampaignSeriesReportPdfArtifactCoreAsync(tenantId, artifactId, cancellationToken);
    }

    public Task<Result<ReportPdfArtifactWorkerRunResponse>> ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
        Guid tenantId,
        int maxArtifacts,
        CancellationToken cancellationToken)
    {
        return ProcessQueuedCampaignSeriesReportPdfArtifactsCoreAsync(tenantId, maxArtifacts, cancellationToken);
    }

    public Task<Result<ReportPdfArtifactWorkerRunResponse>> FailStaleCampaignSeriesReportPdfArtifactsAsync(
        Guid tenantId,
        DateTimeOffset staleBefore,
        int maxArtifacts,
        CancellationToken cancellationToken)
    {
        return FailStaleCampaignSeriesReportPdfArtifactsCoreAsync(
            tenantId,
            staleBefore,
            maxArtifacts,
            cancellationToken);
    }

    public async Task<Result<ReportProofExportArtifactResponse>> GetExportArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifact = await db.ExportArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return artifact is null
            ? Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotFound())
            : Result.Success(ToResponse(artifact));
    }

    public async Task<Result<ExportArtifactDownloadResponse>> GetExportArtifactDownloadAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifact = await db.ExportArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (artifact is null)
        {
            return Result.Failure<ExportArtifactDownloadResponse>(ArtifactNotFound());
        }

        if (!CanDownloadArtifact(artifact))
        {
            return Result.Failure<ExportArtifactDownloadResponse>(ArtifactNotDownloadable());
        }

        if (artifact.StorageKind == ExportArtifactStorageKinds.ExternalObject)
        {
            return await ToExternalObjectDownloadResponseAsync(artifact, cancellationToken);
        }

        return Result.Success(ToDownloadResponse(artifact));
    }

    public async Task<Result<ExportArtifactSignedDownloadUrlResponse>> GetExportArtifactSignedDownloadUrlAsync(
        Guid tenantId,
        Guid artifactId,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifact = await db.ExportArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (artifact is null)
        {
            return Result.Failure<ExportArtifactSignedDownloadUrlResponse>(ArtifactNotFound());
        }

        if (!CanDownloadArtifact(artifact) ||
            artifact.StorageKind != ExportArtifactStorageKinds.ExternalObject ||
            string.IsNullOrWhiteSpace(artifact.StorageKey) ||
            string.IsNullOrWhiteSpace(artifact.ChecksumSha256) ||
            !IsSignedDownloadStorageKeySafe(artifact.StorageKey))
        {
            return Result.Failure<ExportArtifactSignedDownloadUrlResponse>(ArtifactNotDownloadable());
        }

        if (_signedUrlProvider is null)
        {
            return Result.Failure<ExportArtifactSignedDownloadUrlResponse>(SignedUrlsNotSupported());
        }

        var signedUrl = await _signedUrlProvider.CreateReadUrlAsync(
            artifact.StorageKey,
            expiresIn,
            cancellationToken);
        if (signedUrl.IsFailure)
        {
            return Result.Failure<ExportArtifactSignedDownloadUrlResponse>(signedUrl.Error);
        }

        return Result.Success(new ExportArtifactSignedDownloadUrlResponse(
            artifact.Id,
            artifact.FileName,
            artifact.ContentType,
            artifact.ByteSize,
            artifact.ChecksumSha256,
            signedUrl.Value.Url,
            signedUrl.Value.ExpiresAt));
    }

    private async Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportHtmlArtifactCoreAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        var workspace = await _productSurfaceReadStore.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            campaignSeriesId,
            cancellationToken);

        if (workspace.IsFailure)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(workspace.Error);
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var rendered = _reportHtmlRenderer.Render(workspace.Value, generatedAt);
        var htmlBytes = Encoding.UTF8.GetBytes(rendered.Html);
        var checksum = Convert.ToHexString(SHA256.HashData(htmlBytes)).ToLowerInvariant();
        var metadataJson = BuildReportHtmlMetadataJson(
            workspace.Value,
            generatedAt,
            htmlBytes.Length,
            checksum);
        var artifact = new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: workspace.Value.Series.Id,
            artifactType: ExportArtifactTypes.CampaignSeriesReportHtml,
            status: ExportArtifactStatuses.Succeeded,
            format: ExportArtifactFormats.Html,
            fileName: $"campaign-series-{workspace.Value.Series.Id}-report.html",
            contentType: "text/html; charset=utf-8",
            rowCount: rendered.RowCount,
            byteSize: htmlBytes.Length,
            checksumSha256: checksum,
            metadataJson: metadataJson,
            content: rendered.Html,
            codebookJson: rendered.CodebookJson,
            createdAt: generatedAt,
            completedAt: generatedAt);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        db.ExportArtifacts.Add(artifact);
        EnqueueReportPdfArtifactTerminalStateReached(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(artifact));
    }

    private async Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportPdfArtifactCoreAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        var queued = await QueueCampaignSeriesReportPdfArtifactCoreAsync(
            tenantId,
            campaignSeriesId,
            cancellationToken);

        if (queued.IsFailure)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(queued.Error);
        }

        return await ProcessCampaignSeriesReportPdfArtifactCoreAsync(
            tenantId,
            queued.Value.Id,
            cancellationToken);
    }
    private async Task<Result<ReportProofExportArtifactResponse>> PersistFailedCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset startedAt,
        Error error,
        CancellationToken cancellationToken)
    {
        var failedAt = DateTimeOffset.UtcNow;
        var metadataJson = BuildReportPdfFailureMetadataJson(
            workspace,
            startedAt,
            failedAt,
            error.Code);
        var artifact = new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: workspace.Series.Id,
            artifactType: ExportArtifactTypes.CampaignSeriesReportPdf,
            status: ExportArtifactStatuses.Failed,
            format: ExportArtifactFormats.Pdf,
            fileName: $"campaign-series-{workspace.Series.Id}-report.pdf",
            contentType: "application/pdf",
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            metadataJson: metadataJson,
            content: null,
            codebookJson: "{}",
            createdAt: startedAt,
            completedAt: null,
            startedAt: startedAt,
            failedAt: failedAt,
            failureReasonCode: error.Code,
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: null);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        db.ExportArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Failure<ReportProofExportArtifactResponse>(error);
    }

    private async Task<Result<ReportProofExportArtifactResponse>> QueueCampaignSeriesReportPdfArtifactCoreAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken,
        Guid? retryOfArtifactId = null)
    {
        var workspace = await _productSurfaceReadStore.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            campaignSeriesId,
            cancellationToken);

        if (workspace.IsFailure)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(workspace.Error);
        }

        var queuedAt = DateTimeOffset.UtcNow;
        var artifact = new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: workspace.Value.Series.Id,
            artifactType: ExportArtifactTypes.CampaignSeriesReportPdf,
            status: ExportArtifactStatuses.Queued,
            format: ExportArtifactFormats.Pdf,
            fileName: $"campaign-series-{workspace.Value.Series.Id}-report.pdf",
            contentType: "application/pdf",
            rowCount: 0,
            byteSize: 0,
            checksumSha256: null,
            metadataJson: BuildReportPdfQueuedMetadataJson(workspace.Value, queuedAt, retryOfArtifactId),
            content: null,
            codebookJson: "{}",
            createdAt: queuedAt,
            storageKind: ExportArtifactStorageKinds.ExternalObject,
            storageKey: null);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        db.ExportArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(artifact));
    }

    private async Task<Result<ReportProofExportArtifactResponse>> RetryCampaignSeriesReportPdfArtifactCoreAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        Guid campaignSeriesId;

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken))
        {
            var artifact = await db.ExportArtifacts
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

            if (artifact is null)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotFound());
            }

            if (artifact.TargetKind != ExportArtifactTargetKinds.CampaignSeries ||
                artifact.ArtifactType != ExportArtifactTypes.CampaignSeriesReportPdf ||
                artifact.Format != ExportArtifactFormats.Pdf ||
                artifact.CampaignSeriesId is null)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(
                    Error.Conflict(
                        "export_artifact.retry_not_supported",
                        "Only campaign-series report PDF artifacts can be retried."));
            }

            if (artifact.Status != ExportArtifactStatuses.Failed)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(
                    Error.Conflict(
                        "export_artifact.retry_not_failed",
                        "Only failed export artifacts can be retried."));
            }

            campaignSeriesId = artifact.CampaignSeriesId.Value;
            await transaction.CommitAsync(cancellationToken);
        }

        return await QueueCampaignSeriesReportPdfArtifactCoreAsync(
            tenantId,
            campaignSeriesId,
            cancellationToken,
            retryOfArtifactId: artifactId);
    }

    private async Task<Result<ReportProofExportArtifactResponse>> ProcessCampaignSeriesReportPdfArtifactCoreAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        Guid campaignSeriesId;
        DateTimeOffset queuedAt;

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken))
        {
            var artifact = await db.ExportArtifacts
                .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

            if (artifact is null)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotFound());
            }

            if (artifact.TargetKind != ExportArtifactTargetKinds.CampaignSeries ||
                artifact.ArtifactType != ExportArtifactTypes.CampaignSeriesReportPdf)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotProcessable());
            }

            if (artifact.Status != ExportArtifactStatuses.Queued)
            {
                return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotQueued());
            }

            campaignSeriesId = artifact.CampaignSeriesId!.Value;
            queuedAt = artifact.CreatedAt;
            artifact.MarkRendering(
                startedAt,
                BuildReportPdfRenderingMetadataJson(campaignSeriesId, queuedAt, startedAt));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var workspace = await _productSurfaceReadStore.GetCampaignSeriesReportsWorkspaceAsync(
            tenantId,
            campaignSeriesId,
            cancellationToken);

        if (workspace.IsFailure)
        {
            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                workspace.Error,
                workspace: null,
                cancellationToken);
        }

        var objectStore = _objectStore;
        if (objectStore is null)
        {
            var error = Error.Conflict(
                "export_artifact.object_store_unavailable",
                "Export artifact object store is not available.");

            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                error,
                workspace.Value,
                cancellationToken);
        }

        var reportPdfRenderer = _reportPdfRenderer;
        if (reportPdfRenderer is null)
        {
            var error = Error.Conflict(
                "report_pdf.renderer_unavailable",
                "Report PDF renderer is not available.");

            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                error,
                workspace.Value,
                cancellationToken);
        }

        var rendered = _reportHtmlRenderer.Render(workspace.Value, startedAt);
        var pdf = await reportPdfRenderer.RenderAsync(
            new ReportPdfRenderRequest(
                rendered.Html,
                "campaign-series-report",
                1,
                startedAt),
            cancellationToken);

        if (pdf.IsFailure)
        {
            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                pdf.Error,
                workspace.Value,
                cancellationToken);
        }

        if (pdf.Value.PdfBytes.Length == 0)
        {
            var error = Error.Conflict("report_pdf.empty", "Report PDF renderer returned an empty artifact.");

            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                error,
                workspace.Value,
                cancellationToken);
        }

        var storageKey = BuildReportPdfObjectStorageKey(artifactId);
        var byteSize = pdf.Value.PdfBytes.LongLength;
        var checksum = Convert.ToHexString(SHA256.HashData(pdf.Value.PdfBytes)).ToLowerInvariant();
        var stored = await objectStore.StoreAsync(storageKey, pdf.Value.PdfBytes, cancellationToken);
        if (stored.IsFailure)
        {
            return await MarkCampaignSeriesReportPdfArtifactFailedAsync(
                tenantId,
                artifactId,
                campaignSeriesId,
                startedAt,
                stored.Error,
                workspace.Value,
                cancellationToken);
        }

        return await MarkCampaignSeriesReportPdfArtifactSucceededAsync(
            tenantId,
            artifactId,
            workspace.Value,
            rendered.RowCount,
            rendered.CodebookJson,
            pdf.Value,
            startedAt,
            DateTimeOffset.UtcNow,
            byteSize,
            checksum,
            storageKey,
            cancellationToken);
    }

    private async Task<Result<ReportPdfArtifactWorkerRunResponse>> ProcessQueuedCampaignSeriesReportPdfArtifactsCoreAsync(
        Guid tenantId,
        int maxArtifacts,
        CancellationToken cancellationToken)
    {
        if (maxArtifacts <= 0)
        {
            return Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                ProcessedArtifactCount: 0));
        }

        List<Guid> artifactIds;
        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken))
        {
            artifactIds = await db.ExportArtifacts
                .AsNoTracking()
                .Where(artifact =>
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                    artifact.Status == ExportArtifactStatuses.Queued)
                .OrderBy(artifact => artifact.CreatedAt)
                .ThenBy(artifact => artifact.Id)
                .Select(artifact => artifact.Id)
                .Take(maxArtifacts)
                .ToListAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        var processedCount = 0;
        foreach (var artifactId in artifactIds)
        {
            await ProcessCampaignSeriesReportPdfArtifactCoreAsync(
                tenantId,
                artifactId,
                cancellationToken);
            processedCount++;
        }

        return Result.Success(new ReportPdfArtifactWorkerRunResponse(
            tenantId,
            maxArtifacts,
            processedCount));
    }

    private async Task<Result<ReportPdfArtifactWorkerRunResponse>> FailStaleCampaignSeriesReportPdfArtifactsCoreAsync(
        Guid tenantId,
        DateTimeOffset staleBefore,
        int maxArtifacts,
        CancellationToken cancellationToken)
    {
        if (maxArtifacts <= 0)
        {
            return Result.Success(new ReportPdfArtifactWorkerRunResponse(
                tenantId,
                maxArtifacts,
                ProcessedArtifactCount: 0));
        }

        var failedAt = DateTimeOffset.UtcNow;
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifacts = await db.ExportArtifacts
            .Where(artifact =>
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                artifact.Status == ExportArtifactStatuses.Rendering &&
                artifact.StartedAt.HasValue &&
                artifact.StartedAt.Value < staleBefore)
            .OrderBy(artifact => artifact.StartedAt)
            .ThenBy(artifact => artifact.Id)
            .Take(maxArtifacts)
            .ToListAsync(cancellationToken);

        foreach (var artifact in artifacts)
        {
            var startedAt = artifact.StartedAt ?? artifact.CreatedAt;
            artifact.MarkFailed(
                failedAt,
                "report_pdf.rendering_timeout",
                BuildReportPdfFailureMetadataJson(
                    artifact.CampaignSeriesId!.Value,
                    startedAt,
                    failedAt,
                    "report_pdf.rendering_timeout"));
            EnqueueReportPdfArtifactTerminalStateReached(artifact);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ReportPdfArtifactWorkerRunResponse(
            tenantId,
            maxArtifacts,
            artifacts.Count));
    }

    private async Task<Result<ReportProofExportArtifactResponse>> MarkCampaignSeriesReportPdfArtifactSucceededAsync(
        Guid tenantId,
        Guid artifactId,
        CampaignSeriesReportsWorkspaceResponse workspace,
        int rowCount,
        string sourceHtmlCodebookJson,
        ReportPdfRenderResult pdf,
        DateTimeOffset generatedAt,
        DateTimeOffset completedAt,
        long byteSize,
        string checksum,
        string storageKey,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifact = await db.ExportArtifacts
            .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

        if (artifact is null)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotFound());
        }

        if (artifact.Status != ExportArtifactStatuses.Rendering)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotQueued());
        }

        artifact.MarkSucceededExternalObject(
            rowCount,
            byteSize,
            checksum,
            BuildReportPdfMetadataJson(workspace, pdf, generatedAt, byteSize, checksum, storageKey),
            BuildReportPdfCodebookJson(sourceHtmlCodebookJson, pdf, generatedAt, byteSize, checksum),
            completedAt,
            pdf.ContentType,
            storageKey);

        EnqueueReportPdfArtifactTerminalStateReached(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(artifact));
    }

    private async Task<Result<ReportProofExportArtifactResponse>> MarkCampaignSeriesReportPdfArtifactFailedAsync(
        Guid tenantId,
        Guid artifactId,
        Guid campaignSeriesId,
        DateTimeOffset startedAt,
        Error error,
        CampaignSeriesReportsWorkspaceResponse? workspace,
        CancellationToken cancellationToken)
    {
        var failedAt = DateTimeOffset.UtcNow;
        var metadataJson = workspace is null
            ? BuildReportPdfFailureMetadataJson(campaignSeriesId, startedAt, failedAt, error.Code)
            : BuildReportPdfFailureMetadataJson(workspace, startedAt, failedAt, error.Code);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifact = await db.ExportArtifacts
            .SingleOrDefaultAsync(entity => entity.Id == artifactId, cancellationToken);

        if (artifact is null)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotFound());
        }

        if (artifact.Status is not (ExportArtifactStatuses.Queued or ExportArtifactStatuses.Rendering))
        {
            return Result.Failure<ReportProofExportArtifactResponse>(ArtifactNotQueued());
        }

        artifact.MarkFailed(failedAt, error.Code, metadataJson);

        EnqueueReportPdfArtifactTerminalStateReached(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Failure<ReportProofExportArtifactResponse>(error);
    }

    private void EnqueueReportPdfArtifactTerminalStateReached(ExportArtifact artifact)
    {
        if (_outboxEventBuffer is null)
        {
            return;
        }

        if (artifact.ArtifactType != ExportArtifactTypes.CampaignSeriesReportPdf ||
            artifact.TargetKind != ExportArtifactTargetKinds.CampaignSeries ||
            artifact.Format != ExportArtifactFormats.Pdf ||
            artifact.CampaignSeriesId is null ||
            artifact.Status is not (ExportArtifactStatuses.Succeeded or ExportArtifactStatuses.Failed))
        {
            return;
        }

        _outboxEventBuffer.Enqueue(new OutboxMessage(
            artifact.Id,
            ExportArtifactAggregateType,
            ReportPdfArtifactTerminalStateReachedEventType,
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["export_artifact_id"] = artifact.Id,
                ["campaign_series_id"] = artifact.CampaignSeriesId.Value,
                ["artifact_type"] = artifact.ArtifactType,
                ["target_kind"] = artifact.TargetKind,
                ["format"] = artifact.Format,
                ["status"] = artifact.Status,
                ["failure_reason_code"] = artifact.Status == ExportArtifactStatuses.Failed
                    ? artifact.FailureReasonCode
                    : null
            })));
    }

    private static string BuildCsv(CampaignReportProofResponse report)
    {
        var builder = new StringBuilder();
        builder.AppendJoin(',', CsvColumns);
        builder.Append("\r\n");

        foreach (var score in report.Scores)
        {
            builder.AppendJoin(
                ',',
                Escape(report.CampaignId.ToString()),
                Escape(report.CampaignSeriesId?.ToString()),
                Escape(report.CampaignName),
                Escape(report.CampaignStatus),
                Escape(report.ClosedAt?.ToString("O", CultureInfo.InvariantCulture)),
                Escape(report.DataFinality),
                Escape(report.ProofStatus),
                Escape(report.InterpretationStatus),
                Escape(report.LaunchSnapshot.Id.ToString()),
                report.LaunchSnapshot.LaunchPacket.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                Escape(LaunchPacketProvenanceProjection.FormatSections(report.LaunchSnapshot.LaunchPacket)),
                Escape(report.LaunchSnapshot.LaunchPacket.Source),
                Escape(report.LaunchSnapshot.TemplateVersionId.ToString()),
                Escape(report.LaunchSnapshot.ScoringRuleId.ToString()),
                Escape(report.LaunchSnapshot.ScoringRuleDocumentHash),
                Escape(report.LaunchSnapshot.ConsentDocumentId?.ToString()),
                Escape(report.LaunchSnapshot.RetentionPolicyId?.ToString()),
                Escape(report.LaunchSnapshot.DisclosurePolicyId?.ToString()),
                Escape(report.LaunchSnapshot.ResponseIdentityMode),
                Escape(report.LaunchSnapshot.LaunchedAt.ToString("O", CultureInfo.InvariantCulture)),
                Escape(report.DisclosurePolicy.Version),
                report.DisclosurePolicy.KMin.ToString(CultureInfo.InvariantCulture),
                Escape(report.DisclosurePolicy.SuppressionStrategy),
                Escape(score.DimensionCode),
                Escape(score.Disclosure),
                score.SubmittedResponseCount.ToString(CultureInfo.InvariantCulture),
                FormatNullableInt(score.ScoreCount),
                FormatNullableInt(score.NValidTotal),
                FormatNullableInt(score.NExpectedTotal),
                Escape(score.MissingPolicyStatusSummary),
                FormatNullableDecimal(score.Mean),
                FormatNullableDecimal(score.Min),
                FormatNullableDecimal(score.Max),
                Escape(score.SuppressionReason),
                Escape(score.Interpretation?.BandCode),
                Escape(score.Interpretation?.Label),
                Escape(score.Interpretation?.Status),
                Escape(score.Interpretation?.Source),
                Escape(score.Interpretation?.Provenance),
                FormatNullableBoolean(score.Interpretation?.IsValidated),
                FormatNullableBoolean(score.Interpretation?.IsOfficial));
            builder.Append("\r\n");
        }

        return builder.ToString();
    }

    private async Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResponseExportCoreAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new ResponseExportSeriesRow(entity.Id, entity.Name))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var sessions = await LoadResponseExportSessionsAsync(campaignSeriesId, cancellationToken);
        if (sessions.Count == 0)
        {
            return Result.Failure<ReportProofExportArtifactResponse>(
                Error.Validation(
                    "response_export.no_submitted_responses",
                    "Campaign series must have submitted response sessions before response export can be generated."));
        }

        var campaignIds = sessions
            .Select(session => session.CampaignId)
            .Distinct()
            .ToArray();
        var sessionIds = sessions
            .Select(session => session.SessionId)
            .Distinct()
            .ToArray();
        var consentRecordIds = sessions
            .Select(session => session.ConsentRecordId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var templateVersionIds = sessions
            .Select(session => session.TemplateVersionId)
            .Distinct()
            .ToArray();
        var disclosurePolicyIds = sessions
            .Select(session => session.DisclosurePolicyId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        var submittedCounts = await LoadSubmittedResponseCountsByCampaignAsync(campaignIds, cancellationToken);
        var disclosurePolicies = await LoadResponseExportDisclosurePoliciesAsync(disclosurePolicyIds, cancellationToken);
        var consentRecords = await LoadResponseExportConsentRecordsAsync(consentRecordIds, cancellationToken);
        var questions = await LoadResponseExportQuestionsAsync(templateVersionIds, cancellationToken);
        var answers = await LoadResponseExportAnswersAsync(sessionIds, cancellationToken);
        var scoreMetadata = await LoadResponseExportScoreMetadataAsync(sessionIds, cancellationToken);
        var export = BuildResponseExport(
            series,
            sessions,
            submittedCounts,
            disclosurePolicies,
            consentRecords,
            questions,
            answers,
            scoreMetadata);

        var generatedAt = DateTimeOffset.UtcNow;
        var csvBytes = Encoding.UTF8.GetBytes(export.CsvContent);
        var checksum = Convert.ToHexString(SHA256.HashData(csvBytes)).ToLowerInvariant();
        var codebookJson = BuildResponseExportCodebookJson(
            series,
            export,
            generatedAt,
            csvBytes.Length,
            checksum);
        var metadataJson = BuildResponseExportMetadataJson(
            series,
            export,
            generatedAt,
            csvBytes.Length,
            checksum);
        var artifact = new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            $"campaign-series-{campaignSeriesId}-responses.csv",
            "text/csv",
            export.RowCount,
            csvBytes.Length,
            checksum,
            metadataJson,
            export.CsvContent,
            codebookJson,
            generatedAt,
            generatedAt);

        db.ExportArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToResponse(artifact));
    }

    private async Task<List<ResponseExportSessionRow>> LoadResponseExportSessionsAsync(
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return await db.ResponseSessions
            .AsNoTracking()
            .Join(
                db.Assignments.AsNoTracking(),
                session => session.AssignmentId,
                assignment => assignment.Id,
                (session, assignment) => new { session, assignment })
            .Join(
                db.Campaigns.AsNoTracking(),
                row => row.assignment.CampaignId,
                campaign => campaign.Id,
                (row, campaign) => new { row.session, row.assignment, campaign })
            .Join(
                db.CampaignLaunchSnapshots.AsNoTracking(),
                row => row.campaign.Id,
                snapshot => snapshot.CampaignId,
                (row, snapshot) => new { row.session, row.assignment, row.campaign, snapshot })
            .Where(row =>
                row.campaign.CampaignSeriesId == campaignSeriesId &&
                row.session.SubmittedAt.HasValue)
            .OrderBy(row => row.campaign.StartAt)
            .ThenBy(row => row.campaign.Name)
            .ThenBy(row => row.session.SubmittedAt)
            .ThenBy(row => row.session.Id)
            .Select(row => new ResponseExportSessionRow(
                row.session.Id,
                row.campaign.Id,
                row.campaign.Name,
                row.campaign.Status,
                row.campaign.ClosedAt,
                row.snapshot.LaunchPacket,
                row.snapshot.TemplateVersionId,
                row.snapshot.ScoringRuleId,
                row.snapshot.ScoringRuleDocumentHash,
                row.snapshot.ConsentDocumentId,
                row.snapshot.RetentionPolicyId,
                row.snapshot.DisclosurePolicyId,
                row.snapshot.ResponseIdentityMode,
                row.session.ParticipantCodeId,
                row.session.ConsentRecordId,
                row.session.Locale,
                row.session.SubmittedAt!.Value,
                row.session.TimeTakenMs))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadSubmittedResponseCountsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        return await db.ResponseSessions
            .AsNoTracking()
            .Join(
                db.Assignments.AsNoTracking(),
                session => session.AssignmentId,
                assignment => assignment.Id,
                (session, assignment) => new { session, assignment })
            .Where(row =>
                campaignIds.Contains(row.assignment.CampaignId) &&
                row.session.SubmittedAt.HasValue)
            .GroupBy(row => row.assignment.CampaignId)
            .Select(group => new CampaignSubmittedCountRow(group.Key, group.Count()))
            .ToDictionaryAsync(row => row.CampaignId, row => row.Count, cancellationToken);
    }

    private async Task<Dictionary<Guid, ResponseExportDisclosurePolicyRow>> LoadResponseExportDisclosurePoliciesAsync(
        Guid[] disclosurePolicyIds,
        CancellationToken cancellationToken)
    {
        if (disclosurePolicyIds.Length == 0)
        {
            return new Dictionary<Guid, ResponseExportDisclosurePolicyRow>();
        }

        return await db.DisclosurePolicies
            .AsNoTracking()
            .Where(entity => disclosurePolicyIds.Contains(entity.Id))
            .Select(entity => new ResponseExportDisclosurePolicyRow(
                entity.Id,
                entity.Version,
                entity.KMin,
                entity.SuppressionStrategy))
            .ToDictionaryAsync(row => row.Id, cancellationToken);
    }

    private async Task<Dictionary<Guid, ResponseExportConsentRecordRow>> LoadResponseExportConsentRecordsAsync(
        Guid[] consentRecordIds,
        CancellationToken cancellationToken)
    {
        if (consentRecordIds.Length == 0)
        {
            return new Dictionary<Guid, ResponseExportConsentRecordRow>();
        }

        return await db.ConsentRecords
            .AsNoTracking()
            .Where(entity => consentRecordIds.Contains(entity.Id))
            .Select(entity => new ResponseExportConsentRecordRow(
                entity.Id,
                entity.AcceptedGrants,
                entity.AcceptedAt))
            .ToDictionaryAsync(row => row.Id, cancellationToken);
    }

    private async Task<ResponseExportQuestionRow[]> LoadResponseExportQuestionsAsync(
        Guid[] templateVersionIds,
        CancellationToken cancellationToken)
    {
        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Join(
                db.TemplateSections.AsNoTracking(),
                question => question.SectionId,
                section => section.Id,
                (question, section) => new { question, section })
            .GroupJoin(
                db.QuestionScales.AsNoTracking(),
                row => row.question.ScaleId,
                scale => scale.Id,
                (row, scales) => new { row.question, row.section, scales })
            .SelectMany(
                row => row.scales.DefaultIfEmpty(),
                (row, scale) => new { row.question, row.section, scale })
            .Where(row => templateVersionIds.Contains(row.question.TemplateVersionId))
            .OrderBy(row => row.section.Ordinal)
            .ThenBy(row => row.question.Ordinal)
            .ThenBy(row => row.question.Code)
            .Select(row => new ResponseExportQuestionRow(
                row.question.Id,
                row.question.TemplateVersionId,
                row.question.Code,
                row.question.TextDefault,
                row.question.Type,
                row.question.Required,
                row.question.ReverseCoded,
                row.question.VariableLabel,
                row.question.MeasurementLevel,
                row.question.MissingCodes,
                row.question.Payload,
                row.scale == null ? null : row.scale.Code,
                row.scale == null ? null : row.scale.Type,
                row.scale == null ? null : row.scale.MinValue,
                row.scale == null ? null : row.scale.MaxValue,
                row.scale == null ? null : row.scale.Step,
                row.scale == null ? null : row.scale.NaAllowed,
                row.scale == null ? null : row.scale.Anchors))
            .ToListAsync(cancellationToken);

        return questions.ToArray();
    }

    private async Task<ResponseExportAnswerRow[]> LoadResponseExportAnswersAsync(
        Guid[] sessionIds,
        CancellationToken cancellationToken)
    {
        return await db.Answers
            .AsNoTracking()
            .Where(entity => sessionIds.Contains(entity.SessionId))
            .Select(entity => new ResponseExportAnswerRow(
                entity.SessionId,
                entity.QuestionId,
                entity.Value,
                entity.IsSkipped,
                entity.IsNa))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<ResponseExportScoreMetadataRow[]> LoadResponseExportScoreMetadataAsync(
        Guid[] sessionIds,
        CancellationToken cancellationToken)
    {
        if (sessionIds.Length == 0)
        {
            return [];
        }

        var scoreRows = await db.Scores
            .AsNoTracking()
            .Where(entity => sessionIds.Contains(entity.ResponseSessionId))
            .OrderBy(entity => entity.DimensionCode)
            .ThenBy(entity => entity.ResponseSessionId)
            .ThenByDescending(entity => entity.ComputedAt)
            .Select(entity => new ResponseExportScoreMetadataRow(
                entity.ResponseSessionId,
                entity.DimensionCode,
                entity.NValid,
                entity.NExpected,
                entity.MissingPolicyStatus,
                entity.ComputedAt))
            .ToListAsync(cancellationToken);

        return scoreRows
            .GroupBy(score => new { score.SessionId, score.DimensionCode })
            .Select(group => group.First())
            .ToArray();
    }

    private static ResponseExportBuildResult BuildResponseExport(
        ResponseExportSeriesRow series,
        IReadOnlyList<ResponseExportSessionRow> sessions,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, ResponseExportDisclosurePolicyRow> disclosurePolicies,
        IReadOnlyDictionary<Guid, ResponseExportConsentRecordRow> consentRecords,
        IReadOnlyList<ResponseExportQuestionRow> questions,
        IReadOnlyList<ResponseExportAnswerRow> answers,
        IReadOnlyList<ResponseExportScoreMetadataRow> scoreMetadata)
    {
        var questionCodeById = questions.ToDictionary(question => question.Id, question => question.Code);
        var exportQuestions = questions
            .GroupBy(question => question.Code, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(question => question.Code, StringComparer.Ordinal)
            .ToArray();
        var answersBySessionAndCode = answers
            .Where(answer => questionCodeById.ContainsKey(answer.QuestionId))
            .GroupBy(
                answer => (answer.SessionId, questionCodeById[answer.QuestionId]),
                answer => answer)
            .ToDictionary(group => group.Key, group => group.First());
        var answerColumns = exportQuestions
            .Select(question => $"answer_{question.Code}")
            .ToArray();
        var scoreMetadataColumns = CreateScoreMetadataColumns(scoreMetadata);
        var scoreMetadataBySessionAndDimension = scoreMetadata
            .GroupBy(score => (score.SessionId, score.DimensionCode))
            .ToDictionary(group => group.Key, group => group.First());
        var csvColumns = ResponseExportBaseColumns
            .Concat(answerColumns)
            .Concat(scoreMetadataColumns.Select(column => column.ColumnName))
            .ToArray();
        var trajectoryIds = CreateTrajectoryIdMap(sessions, submittedCounts, disclosurePolicies);
        var builder = new StringBuilder();
        builder.AppendJoin(',', csvColumns);
        builder.Append("\r\n");

        var rowNumber = 0;
        foreach (var session in sessions)
        {
            rowNumber++;
            var consent = session.ConsentRecordId.HasValue
                ? consentRecords.GetValueOrDefault(session.ConsentRecordId.Value)
                : null;
            var dataFinality = DetermineSessionDataFinality(session);
            var launchPacket = LaunchPacketProvenanceProjection.FromJson(session.LaunchPacket);
            var rowValues = new List<string>
            {
                $"r{rowNumber:000000}",
                ResolveTrajectoryId(session, trajectoryIds, submittedCounts, disclosurePolicies),
                series.Id.ToString(),
                session.CampaignId.ToString(),
                session.CampaignName,
                session.CampaignStatus,
                session.CampaignClosedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                dataFinality,
                launchPacket.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                LaunchPacketProvenanceProjection.FormatSections(launchPacket),
                launchPacket.Source,
                session.ResponseIdentityMode,
                session.TemplateVersionId.ToString(),
                session.ScoringRuleId.ToString(),
                session.ScoringRuleDocumentHash,
                session.ConsentDocumentId?.ToString() ?? string.Empty,
                consent?.AcceptedAt.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                consent?.AcceptedGrants ?? string.Empty,
                session.RetentionPolicyId?.ToString() ?? string.Empty,
                session.DisclosurePolicyId?.ToString() ?? string.Empty,
                session.Locale,
                session.SubmittedAt.ToString("O", CultureInfo.InvariantCulture),
                session.TimeTakenMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ResolveTrajectoryDisclosure(session, submittedCounts, disclosurePolicies)
            };

            foreach (var question in exportQuestions)
            {
                rowValues.Add(answersBySessionAndCode.TryGetValue((session.SessionId, question.Code), out var answer)
                    ? FormatAnswerValue(answer)
                    : string.Empty);
            }

            foreach (var column in scoreMetadataColumns)
            {
                rowValues.Add(
                    scoreMetadataBySessionAndDimension.TryGetValue(
                        (session.SessionId, column.DimensionCode),
                        out var score)
                            ? FormatScoreMetadataValue(score, column.MetadataKind)
                            : string.Empty);
            }

            builder.AppendJoin(',', rowValues.Select(Escape));
            builder.Append("\r\n");
        }

        return new ResponseExportBuildResult(
            builder.ToString(),
            csvColumns,
            exportQuestions,
            scoreMetadataColumns,
            sessions.Count,
            sessions.Select(session => session.CampaignId).Distinct().Count(),
            trajectoryIds.Count,
            sessions.Count(session => DetermineSessionDataFinality(session) == PreliminaryLiveDataFinality),
            sessions.Count(session => DetermineSessionDataFinality(session) == ClosedWaveDataFinality));
    }

    private static ResponseExportScoreMetadataColumnRow[] CreateScoreMetadataColumns(
        IReadOnlyList<ResponseExportScoreMetadataRow> scoreMetadata)
    {
        var dimensions = scoreMetadata
            .Select(score => score.DimensionCode)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var usedTokens = new HashSet<string>(StringComparer.Ordinal);
        var columns = new List<ResponseExportScoreMetadataColumnRow>();

        foreach (var dimension in dimensions)
        {
            var baseToken = ToCsvColumnToken(dimension);
            var token = baseToken;
            var suffix = 2;
            while (!usedTokens.Add(token))
            {
                token = $"{baseToken}_{suffix}";
                suffix++;
            }

            columns.Add(new ResponseExportScoreMetadataColumnRow(
                $"score_{token}_n_valid",
                dimension,
                "n_valid"));
            columns.Add(new ResponseExportScoreMetadataColumnRow(
                $"score_{token}_n_expected",
                dimension,
                "n_expected"));
            columns.Add(new ResponseExportScoreMetadataColumnRow(
                $"score_{token}_missing_policy_status",
                dimension,
                "missing_policy_status"));
        }

        return columns.ToArray();
    }

    private static string ToCsvColumnToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasUnderscore = false;

        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasUnderscore = false;
                continue;
            }

            if (!previousWasUnderscore)
            {
                builder.Append('_');
                previousWasUnderscore = true;
            }
        }

        var token = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(token) ? "dimension" : token;
    }

    private static string FormatScoreMetadataValue(
        ResponseExportScoreMetadataRow score,
        string metadataKind)
    {
        return metadataKind switch
        {
            "n_valid" => score.NValid.ToString(CultureInfo.InvariantCulture),
            "n_expected" => score.NExpected.ToString(CultureInfo.InvariantCulture),
            "missing_policy_status" => score.MissingPolicyStatus,
            _ => string.Empty
        };
    }

    private static string DetermineSessionDataFinality(ResponseExportSessionRow session)
    {
        return session.CampaignStatus switch
        {
            CampaignStatuses.Closed => ClosedWaveDataFinality,
            CampaignStatuses.Live => PreliminaryLiveDataFinality,
            _ => NotReportableDataFinality
        };
    }

    private static Dictionary<Guid, string> CreateTrajectoryIdMap(
        IReadOnlyList<ResponseExportSessionRow> sessions,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, ResponseExportDisclosurePolicyRow> disclosurePolicies)
    {
        var participantCodeIds = sessions
            .Where(session =>
                session.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal &&
                session.ParticipantCodeId.HasValue &&
                TrajectoryDisclosureAllowed(session, submittedCounts, disclosurePolicies))
            .Select(session => session.ParticipantCodeId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        var result = new Dictionary<Guid, string>();

        for (var index = 0; index < participantCodeIds.Length; index++)
        {
            result[participantCodeIds[index]] = $"t{index + 1:000000}";
        }

        return result;
    }

    private static string ResolveTrajectoryId(
        ResponseExportSessionRow session,
        IReadOnlyDictionary<Guid, string> trajectoryIds,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, ResponseExportDisclosurePolicyRow> disclosurePolicies)
    {
        if (session.ResponseIdentityMode != ResponseIdentityModes.AnonymousLongitudinal ||
            !session.ParticipantCodeId.HasValue ||
            !TrajectoryDisclosureAllowed(session, submittedCounts, disclosurePolicies))
        {
            return string.Empty;
        }

        return trajectoryIds.GetValueOrDefault(session.ParticipantCodeId.Value) ?? string.Empty;
    }

    private static string ResolveTrajectoryDisclosure(
        ResponseExportSessionRow session,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, ResponseExportDisclosurePolicyRow> disclosurePolicies)
    {
        if (session.ResponseIdentityMode != ResponseIdentityModes.AnonymousLongitudinal)
        {
            return "not_applicable";
        }

        return TrajectoryDisclosureAllowed(session, submittedCounts, disclosurePolicies)
            ? "visible"
            : "suppressed_insufficient_responses";
    }

    private static bool TrajectoryDisclosureAllowed(
        ResponseExportSessionRow session,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, ResponseExportDisclosurePolicyRow> disclosurePolicies)
    {
        if (!session.DisclosurePolicyId.HasValue ||
            !disclosurePolicies.TryGetValue(session.DisclosurePolicyId.Value, out var policy))
        {
            return false;
        }

        return submittedCounts.GetValueOrDefault(session.CampaignId) >= policy.KMin;
    }

    private static string FormatAnswerValue(ResponseExportAnswerRow answer)
    {
        if (answer.IsSkipped)
        {
            return "__skipped";
        }

        if (answer.IsNa)
        {
            return "__not_applicable";
        }

        if (string.IsNullOrWhiteSpace(answer.Value))
        {
            return string.Empty;
        }

        using var document = JsonDocument.Parse(answer.Value);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.String => document.RootElement.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => document.RootElement.GetRawText()
        };
    }

    private static string BuildResponseExportMetadataJson(
        ResponseExportSeriesRow series,
        ResponseExportBuildResult export,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
                format = ExportArtifactFormats.CsvCodebook,
                proofStatus = "proof_only",
                generatedAt,
                campaignSeriesId = series.Id,
                campaignSeriesName = series.Name,
                targetKind = ExportArtifactTargetKinds.CampaignSeries,
                exportTarget = ExportArtifactTargetKinds.CampaignSeries,
                rowCount = export.RowCount,
                campaignCount = export.CampaignCount,
                trajectoryCount = export.TrajectoryCount,
                preliminaryLiveResponseCount = export.PreliminaryLiveResponseCount,
                closedWaveResponseCount = export.ClosedWaveResponseCount,
                scoreMetadataDimensionCount = export.ScoreMetadataColumns
                    .Select(column => column.DimensionCode)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                byteSize,
                checksumSha256 = checksum,
                trajectoryIdPolicy = "per_artifact",
                storageNote = "campaign_series_id is the export target"
            },
            JsonOptions);
    }

    private static string BuildResponseExportCodebookJson(
        ResponseExportSeriesRow series,
        ResponseExportBuildResult export,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
                format = ExportArtifactFormats.CsvCodebook,
                proofStatus = "proof_only",
                generatedAt,
                campaignSeriesId = series.Id,
                campaignSeriesName = series.Name,
                targetKind = ExportArtifactTargetKinds.CampaignSeries,
                rowCount = export.RowCount,
                campaignCount = export.CampaignCount,
                trajectoryCount = export.TrajectoryCount,
                preliminaryLiveResponseCount = export.PreliminaryLiveResponseCount,
                closedWaveResponseCount = export.ClosedWaveResponseCount,
                scoreMetadataDimensionCount = export.ScoreMetadataColumns
                    .Select(column => column.DimensionCode)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                dataFinality = new
                {
                    preliminaryLive = PreliminaryLiveDataFinality,
                    closedWave = ClosedWaveDataFinality,
                    notReportable = NotReportableDataFinality
                },
                byteSize,
                checksumSha256 = checksum,
                trajectoryIdPolicy = "per_artifact",
                missingTreatment = new
                {
                    blank = "question_not_answered_or_not_present_in_session_template",
                    skipped = "__skipped",
                    notApplicable = "__not_applicable"
                },
                excludedIdentifiers = ResponseExportExcludedIdentifiers,
                columns = export.Columns.Select(column => CreateResponseExportColumnDefinition(
                    column,
                    export.Questions,
                    export.ScoreMetadataColumns))
            },
            JsonOptions);
    }

    private static object CreateResponseExportColumnDefinition(
        string column,
        IReadOnlyList<ResponseExportQuestionRow> questions,
        IReadOnlyList<ResponseExportScoreMetadataColumnRow> scoreMetadataColumns)
    {
        if (column.StartsWith("answer_", StringComparison.Ordinal))
        {
            var code = column["answer_".Length..];
            var question = questions.Single(item => item.Code == code);
            return new
            {
                name = column,
                label = question.VariableLabel ?? question.TextDefault,
                source = "answer",
                questionCode = question.Code,
                questionText = question.TextDefault,
                questionType = question.Type,
                required = question.Required,
                reverseCoded = question.ReverseCoded,
                measurementLevel = question.MeasurementLevel ?? "nominal",
                missingCodes = JsonSerializer.Deserialize<JsonElement>(question.MissingCodes),
                valueLabels = CreateQuestionValueLabels(question.Payload),
                answerMetadata = CreateQuestionAnswerMetadata(question),
                scale = question.ScaleCode is null
                    ? null
                    : new
                    {
                        code = question.ScaleCode,
                        type = question.ScaleType,
                        minValue = question.ScaleMinValue,
                        maxValue = question.ScaleMaxValue,
                        step = question.ScaleStep,
                        naAllowed = question.ScaleNaAllowed,
                        anchors = JsonSerializer.Deserialize<JsonElement>(question.ScaleAnchors ?? "[]")
                    },
                disclosureTreatment = "item_answer_value"
            };
        }

        var scoreMetadataColumn = scoreMetadataColumns.SingleOrDefault(item => item.ColumnName == column);
        if (scoreMetadataColumn is not null)
        {
            return new
            {
                name = column,
                label = $"{scoreMetadataColumn.DimensionCode} {scoreMetadataColumn.MetadataKind}".Replace('_', ' '),
                source = "score_output_metadata",
                dimensionCode = scoreMetadataColumn.DimensionCode,
                metadataKind = scoreMetadataColumn.MetadataKind,
                measurementLevel = scoreMetadataColumn.MetadataKind is "n_valid" or "n_expected"
                    ? "scale"
                    : "nominal",
                disclosureTreatment = "per_submitted_response_score_metadata"
            };
        }

        return new
        {
            name = column,
            label = column.Replace('_', ' '),
            source = ResponseExportColumnSource(column),
            measurementLevel = ResponseExportColumnMeasurementLevel(column),
            disclosureTreatment = ResponseExportColumnDisclosureTreatment(column)
        };
    }

    private static object? CreateQuestionValueLabels(string payload)
    {
        using var payloadDocument = TryParseQuestionPayload(payload);
        if (payloadDocument is null ||
            !payloadDocument.RootElement.TryGetProperty("options", out var options) ||
            options.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var valueLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var option in options.EnumerateArray())
        {
            index++;
            if (option.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var code = ReadStringProperty(option, "code") ?? $"o{index:00}";
            var label = ReadStringProperty(option, "label") ?? code;
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(label))
            {
                valueLabels[code] = label;
            }
        }

        return valueLabels.Count == 0 ? null : valueLabels;
    }

    private static object? CreateQuestionAnswerMetadata(ResponseExportQuestionRow question)
    {
        using var payloadDocument = TryParseQuestionPayload(question.Payload);
        if (payloadDocument is null)
        {
            return null;
        }

        var root = payloadDocument.RootElement;
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (question.Type is "number")
        {
            AddJsonObjectProperty(metadata, root, "validation");
            AddJsonObjectProperty(metadata, root, "display");
        }
        else if (question.Type is "date")
        {
            AddJsonObjectProperty(metadata, root, "validation");
        }
        else if (question.Type is "text")
        {
            AddJsonObjectProperty(metadata, root, "text");
        }
        else if (question.Type is "single" or "multi")
        {
            AddJsonObjectProperty(metadata, root, "choice");
        }
        else if (question.Type is "ranking")
        {
            AddJsonObjectProperty(metadata, root, "ranking");
        }

        return metadata.Count == 0 ? null : metadata;
    }

    private static void AddJsonObjectProperty(
        Dictionary<string, object?> target,
        JsonElement root,
        string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object)
        {
            target[propertyName] = ToSerializableJsonValue(property);
        }
    }

    private static object? ToSerializableJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ToSerializableJsonValue(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(ToSerializableJsonValue).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static JsonDocument? TryParseQuestionPayload(string payload)
    {
        try
        {
            var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }

            document.Dispose();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string ResponseExportColumnSource(string column)
    {
        return column switch
        {
            "trajectory_id" or "trajectory_disclosure" => "artifact_local_identity",
            "campaign_status" or "campaign_closed_at" or "campaign_data_finality" => "campaign_lifecycle",
            "consent_document_id" or "consent_accepted_at" or "consent_grants" => "consent",
            "retention_policy_id" => "retention_policy",
            "disclosure_policy_id" => "disclosure_policy",
            "launch_packet_schema_version" or "launch_packet_sections" or "launch_packet_source" => "launch_packet",
            "template_version_id" or "scoring_rule_id" or "scoring_rule_document_hash" =>
                "launch_snapshot",
            "response_row_id" => "artifact_local_row_id",
            _ => "response_export_projection"
        };
    }

    private static string ResponseExportColumnMeasurementLevel(string column)
    {
        return column is "time_taken_ms" ? "scale" : "nominal";
    }

    private static string ResponseExportColumnDisclosureTreatment(string column)
    {
        return column switch
        {
            "trajectory_id" => "only_for_anonymous_longitudinal_when_campaign_disclosure_passes",
            "response_row_id" => "artifact_local_identifier",
            "launch_packet_schema_version" or "launch_packet_sections" or "launch_packet_source" =>
                "launch_packet_provenance",
            _ => "provenance"
        };
    }

    private static string BuildMetadataJson(
        CampaignReportProofResponse report,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.ReportProofCsvCodebook,
                format = ExportArtifactFormats.CsvCodebook,
                proofStatus = report.ProofStatus,
                interpretationStatus = report.InterpretationStatus,
                generatedAt,
                report.CampaignId,
                report.CampaignSeriesId,
                campaignStatus = report.CampaignStatus,
                campaignClosedAt = report.ClosedAt,
                dataFinality = report.DataFinality,
                rowCount = report.Scores.Count,
                byteSize,
                checksumSha256 = checksum
            },
            JsonOptions);
    }

    private static string BuildCodebookJson(
        CampaignReportProofResponse report,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.ReportProofCsvCodebook,
                format = ExportArtifactFormats.CsvCodebook,
                proofStatus = report.ProofStatus,
                interpretationStatus = report.InterpretationStatus,
                generatedAt,
                report.CampaignId,
                report.CampaignSeriesId,
                campaignStatus = report.CampaignStatus,
                campaignClosedAt = report.ClosedAt,
                dataFinality = report.DataFinality,
                launchSnapshot = report.LaunchSnapshot,
                disclosurePolicy = report.DisclosurePolicy,
                rowCount = report.Scores.Count,
                byteSize,
                checksumSha256 = checksum,
                suppressionBasis = "same_suppression_as_report_proof",
                dataFinalityLabels = new
                {
                    preliminaryLive = PreliminaryLiveDataFinality,
                    closedWave = ClosedWaveDataFinality,
                    notReportable = NotReportableDataFinality
                },
                excludedIdentifiers = ReportProofExcludedIdentifiers,
                scoreInterpretation = new
                {
                    status = "tenant_attested",
                    source = "tenant_defined",
                    validation = "not validated",
                    official = "not official",
                    disclosureTreatment = "only_when_score_visible"
                },
                columns = CsvColumns.Select(CreateColumnDefinition).ToArray()
            },
            JsonOptions);
    }

    private static string BuildReportHtmlMetadataJson(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportHtml,
                format = ExportArtifactFormats.Html,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                generatedAt,
                byteSize,
                checksumSha256 = checksum,
                campaignSeries = new
                {
                    workspace.Series.Id,
                    workspace.Series.Name,
                    workspace.Series.StudyKind,
                    workspace.Series.IsSample,
                    workspace.Series.SampleScenario
                },
                summary = new
                {
                    workspace.Summary.CampaignCount,
                    workspace.Summary.ReportableCampaignCount,
                    workspace.Summary.SubmittedResponseCount,
                    workspace.Summary.ScoreCount,
                    workspace.Summary.VisibleScoreCount,
                    workspace.Summary.SuppressedScoreCount,
                    workspace.Summary.PreliminaryLiveReportCount,
                    workspace.Summary.ClosedWaveReportCount
                }
            },
            JsonOptions);
    }

    private static string BuildReportPdfMetadataJson(
        CampaignSeriesReportsWorkspaceResponse workspace,
        ReportPdfRenderResult pdf,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum,
        string storageKey)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                sourceHtmlArtifactType = ExportArtifactTypes.CampaignSeriesReportHtml,
                generatedAt,
                byteSize,
                checksumSha256 = checksum,
                storageKind = ExportArtifactStorageKinds.ExternalObject,
                storageKey,
                campaignSeries = new
                {
                    workspace.Series.Id,
                    workspace.Series.Name,
                    workspace.Series.StudyKind,
                    workspace.Series.IsSample,
                    workspace.Series.SampleScenario
                },
                summary = new
                {
                    workspace.Summary.CampaignCount,
                    workspace.Summary.ReportableCampaignCount,
                    workspace.Summary.SubmittedResponseCount,
                    workspace.Summary.ScoreCount,
                    workspace.Summary.VisibleScoreCount,
                    workspace.Summary.SuppressedScoreCount,
                    workspace.Summary.PreliminaryLiveReportCount,
                    workspace.Summary.ClosedWaveReportCount
                },
                renderer = new
                {
                    name = pdf.Renderer,
                    pdf.BrowserVersion,
                    pdf.OptionsHashSha256
                }
            },
            JsonOptions);
    }

    private static string BuildReportPdfCodebookJson(
        string sourceHtmlCodebookJson,
        ReportPdfRenderResult pdf,
        DateTimeOffset generatedAt,
        long byteSize,
        string checksum)
    {
        using var sourceHtmlCodebook = JsonDocument.Parse(sourceHtmlCodebookJson);

        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceArtifactType = ExportArtifactTypes.CampaignSeriesReportHtml,
                generatedAt,
                byteSize,
                checksumSha256 = checksum,
                renderer = new
                {
                    name = pdf.Renderer,
                    pdf.BrowserVersion,
                    pdf.OptionsHashSha256
                },
                sourceHtmlCodebook = sourceHtmlCodebook.RootElement
            },
            JsonOptions);
    }

    private static string BuildReportPdfQueuedMetadataJson(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset queuedAt,
        Guid? retryOfArtifactId = null)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                queuedAt,
                retryOfArtifactId,
                storageKind = ExportArtifactStorageKinds.ExternalObject,
                campaignSeries = new
                {
                    workspace.Series.Id,
                    workspace.Series.Name,
                    workspace.Series.StudyKind,
                    workspace.Series.IsSample,
                    workspace.Series.SampleScenario
                },
                summary = new
                {
                    workspace.Summary.CampaignCount,
                    workspace.Summary.ReportableCampaignCount,
                    workspace.Summary.SubmittedResponseCount,
                    workspace.Summary.ScoreCount,
                    workspace.Summary.VisibleScoreCount,
                    workspace.Summary.SuppressedScoreCount,
                    workspace.Summary.PreliminaryLiveReportCount,
                    workspace.Summary.ClosedWaveReportCount
                }
            },
            JsonOptions);
    }

    private static string BuildReportPdfRenderingMetadataJson(
        Guid campaignSeriesId,
        DateTimeOffset queuedAt,
        DateTimeOffset startedAt)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                queuedAt,
                startedAt,
                storageKind = ExportArtifactStorageKinds.ExternalObject,
                campaignSeries = new
                {
                    Id = campaignSeriesId
                }
            },
            JsonOptions);
    }

    private static string BuildReportPdfFailureMetadataJson(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset startedAt,
        DateTimeOffset failedAt,
        string failureReasonCode)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                startedAt,
                failedAt,
                failureReasonCode,
                storageKind = ExportArtifactStorageKinds.ExternalObject,
                campaignSeries = new
                {
                    workspace.Series.Id,
                    workspace.Series.Name,
                    workspace.Series.StudyKind,
                    workspace.Series.IsSample,
                    workspace.Series.SampleScenario
                },
                summary = new
                {
                    workspace.Summary.CampaignCount,
                    workspace.Summary.ReportableCampaignCount,
                    workspace.Summary.SubmittedResponseCount,
                    workspace.Summary.ScoreCount,
                    workspace.Summary.VisibleScoreCount,
                    workspace.Summary.SuppressedScoreCount,
                    workspace.Summary.PreliminaryLiveReportCount,
                    workspace.Summary.ClosedWaveReportCount
                }
            },
            JsonOptions);
    }

    private static string BuildReportPdfFailureMetadataJson(
        Guid campaignSeriesId,
        DateTimeOffset startedAt,
        DateTimeOffset failedAt,
        string failureReasonCode)
    {
        return JsonSerializer.Serialize(
            new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesReportPdf,
                format = ExportArtifactFormats.Pdf,
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                startedAt,
                failedAt,
                failureReasonCode,
                storageKind = ExportArtifactStorageKinds.ExternalObject,
                campaignSeries = new
                {
                    Id = campaignSeriesId
                }
            },
            JsonOptions);
    }

    private static object CreateColumnDefinition(string column)
    {
        return new
        {
            name = column,
            label = column.Replace('_', ' '),
            source = ColumnSource(column),
            measurementLevel = ColumnMeasurementLevel(column),
            disclosureTreatment = ColumnDisclosureTreatment(column)
        };
    }

    private ReportProofExportArtifactResponse ToResponse(ExportArtifact artifact)
    {
        return new ReportProofExportArtifactResponse(
            artifact.Id,
            artifact.TargetKind,
            artifact.TargetKind == ExportArtifactTargetKinds.Campaign
                ? artifact.CampaignId!.Value
                : artifact.CampaignSeriesId!.Value,
            artifact.TargetKind == ExportArtifactTargetKinds.Campaign ? "Campaign" : "Campaign series",
            artifact.CampaignId,
            artifact.CampaignSeriesId,
            artifact.ArtifactType,
            artifact.Status,
            artifact.Format,
            artifact.FileName,
            artifact.ContentType,
            artifact.RowCount,
            artifact.ByteSize,
            artifact.ChecksumSha256,
            artifact.CreatedAt,
            artifact.CompletedAt,
            artifact.Format is ExportArtifactFormats.Html or ExportArtifactFormats.Pdf
                ? string.Empty
                : artifact.Content ?? string.Empty,
            artifact.CodebookJson,
            artifact.StartedAt,
            artifact.FailedAt,
            artifact.ExpiresAt,
            artifact.DeletedAt,
            artifact.FailureReasonCode,
            CanDownloadArtifact(artifact));
    }

    private static ExportArtifactDownloadResponse ToDownloadResponse(ExportArtifact artifact)
    {
        return new ExportArtifactDownloadResponse(
            artifact.Id,
            artifact.FileName,
            artifact.ContentType,
            artifact.ByteSize,
            artifact.ChecksumSha256!,
            artifact.Content!,
            Encoding.UTF8.GetBytes(artifact.Content!));
    }

    private static string BuildReportPdfObjectStorageKey(Guid artifactId)
    {
        return $"export-artifacts/{artifactId:N}.pdf";
    }

    private static bool IsSignedDownloadStorageKeySafe(string storageKey)
    {
        var segments = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return !segments.Any(segment =>
            segment.Equals("tenants", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("campaign-series", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("campaigns", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Result<ExportArtifactDownloadResponse>> ToExternalObjectDownloadResponseAsync(
        ExportArtifact artifact,
        CancellationToken cancellationToken)
    {
        if (_objectStore is null ||
            string.IsNullOrWhiteSpace(artifact.StorageKey) ||
            string.IsNullOrWhiteSpace(artifact.ChecksumSha256))
        {
            return Result.Failure<ExportArtifactDownloadResponse>(ArtifactNotDownloadable());
        }

        var content = await _objectStore.ReadAsync(artifact.StorageKey, cancellationToken);
        if (content.IsFailure)
        {
            return Result.Failure<ExportArtifactDownloadResponse>(content.Error);
        }

        if (content.Value.LongLength != artifact.ByteSize)
        {
            return Result.Failure<ExportArtifactDownloadResponse>(ArtifactObjectIntegrityMismatch());
        }

        var checksum = Convert.ToHexString(SHA256.HashData(content.Value)).ToLowerInvariant();
        if (!string.Equals(checksum, artifact.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ExportArtifactDownloadResponse>(ArtifactObjectIntegrityMismatch());
        }

        return Result.Success(new ExportArtifactDownloadResponse(
            artifact.Id,
            artifact.FileName,
            artifact.ContentType,
            artifact.ByteSize,
            artifact.ChecksumSha256,
            string.Empty,
            content.Value));
    }

    private bool CanDownloadArtifact(ExportArtifact artifact)
    {
        if (artifact.CanDownload)
        {
            return true;
        }

        return _objectStore is not null &&
            artifact.Status == ExportArtifactStatuses.Succeeded &&
            artifact.StorageKind == ExportArtifactStorageKinds.ExternalObject &&
            !string.IsNullOrWhiteSpace(artifact.StorageKey) &&
            !string.IsNullOrWhiteSpace(artifact.ChecksumSha256);
    }

    private static Error ArtifactNotFound()
    {
        return Error.NotFound("export_artifact.not_found", "Export artifact was not found.");
    }

    private static Error ArtifactNotDownloadable()
    {
        return Error.Conflict(
            "export_artifact.not_downloadable",
            "Export artifact is not downloadable.");
    }

    private static Error ArtifactNotProcessable()
    {
        return Error.Conflict(
            "export_artifact.not_processable",
            "Export artifact is not processable by this worker.");
    }

    private static Error ArtifactNotQueued()
    {
        return Error.Conflict(
            "export_artifact.not_queued",
            "Export artifact is not queued for processing.");
    }

    private static Error ArtifactObjectIntegrityMismatch()
    {
        return Error.Conflict(
            "export_artifact.object_integrity_mismatch",
            "Export artifact object failed integrity validation.");
    }

    private static Error SignedUrlsNotSupported()
    {
        return Error.Conflict(
            "export_artifact_object.signed_urls_not_supported",
            "Export artifact signed URLs are not supported by the configured object store.");
    }

    private static string ColumnSource(string column)
    {
        if (IsInterpretationColumn(column))
        {
            return "tenant_attested_score_interpretation";
        }

        return column switch
        {
            "campaign_closed_at" or "campaign_data_finality" => "campaign_lifecycle",
            "dimension_code" or "disclosure" or "submitted_response_count" or
                "score_count" or "mean" or "min" or "max" or "suppression_reason"
                => "report_proof_score_summary",
            "n_valid_total" or "n_expected_total" or "missing_policy_status_summary"
                => "score_output_metadata",
            "disclosure_policy_version" or "disclosure_k_min" or "suppression_strategy"
                => "disclosure_policy",
            "launch_packet_schema_version" or "launch_packet_sections" or "launch_packet_source"
                => "launch_packet",
            "launch_snapshot_id" or "template_version_id" or "scoring_rule_id" or
                "scoring_rule_document_hash" or "consent_document_id" or "retention_policy_id" or
                "disclosure_policy_id" or "response_identity_mode" or "launched_at"
                => "launch_snapshot",
            _ => "campaign_report_proof"
        };
    }

    private static string ColumnMeasurementLevel(string column)
    {
        return column switch
        {
            "submitted_response_count" or "score_count" or "mean" or "min" or "max" or
                "n_valid_total" or "n_expected_total" or "disclosure_k_min"
                => "scale",
            _ => "nominal"
        };
    }

    private static string ColumnDisclosureTreatment(string column)
    {
        if (IsInterpretationColumn(column))
        {
            return "only_when_score_visible";
        }

        return column switch
        {
            "score_count" or "n_valid_total" or "n_expected_total" or
                "missing_policy_status_summary" or "mean" or "min" or "max" =>
                "suppressed_when_report_proof_suppressed",
            "submitted_response_count" or "suppression_reason" or "disclosure" =>
                "same_suppression_as_report_proof",
            "launch_packet_schema_version" or "launch_packet_sections" or "launch_packet_source" =>
                "launch_packet_provenance",
            _ => "provenance"
        };
    }

    private static bool IsInterpretationColumn(string column)
    {
        return column.StartsWith("interpretation_", StringComparison.Ordinal);
    }

    private static string FormatNullableDecimal(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string FormatNullableBoolean(bool? value)
    {
        return value.HasValue ? value.Value.ToString().ToLowerInvariant() : string.Empty;
    }

    private static string FormatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record ResponseExportSeriesRow(
        Guid Id,
        string Name);

    private sealed record ResponseExportSessionRow(
        Guid SessionId,
        Guid CampaignId,
        string CampaignName,
        string CampaignStatus,
        DateTimeOffset? CampaignClosedAt,
        string LaunchPacket,
        Guid TemplateVersionId,
        Guid ScoringRuleId,
        string ScoringRuleDocumentHash,
        Guid? ConsentDocumentId,
        Guid? RetentionPolicyId,
        Guid? DisclosurePolicyId,
        string ResponseIdentityMode,
        Guid? ParticipantCodeId,
        Guid? ConsentRecordId,
        string Locale,
        DateTimeOffset SubmittedAt,
        int? TimeTakenMs);

    private sealed record CampaignSubmittedCountRow(
        Guid CampaignId,
        int Count);

    private sealed record ResponseExportDisclosurePolicyRow(
        Guid Id,
        string Version,
        int KMin,
        string SuppressionStrategy);

    private sealed record ResponseExportConsentRecordRow(
        Guid Id,
        string AcceptedGrants,
        DateTimeOffset AcceptedAt);

    private sealed record ResponseExportQuestionRow(
        Guid Id,
        Guid TemplateVersionId,
        string Code,
        string TextDefault,
        string Type,
        bool Required,
        bool ReverseCoded,
        string? VariableLabel,
        string? MeasurementLevel,
        string MissingCodes,
        string Payload,
        string? ScaleCode,
        string? ScaleType,
        int? ScaleMinValue,
        int? ScaleMaxValue,
        int? ScaleStep,
        bool? ScaleNaAllowed,
        string? ScaleAnchors);

    private sealed record ResponseExportAnswerRow(
        Guid SessionId,
        Guid QuestionId,
        string? Value,
        bool IsSkipped,
        bool IsNa);

    private sealed record ResponseExportScoreMetadataRow(
        Guid SessionId,
        string DimensionCode,
        int NValid,
        int NExpected,
        string MissingPolicyStatus,
        DateTimeOffset ComputedAt);

    private sealed record ResponseExportScoreMetadataColumnRow(
        string ColumnName,
        string DimensionCode,
        string MetadataKind);

    private sealed record ResponseExportBuildResult(
        string CsvContent,
        IReadOnlyList<string> Columns,
        IReadOnlyList<ResponseExportQuestionRow> Questions,
        IReadOnlyList<ResponseExportScoreMetadataColumnRow> ScoreMetadataColumns,
        int RowCount,
        int CampaignCount,
        int TrajectoryCount,
        int PreliminaryLiveResponseCount,
        int ClosedWaveResponseCount);
}


