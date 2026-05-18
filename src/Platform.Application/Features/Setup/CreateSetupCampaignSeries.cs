using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
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
