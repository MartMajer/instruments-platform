using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public sealed record GetCampaignReportProofQuery(Guid CampaignId)
    : IRequest<Result<CampaignReportProofResponse>>;

public sealed class GetCampaignReportProofValidator : AbstractValidator<GetCampaignReportProofQuery>
{
    public GetCampaignReportProofValidator()
    {
        RuleFor(query => query.CampaignId).NotEmpty();
    }
}

public sealed class GetCampaignReportProofHandler(
    ICurrentTenant currentTenant,
    IReportProofStore store)
    : IRequestHandler<GetCampaignReportProofQuery, Result<CampaignReportProofResponse>>
{
    public Task<Result<CampaignReportProofResponse>> Handle(
        GetCampaignReportProofQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignReportProofAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
