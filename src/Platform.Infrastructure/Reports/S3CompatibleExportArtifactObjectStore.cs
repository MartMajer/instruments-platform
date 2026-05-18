using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

public sealed class S3CompatibleExportArtifactObjectStore : IExportArtifactObjectStore
{
    private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private readonly IOptions<ExportArtifactObjectStoreOptions> _options;
    private readonly HttpClient _httpClient;

    public S3CompatibleExportArtifactObjectStore(IOptions<ExportArtifactObjectStoreOptions> options)
        : this(options, new HttpClient())
    {
    }

    public S3CompatibleExportArtifactObjectStore(
        IOptions<ExportArtifactObjectStoreOptions> options,
        HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    public async Task<Result<bool>> StoreAsync(
        string storageKey,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(HttpMethod.Put, storageKey, content);
        if (request.IsFailure)
        {
            return Result.Failure<bool>(request.Error);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request.Value, cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success(true)
                : Result.Failure<bool>(StoreUnavailable());
        }
        catch (HttpRequestException)
        {
            return Result.Failure<bool>(StoreUnavailable());
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<bool>(StoreUnavailable());
        }
    }

    public async Task<Result<byte[]>> ReadAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(HttpMethod.Get, storageKey, content: null);
        if (request.IsFailure)
        {
            return Result.Failure<byte[]>(request.Error);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request.Value, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<byte[]>(NotFound());
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<byte[]>(StoreUnavailable());
            }

            return Result.Success(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Result.Failure<byte[]>(StoreUnavailable());
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<byte[]>(StoreUnavailable());
        }
    }

    private Result<HttpRequestMessage> BuildRequest(
        HttpMethod method,
        string storageKey,
        byte[]? content)
    {
        var segments = ExportArtifactStorageKey.SplitSafeSegments(storageKey);
        if (segments.IsFailure)
        {
            return Result.Failure<HttpRequestMessage>(segments.Error);
        }

        var validation = ValidateOptions(_options.Value);
        if (validation is not null)
        {
            return Result.Failure<HttpRequestMessage>(validation.Value);
        }

        var options = _options.Value.S3;
        var endpoint = new Uri(options.Endpoint!, UriKind.Absolute);
        var encodedKey = string.Join("/", segments.Value.Select(Uri.EscapeDataString));
        var path = $"/{options.BucketName}/{encodedKey}";
        var uri = new Uri(endpoint, path);
        var payloadHash = content is null ? EmptyPayloadHash : ToHex(SHA256.HashData(content));
        var now = DateTimeOffset.UtcNow;
        var amzDate = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var request = new HttpRequestMessage(method, uri);
        if (content is not null)
        {
            request.Content = new ByteArrayContent(content);
        }

        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256",
            BuildAuthorizationHeader(
                method.Method,
                uri,
                payloadHash,
                amzDate,
                dateStamp,
                options));

        return Result.Success(request);
    }

    internal static Error? ValidateOptions(ExportArtifactObjectStoreOptions options)
    {
        if (!Uri.TryCreate(options.S3.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https"))
        {
            return InvalidConfiguration();
        }

        if (string.IsNullOrWhiteSpace(options.S3.BucketName) ||
            string.IsNullOrWhiteSpace(options.S3.Region) ||
            string.IsNullOrWhiteSpace(options.S3.AccessKeyId) ||
            string.IsNullOrWhiteSpace(options.S3.SecretAccessKey))
        {
            return InvalidConfiguration();
        }

        return null;
    }

    private static string BuildAuthorizationHeader(
        string method,
        Uri uri,
        string payloadHash,
        string amzDate,
        string dateStamp,
        S3CompatibleObjectStoreOptions options)
    {
        var credentialScope = $"{dateStamp}/{options.Region}/s3/aws4_request";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = string.Join(
            "\n",
            method,
            uri.AbsolutePath,
            string.Empty,
            $"host:{uri.Host}",
            $"x-amz-content-sha256:{payloadHash}",
            $"x-amz-date:{amzDate}",
            string.Empty,
            signedHeaders,
            payloadHash);
        var canonicalRequestHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
        var stringToSign = string.Join(
            "\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            canonicalRequestHash);
        var signingKey = GetSignatureKey(options.SecretAccessKey!, dateStamp, options.Region);
        var signature = ToHex(HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        return $"Credential={options.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
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

    private static Error InvalidConfiguration()
    {
        return Error.Conflict(
            "export_artifact_object.invalid_configuration",
            "Export artifact object store configuration is invalid.");
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
