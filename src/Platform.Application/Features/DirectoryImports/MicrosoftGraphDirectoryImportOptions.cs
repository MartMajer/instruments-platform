namespace Platform.Application.Features.DirectoryImports;

public sealed class MicrosoftGraphDirectoryImportOptions
{
    public const string SectionName = "DirectoryImports:MicrosoftGraph";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AdminConsentRedirectUri { get; set; }

    public string? PostConsentRedirectUrl { get; set; }

    public string AdminConsentTenant { get; set; } = "organizations";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsAdminConsentConfigured =>
        IsConfigured &&
        !string.IsNullOrWhiteSpace(AdminConsentRedirectUri) &&
        !string.IsNullOrWhiteSpace(PostConsentRedirectUrl);
}
