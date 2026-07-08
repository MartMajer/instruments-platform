using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record UpdateTenantAppBrandingCommand(UpdateTenantAppBrandingRequest Request)
    : IRequest<Result<TenantSettingsAppBrandingResponse>>;

public sealed class UpdateTenantAppBrandingValidator : AbstractValidator<UpdateTenantAppBrandingCommand>
{
    public UpdateTenantAppBrandingValidator()
    {
        RuleFor(command => command.Request.AccentColorHex)
            .NotEmpty()
            .Must(Tenant.IsAppBrandingAccentColorHex)
            .WithMessage("Accent color must be a hex color token.");
        RuleFor(command => command.Request.LogoObjectKey)
            .MaximumLength(Tenant.AppBrandingLogoObjectKeyMaxLength);
        RuleFor(command => command.Request.LogoContentType)
            .Must(Tenant.IsAppBrandingLogoContentType)
            .When(command => !string.IsNullOrWhiteSpace(command.Request.LogoObjectKey))
            .WithMessage("Logo content type is not supported.");
    }
}

public sealed class UpdateTenantAppBrandingHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<UpdateTenantAppBrandingCommand, Result<TenantSettingsAppBrandingResponse>>
{
    public Task<Result<TenantSettingsAppBrandingResponse>> Handle(
        UpdateTenantAppBrandingCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantSettingsAppBrandingResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.UpdateTenantAppBrandingAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
