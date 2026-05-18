using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record GetLaunchReadinessQuery(Guid CampaignId)
    : IRequest<Result<LaunchReadinessResponse>>;

public sealed class GetLaunchReadinessValidator : AbstractValidator<GetLaunchReadinessQuery>
{
    public GetLaunchReadinessValidator()
    {
        RuleFor(query => query.CampaignId).NotEmpty();
    }
}

public sealed class GetLaunchReadinessHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<GetLaunchReadinessQuery, Result<LaunchReadinessResponse>>
{
    public Task<Result<LaunchReadinessResponse>> Handle(
        GetLaunchReadinessQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetLaunchReadinessAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
