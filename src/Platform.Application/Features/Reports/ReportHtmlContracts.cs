using Platform.Application.Features.ProductSurfaces;

namespace Platform.Application.Features.Reports;

public interface ICampaignSeriesReportHtmlRenderer
{
    CampaignSeriesReportHtmlRenderResult Render(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset generatedAt,
        CampaignSeriesReportBranding? branding = null);
}

public sealed record CampaignSeriesReportBranding(
    string OrganizationLabel,
    string ReportTitle,
    string AccentColorHex,
    string LayoutVariant,
    string BrandingSource);

public sealed record CampaignSeriesReportHtmlRenderResult(
    string Html,
    string CodebookJson,
    int RowCount);
