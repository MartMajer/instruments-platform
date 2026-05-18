using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ArchiveCampaignSeriesCommand(
    Guid CampaignSeriesId,
    ArchiveCampaignSeriesRequest Request)
    : IRequest<Result<CampaignSeriesArchiveStateResponse>>;

public sealed class ArchiveCampaignSeriesValidator : AbstractValidator<ArchiveCampaignSeriesCommand>
{
    public ArchiveCampaignSeriesValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request.Reason)
            .MaximumLength(CampaignSeries.ArchiveReasonMaxLength);
    }
}

public sealed class ArchiveCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ArchiveCampaignSeriesCommand, Result<CampaignSeriesArchiveStateResponse>>
{
    public Task<Result<CampaignSeriesArchiveStateResponse>> Handle(
        ArchiveCampaignSeriesCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ArchiveCampaignSeriesAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
