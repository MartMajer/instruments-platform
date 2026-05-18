using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateTemplateVersionCommand(
    CreateTemplateVersionRequest Request) : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class CreateTemplateVersionValidator
    : AbstractValidator<CreateTemplateVersionCommand>
{
    public CreateTemplateVersionValidator()
    {
        RuleFor(command => command.Request.TemplateName).NotEmpty();
        RuleFor(command => command.Request.Semver).NotEmpty();
        RuleFor(command => command.Request.DefaultLocale).NotEmpty();
        RuleFor(command => command.Request.Sections).NotEmpty();
        RuleFor(command => command.Request.Questions).NotEmpty();
        RuleForEach(command => command.Request.Sections).ChildRules(section =>
        {
            section.RuleFor(value => value.Ordinal).GreaterThan(0);
            section.RuleFor(value => value.Code).NotEmpty();
            section.RuleFor(value => value.TitleDefault).NotEmpty();
        });
        RuleForEach(command => command.Request.Scales).ChildRules(scale =>
        {
            scale.RuleFor(value => value.Code).NotEmpty();
            scale.RuleFor(value => value.Type).NotEmpty();
            scale.RuleFor(value => value.MaxValue).GreaterThan(value => value.MinValue);
            scale.RuleFor(value => value.Step).GreaterThan(0);
            scale.RuleFor(value => value.Anchors).NotEmpty();
        });
        RuleForEach(command => command.Request.Questions).ChildRules(question =>
        {
            question.RuleFor(value => value.Ordinal).GreaterThan(0);
            question.RuleFor(value => value.Code).NotEmpty();
            question.RuleFor(value => value.Type).NotEmpty();
            question.RuleFor(value => value.TextDefault).NotEmpty();
            question.RuleFor(value => value.Payload).NotEmpty();
            question.RuleFor(value => value.MissingCodes).NotEmpty();
        });
    }
}

public sealed class CreateTemplateVersionHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateTemplateVersionCommand, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        CreateTemplateVersionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateTemplateVersionAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.Request,
            cancellationToken);
    }
}
