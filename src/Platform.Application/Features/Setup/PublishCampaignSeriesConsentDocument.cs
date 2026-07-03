using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record PublishCampaignSeriesConsentDocumentCommand(
    Guid CampaignSeriesId,
    PublishConsentDocumentRequest Request) : IRequest<Result<ConsentDocumentSummaryResponse>>;

public sealed class PublishCampaignSeriesConsentDocumentValidator
    : AbstractValidator<PublishCampaignSeriesConsentDocumentCommand>
{
    public PublishCampaignSeriesConsentDocumentValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request.Locale).NotEmpty().MaximumLength(16);
        RuleFor(command => command.Request.Version).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Request.Title).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Request.BodyMarkdown).NotEmpty().MaximumLength(20000);
    }
}

public sealed class PublishCampaignSeriesConsentDocumentHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<PublishCampaignSeriesConsentDocumentCommand, Result<ConsentDocumentSummaryResponse>>
{
    public Task<Result<ConsentDocumentSummaryResponse>> Handle(
        PublishCampaignSeriesConsentDocumentCommand command,
        CancellationToken cancellationToken)
    {
        return store.PublishCampaignSeriesConsentDocumentAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            command.Request,
            cancellationToken);
    }
}
