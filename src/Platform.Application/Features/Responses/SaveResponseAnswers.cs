using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SaveResponseAnswersCommand(Guid SessionId, SaveAnswersRequest Request)
    : IRequest<Result<SaveAnswersResponse>>;

public sealed class SaveResponseAnswersValidator : AbstractValidator<SaveResponseAnswersCommand>
{
    public SaveResponseAnswersValidator()
    {
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

public sealed class SaveResponseAnswersHandler(
    ICurrentTenant currentTenant,
    IResponseCaptureStore store)
    : IRequestHandler<SaveResponseAnswersCommand, Result<SaveAnswersResponse>>
{
    public Task<Result<SaveAnswersResponse>> Handle(
        SaveResponseAnswersCommand command,
        CancellationToken cancellationToken)
    {
        return store.SaveAnswersAsync(
            currentTenant.TenantId,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
