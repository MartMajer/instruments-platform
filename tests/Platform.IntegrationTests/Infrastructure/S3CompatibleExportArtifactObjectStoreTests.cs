using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure;
using Platform.Infrastructure.Reports;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class S3CompatibleExportArtifactObjectStoreTests
{
    [Fact]
    public async Task S3_compatible_object_store_puts_and_reads_bytes_with_signed_requests()
    {
        var handler = new RecordingS3Handler();
        var store = new S3CompatibleExportArtifactObjectStore(
            Options.Create(CreateOptions()),
            new HttpClient(handler));
        byte[] content = [0x00, 0x01, 0xFE, 0xFF];

        var stored = await store.StoreAsync("tenants/tenant-a/reports/report.pdf", content, CancellationToken.None);
        var read = await store.ReadAsync("tenants/tenant-a/reports/report.pdf", CancellationToken.None);

        Assert.True(stored.IsSuccess, stored.Error.ToString());
        Assert.True(read.IsSuccess, read.Error.ToString());
        Assert.Equal(content, read.Value);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("/artifact-bucket/tenants/tenant-a/reports/report.pdf", request.PathAndQuery);
            Assert.StartsWith("AWS4-HMAC-SHA256 ", request.Authorization, StringComparison.Ordinal);
            Assert.Contains("Credential=access-key/", request.Authorization, StringComparison.Ordinal);
            Assert.Contains("SignedHeaders=host;x-amz-content-sha256;x-amz-date", request.Authorization, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(request.ContentSha256));
            Assert.False(string.IsNullOrWhiteSpace(request.AmzDate));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("/absolute/report.bin")]
    [InlineData("C:/absolute/report.bin")]
    [InlineData("tenants\\tenant-a\\report.bin")]
    [InlineData("tenants/../report.bin")]
    [InlineData("tenants//report.bin")]
    [InlineData("tenants/./report.bin")]
    public async Task S3_compatible_object_store_rejects_unsafe_storage_keys_before_http(string storageKey)
    {
        var handler = new RecordingS3Handler();
        var store = new S3CompatibleExportArtifactObjectStore(
            Options.Create(CreateOptions()),
            new HttpClient(handler));

        var stored = await store.StoreAsync(storageKey, [0x01], CancellationToken.None);
        var read = await store.ReadAsync(storageKey, CancellationToken.None);

        Assert.True(stored.IsFailure);
        Assert.Equal("export_artifact_object.invalid_key", stored.Error.Code);
        Assert.True(read.IsFailure);
        Assert.Equal("export_artifact_object.invalid_key", read.Error.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task S3_compatible_object_store_missing_object_fails_safely_without_endpoint_or_bucket()
    {
        var handler = new RecordingS3Handler
        {
            MissingKeys = { "/artifact-bucket/tenants/tenant-a/missing.pdf" }
        };
        var store = new S3CompatibleExportArtifactObjectStore(
            Options.Create(CreateOptions()),
            new HttpClient(handler));

        var read = await store.ReadAsync("tenants/tenant-a/missing.pdf", CancellationToken.None);

        Assert.True(read.IsFailure);
        Assert.Equal("export_artifact_object.not_found", read.Error.Code);
        Assert.DoesNotContain("object-store.example.test", read.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("artifact-bucket", read.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Infrastructure_registration_uses_s3_provider_when_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app",
                ["ExportArtifacts:ObjectStore:Provider"] = ExportArtifactObjectStoreProviders.S3Compatible,
                ["ExportArtifacts:ObjectStore:S3:Endpoint"] = "https://object-store.example.test",
                ["ExportArtifacts:ObjectStore:S3:BucketName"] = "artifact-bucket",
                ["ExportArtifacts:ObjectStore:S3:Region"] = "eu-central-1",
                ["ExportArtifacts:ObjectStore:S3:AccessKeyId"] = "access-key",
                ["ExportArtifacts:ObjectStore:S3:SecretAccessKey"] = "secret-key"
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"))
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IExportArtifactObjectStore>();

        Assert.IsType<S3CompatibleExportArtifactObjectStore>(store);
    }

    [Fact]
    public async Task Object_store_health_reports_s3_compatible_configuration_ok_without_root_path()
    {
        var check = new ExportArtifactObjectStoreHealthCheck(
            Options.Create(CreateOptions()),
            new FakeHostEnvironment("Production"));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Object_store_health_reports_s3_compatible_configuration_unready_when_secret_is_missing()
    {
        var options = CreateOptions();
        options.S3.SecretAccessKey = "";
        var check = new ExportArtifactObjectStoreHealthCheck(
            Options.Create(options),
            new FakeHostEnvironment("Production"));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public async Task Object_store_health_reports_s3_compatible_configuration_unready_for_http_outside_development()
    {
        var options = CreateOptions();
        options.S3.Endpoint = "http://object-store.example.test";
        var check = new ExportArtifactObjectStoreHealthCheck(
            Options.Create(options),
            new FakeHostEnvironment("Production"));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    private static ExportArtifactObjectStoreOptions CreateOptions()
    {
        return new ExportArtifactObjectStoreOptions
        {
            Provider = ExportArtifactObjectStoreProviders.S3Compatible,
            S3 = new S3CompatibleObjectStoreOptions
            {
                Endpoint = "https://object-store.example.test",
                BucketName = "artifact-bucket",
                Region = "eu-central-1",
                AccessKeyId = "access-key",
                SecretAccessKey = "secret-key"
            }
        };
    }

    private sealed class RecordingS3Handler : HttpMessageHandler
    {
        private readonly Dictionary<string, byte[]> _objects = [];

        public List<RequestRecord> Requests { get; } = [];

        public HashSet<string> MissingKeys { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
            Requests.Add(new RequestRecord(
                request.Method.Method,
                pathAndQuery,
                request.Headers.Authorization?.ToString() ?? string.Empty,
                request.Headers.TryGetValues("x-amz-content-sha256", out var contentSha256)
                    ? contentSha256.Single()
                    : string.Empty,
                request.Headers.TryGetValues("x-amz-date", out var amzDate)
                    ? amzDate.Single()
                    : string.Empty));

            if (request.Method == HttpMethod.Put)
            {
                _objects[pathAndQuery] = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                if (MissingKeys.Contains(pathAndQuery) || !_objects.TryGetValue(pathAndQuery, out var bytes))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(bytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }
    }

    private sealed record RequestRecord(
        string Method,
        string PathAndQuery,
        string Authorization,
        string ContentSha256,
        string AmzDate);

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Platform.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
