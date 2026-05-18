namespace Platform.Domain.Reports;

public sealed class ExportArtifact
{
    private ExportArtifact()
    {
    }

    public ExportArtifact(
        Guid id,
        Guid tenantId,
        string targetKind,
        Guid? campaignId,
        Guid? campaignSeriesId,
        string artifactType,
        string status,
        string format,
        string fileName,
        string contentType,
        int rowCount,
        long byteSize,
        string? checksumSha256,
        string metadataJson,
        string? content,
        string codebookJson,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? failedAt = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? deletedAt = null,
        string? failureReasonCode = null,
        string storageKind = ExportArtifactStorageKinds.InlineText,
        string? storageKey = null)
    {
        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rowCount),
                "Export artifact row count must not be negative.");
        }

        if (byteSize < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteSize),
                "Export artifact byte size must not be negative.");
        }

        Id = id;
        TenantId = tenantId;
        TargetKind = NormalizeKnown(
            targetKind,
            nameof(targetKind),
            ExportArtifactTargetKinds.IsKnown,
            "Unknown export artifact target kind.");
        ValidateTargetScope(TargetKind, campaignId, campaignSeriesId);
        CampaignId = campaignId;
        CampaignSeriesId = campaignSeriesId;
        ArtifactType = NormalizeKnown(
            artifactType,
            nameof(artifactType),
            ExportArtifactTypes.IsKnown,
            "Unknown export artifact type.");
        Status = NormalizeKnown(
            status,
            nameof(status),
            ExportArtifactStatuses.IsKnown,
            "Unknown export artifact status.");
        Format = NormalizeKnown(
            format,
            nameof(format),
            ExportArtifactFormats.IsKnown,
            "Unknown export artifact format.");
        FileName = NormalizeRequired(fileName, nameof(fileName));
        ContentType = NormalizeRequired(contentType, nameof(contentType));
        RowCount = rowCount;
        ByteSize = byteSize;
        ChecksumSha256 = NormalizeSha256OrNull(checksumSha256, nameof(checksumSha256));
        MetadataJson = ExportArtifactJson.RequireObject(metadataJson, nameof(metadataJson));
        Content = NormalizeOptional(content);
        CodebookJson = ExportArtifactJson.RequireObject(codebookJson, nameof(codebookJson));
        StorageKind = NormalizeKnown(
            storageKind,
            nameof(storageKind),
            ExportArtifactStorageKinds.IsKnown,
            "Unknown export artifact storage kind.");
        StorageKey = NormalizeOptional(storageKey);
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        CompletedAt = completedAt;
        StartedAt = startedAt;
        FailedAt = failedAt;
        ExpiresAt = expiresAt;
        DeletedAt = deletedAt;
        FailureReasonCode = NormalizeFailureReasonCodeOrNull(failureReasonCode, nameof(failureReasonCode));
        ValidateLifecycle();
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string TargetKind { get; private set; } = string.Empty;

    public Guid? CampaignId { get; private set; }

    public Guid? CampaignSeriesId { get; private set; }

    public string ArtifactType { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string Format { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public int RowCount { get; private set; }

    public long ByteSize { get; private set; }

    public string? ChecksumSha256 { get; private set; }

    public string MetadataJson { get; private set; } = "{}";

    public string? Content { get; private set; }

    public string CodebookJson { get; private set; } = "{}";

    public string StorageKind { get; private set; } = ExportArtifactStorageKinds.InlineText;

    public string? StorageKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? FailureReasonCode { get; private set; }

    public bool CanDownload =>
        Status == ExportArtifactStatuses.Succeeded &&
        ChecksumSha256 is not null &&
        Content is not null &&
        StorageKind == ExportArtifactStorageKinds.InlineText;

    public void MarkRendering(DateTimeOffset startedAt, string metadataJson)
    {
        if (Status != ExportArtifactStatuses.Queued)
        {
            throw new InvalidOperationException("Only queued export artifacts can start rendering.");
        }

        Status = ExportArtifactStatuses.Rendering;
        StartedAt = startedAt;
        MetadataJson = ExportArtifactJson.RequireObject(metadataJson, nameof(metadataJson));
        ValidateLifecycle();
    }

    public void MarkSucceededExternalObject(
        int rowCount,
        long byteSize,
        string checksumSha256,
        string metadataJson,
        string codebookJson,
        DateTimeOffset completedAt,
        string contentType,
        string storageKey)
    {
        if (Status != ExportArtifactStatuses.Rendering)
        {
            throw new InvalidOperationException("Only rendering export artifacts can be marked succeeded.");
        }

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rowCount),
                "Export artifact row count must not be negative.");
        }

        if (byteSize < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteSize),
                "Export artifact byte size must not be negative.");
        }

        RowCount = rowCount;
        ByteSize = byteSize;
        ChecksumSha256 = NormalizeSha256OrNull(checksumSha256, nameof(checksumSha256)) ??
            throw new ArgumentException("Succeeded export artifacts require a checksum.", nameof(checksumSha256));
        MetadataJson = ExportArtifactJson.RequireObject(metadataJson, nameof(metadataJson));
        Content = null;
        CodebookJson = ExportArtifactJson.RequireObject(codebookJson, nameof(codebookJson));
        ContentType = NormalizeRequired(contentType, nameof(contentType));
        Status = ExportArtifactStatuses.Succeeded;
        CompletedAt = completedAt;
        FailedAt = null;
        FailureReasonCode = null;
        ExpiresAt = null;
        DeletedAt = null;
        StorageKind = ExportArtifactStorageKinds.ExternalObject;
        StorageKey = NormalizeOptional(storageKey);
        ValidateLifecycle();
    }

    public void MarkFailed(
        DateTimeOffset failedAt,
        string failureReasonCode,
        string metadataJson)
    {
        if (Status is not (ExportArtifactStatuses.Queued or ExportArtifactStatuses.Rendering))
        {
            throw new InvalidOperationException("Only queued or rendering export artifacts can be marked failed.");
        }

        var normalizedFailureReasonCode = NormalizeFailureReasonCodeOrNull(
            failureReasonCode,
            nameof(failureReasonCode)) ??
            throw new ArgumentException(
                "Failed export artifacts require a safe failure reason.",
                nameof(failureReasonCode));

        Status = ExportArtifactStatuses.Failed;
        RowCount = 0;
        ByteSize = 0;
        ChecksumSha256 = null;
        MetadataJson = ExportArtifactJson.RequireObject(metadataJson, nameof(metadataJson));
        Content = null;
        CodebookJson = "{}";
        CompletedAt = null;
        StartedAt ??= failedAt;
        FailedAt = failedAt;
        ExpiresAt = null;
        DeletedAt = null;
        FailureReasonCode = normalizedFailureReasonCode;
        StorageKind = ExportArtifactStorageKinds.ExternalObject;
        StorageKey = null;
        ValidateLifecycle();
    }

    public bool InvalidateForWithdrawal(DateTimeOffset deletedAt)
    {
        if (Status == ExportArtifactStatuses.Deleted && DeletedAt.HasValue)
        {
            return false;
        }

        Status = ExportArtifactStatuses.Deleted;
        ChecksumSha256 = null;
        Content = null;
        StorageKey = null;
        DeletedAt = deletedAt;
        FailedAt = null;
        FailureReasonCode = null;

        return true;
    }

    private static string NormalizeKnown(
        string value,
        string parameterName,
        Func<string, bool> isKnown,
        string errorMessage)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();
        if (!isKnown(normalized))
        {
            throw new ArgumentException(errorMessage, parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeSha256OrNull(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !char.IsAsciiHexDigitLower(character)))
        {
            throw new ArgumentException("Checksum must be a SHA-256 hex string.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeFailureReasonCodeOrNull(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128 ||
            normalized.Any(character =>
                character is not ('_' or '-' or '.') &&
                !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("Failure reason code must be a privacy-safe code.", parameterName);
        }

        return normalized;
    }

    private static void ValidateTargetScope(string targetKind, Guid? campaignId, Guid? campaignSeriesId)
    {
        if (targetKind == ExportArtifactTargetKinds.Campaign)
        {
            if (!campaignId.HasValue)
            {
                throw new ArgumentException(
                    "Campaign-targeted export artifacts require a campaign id.",
                    nameof(campaignId));
            }

            return;
        }

        if (campaignId.HasValue || !campaignSeriesId.HasValue)
        {
            throw new ArgumentException(
                "Campaign-series-targeted export artifacts require only a campaign series id.",
                nameof(campaignSeriesId));
        }
    }

    private void ValidateLifecycle()
    {
        if (Status == ExportArtifactStatuses.Succeeded)
        {
            ValidateNoFailureShape();

            if (ChecksumSha256 is null)
            {
                throw new ArgumentException(
                    "Succeeded export artifacts require a checksum.",
                    nameof(ChecksumSha256));
            }

            if (Content is null)
            {
                if (StorageKind == ExportArtifactStorageKinds.ExternalObject)
                {
                    if (string.IsNullOrWhiteSpace(StorageKey))
                    {
                        throw new ArgumentException(
                            "External-object export artifacts require a storage key.",
                            nameof(StorageKey));
                    }

                    CompletedAt ??= CreatedAt;
                    return;
                }

                throw new ArgumentException(
                    "Succeeded inline export artifacts require content.",
                    nameof(Content));
            }

            if (StorageKind == ExportArtifactStorageKinds.ExternalObject)
            {
                throw new ArgumentException(
                    "External-object export artifacts must not carry inline content.",
                    nameof(Content));
            }

            if (StorageKey is not null)
            {
                throw new ArgumentException(
                    "Inline export artifacts must not carry a storage key.",
                    nameof(StorageKey));
            }

            CompletedAt ??= CreatedAt;
            return;
        }

        if (ChecksumSha256 is not null || Content is not null || StorageKey is not null)
        {
            throw new ArgumentException(
                "Non-succeeded export artifacts must not carry materialized content.",
                nameof(Status));
        }

        switch (Status)
        {
            case ExportArtifactStatuses.Queued:
                if (StartedAt.HasValue || CompletedAt.HasValue || FailedAt.HasValue ||
                    ExpiresAt.HasValue || DeletedAt.HasValue || FailureReasonCode is not null)
                {
                    throw new ArgumentException(
                        "Queued export artifacts must not have lifecycle completion fields.",
                        nameof(Status));
                }

                break;

            case ExportArtifactStatuses.Rendering:
                if (!StartedAt.HasValue || CompletedAt.HasValue || FailedAt.HasValue ||
                    DeletedAt.HasValue || FailureReasonCode is not null)
                {
                    throw new ArgumentException(
                        "Rendering export artifacts require only a start timestamp.",
                        nameof(Status));
                }

                break;

            case ExportArtifactStatuses.Failed:
                if (!FailedAt.HasValue || FailureReasonCode is null || CompletedAt.HasValue ||
                    DeletedAt.HasValue)
                {
                    throw new ArgumentException(
                        "Failed export artifacts require a failed timestamp and safe failure reason.",
                        nameof(Status));
                }

                break;

            case ExportArtifactStatuses.Expired:
                ValidateNoFailureShape();
                if (!ExpiresAt.HasValue || DeletedAt.HasValue)
                {
                    throw new ArgumentException(
                        "Expired export artifacts require an expiry timestamp.",
                        nameof(Status));
                }

                break;

            case ExportArtifactStatuses.Deleted:
                ValidateNoFailureShape();
                if (!DeletedAt.HasValue)
                {
                    throw new ArgumentException(
                        "Deleted export artifacts require a deletion timestamp.",
                        nameof(Status));
                }

                break;
        }
    }

    private void ValidateNoFailureShape()
    {
        if (FailedAt.HasValue || FailureReasonCode is not null)
        {
            throw new ArgumentException(
                "Only failed export artifacts can include failure metadata.",
                nameof(Status));
        }
    }
}
