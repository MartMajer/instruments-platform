using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record CompleteMicrosoftGraphConsentCallbackCommand(CompleteMicrosoftGraphConsentCallbackRequest Request)
    : IRequest<Result<MicrosoftGraphConsentCallbackResponse>>;

public sealed class CompleteMicrosoftGraphConsentCallbackValidator
    : AbstractValidator<CompleteMicrosoftGraphConsentCallbackCommand>
{
    public CompleteMicrosoftGraphConsentCallbackValidator()
    {
        RuleFor(command => command.Request.State)
            .NotEmpty()
            .MaximumLength(512);
        RuleFor(command => command.Request.Nonce)
            .MaximumLength(512);
        RuleFor(command => command.Request.MicrosoftTenantId)
            .MaximumLength(128);
        RuleFor(command => command.Request.DisplayName)
            .MaximumLength(256);
        RuleFor(command => command.Request.PrimaryDomain)
            .MaximumLength(256);
        RuleFor(command => command.Request.Error)
            .MaximumLength(128);
        RuleFor(command => command.Request.ErrorDescription)
            .MaximumLength(512);
    }
}

public sealed class CompleteMicrosoftGraphConsentCallbackHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<CompleteMicrosoftGraphConsentCallbackCommand, Result<MicrosoftGraphConsentCallbackResponse>>
{
    public Task<Result<MicrosoftGraphConsentCallbackResponse>> Handle(
        CompleteMicrosoftGraphConsentCallbackCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CompleteMicrosoftGraphConsentCallbackAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
