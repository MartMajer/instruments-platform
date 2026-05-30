using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListSubjectsQuery(
    string? Search = null,
    int Skip = 0,
    int? Take = null,
    string? Sort = null,
    string? Source = null,
    string? Status = null,
    Guid? GroupId = null,
    string? Manager = null,
    string? Contact = null)
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
        RuleFor(query => query.Sort)
            .Must(value => IsKnownOrEmpty(value, SubjectDirectorySorts.IsKnown))
            .WithMessage("Unknown subject directory sort.");
        RuleFor(query => query.Source)
            .Must(value => IsKnownOrEmpty(value, SubjectDirectorySources.IsKnown))
            .WithMessage("Unknown subject directory source filter.");
        RuleFor(query => query.Status)
            .Must(value => IsKnownOrEmpty(value, SubjectDirectoryStatuses.IsKnown))
            .WithMessage("Unknown subject directory status filter.");
        RuleFor(query => query.Manager)
            .Must(value => IsKnownOrEmpty(value, SubjectDirectoryManagerFilters.IsKnown))
            .WithMessage("Unknown subject directory manager filter.");
        RuleFor(query => query.Contact)
            .Must(value => IsKnownOrEmpty(value, SubjectDirectoryContactFilters.IsKnown))
            .WithMessage("Unknown subject directory contact filter.");
    }

    private static bool IsKnownOrEmpty(string? value, Func<string, bool> isKnown)
    {
        return string.IsNullOrWhiteSpace(value) || isKnown(value.Trim());
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
                query.Take,
                NormalizeFilter(query.Sort),
                NormalizeFilter(query.Source),
                NormalizeFilter(query.Status),
                query.GroupId,
                NormalizeFilter(query.Manager),
                NormalizeFilter(query.Contact)),
            cancellationToken);
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeFilter(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
