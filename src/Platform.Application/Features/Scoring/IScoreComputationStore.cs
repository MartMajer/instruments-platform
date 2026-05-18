using Platform.SharedKernel;

namespace Platform.Application.Features.Scoring;

public interface IScoreComputationStore
{
    Task<Result<ComputeScoresResponse>> ComputeResponseScoresAsync(
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
