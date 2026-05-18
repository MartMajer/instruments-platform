using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record CreateSubjectGroupCommand(CreateSubjectGroupRequest Request)
    : IRequest<Result<SubjectGroupResponse>>;

public sealed class CreateSubjectGroupValidator : AbstractValidator<CreateSubjectGroupCommand>
{
    public CreateSubjectGroupValidator()
    {
        RuleFor(command => command.Request.Type)
            .NotEmpty()
            .MaximumLength(128);
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(command => command.Request.Attributes)
            .NotEmpty();
    }
}

public sealed class CreateSubjectGroupHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<CreateSubjectGroupCommand, Result<SubjectGroupResponse>>
{
    public Task<Result<SubjectGroupResponse>> Handle(
        CreateSubjectGroupCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectGroupResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CreateSubjectGroupAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
