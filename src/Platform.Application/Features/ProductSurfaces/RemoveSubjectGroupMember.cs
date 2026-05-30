using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record RemoveSubjectGroupMemberCommand(Guid GroupId, Guid SubjectId)
    : IRequest<Result<SubjectGroupMembershipRemovalResponse>>;

public sealed class RemoveSubjectGroupMemberValidator : AbstractValidator<RemoveSubjectGroupMemberCommand>
{
    public RemoveSubjectGroupMemberValidator()
    {
        RuleFor(command => command.GroupId)
            .NotEmpty();
        RuleFor(command => command.SubjectId)
            .NotEmpty();
    }
}

public sealed class RemoveSubjectGroupMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<RemoveSubjectGroupMemberCommand, Result<SubjectGroupMembershipRemovalResponse>>
{
    public Task<Result<SubjectGroupMembershipRemovalResponse>> Handle(
        RemoveSubjectGroupMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectGroupMembershipRemovalResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.RemoveSubjectGroupMemberAsync(
            currentTenant.TenantId,
            command.GroupId,
            command.SubjectId,
            actor.UserId.Value,
            cancellationToken);
    }
}
