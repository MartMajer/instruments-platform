using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record ListCampaignAssignmentsQuery(Guid CampaignId)
    : IRequest<Result<CampaignAssignmentListResponse>>;

public sealed class ListCampaignAssignmentsValidator : AbstractValidator<ListCampaignAssignmentsQuery>
{
    public ListCampaignAssignmentsValidator()
    {
        RuleFor(query => query.CampaignId).NotEmpty();
    }
}

public sealed class ListCampaignAssignmentsHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<ListCampaignAssignmentsQuery, Result<CampaignAssignmentListResponse>>
{
    public Task<Result<CampaignAssignmentListResponse>> Handle(
        ListCampaignAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListCampaignAssignmentsAsync(
            currentTenant.TenantId,
            query.CampaignId,
            cancellationToken);
    }
}
