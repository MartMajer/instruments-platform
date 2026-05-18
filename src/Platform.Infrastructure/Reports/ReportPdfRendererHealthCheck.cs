using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Platform.Application.Features.System.GetHealth;

namespace Platform.Infrastructure.Reports;

public sealed class ReportPdfRendererHealthCheck(
    IOptions<ReportPdfRendererOptions> options,
    IHostEnvironment environment) : IPlatformHealthCheck
{
    public string Name => "report_pdf_renderer";

    public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment() &&
            string.IsNullOrWhiteSpace(options.Value.BrowserExecutablePath))
        {
            return Task.FromResult(PlatformHealthCheckResult.Ok(Name));
        }

        if (string.IsNullOrWhiteSpace(options.Value.BrowserExecutablePath))
        {
            return Task.FromResult(PlatformHealthCheckResult.Unready(Name));
        }

        try
        {
            var browserPath = options.Value.BrowserExecutablePath.Trim();
            if (!Path.IsPathFullyQualified(browserPath))
            {
                return Task.FromResult(PlatformHealthCheckResult.Unready(Name));
            }

            return Task.FromResult(File.Exists(browserPath)
                ? PlatformHealthCheckResult.Ok(Name)
                : PlatformHealthCheckResult.Unready(Name));
        }
        catch
        {
            return Task.FromResult(PlatformHealthCheckResult.Unready(Name));
        }
    }
}
