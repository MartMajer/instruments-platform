using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ResetTenantEmailTemplateCommand(
    string TemplateCode,
    string Locale)
    : IRequest<Result<ResetEmailTemplateResponse>>;

public sealed class ResetTenantEmailTemplateValidator : AbstractValidator<ResetTenantEmailTemplateCommand>
{
    public ResetTenantEmailTemplateValidator()
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
    }
}

public sealed class ResetTenantEmailTemplateHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ResetTenantEmailTemplateCommand, Result<ResetEmailTemplateResponse>>
{
    public Task<Result<ResetEmailTemplateResponse>> Handle(
        ResetTenantEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<ResetEmailTemplateResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ResetTenantEmailTemplateAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.TemplateCode,
            command.Locale,
            cancellationToken);
    }
}
