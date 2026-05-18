using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record CloseCampaignCommand(
    Guid CampaignSeriesId,
    Guid CampaignId,
    CloseCampaignRequest Request)
    : IRequest<Result<CampaignCloseStateResponse>>;

public sealed class CloseCampaignValidator : AbstractValidator<CloseCampaignCommand>
{
    public CloseCampaignValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.Reason)
            .MaximumLength(Campaign.CloseReasonMaxLength);
    }
}

public sealed class CloseCampaignHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<CloseCampaignCommand, Result<CampaignCloseStateResponse>>
{
    public Task<Result<CampaignCloseStateResponse>> Handle(
        CloseCampaignCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<CampaignCloseStateResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CloseCampaignAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            command.CampaignId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
