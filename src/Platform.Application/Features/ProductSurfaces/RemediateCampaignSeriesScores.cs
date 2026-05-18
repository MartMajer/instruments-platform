using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record RemediateCampaignSeriesScoresCommand(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesScoreRemediationResponse>>;

public sealed class RemediateCampaignSeriesScoresValidator : AbstractValidator<RemediateCampaignSeriesScoresCommand>
{
    public RemediateCampaignSeriesScoresValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class RemediateCampaignSeriesScoresHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<RemediateCampaignSeriesScoresCommand, Result<CampaignSeriesScoreRemediationResponse>>
{
    public Task<Result<CampaignSeriesScoreRemediationResponse>> Handle(
        RemediateCampaignSeriesScoresCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<CampaignSeriesScoreRemediationResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.RemediateCampaignSeriesScoresAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            actor.UserId.Value,
            cancellationToken);
    }
}
