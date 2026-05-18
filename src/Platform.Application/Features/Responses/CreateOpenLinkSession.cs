using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record CreateOpenLinkSessionCommand(string Token, CreateOpenLinkSessionRequest Request)
    : IRequest<Result<ResponseSessionResponse>>;

public sealed class CreateOpenLinkSessionValidator : AbstractValidator<CreateOpenLinkSessionCommand>
{
    public CreateOpenLinkSessionValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.Request.Locale).NotEmpty();
    }
}

public sealed class CreateOpenLinkSessionHandler(IResponseCaptureStore store)
    : IRequestHandler<CreateOpenLinkSessionCommand, Result<ResponseSessionResponse>>
{
    public Task<Result<ResponseSessionResponse>> Handle(
        CreateOpenLinkSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateOpenLinkSessionAsync(
            command.Token,
            command.Request,
            cancellationToken);
    }
}
