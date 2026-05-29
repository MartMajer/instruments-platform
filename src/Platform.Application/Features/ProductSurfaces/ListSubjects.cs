using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListSubjectsQuery(
    string? Search = null,
    int Skip = 0,
    int? Take = null)
    : IRequest<SubjectDirectoryResponse>;

public sealed class ListSubjectsValidator : AbstractValidator<ListSubjectsQuery>
{
    public ListSubjectsValidator()
    {
        RuleFor(query => query.Search).MaximumLength(128);
        RuleFor(query => query.Skip).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Take)
            .Must(value => value is null or >= 1 and <= 100)
            .WithMessage("Subject directory page size must be between 1 and 100.");
    }
}

public sealed class ListSubjectsHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListSubjectsQuery, SubjectDirectoryResponse>
{
    public Task<SubjectDirectoryResponse> Handle(
        ListSubjectsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListSubjectsAsync(
            currentTenant.TenantId,
            new SubjectDirectoryQuery(
                NormalizeSearch(query.Search),
                query.Skip,
                query.Take),
            cancellationToken);
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
