namespace Platform.Infrastructure.ProductSurfaces;

public sealed class MicrosoftGraphAdminConsentOptions
{
    public const string SectionName = "MicrosoftGraph";

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string? RedirectUri { get; init; }

    public string AuthorityBaseUrl { get; init; } = "https://login.microsoftonline.com/common/adminconsent";

    public string TokenAuthorityTemplate { get; init; } =
        "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

    public string GraphBaseUrl { get; init; } = "https://graph.microsoft.com/v1.0";

    public int MaxUsers { get; init; } = 10_000;

    public int MaxGroups { get; init; } = 2_000;

    public int MaxMemberships { get; init; } = 50_000;
}
