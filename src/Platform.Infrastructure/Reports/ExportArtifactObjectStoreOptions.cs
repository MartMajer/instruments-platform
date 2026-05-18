namespace Platform.Infrastructure.Reports;

public sealed class ExportArtifactObjectStoreOptions
{
    public const string SectionName = "ExportArtifacts:ObjectStore";

    public string Provider { get; set; } = ExportArtifactObjectStoreProviders.Local;

    public string? RootPath { get; set; }

    public S3CompatibleObjectStoreOptions S3 { get; set; } = new();

    public string GetEffectiveRootPath()
    {
        return string.IsNullOrWhiteSpace(RootPath)
            ? Path.Combine(Path.GetTempPath(), "instruments-platform", "export-artifacts")
            : RootPath;
    }

    public bool UsesLocalProvider()
    {
        return string.Equals(Provider, ExportArtifactObjectStoreProviders.Local, StringComparison.OrdinalIgnoreCase);
    }

    public bool UsesS3CompatibleProvider()
    {
        return string.Equals(
            Provider,
            ExportArtifactObjectStoreProviders.S3Compatible,
            StringComparison.OrdinalIgnoreCase);
    }
}

public static class ExportArtifactObjectStoreProviders
{
    public const string Local = "local";
    public const string S3Compatible = "s3_compatible";
}

public sealed class S3CompatibleObjectStoreOptions
{
    public string? Endpoint { get; set; }

    public string? BucketName { get; set; }

    public string Region { get; set; } = "us-east-1";

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }
}
