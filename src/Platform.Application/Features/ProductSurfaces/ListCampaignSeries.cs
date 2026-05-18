using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ListCampaignSeriesQuery(
    string? Search = null,
    string? Status = null,
    string? Sort = null,
    string? Visibility = null)
    : IRequest<CampaignSeriesListResponse>;

public sealed class ListCampaignSeriesValidator : AbstractValidator<ListCampaignSeriesQuery>
{
    public ListCampaignSeriesValidator()
    {
        RuleFor(query => query.Search).MaximumLength(128);
        RuleFor(query => query.Status)
            .Must(value => value is null || CampaignSeriesPortfolioStatuses.IsKnown(Normalize(value)))
            .WithMessage("Campaign series portfolio status is not supported.");
        RuleFor(query => query.Sort)
            .Must(value => value is null || CampaignSeriesPortfolioSorts.IsKnown(Normalize(value)))
            .WithMessage("Campaign series portfolio sort is not supported.");
        RuleFor(query => query.Visibility)
            .Must(value => value is null || CampaignSeriesPortfolioVisibilities.IsKnown(Normalize(value)))
            .WithMessage("Campaign series portfolio visibility is not supported.");
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}

public sealed class ListCampaignSeriesHandler(
    ICurrentTenant currentTenant,
    IProductSurfaceReadStore store)
    : IRequestHandler<ListCampaignSeriesQuery, CampaignSeriesListResponse>
{
    public Task<CampaignSeriesListResponse> Handle(
        ListCampaignSeriesQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListCampaignSeriesAsync(
            currentTenant.TenantId,
            new CampaignSeriesPortfolioQuery(
                NormalizeSearch(query.Search),
                NormalizeOrDefault(query.Status, CampaignSeriesPortfolioStatuses.All),
                NormalizeOrDefault(query.Sort, CampaignSeriesPortfolioSorts.ActivityDesc),
                NormalizeOrDefault(query.Visibility, CampaignSeriesPortfolioVisibilities.Active)),
            cancellationToken);
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeOrDefault(string? value, string defaultValue)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        return string.IsNullOrWhiteSpace(normalized) ? defaultValue : normalized;
    }
}
