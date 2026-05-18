using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateCampaignIdentifiedEntryCommand(Guid CampaignId)
    : IRequest<Result<CampaignIdentifiedEntryResponse>>;

public sealed class CreateCampaignIdentifiedEntryValidator
    : AbstractValidator<CreateCampaignIdentifiedEntryCommand>
{
    public CreateCampaignIdentifiedEntryValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateCampaignIdentifiedEntryHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateCampaignIdentifiedEntryCommand, Result<CampaignIdentifiedEntryResponse>>
{
    public Task<Result<CampaignIdentifiedEntryResponse>> Handle(
        CreateCampaignIdentifiedEntryCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignIdentifiedEntryAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}
