using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.Workers.Reports;

public static class ReportPdfArtifactWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddReportPdfArtifactWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ReportPdfArtifactWorkerOptions>()
            .Bind(configuration.GetSection(ReportPdfArtifactWorkerOptions.SectionName))
            .Validate(
                options => options.InitialDelaySeconds > 0,
                "ReportPdfArtifacts:InitialDelaySeconds must be greater than zero.")
            .Validate(
                options => options.IntervalSeconds > 0,
                "ReportPdfArtifacts:IntervalSeconds must be greater than zero.")
            .Validate(
                options => options.RenderingTimeoutMinutes > 0,
                "ReportPdfArtifacts:RenderingTimeoutMinutes must be greater than zero.")
            .Validate(
                options => options.MaxArtifactsPerTenant > 0,
                "ReportPdfArtifacts:MaxArtifactsPerTenant must be greater than zero.")
            .Validate(
                options => options.MaxArtifactsPerTenant <= ReportPdfArtifactWorkerOptions.MaxAllowedArtifactsPerTenant,
                $"ReportPdfArtifacts:MaxArtifactsPerTenant must be less than or equal to {ReportPdfArtifactWorkerOptions.MaxAllowedArtifactsPerTenant}.")
            .Validate(
                options => options.MaxTenantsPerTick > 0,
                "ReportPdfArtifacts:MaxTenantsPerTick must be greater than zero.")
            .Validate(
                options => options.MaxTenantsPerTick <= ReportPdfArtifactWorkerOptions.MaxAllowedTenantsPerTick,
                $"ReportPdfArtifacts:MaxTenantsPerTick must be less than or equal to {ReportPdfArtifactWorkerOptions.MaxAllowedTenantsPerTick}.");

        services.AddHostedService<ReportPdfArtifactWorker>();

        return services;
    }
}
