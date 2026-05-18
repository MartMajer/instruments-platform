using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record AddSubjectGroupMemberCommand(Guid GroupId, AddSubjectGroupMemberRequest Request)
    : IRequest<Result<SubjectGroupMembershipResponse>>;

public sealed class AddSubjectGroupMemberValidator : AbstractValidator<AddSubjectGroupMemberCommand>
{
    public AddSubjectGroupMemberValidator()
    {
        RuleFor(command => command.GroupId)
            .NotEmpty();
        RuleFor(command => command.Request.SubjectId)
            .NotEmpty();
        RuleFor(command => command.Request.RoleInGroup)
            .MaximumLength(128);
    }
}

public sealed class AddSubjectGroupMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<AddSubjectGroupMemberCommand, Result<SubjectGroupMembershipResponse>>
{
    public Task<Result<SubjectGroupMembershipResponse>> Handle(
        AddSubjectGroupMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectGroupMembershipResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.AddSubjectGroupMemberAsync(
            currentTenant.TenantId,
            command.GroupId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
