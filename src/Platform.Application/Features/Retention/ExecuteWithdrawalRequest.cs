using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record ExecuteWithdrawalRequestCommand(Guid RequestId)
    : IRequest<Result<WithdrawalExecutionStateResponse>>;

public sealed class ExecuteWithdrawalRequestValidator
    : AbstractValidator<ExecuteWithdrawalRequestCommand>
{
    public ExecuteWithdrawalRequestValidator()
    {
        RuleFor(command => command.RequestId)
            .NotEmpty();
    }
}

public sealed class ExecuteWithdrawalRequestHandler(
    ICurrentTenant currentTenant,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<ExecuteWithdrawalRequestCommand, Result<WithdrawalExecutionStateResponse>>
{
    public Task<Result<WithdrawalExecutionStateResponse>> Handle(
        ExecuteWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        return store.ExecuteWithdrawalAsync(
            currentTenant.TenantId,
            command.RequestId,
            cancellationToken);
    }
}
