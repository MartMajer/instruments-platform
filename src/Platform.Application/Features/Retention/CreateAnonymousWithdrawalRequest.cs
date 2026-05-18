using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record CreateAnonymousWithdrawalRequestEndpointCommand(
    CreateAnonymousWithdrawalRequestRequest Request) : IRequest<Result<WithdrawalRequestResponse>>;

public sealed class CreateAnonymousWithdrawalRequestValidator
    : AbstractValidator<CreateAnonymousWithdrawalRequestEndpointCommand>
{
    public CreateAnonymousWithdrawalRequestValidator()
    {
        RuleFor(command => command.Request.Token)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(command => command.Request.RequestedAction)
            .NotEmpty()
            .MaximumLength(64);
        RuleFor(command => command.Request.ReasonCode)
            .MaximumLength(64);
    }
}

public sealed class CreateAnonymousWithdrawalRequestHandler(
    IWithdrawalRuntimeStore store)
    : IRequestHandler<CreateAnonymousWithdrawalRequestEndpointCommand, Result<WithdrawalRequestResponse>>
{
    public Task<Result<WithdrawalRequestResponse>> Handle(
        CreateAnonymousWithdrawalRequestEndpointCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateAnonymousWithdrawalRequestAsync(
            new CreateAnonymousWithdrawalRequestCommand(
                command.Request.Token,
                command.Request.RequestedAction,
                command.Request.ReasonCode),
            cancellationToken);
    }
}
