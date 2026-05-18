namespace Platform.Domain.Reports;

public static class ExportArtifactStorageKinds
{
    public const string InlineText = "inline_text";
    public const string ExternalObject = "external_object";

    public static bool IsKnown(string value)
    {
        return value is InlineText or ExternalObject;
    }
}
