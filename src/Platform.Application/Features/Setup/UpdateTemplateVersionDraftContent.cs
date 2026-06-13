using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record UpdateTemplateVersionDraftContentCommand(
    Guid TemplateVersionId,
    UpdateTemplateVersionDraftContentRequest Request) : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class UpdateTemplateVersionDraftContentValidator
    : AbstractValidator<UpdateTemplateVersionDraftContentCommand>
{
    public UpdateTemplateVersionDraftContentValidator()
    {
        RuleFor(command => command.TemplateVersionId).NotEmpty();
        RuleFor(command => command.Request).Custom(ValidateDraftContent);
    }

    private static void ValidateDraftContent(
        UpdateTemplateVersionDraftContentRequest request,
        ValidationContext<UpdateTemplateVersionDraftContentCommand> context)
    {
        var createRequest = new CreateTemplateVersionRequest(
            "draft-content-validation",
            "0.0.0",
            "en",
            InstrumentId: null,
            request.Sections,
            request.Scales,
            request.Questions);
        var validation = new CreateTemplateVersionValidator()
            .Validate(new CreateTemplateVersionCommand(createRequest));

        foreach (var failure in validation.Errors)
        {
            context.AddFailure(failure.PropertyName, failure.ErrorMessage);
        }
    }
}

public sealed class UpdateTemplateVersionDraftContentHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<UpdateTemplateVersionDraftContentCommand, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        UpdateTemplateVersionDraftContentCommand command,
        CancellationToken cancellationToken)
    {
        return store.UpdateTemplateVersionDraftContentAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.TemplateVersionId,
            command.Request,
            cancellationToken);
    }
}
