using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.Domain.Scoring;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateSetupScoringRuleCommand(
    CreateScoringRuleRequest Request) : IRequest<Result<SetupIdResponse>>;

public sealed class CreateSetupScoringRuleValidator
    : AbstractValidator<CreateSetupScoringRuleCommand>
{
    public CreateSetupScoringRuleValidator()
    {
        RuleFor(command => command.Request.TemplateVersionId).NotEmpty();
        RuleFor(command => command.Request.RuleKey).NotEmpty();
        RuleFor(command => command.Request.RuleVersion).NotEmpty();
        RuleFor(command => command.Request.SchemaVersion).NotEmpty();
        RuleFor(command => command.Request.EngineMinVersion).NotEmpty();
        RuleFor(command => command.Request.Document).NotEmpty();
        RuleFor(command => command.Request.Produces).NotEmpty();
        RuleFor(command => command.Request.Compatibility).NotEmpty();
    }
}

public sealed class CreateSetupScoringRuleHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateSetupScoringRuleCommand, Result<SetupIdResponse>>
{
    public Task<Result<SetupIdResponse>> Handle(
        CreateSetupScoringRuleCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ScoringRuleValidator.Validate(new ScoringRuleValidationRequest(
            command.Request.RuleKey,
            command.Request.RuleVersion,
            command.Request.SchemaVersion,
            command.Request.EngineMinVersion,
            command.Request.Document,
            command.Request.Produces,
            command.Request.Compatibility));

        if (validation.IsFailure)
        {
            return Task.FromResult(Result.Failure<SetupIdResponse>(validation.Error));
        }

        return store.CreateScoringRuleAsync(
            currentTenant.TenantId,
            command.Request,
            cancellationToken);
    }
}
