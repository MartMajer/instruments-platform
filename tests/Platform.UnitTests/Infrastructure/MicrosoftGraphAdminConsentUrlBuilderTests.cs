using Microsoft.Extensions.Options;
using Platform.Application.Features.ProductSurfaces;
using Platform.Infrastructure.ProductSurfaces;

namespace Platform.UnitTests.Infrastructure;

public sealed class MicrosoftGraphAdminConsentUrlBuilderTests
{
    [Fact]
    public void Build_returns_null_when_client_configuration_is_missing()
    {
        var builder = new MicrosoftGraphAdminConsentUrlBuilder(
            Options.Create(new MicrosoftGraphAdminConsentOptions()));

        var url = builder.Build(CreateResponse());

        Assert.Null(url);
    }

    [Fact]
    public void Build_constructs_admin_consent_url_without_nonce()
    {
        var builder = new MicrosoftGraphAdminConsentUrlBuilder(
            Options.Create(new MicrosoftGraphAdminConsentOptions
            {
                ClientId = "client id",
                RedirectUri = "https://platform.example.test/directory-connections/microsoft-graph/consent-callback"
            }));

        var url = builder.Build(CreateResponse());

        Assert.NotNull(url);
        Assert.StartsWith("https://login.microsoftonline.com/common/adminconsent?", url, StringComparison.Ordinal);
        Assert.Contains("client_id=client%20id", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fplatform.example.test%2Fdirectory-connections%2Fmicrosoft-graph%2Fconsent-callback", url, StringComparison.Ordinal);
        Assert.Contains("state=state-value", url, StringComparison.Ordinal);
        Assert.DoesNotContain("nonce-value", url, StringComparison.Ordinal);
    }

    private static MicrosoftGraphConsentRequestResponse CreateResponse()
    {
        return new MicrosoftGraphConsentRequestResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "microsoft_graph",
            "pending",
            ["User.Read.All"],
            DateTimeOffset.Parse("2026-06-12T12:20:00+00:00"),
            "state-value",
            "nonce-value",
            "/directory-connections/microsoft-graph/consent-callback");
    }
}
