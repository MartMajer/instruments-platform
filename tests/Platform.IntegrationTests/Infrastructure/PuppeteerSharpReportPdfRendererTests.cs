using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Reports;
using Platform.Infrastructure.Reports;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class PuppeteerSharpReportPdfRendererTests
{
    private const string RunSmokeVariable = "RUN_PDF_RENDERER_SMOKE";
    private const string BrowserPathVariable = "PUPPETEER_EXECUTABLE_PATH";
    private static readonly string[] ApprovedFontFamilies = ["Noto Sans", "Liberation Sans"];

    [LocalBrowserFact]
    public async Task Puppeteer_renderer_creates_pdf_bytes_from_fixture_html()
    {
        var browserPath = FindBrowserExecutable();
        Assert.NotNull(browserPath);
        AssertApprovedLocalFontAvailable();
        var renderer = new PuppeteerSharpReportPdfRenderer(Options.Create(new ReportPdfRendererOptions
        {
            BrowserExecutablePath = browserPath,
            TimeoutMilliseconds = 30_000
        }));

        var result = await renderer.RenderAsync(
            new ReportPdfRenderRequest(
                """
                <!doctype html>
                <html lang="hr">
                <head>
                  <meta charset="utf-8">
                  <title>PDF renderer localized font smoke</title>
                  <style>
                    @page { size: A4; margin: 20mm; }
                    body { font-family: "Noto Sans", "Liberation Sans", sans-serif; }
                    .localized-glyphs { font-weight: 600; letter-spacing: 0.01em; }
                  </style>
                </head>
                <body>
                  <main>
                    <h1>PDF renderer localized font smoke</h1>
                    <p>English report sentence using the approved local report font stack.</p>
                    <p class="localized-glyphs">Croatian glyph fixture: ČĆŽŠĐ čćžšđ.</p>
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
        Assert.True(result.Value.ByteSize > 1_024);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(result.Value.PdfBytes.AsSpan(0, 5)));
        Assert.Equal("puppeteersharp", result.Value.Renderer);
        Assert.NotEmpty(result.Value.BrowserVersion);
        Assert.Matches("^[a-f0-9]{64}$", result.Value.OptionsHashSha256);
    }

    private static void AssertApprovedLocalFontAvailable()
    {
        var matches = ApprovedFontFamilies
            .Select(RunFontConfigMatch)
            .OfType<string>()
            .Where(match => !string.IsNullOrWhiteSpace(match))
            .ToArray();

        Assert.Contains(
            matches,
            match => match.Contains("NotoSans", StringComparison.OrdinalIgnoreCase) ||
                match.Contains("Noto Sans", StringComparison.OrdinalIgnoreCase) ||
                match.Contains("LiberationSans", StringComparison.OrdinalIgnoreCase) ||
                match.Contains("Liberation Sans", StringComparison.OrdinalIgnoreCase));
    }

    private static string? RunFontConfigMatch(string fontFamily)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "fc-match",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add(fontFamily);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            if (!process.WaitForExit(5_000))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0
                ? process.StandardOutput.ReadToEnd()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static string? FindBrowserExecutable()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable(RunSmokeVariable),
            "1",
            StringComparison.Ordinal))
        {
            return null;
        }

        var configured = Environment.GetEnvironmentVariable(BrowserPathVariable);
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        return null;
    }

    public sealed class LocalBrowserFactAttribute : FactAttribute
    {
        public LocalBrowserFactAttribute()
        {
            if (FindBrowserExecutable() is null)
            {
                Skip =
                    $"Set {RunSmokeVariable}=1 and {BrowserPathVariable} to an installed Chrome/Chromium/Edge executable to run this browser-backed PDF smoke test.";
            }
        }
    }
}
