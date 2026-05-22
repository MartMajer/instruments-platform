using Platform.SharedKernel;

namespace Platform.Application.Features.TestData;

public interface ITestDataSimulatorStore
{
    Task<Result<CreateCampaignTestRecipientsResponse>> CreateCampaignTestRecipientsAsync(
        Guid tenantId,
        Guid? actorUserId,
        Guid campaignId,
        CreateCampaignTestRecipientsRequest request,
        CancellationToken cancellationToken);

    Task<Result<CreateCampaignTestResponsesResponse>> CreateCampaignTestResponsesAsync(
        Guid tenantId,
        Guid? actorUserId,
        Guid campaignId,
        CreateCampaignTestResponsesRequest request,
        CancellationToken cancellationToken);
}
