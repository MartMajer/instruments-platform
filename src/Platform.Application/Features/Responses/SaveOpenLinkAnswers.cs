using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SaveOpenLinkAnswersCommand(
    string Token,
    Guid SessionId,
    SaveAnswersRequest Request)
    : IRequest<Result<SaveAnswersResponse>>;

public sealed class SaveOpenLinkAnswersValidator : AbstractValidator<SaveOpenLinkAnswersCommand>
{
    public SaveOpenLinkAnswersValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
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

public sealed class SaveOpenLinkAnswersHandler(IResponseCaptureStore store)
    : IRequestHandler<SaveOpenLinkAnswersCommand, Result<SaveAnswersResponse>>
{
    public Task<Result<SaveAnswersResponse>> Handle(
        SaveOpenLinkAnswersCommand command,
        CancellationToken cancellationToken)
    {
        return store.SaveOpenLinkAnswersAsync(
            command.Token,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
