using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record DeactivateSubjectCommand(Guid SubjectId, DeactivateSubjectRequest Request)
    : IRequest<Result<SubjectDirectoryItemResponse>>;

public sealed class DeactivateSubjectValidator : AbstractValidator<DeactivateSubjectCommand>
{
    public DeactivateSubjectValidator()
    {
        RuleFor(command => command.SubjectId).NotEmpty();
        RuleFor(command => command.Request.Reason).MaximumLength(256);
    }
}

public sealed class DeactivateSubjectHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<DeactivateSubjectCommand, Result<SubjectDirectoryItemResponse>>
{
    public Task<Result<SubjectDirectoryItemResponse>> Handle(
        DeactivateSubjectCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectDirectoryItemResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.DeactivateSubjectAsync(
            currentTenant.TenantId,
            command.SubjectId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
