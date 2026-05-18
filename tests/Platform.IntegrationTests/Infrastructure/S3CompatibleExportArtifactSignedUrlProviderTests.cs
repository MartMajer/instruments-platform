using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.Infrastructure;
using Platform.Infrastructure.Reports;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class S3CompatibleExportArtifactSignedUrlProviderTests
{
    [Fact]
    public async Task S3_compatible_signed_url_provider_creates_presigned_get_url_without_secret()
    {
        var provider = new S3CompatibleExportArtifactSignedUrlProvider(Options.Create(CreateOptions()));
        var before = DateTimeOffset.UtcNow;

        var result = await provider.CreateReadUrlAsync(
            "tenants/tenant-a/reports/report.pdf",
            TimeSpan.FromMinutes(15),
            CancellationToken.None);

        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.InRange(
            result.Value.ExpiresAt,
            before.AddMinutes(15).AddSeconds(-1),
            after.AddMinutes(15).AddSeconds(1));

        var uri = new Uri(result.Value.Url);
        Assert.Equal("https", uri.Scheme);
        Assert.Equal("object-store.example.test", uri.Host);
        Assert.Equal("/artifact-bucket/tenants/tenant-a/reports/report.pdf", uri.AbsolutePath);

        var query = ParseQuery(uri.Query);
        Assert.Equal("AWS4-HMAC-SHA256", query["X-Amz-Algorithm"]);
        Assert.StartsWith("access-key/20", query["X-Amz-Credential"], StringComparison.Ordinal);
        Assert.Contains("/eu-central-1/s3/aws4_request", query["X-Amz-Credential"], StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(query["X-Amz-Date"]));
        Assert.Equal("900", query["X-Amz-Expires"]);
        Assert.Equal("host", query["X-Amz-SignedHeaders"]);
        Assert.False(string.IsNullOrWhiteSpace(query["X-Amz-Signature"]));
        Assert.DoesNotContain("secret-key", result.Value.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/absolute/report.pdf")]
    [InlineData("C:/absolute/report.pdf")]
    [InlineData("tenants\\tenant-a\\report.pdf")]
    [InlineData("tenants/../report.pdf")]
    [InlineData("tenants//report.pdf")]
    [InlineData("tenants/./report.pdf")]
    public async Task S3_compatible_signed_url_provider_rejects_unsafe_storage_keys(string storageKey)
    {
        var provider = new S3CompatibleExportArtifactSignedUrlProvider(Options.Create(CreateOptions()));

        var result = await provider.CreateReadUrlAsync(
            storageKey,
            TimeSpan.FromMinutes(15),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("export_artifact_object.invalid_key", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(604801)]
    public async Task S3_compatible_signed_url_provider_rejects_invalid_ttl(int ttlSeconds)
    {
        var provider = new S3CompatibleExportArtifactSignedUrlProvider(Options.Create(CreateOptions()));

        var result = await provider.CreateReadUrlAsync(
            "tenants/tenant-a/reports/report.pdf",
            TimeSpan.FromSeconds(ttlSeconds),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("export_artifact_object.invalid_signed_url_ttl", result.Error.Code);
    }

    [Fact]
    public async Task Unsupported_signed_url_provider_fails_safely_for_local_object_storage()
    {
        var provider = new UnsupportedExportArtifactSignedUrlProvider();

        var result = await provider.CreateReadUrlAsync(
            "tenants/tenant-a/reports/report.pdf",
            TimeSpan.FromMinutes(15),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("export_artifact_object.signed_urls_not_supported", result.Error.Code);
        Assert.DoesNotContain("tenants/tenant-a/reports/report.pdf", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Infrastructure_registration_uses_s3_signed_url_provider_when_configured()
    {
        var configuration = CreateConfiguration(ExportArtifactObjectStoreProviders.S3Compatible);

        using var provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"))
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var signedUrlProvider = scope.ServiceProvider.GetRequiredService<IExportArtifactSignedUrlProvider>();

        Assert.IsType<S3CompatibleExportArtifactSignedUrlProvider>(signedUrlProvider);
    }

    [Fact]
    public void Infrastructure_registration_uses_unsupported_signed_url_provider_for_local_storage()
    {
        var configuration = CreateConfiguration(ExportArtifactObjectStoreProviders.Local);

        using var provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"))
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var signedUrlProvider = scope.ServiceProvider.GetRequiredService<IExportArtifactSignedUrlProvider>();

        Assert.IsType<UnsupportedExportArtifactSignedUrlProvider>(signedUrlProvider);
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

    private static IConfiguration CreateConfiguration(string provider)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"] = "Host=127.0.0.1;Port=1;Database=instruments_platform;Username=platform_app;Password=platform_app",
                ["ExportArtifacts:ObjectStore:Provider"] = provider,
                ["ExportArtifacts:ObjectStore:S3:Endpoint"] = "https://object-store.example.test",
                ["ExportArtifacts:ObjectStore:S3:BucketName"] = "artifact-bucket",
                ["ExportArtifacts:ObjectStore:S3:Region"] = "eu-central-1",
                ["ExportArtifacts:ObjectStore:S3:AccessKeyId"] = "access-key",
                ["ExportArtifacts:ObjectStore:S3:SecretAccessKey"] = "secret-key"
            })
            .Build();
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty,
                StringComparer.Ordinal);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Platform.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
