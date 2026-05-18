using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class S3CompatibleExportArtifactSignedUrlProvider : IExportArtifactSignedUrlProvider
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string UnsignedPayload = "UNSIGNED-PAYLOAD";
    private const int MaxExpiresInSeconds = 604_800;

    private readonly IOptions<ExportArtifactObjectStoreOptions> _options;

    public S3CompatibleExportArtifactSignedUrlProvider(IOptions<ExportArtifactObjectStoreOptions> options)
    {
        _options = options;
    }

    public Task<Result<ExportArtifactSignedReadUrlResponse>> CreateReadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ttlSeconds = (long)Math.Ceiling(expiresIn.TotalSeconds);
        if (ttlSeconds <= 0 || ttlSeconds > MaxExpiresInSeconds)
        {
            return Task.FromResult(Result.Failure<ExportArtifactSignedReadUrlResponse>(InvalidSignedUrlTtl()));
        }

        var segments = ExportArtifactStorageKey.SplitSafeSegments(storageKey);
        if (segments.IsFailure)
        {
            return Task.FromResult(Result.Failure<ExportArtifactSignedReadUrlResponse>(segments.Error));
        }

        var validation = S3CompatibleExportArtifactObjectStore.ValidateOptions(_options.Value);
        if (validation is not null)
        {
            return Task.FromResult(Result.Failure<ExportArtifactSignedReadUrlResponse>(validation.Value));
        }

        var options = _options.Value.S3;
        var endpoint = new Uri(options.Endpoint!, UriKind.Absolute);
        var encodedKey = string.Join("/", segments.Value.Select(Uri.EscapeDataString));
        var objectUri = new Uri(endpoint, $"/{options.BucketName}/{encodedKey}");
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(ttlSeconds);
        var amzDate = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var credentialScope = $"{dateStamp}/{options.Region}/s3/aws4_request";
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = Algorithm,
            ["X-Amz-Credential"] = $"{options.AccessKeyId}/{credentialScope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = ttlSeconds.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host"
        };
        var canonicalQueryString = BuildQueryString(query);
        var canonicalRequest = string.Join(
            "\n",
            HttpMethod.Get.Method,
            objectUri.AbsolutePath,
            canonicalQueryString,
            $"host:{BuildCanonicalHost(objectUri)}",
            string.Empty,
            "host",
            UnsignedPayload);
        var canonicalRequestHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
        var stringToSign = string.Join(
            "\n",
            Algorithm,
            amzDate,
            credentialScope,
            canonicalRequestHash);
        var signingKey = GetSignatureKey(options.SecretAccessKey!, dateStamp, options.Region);
        var signature = ToHex(HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));
        var signedUrl = $"{objectUri.GetLeftPart(UriPartial.Path)}?{canonicalQueryString}&X-Amz-Signature={signature}";

        return Task.FromResult(Result.Success(new ExportArtifactSignedReadUrlResponse(signedUrl, expiresAt)));
    }

    private static string BuildQueryString(SortedDictionary<string, string> query)
    {
        return string.Join(
            "&",
            query.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    private static string BuildCanonicalHost(Uri uri)
    {
        return uri.IsDefaultPort
            ? uri.Host
            : string.Create(CultureInfo.InvariantCulture, $"{uri.Host}:{uri.Port}");
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName)
    {
        var kDate = HMACSHA256.HashData(Encoding.UTF8.GetBytes("AWS4" + key), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(regionName));
        var kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes("s3"));

        return HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }

    private static string ToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Error InvalidSignedUrlTtl()
    {
        return Error.Validation(
            "export_artifact_object.invalid_signed_url_ttl",
            "Export artifact signed URL TTL must be between 1 second and 7 days.");
    }
}
