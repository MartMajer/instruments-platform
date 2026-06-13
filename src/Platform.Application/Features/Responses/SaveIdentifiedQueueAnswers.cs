using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SaveIdentifiedQueueAnswersCommand(
    string Token,
    Guid AssignmentId,
    Guid SessionId,
    SaveAnswersRequest Request)
    : IRequest<Result<SaveAnswersResponse>>;

public sealed class SaveIdentifiedQueueAnswersValidator : AbstractValidator<SaveIdentifiedQueueAnswersCommand>
{
    public SaveIdentifiedQueueAnswersValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.SessionId).NotEmpty();
        RuleFor(command => command.Request.Answers).NotEmpty();
        RuleForEach(command => command.Request.Answers).ChildRules(answer =>
        {
            answer.RuleFor(item => item.QuestionId).NotEmpty();
            answer.RuleFor(item => item)
                .Must(item => !(item.IsSkipped && item.IsNa))
                .WithMessage("Answer cannot be both skipped and not applicable.");
        });
    }
}

public sealed class SaveIdentifiedQueueAnswersHandler(IResponseCaptureStore store)
    : IRequestHandler<SaveIdentifiedQueueAnswersCommand, Result<SaveAnswersResponse>>
{
    public Task<Result<SaveAnswersResponse>> Handle(
        SaveIdentifiedQueueAnswersCommand command,
        CancellationToken cancellationToken)
    {
        return store.SaveIdentifiedQueueAnswersAsync(
            command.Token,
            command.AssignmentId,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
