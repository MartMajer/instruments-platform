using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record SubmitResponseSessionCommand(Guid SessionId, SubmitResponseSessionRequest Request)
    : IRequest<Result<SubmitResponseSessionResponse>>;

public sealed class SubmitResponseSessionValidator : AbstractValidator<SubmitResponseSessionCommand>
{
    public SubmitResponseSessionValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty();
        RuleFor(command => command.Request.TimeTakenMs)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Request.TimeTakenMs.HasValue);
    }
}

public sealed class SubmitResponseSessionHandler(
    ICurrentTenant currentTenant,
    IResponseCaptureStore store)
    : IRequestHandler<SubmitResponseSessionCommand, Result<SubmitResponseSessionResponse>>
{
    public Task<Result<SubmitResponseSessionResponse>> Handle(
        SubmitResponseSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.SubmitSessionAsync(
            currentTenant.TenantId,
            command.SessionId,
            command.Request,
            cancellationToken);
    }
}
