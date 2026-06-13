using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Platform.Infrastructure.Reports;

public sealed class PuppeteerSharpReportPdfRenderer(
    IOptions<ReportPdfRendererOptions> options) : IReportPdfRenderer
{
    private const string RendererName = "puppeteersharp";
    private const string ContentType = "application/pdf";

    public async Task<Result<ReportPdfRenderResult>> RenderAsync(
        ReportPdfRenderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Result.Failure<ReportPdfRenderResult>(Error.Validation(
                "report_pdf.html_missing",
                "Report PDF rendering requires HTML content."));
        }

        var browserExecutablePath = options.Value.BrowserExecutablePath;
        if (string.IsNullOrWhiteSpace(browserExecutablePath) ||
            !File.Exists(browserExecutablePath))
        {
            return Result.Failure<ReportPdfRenderResult>(Error.Conflict(
                "report_pdf.browser_unavailable",
                "Report PDF renderer browser is unavailable."));
        }

        if (options.Value.TimeoutMilliseconds <= 0)
        {
            return Result.Failure<ReportPdfRenderResult>(Error.Validation(
                "report_pdf.timeout_invalid",
                "Report PDF renderer timeout must be positive."));
        }

        IBrowser? browser = null;
        IPage? page = null;
        try
        {
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                ExecutablePath = browserExecutablePath,
                Headless = true,
                Timeout = options.Value.TimeoutMilliseconds,
                Args = options.Value.DisableSandbox
                    ? ["--no-sandbox", "--disable-setuid-sandbox"]
                    : []
            });
            var browserVersion = await browser.GetVersionAsync();
            page = await browser.NewPageAsync();
            await page.SetContentAsync(request.Html);
            await page.BringToFrontAsync();

            var pdfOptions = CreatePdfOptions(options.Value.TimeoutMilliseconds);
            var pdfBytes = await page.PdfDataAsync(pdfOptions);

            return Result.Success(new ReportPdfRenderResult(
                pdfBytes,
                ContentType,
                pdfBytes.LongLength,
                RendererName,
                browserVersion,
                CreateOptionsHash(request, pdfOptions)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result.Failure<ReportPdfRenderResult>(Error.Conflict(
                "report_pdf.render_failed",
                "Report PDF rendering failed."));
        }
        finally
        {
            if (page is not null)
            {
                await page.CloseAsync();
            }

            if (browser is not null)
            {
                await browser.CloseAsync();
            }
        }
    }

    private static PdfOptions CreatePdfOptions(int timeoutMilliseconds)
    {
        return new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            PreferCSSPageSize = true,
            WaitForFonts = true,
            Timeout = timeoutMilliseconds,
            MarginOptions = new MarginOptions
            {
                Top = "20mm",
                Right = "15mm",
                Bottom = "20mm",
                Left = "15mm"
            }
        };
    }

    private static string CreateOptionsHash(
        ReportPdfRenderRequest request,
        PdfOptions options)
    {
        var payload = JsonSerializer.Serialize(new
        {
            request.TemplateId,
            request.TemplateVersion,
            format = "A4",
            options.PrintBackground,
            options.PreferCSSPageSize,
            options.WaitForFonts,
            options.Timeout,
            margin = new
            {
                options.MarginOptions?.Top,
                options.MarginOptions?.Right,
                options.MarginOptions?.Bottom,
                options.MarginOptions?.Left
            }
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
