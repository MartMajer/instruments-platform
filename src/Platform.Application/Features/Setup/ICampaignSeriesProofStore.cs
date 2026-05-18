using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public interface ICampaignSeriesProofStore
{
    Task<Result<CampaignSeriesTwoWaveProofResponse>> GetTwoWaveProofAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);
}
