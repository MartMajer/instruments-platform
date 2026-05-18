using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class LocalExportArtifactObjectStore(
    IOptions<ExportArtifactObjectStoreOptions> options) : IExportArtifactObjectStore
{
    public async Task<Result<bool>> StoreAsync(
        string storageKey,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var resolved = ResolveStoragePath(storageKey);
        if (resolved.IsFailure)
        {
            return Result.Failure<bool>(resolved.Error);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolved.Value)!);
            await File.WriteAllBytesAsync(resolved.Value, content, cancellationToken);

            return Result.Success(true);
        }
        catch (IOException)
        {
            return Result.Failure<bool>(StoreUnavailable());
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure<bool>(StoreUnavailable());
        }
    }

    public async Task<Result<byte[]>> ReadAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        var resolved = ResolveStoragePath(storageKey);
        if (resolved.IsFailure)
        {
            return Result.Failure<byte[]>(resolved.Error);
        }

        if (!File.Exists(resolved.Value))
        {
            return Result.Failure<byte[]>(NotFound());
        }

        try
        {
            return Result.Success(await File.ReadAllBytesAsync(resolved.Value, cancellationToken));
        }
        catch (IOException)
        {
            return Result.Failure<byte[]>(StoreUnavailable());
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure<byte[]>(StoreUnavailable());
        }
    }

    private Result<string> ResolveStoragePath(string storageKey)
    {
        var segments = ExportArtifactStorageKey.SplitSafeSegments(storageKey);
        if (segments.IsFailure)
        {
            return Result.Failure<string>(segments.Error);
        }

        var rootPath = EnsureTrailingSeparator(Path.GetFullPath(options.Value.GetEffectiveRootPath()));
        var relativePath = Path.Combine(segments.Value);
        var resolvedPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

        return resolvedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
            ? Result.Success(resolvedPath)
            : Result.Failure<string>(ExportArtifactStorageKey.InvalidKey());
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static Error NotFound()
    {
        return Error.NotFound(
            "export_artifact_object.not_found",
            "Export artifact object was not found.");
    }

    private static Error StoreUnavailable()
    {
        return Error.Conflict(
            "export_artifact_object.unavailable",
            "Export artifact object store is unavailable.");
    }
}
