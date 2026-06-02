using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record CreateIdentifiedQueueAssignmentSessionCommand(
    string Token,
    Guid AssignmentId,
    CreateOpenLinkSessionRequest Request)
    : IRequest<Result<IdentifiedQueueSessionDraftResponse>>;

public sealed class CreateIdentifiedQueueAssignmentSessionValidator
    : AbstractValidator<CreateIdentifiedQueueAssignmentSessionCommand>
{
    public CreateIdentifiedQueueAssignmentSessionValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.Request.Locale).NotEmpty();
    }
}

public sealed class CreateIdentifiedQueueAssignmentSessionHandler(IResponseCaptureStore store)
    : IRequestHandler<CreateIdentifiedQueueAssignmentSessionCommand, Result<IdentifiedQueueSessionDraftResponse>>
{
    public Task<Result<IdentifiedQueueSessionDraftResponse>> Handle(
        CreateIdentifiedQueueAssignmentSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateIdentifiedQueueAssignmentSessionAsync(
            command.Token,
            command.AssignmentId,
            command.Request,
            cancellationToken);
    }
}
