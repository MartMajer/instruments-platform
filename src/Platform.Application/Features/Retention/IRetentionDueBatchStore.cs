using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public interface IRetentionDueBatchStore
{
    Task<Result<RetentionDueBatchResponse>> PlanDueBatchAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchDryRunResponse>> DryRunDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchResponse>> ClaimDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        DateTimeOffset processingStartedAt,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchResponse>> CompleteDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchResponse>> FailDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        string failureCode,
        string? failureDetail,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchExecutionResponse>> ExecuteDueBatchAsync(
        Guid tenantId,
        Guid dueBatchId,
        CancellationToken cancellationToken);

    Task<Result<RetentionDueBatchAutomationRunResponse>> RunDueBatchAutomationAsync(
        Guid tenantId,
        DateTimeOffset asOf,
        int maxBatches,
        CancellationToken cancellationToken);
}
