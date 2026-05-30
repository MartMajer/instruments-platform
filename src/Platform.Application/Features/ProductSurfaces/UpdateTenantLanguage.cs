using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record UpdateTenantLanguageCommand(UpdateTenantLanguageRequest Request)
    : IRequest<Result<TenantLanguageResponse>>;

public sealed class UpdateTenantLanguageValidator : AbstractValidator<UpdateTenantLanguageCommand>
{
    public UpdateTenantLanguageValidator()
    {
        RuleFor(command => command.Request.DefaultLocale)
            .NotEmpty()
            .MaximumLength(16)
            .Must(EmailTemplateLocales.IsSupported)
            .WithMessage("Workspace language must be en or hr-HR.");
    }
}

public sealed class UpdateTenantLanguageHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<UpdateTenantLanguageCommand, Result<TenantLanguageResponse>>
{
    public Task<Result<TenantLanguageResponse>> Handle(
        UpdateTenantLanguageCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<TenantLanguageResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.UpdateTenantLanguageAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
