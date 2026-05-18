namespace Platform.Domain.Reports;

public static class ExportArtifactTypes
{
    public const string ReportProofCsvCodebook = "report_proof_csv_codebook";
    public const string CampaignSeriesResponseCsvCodebook = "campaign_series_response_csv_codebook";
    public const string CampaignSeriesReportHtml = "campaign_series_report_html";
    public const string CampaignSeriesReportPdf = "campaign_series_report_pdf";

    public static bool IsKnown(string value)
    {
        return value is ReportProofCsvCodebook
            or CampaignSeriesResponseCsvCodebook
            or CampaignSeriesReportHtml
            or CampaignSeriesReportPdf;
    }
}
