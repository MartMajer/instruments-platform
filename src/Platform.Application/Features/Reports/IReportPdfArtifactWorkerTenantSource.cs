namespace Platform.Application.Features.Reports;

public interface IReportPdfArtifactWorkerTenantSource
{
    Task<IReadOnlyList<Guid>> ListTenantIdsWithQueuedReportPdfArtifactsAsync(
        int maxTenantsPerTick,
        DateTimeOffset staleRenderingBefore,
        CancellationToken cancellationToken);
}
