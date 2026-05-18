namespace Platform.Domain.Reports;

public static class ExportArtifactStatuses
{
    public const string Queued = "queued";

    public const string Rendering = "rendering";

    public const string Succeeded = "succeeded";

    public const string Failed = "failed";

    public const string Expired = "expired";

    public const string Deleted = "deleted";

    public static bool IsKnown(string value)
    {
        return value is Queued or Rendering or Succeeded or Failed or Expired or Deleted;
    }
}
