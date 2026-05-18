using Microsoft.Extensions.Options;
using Platform.Infrastructure.Reports;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class LocalExportArtifactObjectStoreTests
{
    [Fact]
    public async Task Local_object_store_roundtrips_bytes()
    {
        var rootPath = CreateRootPath();
        var store = new LocalExportArtifactObjectStore(Options.Create(new ExportArtifactObjectStoreOptions
        {
            RootPath = rootPath
        }));
        byte[] content = [0x00, 0x01, 0xFE, 0xFF];

        var stored = await store.StoreAsync(
            "tenants/tenant-a/reports/report.bin",
            content,
            CancellationToken.None);
        var read = await store.ReadAsync(
            "tenants/tenant-a/reports/report.bin",
            CancellationToken.None);

        Assert.True(stored.IsSuccess, stored.Error.ToString());
        Assert.True(read.IsSuccess, read.Error.ToString());
        Assert.Equal(content, read.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/absolute/report.bin")]
    [InlineData("C:/absolute/report.bin")]
    [InlineData("tenants\\tenant-a\\report.bin")]
    [InlineData("tenants/../report.bin")]
    [InlineData("tenants//report.bin")]
    [InlineData("tenants/./report.bin")]
    public async Task Local_object_store_rejects_unsafe_storage_keys(string storageKey)
    {
        var store = new LocalExportArtifactObjectStore(Options.Create(new ExportArtifactObjectStoreOptions
        {
            RootPath = CreateRootPath()
        }));

        var stored = await store.StoreAsync(storageKey, [0x01], CancellationToken.None);
        var read = await store.ReadAsync(storageKey, CancellationToken.None);

        Assert.True(stored.IsFailure);
        Assert.Equal("export_artifact_object.invalid_key", stored.Error.Code);
        Assert.True(read.IsFailure);
        Assert.Equal("export_artifact_object.invalid_key", read.Error.Code);
    }

    [Fact]
    public async Task Local_object_store_missing_object_fails_safely_without_root_path()
    {
        var rootPath = CreateRootPath();
        var store = new LocalExportArtifactObjectStore(Options.Create(new ExportArtifactObjectStoreOptions
        {
            RootPath = rootPath
        }));

        var read = await store.ReadAsync("tenants/tenant-a/missing.bin", CancellationToken.None);

        Assert.True(read.IsFailure);
        Assert.Equal("export_artifact_object.not_found", read.Error.Code);
        Assert.DoesNotContain(rootPath, read.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRootPath()
    {
        return Path.Combine(Path.GetTempPath(), "instruments-platform-tests", Guid.NewGuid().ToString("N"));
    }
}
