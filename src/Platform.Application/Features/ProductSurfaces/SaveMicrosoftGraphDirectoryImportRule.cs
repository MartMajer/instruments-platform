using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record SaveMicrosoftGraphDirectoryImportRuleCommand(SaveMicrosoftGraphImportRuleRequest Request)
    : IRequest<Result<DirectoryImportRuleResponse>>;

public sealed class SaveMicrosoftGraphDirectoryImportRuleValidator
    : AbstractValidator<SaveMicrosoftGraphDirectoryImportRuleCommand>
{
    public SaveMicrosoftGraphDirectoryImportRuleValidator()
    {
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(256);
        RuleForEach(command => command.Request.RetainedFields)
            .MaximumLength(128);
    }
}

public sealed class SaveMicrosoftGraphDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<SaveMicrosoftGraphDirectoryImportRuleCommand, Result<DirectoryImportRuleResponse>>
{
    public Task<Result<DirectoryImportRuleResponse>> Handle(
        SaveMicrosoftGraphDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<DirectoryImportRuleResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.SaveMicrosoftGraphDirectoryImportRuleAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
