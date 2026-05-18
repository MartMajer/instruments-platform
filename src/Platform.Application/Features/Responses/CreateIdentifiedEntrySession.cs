using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record CreateIdentifiedEntrySessionCommand(
    string Token,
    CreateOpenLinkSessionRequest Request)
    : IRequest<Result<ResponseSessionResponse>>;

public sealed class CreateIdentifiedEntrySessionValidator
    : AbstractValidator<CreateIdentifiedEntrySessionCommand>
{
    public CreateIdentifiedEntrySessionValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.Request.Locale).NotEmpty();
    }
}

public sealed class CreateIdentifiedEntrySessionHandler(IResponseCaptureStore store)
    : IRequestHandler<CreateIdentifiedEntrySessionCommand, Result<ResponseSessionResponse>>
{
    public Task<Result<ResponseSessionResponse>> Handle(
        CreateIdentifiedEntrySessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateIdentifiedEntrySessionAsync(
            command.Token,
            command.Request,
            cancellationToken);
    }
}
