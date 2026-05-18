using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IExportArtifactSignedUrlProvider
{
    Task<Result<ExportArtifactSignedReadUrlResponse>> CreateReadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);
}

public sealed record ExportArtifactSignedReadUrlResponse(
    string Url,
    DateTimeOffset ExpiresAt);
