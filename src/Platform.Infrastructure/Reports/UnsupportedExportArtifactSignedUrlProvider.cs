using Platform.Application.Features.Reports;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class UnsupportedExportArtifactSignedUrlProvider : IExportArtifactSignedUrlProvider
{
    public Task<Result<ExportArtifactSignedReadUrlResponse>> CreateReadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result.Failure<ExportArtifactSignedReadUrlResponse>(
            Error.Conflict(
                "export_artifact_object.signed_urls_not_supported",
                "Export artifact signed URLs are not supported by the configured object store.")));
    }
}
