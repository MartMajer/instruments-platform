using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record UnsubscribeEmailInvitationCommand(string Token, bool Confirmed, bool WorkspaceWide = false)
    : IRequest<Result<EmailInvitationUnsubscribeResponse>>;

public sealed class UnsubscribeEmailInvitationValidator : AbstractValidator<UnsubscribeEmailInvitationCommand>
{
    public UnsubscribeEmailInvitationValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.Confirmed)
            .Equal(true)
            .WithMessage("Confirm the unsubscribe request before applying do-not-contact suppression.");
    }
}

public sealed class UnsubscribeEmailInvitationHandler(IResponseCaptureStore store)
    : IRequestHandler<UnsubscribeEmailInvitationCommand, Result<EmailInvitationUnsubscribeResponse>>
{
    public Task<Result<EmailInvitationUnsubscribeResponse>> Handle(
        UnsubscribeEmailInvitationCommand command,
        CancellationToken cancellationToken)
    {
        return store.UnsubscribeEmailInvitationAsync(command.Token, command.WorkspaceWide, cancellationToken);
    }
}
