using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record DuplicateCampaignSeriesCommand(
    Guid CampaignSeriesId,
    DuplicateCampaignSeriesRequest Request)
    : IRequest<Result<CampaignSeriesDuplicateResponse>>;

public sealed class DuplicateCampaignSeriesValidator : AbstractValidator<DuplicateCampaignSeriesCommand>
{
    public DuplicateCampaignSeriesValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(CampaignSeries.NameMaxLength);
    }
}

public sealed class DuplicateCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<DuplicateCampaignSeriesCommand, Result<CampaignSeriesDuplicateResponse>>
{
    public Task<Result<CampaignSeriesDuplicateResponse>> Handle(
        DuplicateCampaignSeriesCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.DuplicateCampaignSeriesAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
