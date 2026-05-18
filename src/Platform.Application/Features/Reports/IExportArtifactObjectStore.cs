using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IExportArtifactObjectStore
{
    Task<Result<bool>> StoreAsync(
        string storageKey,
        byte[] content,
        CancellationToken cancellationToken);

    Task<Result<byte[]>> ReadAsync(
        string storageKey,
        CancellationToken cancellationToken);
}
