namespace Platform.Infrastructure.DirectoryImports;

public sealed class MicrosoftGraphDirectoryImportOptions
{
    public const string SectionName = "DirectoryImports:MicrosoftGraph";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
