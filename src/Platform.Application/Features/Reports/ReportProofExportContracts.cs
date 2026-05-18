namespace Platform.Application.Features.Reports;

public sealed record ReportProofExportArtifactResponse(
    Guid Id,
    string TargetKind,
    Guid TargetId,
    string TargetLabel,
    Guid? CampaignId,
    Guid? CampaignSeriesId,
    string ArtifactType,
    string Status,
    string Format,
    string FileName,
    string ContentType,
    int RowCount,
    long ByteSize,
    string? ChecksumSha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string CsvContent,
    string CodebookJson,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? FailedAt = null,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? DeletedAt = null,
    string? FailureReasonCode = null,
    bool CanDownload = false);

public sealed record ExportArtifactDownloadResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long ByteSize,
    string ChecksumSha256,
    string Content,
    byte[]? ContentBytes = null);

public sealed record ExportArtifactSignedDownloadUrlResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long ByteSize,
    string ChecksumSha256,
    string Url,
    DateTimeOffset ExpiresAt);

public sealed record ReportPdfArtifactWorkerRunResponse(
    Guid TenantId,
    int MaxArtifacts,
    int ProcessedArtifactCount);
