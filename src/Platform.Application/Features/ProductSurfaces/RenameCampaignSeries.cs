using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record RenameCampaignSeriesCommand(
    Guid CampaignSeriesId,
    RenameCampaignSeriesRequest Request)
    : IRequest<Result<CampaignSeriesRenameResponse>>;

public sealed class RenameCampaignSeriesValidator : AbstractValidator<RenameCampaignSeriesCommand>
{
    public RenameCampaignSeriesValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(CampaignSeries.NameMaxLength);
    }
}

public sealed class RenameCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceWriteStore store)
    : IRequestHandler<RenameCampaignSeriesCommand, Result<CampaignSeriesRenameResponse>>
{
    public Task<Result<CampaignSeriesRenameResponse>> Handle(
        RenameCampaignSeriesCommand command,
        CancellationToken cancellationToken)
    {
        return store.RenameCampaignSeriesAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            command.Request,
            cancellationToken);
    }
}
