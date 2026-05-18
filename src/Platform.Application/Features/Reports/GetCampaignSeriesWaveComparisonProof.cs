using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public sealed record GetCampaignSeriesWaveComparisonProofQuery(Guid CampaignSeriesId)
    : IRequest<Result<CampaignSeriesWaveComparisonProofResponse>>;

public sealed class GetCampaignSeriesWaveComparisonProofValidator
    : AbstractValidator<GetCampaignSeriesWaveComparisonProofQuery>
{
    public GetCampaignSeriesWaveComparisonProofValidator()
    {
        RuleFor(query => query.CampaignSeriesId).NotEmpty();
    }
}

public sealed class GetCampaignSeriesWaveComparisonProofHandler(
    ICurrentTenant currentTenant,
    IWaveComparisonProofStore store)
    : IRequestHandler<GetCampaignSeriesWaveComparisonProofQuery, Result<CampaignSeriesWaveComparisonProofResponse>>
{
    public Task<Result<CampaignSeriesWaveComparisonProofResponse>> Handle(
        GetCampaignSeriesWaveComparisonProofQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignSeriesWaveComparisonProofAsync(
            currentTenant.TenantId,
            query.CampaignSeriesId,
            cancellationToken);
    }
}
