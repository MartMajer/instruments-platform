using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.TestData;

public sealed record CreateCampaignTestResponsesCommand(
    Guid CampaignId,
    CreateCampaignTestResponsesRequest Request)
    : IRequest<Result<CreateCampaignTestResponsesResponse>>;

public sealed class CreateCampaignTestResponsesValidator
    : AbstractValidator<CreateCampaignTestResponsesCommand>
{
    public CreateCampaignTestResponsesValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.ResponseCount).InclusiveBetween(1, 1000);
        RuleFor(command => command.Request.TargetOutcome).InclusiveBetween(0m, 10m);
        RuleFor(command => command.Request.Variation)
            .Must(value => value is "tight" or "normal" or "noisy")
            .WithMessage("Variation must be tight, normal, or noisy.");
    }
}

public sealed class CreateCampaignTestResponsesHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ITestDataSimulatorStore store)
    : IRequestHandler<CreateCampaignTestResponsesCommand, Result<CreateCampaignTestResponsesResponse>>
{
    public Task<Result<CreateCampaignTestResponsesResponse>> Handle(
        CreateCampaignTestResponsesCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignTestResponsesAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
