using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IReportProofStore
{
    Task<Result<CampaignReportProofResponse>> GetCampaignReportProofAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);
}
