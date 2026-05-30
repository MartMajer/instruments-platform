using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record SuspendTenantMemberCommand(Guid UserId) : IRequest<Result<TenantMemberMutationResponse>>;

public sealed record ReactivateTenantMemberCommand(Guid UserId) : IRequest<Result<TenantMemberMutationResponse>>;

public sealed record RemoveTenantMemberCommand(Guid UserId) : IRequest<Result<TenantMemberRemovalResponse>>;

public sealed record TenantMemberRemovalResponse(Guid UserId, bool Removed);

public sealed class SuspendTenantMemberValidator : AbstractValidator<SuspendTenantMemberCommand>
{
    public SuspendTenantMemberValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}

public sealed class ReactivateTenantMemberValidator : AbstractValidator<ReactivateTenantMemberCommand>
{
    public ReactivateTenantMemberValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}

public sealed class RemoveTenantMemberValidator : AbstractValidator<RemoveTenantMemberCommand>
{
    public RemoveTenantMemberValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}

public sealed class SuspendTenantMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<SuspendTenantMemberCommand, Result<TenantMemberMutationResponse>>
{
    public Task<Result<TenantMemberMutationResponse>> Handle(
        SuspendTenantMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantMemberMutationResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.SuspendTenantMemberAsync(
            currentTenant.TenantId,
            command.UserId,
            actor.UserId.Value,
            cancellationToken);
    }
}

public sealed class ReactivateTenantMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ReactivateTenantMemberCommand, Result<TenantMemberMutationResponse>>
{
    public Task<Result<TenantMemberMutationResponse>> Handle(
        ReactivateTenantMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantMemberMutationResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ReactivateTenantMemberAsync(
            currentTenant.TenantId,
            command.UserId,
            actor.UserId.Value,
            cancellationToken);
    }
}

public sealed class RemoveTenantMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<RemoveTenantMemberCommand, Result<TenantMemberRemovalResponse>>
{
    public Task<Result<TenantMemberRemovalResponse>> Handle(
        RemoveTenantMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantMemberRemovalResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.RemoveTenantMemberAsync(
            currentTenant.TenantId,
            command.UserId,
            actor.UserId.Value,
            cancellationToken);
    }
}
