using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record UpdateTenantEmailTemplateCommand(
    string TemplateCode,
    string Locale,
    UpdateEmailTemplateRequest Request)
    : IRequest<Result<TenantEmailTemplateSettingsResponse>>;

public sealed class UpdateTenantEmailTemplateValidator : AbstractValidator<UpdateTenantEmailTemplateCommand>
{
    public UpdateTenantEmailTemplateValidator()
    {
        RuleFor(command => command.TemplateCode)
            .NotEmpty()
            .Must(EmailTemplateCodes.IsKnown)
            .WithMessage("Email template code must be invitation or reminder.");
        RuleFor(command => command.Locale)
            .NotEmpty()
            .MaximumLength(16)
            .Must(EmailTemplateLocales.IsSupported)
            .WithMessage("Email template locale must be en or hr-HR.");
        RuleFor(command => command.Request.Subject)
            .NotEmpty()
            .MaximumLength(EmailTemplate.MaxSubjectLength);
        RuleFor(command => command.Request.BodyText)
            .NotEmpty()
            .MinimumLength(EmailTemplate.MinBodyTextLength)
            .MaximumLength(EmailTemplate.MaxBodyTextLength);
    }
}

public sealed class UpdateTenantEmailTemplateHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<UpdateTenantEmailTemplateCommand, Result<TenantEmailTemplateSettingsResponse>>
{
    public Task<Result<TenantEmailTemplateSettingsResponse>> Handle(
        UpdateTenantEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantEmailTemplateSettingsResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.UpdateTenantEmailTemplateAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.TemplateCode,
            command.Locale,
            command.Request,
            cancellationToken);
    }
}
