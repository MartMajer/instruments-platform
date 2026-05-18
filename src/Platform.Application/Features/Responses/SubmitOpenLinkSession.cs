using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SubmitOpenLinkSessionCommand(
    string Token,
    Guid SessionId,
    SubmitResponseSessionRequest Request)
    : IRequest<Result<SubmitResponseSessionResponse>>;

public sealed class SubmitOpenLinkSessionValidator : AbstractValidator<SubmitOpenLinkSessionCommand>
{
    public SubmitOpenLinkSessionValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.SessionId).NotEmpty();
        RuleFor(command => command.Request.TimeTakenMs)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Request.TimeTakenMs.HasValue);
    }
}

public sealed class SubmitOpenLinkSessionHandler(IResponseCaptureStore store)
    : IRequestHandler<SubmitOpenLinkSessionCommand, Result<SubmitResponseSessionResponse>>
{
    public Task<Result<SubmitResponseSessionResponse>> Handle(
        SubmitOpenLinkSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.SubmitOpenLinkSessionAsync(
            command.Token,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
