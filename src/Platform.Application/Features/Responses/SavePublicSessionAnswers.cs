using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SavePublicSessionAnswersCommand(
    string Handle,
    SaveAnswersRequest Request)
    : IRequest<Result<SaveAnswersResponse>>;

public sealed class SavePublicSessionAnswersValidator : AbstractValidator<SavePublicSessionAnswersCommand>
{
    public SavePublicSessionAnswersValidator()
    {
        RuleFor(command => command.Handle).NotEmpty();
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

public sealed class SavePublicSessionAnswersHandler(IResponseCaptureStore store)
    : IRequestHandler<SavePublicSessionAnswersCommand, Result<SaveAnswersResponse>>
{
    public Task<Result<SaveAnswersResponse>> Handle(
        SavePublicSessionAnswersCommand command,
        CancellationToken cancellationToken)
    {
        return store.SavePublicSessionAnswersAsync(
            command.Handle,
            command.Request,
            cancellationToken);
    }
}
