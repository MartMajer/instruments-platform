using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateSetupCampaignSeriesCommand(
    CreateCampaignSeriesRequest Request) : IRequest<Result<SetupIdResponse>>;

public sealed class CreateSetupCampaignSeriesValidator
    : AbstractValidator<CreateSetupCampaignSeriesCommand>
{
    public CreateSetupCampaignSeriesValidator()
    {
        RuleFor(command => command.Request.Name).NotEmpty();
        When(command => command.Request.StudyBrief is not null, () =>
        {
            RuleFor(command => command.Request.StudyBrief!.Purpose)
                .MaximumLength(CampaignSeries.StudyPurposeMaxLength);
            RuleFor(command => command.Request.StudyBrief!.Audience)
                .MaximumLength(CampaignSeries.StudyAudienceMaxLength);
            RuleFor(command => command.Request.StudyBrief!.DesignType)
                .MaximumLength(CampaignSeries.StudyDesignTypeMaxLength)
                .Must(value => string.IsNullOrWhiteSpace(value) || CampaignSeriesStudyDesignTypes.IsKnown(value.Trim()))
                .WithMessage("Study design type is not supported.");
            RuleFor(command => command.Request.StudyBrief!.IntendedUse)
                .MaximumLength(CampaignSeries.StudyIntendedUseMaxLength)
                .Must(value => string.IsNullOrWhiteSpace(value) || CampaignSeriesStudyIntendedUseTypes.IsKnown(value.Trim()))
                .WithMessage("Study intended use is not supported.");
            RuleFor(command => command.Request.StudyBrief!.InterpretationBoundary)
                .MaximumLength(CampaignSeries.StudyInterpretationBoundaryMaxLength);
            RuleFor(command => command.Request.StudyBrief!.OwnerNotes)
                .MaximumLength(CampaignSeries.StudyOwnerNotesMaxLength);
        });
    }
}

public sealed class CreateSetupCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateSetupCampaignSeriesCommand, Result<SetupIdResponse>>
{
    public Task<Result<SetupIdResponse>> Handle(
        CreateSetupCampaignSeriesCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignSeriesAsync(
            currentTenant.TenantId,
            command.Request,
            cancellationToken);
    }
}
