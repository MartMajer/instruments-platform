using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public interface IRetentionDueCandidateStore
{
    Task<Result<RetentionDueCandidatePlanResponse>> PlanDueCandidatesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);
}
