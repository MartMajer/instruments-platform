using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record GetTemplateVersionQuery(Guid TemplateVersionId)
    : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class GetTemplateVersionValidator : AbstractValidator<GetTemplateVersionQuery>
{
    public GetTemplateVersionValidator()
    {
        RuleFor(query => query.TemplateVersionId).NotEmpty();
    }
}

public sealed class GetTemplateVersionHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<GetTemplateVersionQuery, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        GetTemplateVersionQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetTemplateVersionAsync(
            currentTenant.TenantId,
            query.TemplateVersionId,
            cancellationToken);
    }
}
