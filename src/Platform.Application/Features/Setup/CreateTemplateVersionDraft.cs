using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateTemplateVersionDraftCommand(
    Guid SourceTemplateVersionId,
    CreateTemplateVersionDraftRequest Request) : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class CreateTemplateVersionDraftValidator
    : AbstractValidator<CreateTemplateVersionDraftCommand>
{
    public CreateTemplateVersionDraftValidator()
    {
        RuleFor(command => command.SourceTemplateVersionId).NotEmpty();
        RuleFor(command => command.Request.Semver).NotEmpty();
    }
}

public sealed class CreateTemplateVersionDraftHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateTemplateVersionDraftCommand, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        CreateTemplateVersionDraftCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateTemplateVersionDraftAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.SourceTemplateVersionId,
            command.Request,
            cancellationToken);
    }
}
