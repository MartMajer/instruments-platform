using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IReportProofExportStore
{
    Task<Result<ReportProofExportArtifactResponse>> CreateCampaignReportProofExportAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesResponseExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportHtmlArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> CreateCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> QueueCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> ProcessCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> RetryCampaignSeriesReportPdfArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken);

    Task<Result<ReportPdfArtifactWorkerRunResponse>> ProcessQueuedCampaignSeriesReportPdfArtifactsAsync(
        Guid tenantId,
        int maxArtifacts,
        CancellationToken cancellationToken);

    Task<Result<ReportPdfArtifactWorkerRunResponse>> FailStaleCampaignSeriesReportPdfArtifactsAsync(
        Guid tenantId,
        DateTimeOffset staleBefore,
        int maxArtifacts,
        CancellationToken cancellationToken);

    Task<Result<ReportProofExportArtifactResponse>> GetExportArtifactAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken);

    Task<Result<ExportArtifactDownloadResponse>> GetExportArtifactDownloadAsync(
        Guid tenantId,
        Guid artifactId,
        CancellationToken cancellationToken);

    Task<Result<ExportArtifactSignedDownloadUrlResponse>> GetExportArtifactSignedDownloadUrlAsync(
        Guid tenantId,
        Guid artifactId,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);
}
