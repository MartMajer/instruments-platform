using Platform.Application.Features.ProductSurfaces;

namespace Platform.Application.Features.Reports;

public interface ICampaignSeriesReportHtmlRenderer
{
    CampaignSeriesReportHtmlRenderResult Render(
        CampaignSeriesReportsWorkspaceResponse workspace,
        DateTimeOffset generatedAt);
}

public sealed record CampaignSeriesReportHtmlRenderResult(
    string Html,
    string CodebookJson,
    int RowCount);
