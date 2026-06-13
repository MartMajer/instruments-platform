using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record UpdateTenantReportBrandingCommand(UpdateTenantReportBrandingRequest Request)
    : IRequest<Result<TenantSettingsReportBrandingResponse>>;

public sealed class UpdateTenantReportBrandingValidator : AbstractValidator<UpdateTenantReportBrandingCommand>
{
    public UpdateTenantReportBrandingValidator()
    {
        RuleFor(command => command.Request.OrganizationLabel)
            .NotEmpty()
            .MaximumLength(Tenant.ReportBrandingOrganizationLabelMaxLength);
        RuleFor(command => command.Request.ReportTitle)
            .NotEmpty()
            .MaximumLength(Tenant.ReportBrandingReportTitleMaxLength);
        RuleFor(command => command.Request.AccentColorHex)
            .NotEmpty()
            .Must(Tenant.IsReportBrandingAccentColorHex)
            .WithMessage("Accent color must be a hex color token.");
        RuleFor(command => command.Request.LayoutVariant)
            .NotEmpty()
            .MaximumLength(Tenant.ReportBrandingLayoutVariantMaxLength)
            .Must(Tenant.IsReportBrandingLayoutVariantKnown)
            .WithMessage("Layout variant is not supported.");
    }
}

public sealed class UpdateTenantReportBrandingHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<UpdateTenantReportBrandingCommand, Result<TenantSettingsReportBrandingResponse>>
{
    public Task<Result<TenantSettingsReportBrandingResponse>> Handle(
        UpdateTenantReportBrandingCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantSettingsReportBrandingResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.UpdateTenantReportBrandingAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
