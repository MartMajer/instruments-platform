using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Platform.Application.Features.System.GetHealth;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class ExportArtifactObjectStoreHealthCheck(
    IOptions<ExportArtifactObjectStoreOptions> options,
    IHostEnvironment environment) : IPlatformHealthCheck
{
    private static readonly byte[] ProbeBytes = [0x45, 0x58, 0x50];

    public string Name => "export_artifact_object_store";

    public async Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (options.Value.UsesS3CompatibleProvider())
            {
                return CheckS3CompatibleConfiguration()
                    ? PlatformHealthCheckResult.Ok(Name)
                    : PlatformHealthCheckResult.Unready(Name);
            }

            if (!options.Value.UsesLocalProvider())
            {
                return PlatformHealthCheckResult.Unready(Name);
            }

            if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(options.Value.RootPath))
            {
                return PlatformHealthCheckResult.Unready(Name);
            }

            var rootPath = options.Value.GetEffectiveRootPath();
            if (!Path.IsPathFullyQualified(rootPath))
            {
                return PlatformHealthCheckResult.Unready(Name);
            }

            var fullRootPath = Path.GetFullPath(rootPath);
            if (!environment.IsDevelopment() && IsUnderTempPath(fullRootPath))
            {
                return PlatformHealthCheckResult.Unready(Name);
            }

            return await ProbeRootAsync(fullRootPath, cancellationToken)
                ? PlatformHealthCheckResult.Ok(Name)
                : PlatformHealthCheckResult.Unready(Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return PlatformHealthCheckResult.Unready(Name);
        }
    }

    private bool CheckS3CompatibleConfiguration()
    {
        if (S3CompatibleExportArtifactObjectStore.ValidateOptions(options.Value) is not null)
        {
            return false;
        }

        if (!environment.IsDevelopment() &&
            Uri.TryCreate(options.Value.S3.Endpoint, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return true;
    }

    private static async Task<bool> ProbeRootAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var probeDirectory = Path.Combine(rootPath, ".health");
        var probePath = Path.Combine(probeDirectory, $"{PlatformIds.NewId():N}.probe");

        Directory.CreateDirectory(probeDirectory);
        await File.WriteAllBytesAsync(probePath, ProbeBytes, cancellationToken);
        var readBytes = await File.ReadAllBytesAsync(probePath, cancellationToken);
        File.Delete(probePath);

        return readBytes.SequenceEqual(ProbeBytes);
    }

    private static bool IsUnderTempPath(string path)
    {
        var fullTempPath = EnsureTrailingSeparator(Path.GetFullPath(Path.GetTempPath()));
        var fullPath = EnsureTrailingSeparator(path);

        return fullPath.StartsWith(fullTempPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
