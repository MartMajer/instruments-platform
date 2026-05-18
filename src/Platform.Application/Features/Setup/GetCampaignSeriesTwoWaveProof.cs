using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record GetCampaignSeriesTwoWaveProofQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesTwoWaveProofResponse>>;

public sealed class GetCampaignSeriesTwoWaveProofValidator
    : AbstractValidator<GetCampaignSeriesTwoWaveProofQuery>
{
    public GetCampaignSeriesTwoWaveProofValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesTwoWaveProofHandler(
    ICurrentTenant currentTenant,
    ICampaignSeriesProofStore store)
    : IRequestHandler<GetCampaignSeriesTwoWaveProofQuery, Result<CampaignSeriesTwoWaveProofResponse>>
{
    public Task<Result<CampaignSeriesTwoWaveProofResponse>> Handle(
        GetCampaignSeriesTwoWaveProofQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetTwoWaveProofAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
