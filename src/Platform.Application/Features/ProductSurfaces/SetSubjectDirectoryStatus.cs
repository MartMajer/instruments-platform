using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record SetSubjectDirectoryStatusCommand(Guid SubjectId, SetSubjectDirectoryStatusRequest Request)
    : IRequest<Result<SubjectDirectoryItemResponse>>;

public sealed class SetSubjectDirectoryStatusValidator : AbstractValidator<SetSubjectDirectoryStatusCommand>
{
    public SetSubjectDirectoryStatusValidator()
    {
        RuleFor(command => command.SubjectId).NotEmpty();
        RuleFor(command => command.Request.Status)
            .NotEmpty()
            .Must(value =>
                !string.IsNullOrWhiteSpace(value) &&
                SubjectDirectoryStatuses.IsMutable(value.Trim().ToLowerInvariant()))
            .WithMessage("Directory status is not supported.");
        RuleFor(command => command.Request.Reason).MaximumLength(256);
    }
}

public sealed class SetSubjectDirectoryStatusHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<SetSubjectDirectoryStatusCommand, Result<SubjectDirectoryItemResponse>>
{
    public Task<Result<SubjectDirectoryItemResponse>> Handle(
        SetSubjectDirectoryStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectDirectoryItemResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.SetSubjectDirectoryStatusAsync(
            currentTenant.TenantId,
            command.SubjectId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
