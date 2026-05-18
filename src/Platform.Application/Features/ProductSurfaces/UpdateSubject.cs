using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record UpdateSubjectCommand(Guid SubjectId, UpdateSubjectRequest Request)
    : IRequest<Result<SubjectDirectoryItemResponse>>;

public sealed class UpdateSubjectValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectValidator()
    {
        RuleFor(command => command.SubjectId)
            .NotEmpty();
        RuleFor(command => command.Request.Locale)
            .NotEmpty()
            .MaximumLength(16);
        RuleFor(command => command.Request.Attributes)
            .NotEmpty();
        RuleFor(command => command.Request.Email)
            .MaximumLength(320);
        RuleFor(command => command.Request.ExternalId)
            .MaximumLength(256);
        RuleFor(command => command.Request.DisplayName)
            .MaximumLength(256);
    }
}

public sealed class UpdateSubjectHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<UpdateSubjectCommand, Result<SubjectDirectoryItemResponse>>
{
    public Task<Result<SubjectDirectoryItemResponse>> Handle(
        UpdateSubjectCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectDirectoryItemResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.UpdateSubjectAsync(
            currentTenant.TenantId,
            command.SubjectId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
