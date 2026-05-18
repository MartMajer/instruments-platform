using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Platform.Application.Features.ProductSurfaces;

namespace Platform.Application.Features.Reports;

public sealed class CampaignSeriesReportHtmlRenderer : ICampaignSeriesReportHtmlRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private static readonly string[] SensitiveMarkers =
    [
        "token",
        "wdr_",
        "participant_code",
        "provider_message",
        "recipient",
        "secret",
        "salt"
    ];

    public CampaignSeriesReportHtmlRenderResult Render(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset generatedAt)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.Append("  <title>");
        AppendEncoded(html, workspace.Series.Name);
        html.AppendLine(" report</title>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <main data-template-id=\"campaign-series-report\" data-template-version=\"1\">");
        html.AppendLine("    <header>");
        html.AppendLine("      <p>Campaign series report</p>");
        html.Append("      <h1>");
        AppendEncoded(html, workspace.Series.Name);
        html.AppendLine("</h1>");
        html.Append("      <time data-field=\"generated-at\" datetime=\"");
        html.Append(HtmlEncoder.Default.Encode(generatedAt.UtcDateTime.ToString("O")));
        html.Append("\">");
        html.Append(HtmlEncoder.Default.Encode(generatedAt.UtcDateTime.ToString("O")));
        html.AppendLine("</time>");
        html.AppendLine("    </header>");
        html.AppendLine("    <section aria-labelledby=\"summary-heading\">");
        html.AppendLine("      <h2 id=\"summary-heading\">Summary</h2>");
        html.AppendLine("      <dl>");
        AppendMetric(html, "Campaigns", workspace.Summary.CampaignCount);
        AppendMetric(html, "Reportable campaigns", workspace.Summary.ReportableCampaignCount);
        AppendMetric(html, "Submitted responses", workspace.Summary.SubmittedResponseCount);
        AppendMetric(html, "Scores", workspace.Summary.ScoreCount);
        AppendMetric(html, "Visible scores", workspace.Summary.VisibleScoreCount);
        AppendMetric(html, "Suppressed scores", workspace.Summary.SuppressedScoreCount);
        AppendMetric(html, "Preliminary live reports", workspace.Summary.PreliminaryLiveReportCount);
        AppendMetric(html, "Closed wave reports", workspace.Summary.ClosedWaveReportCount);
        html.AppendLine("      </dl>");
        html.AppendLine("    </section>");

        if (workspace.SelectedCampaign is not null)
        {
            html.AppendLine("    <section aria-labelledby=\"selected-campaign-heading\">");
            html.AppendLine("      <h2 id=\"selected-campaign-heading\">Selected campaign</h2>");
            html.AppendLine("      <dl>");
            AppendMetric(html, "Name", workspace.SelectedCampaign.Name);
            AppendMetric(html, "Status", workspace.SelectedCampaign.Status);
            AppendMetric(html, "Data finality", workspace.SelectedCampaign.DataFinality);
            AppendMetric(html, "Report status", workspace.SelectedCampaign.ReportStatus);
            AppendMetric(html, "Interpretation", workspace.SelectedCampaign.InterpretationStatus);
            AppendMetric(html, "Disclosure", workspace.SelectedCampaign.DisclosureState);
            html.AppendLine("      </dl>");
            html.AppendLine("    </section>");
        }

        html.AppendLine("    <section aria-labelledby=\"campaigns-heading\">");
        html.AppendLine("      <h2 id=\"campaigns-heading\">Campaigns</h2>");
        html.AppendLine("      <table>");
        html.AppendLine("        <thead><tr><th>Name</th><th>Status</th><th>Finality</th><th>Submitted</th><th>Scores</th><th>Visible</th><th>Suppressed</th></tr></thead>");
        html.AppendLine("        <tbody>");
        foreach (var campaign in workspace.Campaigns)
        {
            html.AppendLine("          <tr>");
            AppendCell(html, campaign.Name);
            AppendCell(html, campaign.Status);
            AppendCell(html, campaign.DataFinality);
            AppendCell(html, campaign.SubmittedResponseCount.ToString(CultureInfo.InvariantCulture));
            AppendCell(html, campaign.ScoreCount.ToString(CultureInfo.InvariantCulture));
            AppendCell(html, campaign.VisibleScoreCount.ToString(CultureInfo.InvariantCulture));
            AppendCell(html, campaign.SuppressedScoreCount.ToString(CultureInfo.InvariantCulture));
            html.AppendLine("          </tr>");
        }
        html.AppendLine("        </tbody>");
        html.AppendLine("      </table>");
        html.AppendLine("    </section>");
        html.AppendLine("  </main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        var codebookJson = JsonSerializer.Serialize(
            new
            {
                artifactType = "campaign_series_report_html",
                templateId = "campaign-series-report",
                templateVersion = 1,
                sourceProjection = "campaign_series_reports_workspace",
                sections = new[]
                {
                    "summary",
                    "selected_campaign",
                    "campaigns"
                },
                fields = new[]
                {
                    "campaign_count",
                    "reportable_campaign_count",
                    "submitted_response_count",
                    "score_count",
                    "visible_score_count",
                    "suppressed_score_count",
                    "data_finality",
                    "report_status",
                    "interpretation_status",
                    "disclosure_state"
                }
            },
            JsonOptions);

        return new CampaignSeriesReportHtmlRenderResult(
            html.ToString(),
            codebookJson,
            workspace.Campaigns.Count);
    }

    private static void AppendMetric(StringBuilder html, string label, int value)
    {
        AppendMetric(html, label, value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendMetric(StringBuilder html, string label, string value)
    {
        html.Append("        <dt>");
        AppendEncoded(html, label);
        html.AppendLine("</dt>");
        html.Append("        <dd>");
        AppendEncoded(html, value);
        html.AppendLine("</dd>");
    }

    private static void AppendCell(StringBuilder html, string value)
    {
        html.Append("            <td>");
        AppendEncoded(html, value);
        html.AppendLine("</td>");
    }

    private static void AppendEncoded(StringBuilder html, string? value)
    {
        html.Append(HtmlEncoder.Default.Encode(SafeText(value)));
    }

    private static string SafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
            ? "[redacted]"
            : value;
    }
}
