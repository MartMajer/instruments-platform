using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record RestoreCampaignSeriesCommand(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesArchiveStateResponse>>;

public sealed class RestoreCampaignSeriesValidator : AbstractValidator<RestoreCampaignSeriesCommand>
{
    public RestoreCampaignSeriesValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class RestoreCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<RestoreCampaignSeriesCommand, Result<CampaignSeriesArchiveStateResponse>>
{
    public Task<Result<CampaignSeriesArchiveStateResponse>> Handle(
        RestoreCampaignSeriesCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.RestoreCampaignSeriesAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            actor.UserId.Value,
            cancellationToken);
    }
}
