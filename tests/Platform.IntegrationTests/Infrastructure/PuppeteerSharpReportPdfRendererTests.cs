using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.Infrastructure.Reports;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class PuppeteerSharpReportPdfRendererTests
{
    [LocalBrowserFact]
    public async Task Puppeteer_renderer_creates_pdf_bytes_from_fixture_html()
    {
        var browserPath = FindBrowserExecutable();
        Assert.NotNull(browserPath);
        var renderer = new PuppeteerSharpReportPdfRenderer(Options.Create(new ReportPdfRendererOptions
        {
            BrowserExecutablePath = browserPath,
            TimeoutMilliseconds = 30_000
        }));

        var result = await renderer.RenderAsync(
            new ReportPdfRenderRequest(
                """
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <title>PDF renderer smoke</title>
                  <style>
                    @page { size: A4; margin: 20mm; }
                    body { font-family: Arial, sans-serif; }
                  </style>
                </head>
                <body>
                  <main>
                    <h1>PDF renderer smoke</h1>
                    <p>Fixture document with Croatian glyphs: ČŽŠ ćžš.</p>
                  </main>
                </body>
                </html>
                """,
                "pdf-renderer-smoke",
                1,
                DateTimeOffset.Parse("2026-05-18T12:00:00+00:00")),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.Equal(result.Value.PdfBytes.Length, result.Value.ByteSize);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(result.Value.PdfBytes.AsSpan(0, 5)));
        Assert.Equal("puppeteersharp", result.Value.Renderer);
        Assert.NotEmpty(result.Value.BrowserVersion);
        Assert.Matches("^[a-f0-9]{64}$", result.Value.OptionsHashSha256);
    }

    public static string? FindBrowserExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public sealed class LocalBrowserFactAttribute : FactAttribute
    {
        public LocalBrowserFactAttribute()
        {
            if (FindBrowserExecutable() is null)
            {
                Skip = "Install Chrome/Edge or set PUPPETEER_EXECUTABLE_PATH to run this browser-backed PDF smoke test.";
            }
        }
    }
}
