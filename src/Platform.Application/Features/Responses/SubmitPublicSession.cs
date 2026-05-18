using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SubmitPublicSessionCommand(
    string Handle,
    SubmitResponseSessionRequest Request)
    : IRequest<Result<SubmitResponseSessionResponse>>;

public sealed class SubmitPublicSessionValidator : AbstractValidator<SubmitPublicSessionCommand>
{
    public SubmitPublicSessionValidator()
    {
        RuleFor(command => command.Handle).NotEmpty();
        RuleFor(command => command.Request.TimeTakenMs)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Request.TimeTakenMs.HasValue);
    }
}

public sealed class SubmitPublicSessionHandler(IResponseCaptureStore store)
    : IRequestHandler<SubmitPublicSessionCommand, Result<SubmitResponseSessionResponse>>
{
    public Task<Result<SubmitResponseSessionResponse>> Handle(
        SubmitPublicSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.SubmitPublicSessionAsync(
            command.Handle,
            command.Request,
            cancellationToken);
    }
}
