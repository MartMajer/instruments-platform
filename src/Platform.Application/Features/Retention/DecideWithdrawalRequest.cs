using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record ApproveWithdrawalRequestCommand(
    Guid RequestId,
    WithdrawalRequestDecisionRequest Request) : IRequest<Result<WithdrawalRequestReviewResponse>>;

public sealed class ApproveWithdrawalRequestValidator
    : AbstractValidator<ApproveWithdrawalRequestCommand>
{
    public ApproveWithdrawalRequestValidator()
    {
        RuleFor(command => command.RequestId)
            .NotEmpty();
        RuleFor(command => command.Request.ReasonCode)
            .MaximumLength(64);
    }
}

public sealed class ApproveWithdrawalRequestHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<ApproveWithdrawalRequestCommand, Result<WithdrawalRequestReviewResponse>>
{
    public Task<Result<WithdrawalRequestReviewResponse>> Handle(
        ApproveWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ApproveWithdrawalRequestAsync(
            currentTenant.TenantId,
            command.RequestId,
            new WithdrawalRequestDecisionCommand(actor.UserId.Value, command.Request.ReasonCode),
            cancellationToken);
    }
}

public sealed record DenyWithdrawalRequestCommand(
    Guid RequestId,
    WithdrawalRequestDecisionRequest Request) : IRequest<Result<WithdrawalRequestReviewResponse>>;

public sealed class DenyWithdrawalRequestValidator
    : AbstractValidator<DenyWithdrawalRequestCommand>
{
    public DenyWithdrawalRequestValidator()
    {
        RuleFor(command => command.RequestId)
            .NotEmpty();
        RuleFor(command => command.Request.ReasonCode)
            .MaximumLength(64);
    }
}

public sealed class DenyWithdrawalRequestHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<DenyWithdrawalRequestCommand, Result<WithdrawalRequestReviewResponse>>
{
    public Task<Result<WithdrawalRequestReviewResponse>> Handle(
        DenyWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<WithdrawalRequestReviewResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.DenyWithdrawalRequestAsync(
            currentTenant.TenantId,
            command.RequestId,
            new WithdrawalRequestDecisionCommand(actor.UserId.Value, command.Request.ReasonCode),
            cancellationToken);
    }
}
