using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record SetSubjectManagerCommand(Guid SubjectId, SetSubjectManagerRequest Request)
    : IRequest<Result<SubjectDirectoryItemResponse>>;

public sealed class SetSubjectManagerValidator : AbstractValidator<SetSubjectManagerCommand>
{
    public SetSubjectManagerValidator()
    {
        RuleFor(command => command.SubjectId)
            .NotEmpty();
    }
}

public sealed class SetSubjectManagerHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<SetSubjectManagerCommand, Result<SubjectDirectoryItemResponse>>
{
    public Task<Result<SubjectDirectoryItemResponse>> Handle(
        SetSubjectManagerCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectDirectoryItemResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.SetSubjectManagerAsync(
            currentTenant.TenantId,
            command.SubjectId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
