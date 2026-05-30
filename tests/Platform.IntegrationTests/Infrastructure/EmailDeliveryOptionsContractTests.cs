using Platform.Application.Features.Notifications;
using Platform.Application.Features.System.GetHealth;
using Platform.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class EmailDeliveryOptionsContractTests
{
    [Fact]
    public async Task Email_delivery_configuration_health_check_returns_ok_for_local_dev()
    {
        var check = new EmailDeliveryConfigurationHealthCheck(
            Options.Create(new EmailDeliveryOptions()));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("email_delivery_configuration", result.Name);
        Assert.Equal(PlatformHealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Email_delivery_configuration_health_check_returns_unready_for_invalid_acs_config()
    {
        var check = new EmailDeliveryConfigurationHealthCheck(
            Options.Create(new EmailDeliveryOptions
            {
                Provider = EmailDeliveryProviderNames.AzureCommunicationEmail,
                FromAddress = "noreply@example.test",
                AzureCommunicationServices = new AzureCommunicationServicesEmailDeliveryOptions()
            }));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("email_delivery_configuration", result.Name);
        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Fact]
    public void Acs_options_accept_verified_sender_domain_and_connection_string()
    {
        var options = CreateValidAcsOptions();

        options.EnsureValidProviderConfiguration();
    }

    [Fact]
    public void Acs_options_reject_smtp_provider()
    {
        var options = CreateValidAcsOptions();
        options.Provider = "smtp";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.provider_unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Acs_options_reject_aws_ses_provider_name()
    {
        var options = CreateValidAcsOptions();
        options.Provider = "aws-ses";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.provider_unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Acs_options_require_verified_sender_domain_attestation()
    {
        var options = CreateValidAcsOptions();
        options.SenderDomainVerified = false;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.sender_domain_unverified", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Acs_options_require_verified_sender_domain_value()
    {
        var options = CreateValidAcsOptions();
        options.VerifiedSenderDomain = null;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.verified_sender_domain_missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Acs_options_require_from_address_to_match_verified_sender_domain()
    {
        var options = CreateValidAcsOptions();
        options.FromAddress = "noreply@other.example.test";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.from_address_domain_mismatch", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("other.example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public void Acs_options_require_safe_event_grid_webhook_secret(string? eventGridWebhookSecret)
    {
        var options = CreateValidAcsOptions();
        options.AzureCommunicationServices.EventGridWebhookSecret = eventGridWebhookSecret;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.acs_event_grid_webhook_secret", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("too-short", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://app.example.test")]
    [InlineData("https://user:password@app.example.test")]
    [InlineData("https://app.example.test/path")]
    [InlineData("https://app.example.test?return=/r/inv_secret")]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    public void Acs_options_require_safe_https_public_app_origin(string publicAppBaseUrl)
    {
        var options = CreateValidAcsOptions();
        options.PublicAppBaseUrl = publicAppBaseUrl;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.public_app_base_url_missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("password", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inv_secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("endpoint=https://resource.communication.azure.com/;", null)]
    [InlineData("https://validatedscale.communication.azure.com", null)]
    [InlineData(null, "access-key")]
    public void Acs_options_require_connection_string_or_endpoint_and_access_key(
        string? endpoint,
        string? accessKey)
    {
        var options = CreateValidAcsOptions();
        options.AzureCommunicationServices.ConnectionString = null;
        options.AzureCommunicationServices.Endpoint = endpoint;
        options.AzureCommunicationServices.AccessKey = accessKey;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.acs_credentials_missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("access-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://validatedscale.communication.azure.com")]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://validatedscale.communication.azure.com/path")]
    [InlineData("https://user:pass@validatedscale.communication.azure.com")]
    public void Acs_options_reject_unsafe_endpoint_values(string endpoint)
    {
        var options = CreateValidAcsOptions();
        options.AzureCommunicationServices.ConnectionString = null;
        options.AzureCommunicationServices.Endpoint = endpoint;
        options.AzureCommunicationServices.AccessKey = "acs-access-key";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.acs_endpoint_invalid", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("pass", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static EmailDeliveryOptions CreateValidAcsOptions()
    {
        return new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.AzureCommunicationEmail,
            SenderDomainVerified = true,
            VerifiedSenderDomain = "example.test",
            FromAddress = "noreply@example.test",
            PublicAppBaseUrl = "https://app.example.test",
            InvitationFooterText = "You received this study invitation from the configured workspace.",
            AzureCommunicationServices = new AzureCommunicationServicesEmailDeliveryOptions
            {
                ConnectionString = "endpoint=https://validatedscale.communication.azure.com/;accesskey=test-access-key",
                EventGridWebhookSecret = "test-event-grid-webhook-secret-32-chars",
                DisableUserEngagementTracking = true
            }
        };
    }
}
