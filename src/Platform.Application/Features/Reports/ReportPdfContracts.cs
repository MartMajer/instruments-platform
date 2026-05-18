using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IReportPdfRenderer
{
    Task<Result<ReportPdfRenderResult>> RenderAsync(
        ReportPdfRenderRequest request,
        CancellationToken cancellationToken);
}

public sealed record ReportPdfRenderRequest(
    string Html,
    string TemplateId,
    int TemplateVersion,
    DateTimeOffset GeneratedAt);

public sealed record ReportPdfRenderResult(
    byte[] PdfBytes,
    string ContentType,
    long ByteSize,
    string Renderer,
    string BrowserVersion,
    string OptionsHashSha256);
