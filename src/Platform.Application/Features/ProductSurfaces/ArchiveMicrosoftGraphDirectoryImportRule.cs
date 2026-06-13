using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ArchiveMicrosoftGraphDirectoryImportRuleCommand(Guid RuleId)
    : IRequest<Result<DirectoryImportRuleResponse>>;

public sealed class ArchiveMicrosoftGraphDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ArchiveMicrosoftGraphDirectoryImportRuleCommand, Result<DirectoryImportRuleResponse>>
{
    public Task<Result<DirectoryImportRuleResponse>> Handle(
        ArchiveMicrosoftGraphDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<DirectoryImportRuleResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ArchiveMicrosoftGraphDirectoryImportRuleAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.RuleId,
            cancellationToken);
    }
}
