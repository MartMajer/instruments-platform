using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record RetireTemplateVersionDraftScoringCommand(Guid TemplateVersionId)
    : IRequest<Result<RetireTemplateVersionDraftScoringResponse>>;

public sealed class RetireTemplateVersionDraftScoringValidator
    : AbstractValidator<RetireTemplateVersionDraftScoringCommand>
{
    public RetireTemplateVersionDraftScoringValidator()
    {
        RuleFor(command => command.TemplateVersionId).NotEmpty();
    }
}

public sealed class RetireTemplateVersionDraftScoringHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<RetireTemplateVersionDraftScoringCommand, Result<RetireTemplateVersionDraftScoringResponse>>
{
    public Task<Result<RetireTemplateVersionDraftScoringResponse>> Handle(
        RetireTemplateVersionDraftScoringCommand command,
        CancellationToken cancellationToken)
    {
        return store.RetireTemplateVersionDraftScoringAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.TemplateVersionId,
            cancellationToken);
    }
}
