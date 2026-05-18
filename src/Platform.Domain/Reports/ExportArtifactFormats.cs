namespace Platform.Domain.Reports;

public static class ExportArtifactFormats
{
    public const string CsvCodebook = "csv_codebook";
    public const string Html = "html";
    public const string Pdf = "pdf";

    public static bool IsKnown(string value)
    {
        return value is CsvCodebook or Html or Pdf;
    }
}
