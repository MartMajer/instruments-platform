using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record PublishTemplateVersionCommand(Guid TemplateVersionId)
    : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class PublishTemplateVersionValidator : AbstractValidator<PublishTemplateVersionCommand>
{
    public PublishTemplateVersionValidator()
    {
        RuleFor(command => command.TemplateVersionId).NotEmpty();
    }
}

public sealed class PublishTemplateVersionHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<PublishTemplateVersionCommand, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        PublishTemplateVersionCommand command,
        CancellationToken cancellationToken)
    {
        return store.PublishTemplateVersionAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.TemplateVersionId,
            cancellationToken);
    }
}
