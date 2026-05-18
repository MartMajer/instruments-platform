using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record CreateResponseSessionCommand(CreateResponseSessionRequest Request)
    : IRequest<Result<ResponseSessionResponse>>;

public sealed class CreateResponseSessionValidator : AbstractValidator<CreateResponseSessionCommand>
{
    public CreateResponseSessionValidator()
    {
        RuleFor(command => command.Request.AssignmentId).NotEmpty();
        RuleFor(command => command.Request.Locale).NotEmpty();
    }
}

public sealed class CreateResponseSessionHandler(
    ICurrentTenant currentTenant,
    IResponseCaptureStore store)
    : IRequestHandler<CreateResponseSessionCommand, Result<ResponseSessionResponse>>
{
    public Task<Result<ResponseSessionResponse>> Handle(
        CreateResponseSessionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateSessionAsync(currentTenant.TenantId, command.Request, cancellationToken);
    }
}
