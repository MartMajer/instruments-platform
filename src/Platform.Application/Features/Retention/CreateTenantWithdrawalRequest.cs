using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record CreateTenantWithdrawalRequestCommand(
    CreateWithdrawalRequestRequest Request) : IRequest<Result<WithdrawalRequestResponse>>;

public sealed class CreateTenantWithdrawalRequestValidator
    : AbstractValidator<CreateTenantWithdrawalRequestCommand>
{
    public CreateTenantWithdrawalRequestValidator()
    {
        RuleFor(command => command.Request.TargetKind)
            .NotEmpty()
            .MaximumLength(64);
        RuleFor(command => command.Request.TargetId)
            .NotEmpty();
        RuleFor(command => command.Request.RequestedAction)
            .NotEmpty()
            .MaximumLength(64);
        RuleFor(command => command.Request.ReasonCode)
            .MaximumLength(64);
    }
}

public sealed class CreateTenantWithdrawalRequestHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<CreateTenantWithdrawalRequestCommand, Result<WithdrawalRequestResponse>>
{
    public Task<Result<WithdrawalRequestResponse>> Handle(
        CreateTenantWithdrawalRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<WithdrawalRequestResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CreateWithdrawalRequestAsync(
            currentTenant.TenantId,
            new CreateWithdrawalRequestCommand(
                command.Request.TargetKind,
                command.Request.TargetId,
                command.Request.RequestedAction,
                actor.UserId.Value,
                command.Request.ReasonCode),
            cancellationToken);
    }
}
