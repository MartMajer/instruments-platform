using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ChangeTenantMemberRoleRequest(string RoleCode);

public sealed record ChangeTenantMemberRoleCommand(
    Guid UserId,
    ChangeTenantMemberRoleRequest Request)
    : IRequest<Result<TenantMemberMutationResponse>>;

public sealed class ChangeTenantMemberRoleValidator : AbstractValidator<ChangeTenantMemberRoleCommand>
{
    public ChangeTenantMemberRoleValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Request.RoleCode)
            .NotEmpty()
            .MaximumLength(128);
    }
}

public sealed class ChangeTenantMemberRoleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ChangeTenantMemberRoleCommand, Result<TenantMemberMutationResponse>>
{
    public Task<Result<TenantMemberMutationResponse>> Handle(
        ChangeTenantMemberRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantMemberMutationResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ChangeTenantMemberRoleAsync(
            currentTenant.TenantId,
            command.UserId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}

public sealed record TenantMemberMutationResponse(TenantMemberResponse Member);
