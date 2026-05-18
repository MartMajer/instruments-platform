using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetRespondentCampaignQuery(Guid CampaignId)
    : IRequest<Result<RespondentCampaignResponse>>;

public sealed class GetRespondentCampaignValidator : AbstractValidator<GetRespondentCampaignQuery>
{
    public GetRespondentCampaignValidator()
    {
        RuleFor(query => query.CampaignId).NotEmpty();
    }
}

public sealed class GetRespondentCampaignHandler(
    ICurrentTenant currentTenant,
    IResponseCaptureStore store)
    : IRequestHandler<GetRespondentCampaignQuery, Result<RespondentCampaignResponse>>
{
    public Task<Result<RespondentCampaignResponse>> Handle(
        GetRespondentCampaignQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetCampaignAsync(currentTenant.TenantId, query.CampaignId, cancellationToken);
    }
}
