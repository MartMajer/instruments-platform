using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record CreateTenantMemberRequest(
    string Email,
    string RoleCode,
    string Locale = "en");

public sealed record CreateTenantMemberCommand(CreateTenantMemberRequest Request)
    : IRequest<Result<TenantMemberMutationResponse>>;

public sealed class CreateTenantMemberValidator : AbstractValidator<CreateTenantMemberCommand>
{
    public CreateTenantMemberValidator()
    {
        RuleFor(command => command.Request.Email)
            .NotEmpty()
            .MaximumLength(320);
        RuleFor(command => command.Request.RoleCode)
            .NotEmpty()
            .MaximumLength(128);
        RuleFor(command => command.Request.Locale)
            .NotEmpty()
            .MaximumLength(16);
    }
}

public sealed class CreateTenantMemberHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<CreateTenantMemberCommand, Result<TenantMemberMutationResponse>>
{
    public Task<Result<TenantMemberMutationResponse>> Handle(
        CreateTenantMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantMemberMutationResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CreateTenantMemberAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
