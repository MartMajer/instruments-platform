using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record ListTemplateVersionsQuery(Guid AnchorTemplateVersionId)
    : IRequest<Result<TemplateVersionListResponse>>;

public sealed class ListTemplateVersionsValidator : AbstractValidator<ListTemplateVersionsQuery>
{
    public ListTemplateVersionsValidator()
    {
        RuleFor(query => query.AnchorTemplateVersionId).NotEmpty();
    }
}

public sealed class ListTemplateVersionsHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<ListTemplateVersionsQuery, Result<TemplateVersionListResponse>>
{
    public Task<Result<TemplateVersionListResponse>> Handle(
        ListTemplateVersionsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListTemplateVersionsAsync(
            currentTenant.TenantId,
            query.AnchorTemplateVersionId,
            cancellationToken);
    }
}
