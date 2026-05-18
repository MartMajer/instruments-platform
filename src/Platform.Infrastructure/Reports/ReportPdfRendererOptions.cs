namespace Platform.Infrastructure.Reports;

public sealed class ReportPdfRendererOptions
{
    public const string SectionName = "Reports:PdfRenderer";

    public string? BrowserExecutablePath { get; set; }

    public int TimeoutMilliseconds { get; set; } = 30_000;

    public bool DisableSandbox { get; set; } = true;
}
