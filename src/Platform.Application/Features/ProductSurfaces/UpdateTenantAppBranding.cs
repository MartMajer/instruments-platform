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

        RuleFor(command => command.Request.TopbarColorHex)
            .Must(Tenant.IsAppBrandingAccentColorHex)
            .When(command => !string.IsNullOrWhiteSpace(command.Request.TopbarColorHex))
            .WithMessage("Topbar color must be a hex color token.");
        RuleFor(command => command.Request.BackgroundColorHex)
            .Must(Tenant.IsAppBrandingAccentColorHex)
            .When(command => !string.IsNullOrWhiteSpace(command.Request.BackgroundColorHex))
            .WithMessage("Background color must be a hex color token.");
        RuleFor(command => command.Request.SurfaceColorHex)
            .Must(Tenant.IsAppBrandingAccentColorHex)
            .When(command => !string.IsNullOrWhiteSpace(command.Request.SurfaceColorHex))
            .WithMessage("Surface color must be a hex color token.");
        RuleFor(command => command.Request.InkColorHex)
            .Must(Tenant.IsAppBrandingAccentColorHex)
            .When(command => !string.IsNullOrWhiteSpace(command.Request.InkColorHex))
            .WithMessage("Text color must be a hex color token.");
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
