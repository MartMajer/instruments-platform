using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record CreateMicrosoftGraphConsentRequestCommand(CreateMicrosoftGraphConsentRequest Request)
    : IRequest<Result<MicrosoftGraphConsentRequestResponse>>;

public sealed class CreateMicrosoftGraphConsentRequestValidator
    : AbstractValidator<CreateMicrosoftGraphConsentRequestCommand>
{
    public CreateMicrosoftGraphConsentRequestValidator()
    {
        RuleForEach(command => command.Request.RequestedScopes)
            .NotEmpty()
            .MaximumLength(128)
            .Must(MicrosoftGraphDirectoryConsentScopes.IsKnown)
            .WithMessage("Unsupported Microsoft Graph permission scope.");
    }
}

public sealed class CreateMicrosoftGraphConsentRequestHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store,
    IMicrosoftGraphAdminConsentUrlBuilder adminConsentUrlBuilder)
    : IRequestHandler<CreateMicrosoftGraphConsentRequestCommand, Result<MicrosoftGraphConsentRequestResponse>>
{
    public async Task<Result<MicrosoftGraphConsentRequestResponse>> Handle(
        CreateMicrosoftGraphConsentRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<MicrosoftGraphConsentRequestResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var result = await store.CreateMicrosoftGraphConsentRequestAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        return Result.Success(result.Value with
        {
            AdminConsentUrl = adminConsentUrlBuilder.Build(result.Value)
        });
    }
}
