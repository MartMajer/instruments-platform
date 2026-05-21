using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateCampaignInvitationBatchCommand(
    Guid CampaignId,
    CreateCampaignInvitationBatchRequest Request)
    : IRequest<Result<CampaignInvitationBatchResponse>>;

public sealed class CreateCampaignInvitationBatchValidator
    : AbstractValidator<CreateCampaignInvitationBatchCommand>
{
    public const int MaxRecipientCount = 500;

    public CreateCampaignInvitationBatchValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.Recipients)
            .NotEmpty()
            .Must(recipients => recipients.Count <= MaxRecipientCount)
            .WithMessage($"Invitation batches support at most {MaxRecipientCount} recipients per request.");
        RuleForEach(command => command.Request.Recipients).ChildRules(recipient =>
        {
            recipient.RuleFor(item => item.Email).NotEmpty().EmailAddress();
        });
    }
}

public sealed class CreateCampaignInvitationBatchHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateCampaignInvitationBatchCommand, Result<CampaignInvitationBatchResponse>>
{
    public Task<Result<CampaignInvitationBatchResponse>> Handle(
        CreateCampaignInvitationBatchCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignInvitationBatchAsync(
            currentTenant.TenantId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
