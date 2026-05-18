using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Reports;
using Platform.Domain.Reports;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Reports;

public sealed class ReportPdfArtifactWorkerTenantSource(ApplicationDbContext db) : IReportPdfArtifactWorkerTenantSource
{
    public async Task<IReadOnlyList<Guid>> ListTenantIdsWithQueuedReportPdfArtifactsAsync(
        int maxTenantsPerTick,
        DateTimeOffset staleRenderingBefore,
        CancellationToken cancellationToken)
    {
        if (maxTenantsPerTick <= 0)
        {
            return [];
        }

        return await db.ExportArtifacts
            .AsNoTracking()
            .Where(artifact =>
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                (artifact.Status == ExportArtifactStatuses.Queued ||
                    (artifact.Status == ExportArtifactStatuses.Rendering &&
                        artifact.StartedAt.HasValue &&
                        artifact.StartedAt.Value < staleRenderingBefore)))
            .Select(artifact => artifact.TenantId)
            .Distinct()
            .OrderBy(tenantId => tenantId)
            .Take(maxTenantsPerTick)
            .ToListAsync(cancellationToken);
    }
}
