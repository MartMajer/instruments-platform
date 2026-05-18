using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record IssueWithdrawalRequestTokenEndpointCommand(
    IssueWithdrawalRequestTokenRequest Request) : IRequest<Result<WithdrawalRequestTokenIssueResponse>>;

public sealed class IssueWithdrawalRequestTokenValidator
    : AbstractValidator<IssueWithdrawalRequestTokenEndpointCommand>
{
    public IssueWithdrawalRequestTokenValidator()
    {
        RuleFor(command => command.Request.ResponseSessionId)
            .NotEmpty();
        RuleFor(command => command.Request.RequestedAction)
            .NotEmpty()
            .MaximumLength(64);
        RuleFor(command => command.Request.ExpiresAt)
            .Must(expiresAt => expiresAt > DateTimeOffset.UtcNow)
            .WithMessage("Withdrawal token expiry must be in the future.");
        RuleFor(command => command.Request.ReasonCode)
            .MaximumLength(64);
    }
}

public sealed class IssueWithdrawalRequestTokenHandler(
    ICurrentTenant currentTenant,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<IssueWithdrawalRequestTokenEndpointCommand, Result<WithdrawalRequestTokenIssueResponse>>
{
    public Task<Result<WithdrawalRequestTokenIssueResponse>> Handle(
        IssueWithdrawalRequestTokenEndpointCommand command,
        CancellationToken cancellationToken)
    {
        return store.IssueWithdrawalRequestTokenAsync(
            currentTenant.TenantId,
            new IssueWithdrawalRequestTokenCommand(
                command.Request.ResponseSessionId,
                command.Request.RequestedAction,
                command.Request.ExpiresAt,
                command.Request.ReasonCode),
            cancellationToken);
    }
}
