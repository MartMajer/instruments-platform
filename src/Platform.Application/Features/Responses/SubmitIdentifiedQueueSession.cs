using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SubmitIdentifiedQueueSessionCommand(
    string Token,
    Guid AssignmentId,
    Guid SessionId,
    SubmitResponseSessionRequest Request)
    : IRequest<Result<SubmitResponseSessionResponse>>;

public sealed class SubmitIdentifiedQueueSessionValidator
    : AbstractValidator<SubmitIdentifiedQueueSessionCommand>
{
    public SubmitIdentifiedQueueSessionValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.SessionId).NotEmpty();
        RuleFor(command => command.Request.TimeTakenMs)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Request.TimeTakenMs.HasValue);
    }
}

public sealed class SubmitIdentifiedQueueSessionHandler(IResponseCaptureStore store)
    : IRequestHandler<SubmitIdentifiedQueueSessionCommand, Result<SubmitResponseSessionResponse>>
{
    public Task<Result<SubmitResponseSessionResponse>> Handle(
        SubmitIdentifiedQueueSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.SubmitIdentifiedQueueSessionAsync(
            command.Token,
            command.AssignmentId,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
