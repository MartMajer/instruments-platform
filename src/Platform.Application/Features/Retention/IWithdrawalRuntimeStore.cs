using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public interface IWithdrawalRuntimeStore
{
    Task<Result<WithdrawalRequestResponse>> CreateWithdrawalRequestAsync(
        Guid tenantId,
        CreateWithdrawalRequestCommand command,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalRequestTokenIssueResponse>> IssueWithdrawalRequestTokenAsync(
        Guid tenantId,
        IssueWithdrawalRequestTokenCommand command,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalRequestResponse>> CreateAnonymousWithdrawalRequestAsync(
        CreateAnonymousWithdrawalRequestCommand command,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<WithdrawalRequestReviewResponse>>> ListWithdrawalRequestsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalRequestReviewResponse>> GetWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalRequestReviewResponse>> ApproveWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        WithdrawalRequestDecisionCommand command,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalRequestReviewResponse>> DenyWithdrawalRequestAsync(
        Guid tenantId,
        Guid requestId,
        WithdrawalRequestDecisionCommand command,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalEventResponse>> PlanIdentifiedWithdrawalAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid subjectId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalEventResponse>> PlanAnonymousLongitudinalWithdrawalAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        string rawParticipantCode,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalDryRunResponse>> DryRunWithdrawalAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalExecutionStateResponse>> ClaimWithdrawalForExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalExecutionStateResponse>> CompleteWithdrawalExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalExecutionStateResponse>> FailWithdrawalExecutionAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken);

    Task<Result<WithdrawalExecutionStateResponse>> ExecuteWithdrawalAsync(
        Guid tenantId,
        Guid withdrawalEventId,
        CancellationToken cancellationToken);
}
