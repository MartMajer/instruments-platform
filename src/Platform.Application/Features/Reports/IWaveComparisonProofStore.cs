using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public interface IWaveComparisonProofStore
{
    Task<Result<CampaignSeriesWaveComparisonProofResponse>> GetCampaignSeriesWaveComparisonProofAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);
}
