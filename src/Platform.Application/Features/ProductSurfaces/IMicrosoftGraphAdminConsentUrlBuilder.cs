using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public interface IMicrosoftGraphAdminConsentUrlBuilder
{
    string? Build(MicrosoftGraphConsentRequestResponse response);
}

public sealed class NoOpMicrosoftGraphAdminConsentUrlBuilder : IMicrosoftGraphAdminConsentUrlBuilder
{
    public string? Build(MicrosoftGraphConsentRequestResponse response)
    {
        return null;
    }
}

public interface IMicrosoftGraphDirectorySnapshotConnector
{
    Task<Result<MicrosoftGraphDirectoryImportSnapshot>> FetchSnapshotAsync(
        string microsoftTenantId,
        CancellationToken cancellationToken);
}
