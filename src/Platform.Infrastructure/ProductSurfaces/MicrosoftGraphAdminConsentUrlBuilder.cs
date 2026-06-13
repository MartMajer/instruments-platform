using Microsoft.Extensions.Options;
using Platform.Application.Features.ProductSurfaces;

namespace Platform.Infrastructure.ProductSurfaces;

public sealed class MicrosoftGraphAdminConsentUrlBuilder(
    IOptions<MicrosoftGraphAdminConsentOptions> options)
    : IMicrosoftGraphAdminConsentUrlBuilder
{
    public string? Build(MicrosoftGraphConsentRequestResponse response)
    {
        var current = options.Value;
        if (string.IsNullOrWhiteSpace(current.ClientId) ||
            string.IsNullOrWhiteSpace(current.RedirectUri) ||
            string.IsNullOrWhiteSpace(current.AuthorityBaseUrl) ||
            !Uri.TryCreate(current.AuthorityBaseUrl, UriKind.Absolute, out var authorityUri) ||
            !Uri.TryCreate(current.RedirectUri, UriKind.Absolute, out var redirectUri))
        {
            return null;
        }

        var separator = string.IsNullOrEmpty(authorityUri.Query) ? '?' : '&';
        return string.Concat(
            authorityUri.AbsoluteUri,
            separator,
            "client_id=",
            Uri.EscapeDataString(current.ClientId.Trim()),
            "&redirect_uri=",
            Uri.EscapeDataString(redirectUri.AbsoluteUri),
            "&state=",
            Uri.EscapeDataString(response.State));
    }
}
