using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Platform.Application.Features.ProductSurfaces;

namespace Platform.Application.Features.Reports;

public sealed class CampaignSeriesReportHtmlRenderer : ICampaignSeriesReportHtmlRenderer
{
    private const string DefaultAccentColorHex = "#2563eb";

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
        DateTimeOffset generatedAt,
        CampaignSeriesReportBranding? branding = null)
    {
        var safeBranding = NormalizeBranding(workspace, branding);
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.Append("  <title>");
        AppendEncoded(html, safeBranding.ReportTitle);
        html.AppendLine("</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    [data-template-id=\"campaign-series-report\"] { font-family: \"Noto Sans\", \"Liberation Sans\", sans-serif; }");
        html.Append("    [data-template-id=\"campaign-series-report\"] > header { border-top: 0.5rem solid ");
        html.Append(safeBranding.AccentColorHex);
        html.AppendLine("; padding-top: 1rem; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.Append("  <main data-template-id=\"campaign-series-report\" data-template-version=\"1\" data-branding-source=\"");
        AppendAttributeEncoded(html, safeBranding.BrandingSource);
        html.Append("\" data-brand-accent=\"");
        AppendAttributeEncoded(html, safeBranding.AccentColorHex);
        html.Append("\" data-layout-variant=\"");
        AppendAttributeEncoded(html, safeBranding.LayoutVariant);
        html.AppendLine("\">");
        html.AppendLine("    <header>");
        html.AppendLine("      <p>Campaign series report</p>");
        html.Append("      <p data-field=\"organization-label\">");
        AppendEncoded(html, safeBranding.OrganizationLabel);
        html.AppendLine("</p>");
        html.Append("      <h1 data-field=\"report-title\">");
        AppendEncoded(html, safeBranding.ReportTitle);
        html.AppendLine("</h1>");
        html.Append("      <p data-field=\"campaign-series-name\">Campaign series: ");
        AppendEncoded(html, workspace.Series.Name);
        html.AppendLine("</p>");
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

        AppendResultsAnalytics(html, workspace);

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
                    "branding",
                    "summary",
                    "selected_campaign",
                    "result_charts",
                    "result_outputs",
                    "group_breakdowns",
                    "wave_trends",
                    "insights",
                    "method_and_safeguards",
                    "campaigns"
                },
                branding = new
                {
                    boundary = "typed_slots_no_arbitrary_css",
                    brandingSource = safeBranding.BrandingSource,
                    organizationLabel = safeBranding.OrganizationLabel,
                    reportTitle = safeBranding.ReportTitle,
                    accentColorHex = safeBranding.AccentColorHex,
                    layoutVariant = safeBranding.LayoutVariant,
                    logoMode = "none",
                    allowedSlots = new[]
                    {
                        "organization_label",
                        "report_title",
                        "accent_color_hex",
                        "layout_variant",
                        "branding_source"
                    }
                },
                charts = new
                {
                    source = "server_inline_svg",
                    inputs = new[]
                    {
                        "results_analytics.score_outputs"
                    },
                    includedRows = "visible_score_outputs_with_mean",
                    excludedRows = "suppressed_or_missing_mean"
                },
                fields = new[]
                {
                    "organization_label",
                    "report_title",
                    "accent_color_hex",
                    "layout_variant",
                    "branding_source",
                    "chart_source",
                    "chart_output",
                    "chart_mean",
                    "campaign_count",
                    "reportable_campaign_count",
                    "submitted_response_count",
                    "score_count",
                    "visible_score_count",
                    "suppressed_score_count",
                    "data_finality",
                    "report_status",
                    "interpretation_status",
                    "disclosure_state",
                    "dimension_code",
                    "mean",
                    "median",
                    "standard_deviation",
                    "min",
                    "max",
                    "n_valid_total",
                    "n_expected_total",
                    "missing_policy_status_summary",
                    "suppression_reason",
                    "group_type",
                    "group_name",
                    "delta_from_previous_mean",
                    "delta_from_first_mean",
                    "comparison_state"
                }
            },
            JsonOptions);

        return new CampaignSeriesReportHtmlRenderResult(
            html.ToString(),
            codebookJson,
            ReportRowCount(workspace));
    }

    private static void AppendResultsAnalytics(
        StringBuilder html,
        CampaignSeriesReportsWorkspaceResponse workspace)
    {
        var analytics = workspace.ResultsAnalytics;
        if (analytics is null)
        {
            return;
        }

        AppendResultOutputChart(html, analytics);

        html.AppendLine("    <section aria-labelledby=\"result-outputs-heading\">");
        html.AppendLine("      <h2 id=\"result-outputs-heading\">Result outputs</h2>");
        html.AppendLine("      <table>");
        html.AppendLine("        <thead><tr><th>Output</th><th>Disclosure</th><th>Submitted</th><th>Scores</th><th>Mean</th><th>Median</th><th>SD</th><th>Min</th><th>Max</th><th>n valid</th><th>n expected</th><th>Missing policy</th><th>Suppression</th></tr></thead>");
        html.AppendLine("        <tbody>");
        foreach (var output in analytics.ScoreOutputs)
        {
            html.AppendLine("          <tr>");
            AppendCell(html, output.DimensionCode);
            AppendCell(html, output.Disclosure);
            AppendCell(html, FormatNullable(output.SubmittedResponseCount));
            AppendCell(html, FormatNullable(output.ScoreCount));
            AppendCell(html, FormatNullable(output.Mean));
            AppendCell(html, FormatNullable(output.Median));
            AppendCell(html, FormatNullable(output.StandardDeviation));
            AppendCell(html, FormatNullable(output.Min));
            AppendCell(html, FormatNullable(output.Max));
            AppendCell(html, FormatNullable(output.NValidTotal));
            AppendCell(html, FormatNullable(output.NExpectedTotal));
            AppendCell(html, output.MissingPolicyStatusSummary ?? "suppressed_or_not_available");
            AppendCell(html, output.SuppressionReason ?? string.Empty);
            html.AppendLine("          </tr>");
        }
        html.AppendLine("        </tbody>");
        html.AppendLine("      </table>");
        html.AppendLine("    </section>");

        if (analytics.GroupRows.Count > 0)
        {
            html.AppendLine("    <section aria-labelledby=\"group-breakdowns-heading\">");
            html.AppendLine("      <h2 id=\"group-breakdowns-heading\">Group breakdowns</h2>");
            html.AppendLine("      <table>");
            html.AppendLine("        <thead><tr><th>Group type</th><th>Group</th><th>Output</th><th>Disclosure</th><th>Submitted</th><th>Scores</th><th>Mean</th><th>Suppression</th></tr></thead>");
            html.AppendLine("        <tbody>");
            foreach (var row in analytics.GroupRows)
            {
                html.AppendLine("          <tr>");
                AppendCell(html, row.GroupType);
                AppendCell(html, row.GroupName);
                AppendCell(html, row.DimensionCode);
                AppendCell(html, row.Disclosure);
                AppendCell(html, FormatNullable(row.SubmittedResponseCount));
                AppendCell(html, FormatNullable(row.ScoreCount));
                AppendCell(html, FormatNullable(row.Mean));
                AppendCell(html, row.SuppressionReason ?? string.Empty);
                html.AppendLine("          </tr>");
            }
            html.AppendLine("        </tbody>");
            html.AppendLine("      </table>");
            html.AppendLine("    </section>");
        }

        if (analytics.WaveRows.Count > 0)
        {
            html.AppendLine("    <section aria-labelledby=\"wave-trends-heading\">");
            html.AppendLine("      <h2 id=\"wave-trends-heading\">Wave trends and finality</h2>");
            html.AppendLine("      <table>");
            html.AppendLine("        <thead><tr><th>Campaign</th><th>Status</th><th>Finality</th><th>Closed at</th><th>Output</th><th>Disclosure</th><th>Submitted</th><th>Scores</th><th>Mean</th><th>Delta previous</th><th>Delta first</th><th>Comparison</th><th>Suppression</th></tr></thead>");
            html.AppendLine("        <tbody>");
            foreach (var row in analytics.WaveRows)
            {
                html.AppendLine("          <tr>");
                AppendCell(html, row.CampaignName);
                AppendCell(html, row.CampaignStatus);
                AppendCell(html, row.DataFinality);
                AppendCell(html, FormatNullable(row.ClosedAt));
                AppendCell(html, row.DimensionCode);
                AppendCell(html, row.Disclosure);
                AppendCell(html, FormatNullable(row.SubmittedResponseCount));
                AppendCell(html, FormatNullable(row.ScoreCount));
                AppendCell(html, FormatNullable(row.Mean));
                AppendCell(html, FormatNullable(row.DeltaFromPreviousMean));
                AppendCell(html, FormatNullable(row.DeltaFromFirstMean));
                AppendCell(html, row.ComparisonState);
                AppendCell(html, row.SuppressionReason ?? string.Empty);
                html.AppendLine("          </tr>");
            }
            html.AppendLine("        </tbody>");
            html.AppendLine("      </table>");
            html.AppendLine("    </section>");
        }

        if (analytics.Insights.Count > 0)
        {
            html.AppendLine("    <section aria-labelledby=\"insights-heading\">");
            html.AppendLine("      <h2 id=\"insights-heading\">Report insights</h2>");
            html.AppendLine("      <ul>");
            foreach (var insight in analytics.Insights)
            {
                html.Append("        <li><strong>");
                AppendEncoded(html, insight.Title);
                html.Append("</strong> ");
                AppendEncoded(html, insight.Detail);
                html.Append(" <span>");
                AppendEncoded(html, $"{insight.Kind} / {insight.Severity}");
                html.AppendLine("</span></li>");
            }
            html.AppendLine("      </ul>");
            html.AppendLine("    </section>");
        }

        html.AppendLine("    <section aria-labelledby=\"method-heading\">");
        html.AppendLine("      <h2 id=\"method-heading\">Method and safeguards</h2>");
        html.AppendLine("      <dl>");
        AppendMetric(html, "Source projection", "campaign_series_reports_workspace");
        AppendMetric(html, "Disclosure state", analytics.DisclosureState);
        AppendMetric(html, "Disclosure k-min", analytics.DisclosureKMin);
        AppendMetric(html, "Result finality", "preliminary_live and closed_wave values are shown per campaign or wave.");
        AppendMetric(html, "Suppression", "Suppressed rows intentionally blank protected counts and values.");
        AppendMetric(html, "Interpretation", workspace.SelectedCampaign?.InterpretationStatus ?? "not_available");
        html.AppendLine("      </dl>");
        html.AppendLine("    </section>");
    }

    private static void AppendResultOutputChart(
        StringBuilder html,
        CampaignSeriesResultsAnalyticsResponse analytics)
    {
        var rows = analytics.ScoreOutputs
            .Where(output => output.Mean.HasValue &&
                string.Equals(output.Disclosure, "visible", StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToArray();

        if (rows.Length == 0)
        {
            return;
        }

        var maxMean = rows.Max(row => row.Mean!.Value);
        if (maxMean <= 0)
        {
            return;
        }

        const int width = 720;
        const int labelWidth = 190;
        const int barMaxWidth = 390;
        const int rowHeight = 38;
        const int top = 48;
        const int left = 24;
        var height = top + rows.Length * rowHeight + 32;

        html.AppendLine("    <section aria-labelledby=\"result-chart-heading\" data-chart-source=\"server_inline_svg\">");
        html.AppendLine("      <h2 id=\"result-chart-heading\">Result charts</h2>");
        html.AppendLine("      <p>Visible score outputs with available means are rendered as server-side inline SVG. Suppressed or missing rows are not charted.</p>");
        html.Append("      <svg role=\"img\" aria-labelledby=\"result-chart-title\" viewBox=\"0 0 ");
        html.Append(width.ToString(CultureInfo.InvariantCulture));
        html.Append(' ');
        html.Append(height.ToString(CultureInfo.InvariantCulture));
        html.AppendLine("\" xmlns=\"http://www.w3.org/2000/svg\">");
        html.AppendLine("        <title id=\"result-chart-title\">Visible result output means</title>");
        html.AppendLine("        <rect width=\"720\" height=\"100%\" fill=\"#ffffff\"/>");

        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var mean = row.Mean!.Value;
            var y = top + index * rowHeight;
            var barWidth = Math.Max(2, (int)Math.Round((double)(mean / maxMean) * barMaxWidth));

            html.Append("        <text x=\"");
            html.Append(left.ToString(CultureInfo.InvariantCulture));
            html.Append("\" y=\"");
            html.Append((y + 18).ToString(CultureInfo.InvariantCulture));
            html.Append("\" font-size=\"13\" fill=\"#111827\">");
            AppendEncoded(html, row.DimensionCode);
            html.AppendLine("</text>");
            html.Append("        <rect x=\"");
            html.Append((left + labelWidth).ToString(CultureInfo.InvariantCulture));
            html.Append("\" y=\"");
            html.Append(y.ToString(CultureInfo.InvariantCulture));
            html.Append("\" width=\"");
            html.Append(barWidth.ToString(CultureInfo.InvariantCulture));
            html.Append("\" height=\"22\" rx=\"4\" fill=\"#2563eb\"/>");
            html.AppendLine();
            html.Append("        <text x=\"");
            html.Append((left + labelWidth + barWidth + 10).ToString(CultureInfo.InvariantCulture));
            html.Append("\" y=\"");
            html.Append((y + 17).ToString(CultureInfo.InvariantCulture));
            html.Append("\" font-size=\"12\" fill=\"#374151\">");
            AppendEncoded(html, FormatNullable(mean));
            html.AppendLine("</text>");
        }

        html.AppendLine("      </svg>");
        html.AppendLine("    </section>");
    }

    private static CampaignSeriesReportBranding NormalizeBranding(
        CampaignSeriesReportsWorkspaceResponse workspace,
        CampaignSeriesReportBranding? branding)
    {
        var defaults = DefaultBranding(workspace);
        var candidate = branding ?? defaults;

        return new CampaignSeriesReportBranding(
            SafeTextOrDefault(candidate.OrganizationLabel, defaults.OrganizationLabel),
            SafeTextOrDefault(candidate.ReportTitle, defaults.ReportTitle),
            NormalizeAccentColorHex(candidate.AccentColorHex),
            NormalizeKnownToken(
                candidate.LayoutVariant,
                defaults.LayoutVariant,
                "standard",
                "compact",
                "compliance"),
            NormalizeKnownToken(
                candidate.BrandingSource,
                defaults.BrandingSource,
                "platform_default",
                "explicit_input",
                "tenant_profile",
                "tenant_settings"));
    }

    private static CampaignSeriesReportBranding DefaultBranding(CampaignSeriesReportsWorkspaceResponse workspace)
    {
        var seriesName = SafeTextOrDefault(workspace.Series.Name, "Campaign series");
        return new CampaignSeriesReportBranding(
            "Instruments Platform",
            $"{seriesName} report",
            DefaultAccentColorHex,
            "standard",
            "platform_default");
    }

    private static string NormalizeAccentColorHex(string? value)
    {
        var candidate = value?.Trim();
        if (candidate is null ||
            candidate.Length != 7 ||
            candidate[0] != '#' ||
            !candidate[1..].All(Uri.IsHexDigit))
        {
            return DefaultAccentColorHex;
        }

        return candidate.ToLowerInvariant();
    }

    private static string NormalizeKnownToken(string? value, string fallback, params string[] allowedValues)
    {
        var candidate = SafeText(value).Trim().ToLowerInvariant();
        return allowedValues.Contains(candidate, StringComparer.Ordinal)
            ? candidate
            : fallback;
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

    private static string FormatNullable(int? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "suppressed";
    }

    private static string FormatNullable(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.####", CultureInfo.InvariantCulture)
            : "suppressed";
    }

    private static string FormatNullable(DateTimeOffset? value)
    {
        return value.HasValue
            ? value.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static int ReportRowCount(CampaignSeriesReportsWorkspaceResponse workspace)
    {
        var analytics = workspace.ResultsAnalytics;
        return workspace.Campaigns.Count +
            (analytics?.ScoreOutputs.Count ?? 0) +
            (analytics?.GroupRows.Count ?? 0) +
            (analytics?.WaveRows.Count ?? 0) +
            (analytics?.Insights.Count ?? 0);
    }

    private static void AppendEncoded(StringBuilder html, string? value)
    {
        html.Append(HtmlEncoder.Default.Encode(SafeText(value)));
    }

    private static void AppendAttributeEncoded(StringBuilder html, string? value)
    {
        html.Append(HtmlEncoder.Default.Encode(SafeText(value)));
    }

    private static string SafeTextOrDefault(string? value, string fallback)
    {
        var safe = SafeText(value).Trim();
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
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
