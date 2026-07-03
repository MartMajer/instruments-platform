using Platform.Application.Features.Notifications;
using Platform.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class EmailDeliveryProviderTests
{
    [Fact]
    public async Task Local_dev_provider_returns_provider_name_without_external_io()
    {
        var provider = new LocalDevEmailDeliveryProvider();
        var notificationId = Guid.NewGuid();

        Assert.Equal(EmailDeliveryProviderNames.LocalDev, provider.Provider);

        var result = await provider.SendAsync(
            new EmailDeliveryMessage(
                notificationId,
                "campaign-email:tenant:notification:attempt",
                "researcher@example.com",
                "Invitation",
                "Open /r/inv_raw-token"),
            CancellationToken.None);

        Assert.Equal(EmailDeliveryProviderNames.LocalDev, result.Provider);
        Assert.Null(result.ProviderMessageId);
        Assert.NotEqual(default, result.SentAt);
    }

    [Fact]
    public void Smtp_options_fail_closed_when_required_config_is_missing()
    {
        var options = new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.Smtp
        };

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.managed_provider_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.sender_domain_unverified", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.verified_sender_domain_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.provider_webhook_disabled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.smtp_host_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.smtp_tls_disabled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.from_address_missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Acs_options_fail_closed_when_required_config_is_missing()
    {
        var options = new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.AzureCommunicationEmail
        };

        var exception = Assert.Throws<InvalidOperationException>(
            options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.sender_domain_unverified", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.verified_sender_domain_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.acs_credentials_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.acs_event_grid_webhook_secret_missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("email_delivery.from_address_missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Acs_provider_sends_message_with_required_headers_and_returns_provider_message_id()
    {
        var acceptedAt = DateTimeOffset.Parse("2026-05-30T12:00:00Z");
        var sender = new CapturingAzureCommunicationEmailSender(
            new AzureCommunicationEmailSendResult("acs-message-123", acceptedAt));
        var provider = new AzureCommunicationEmailDeliveryProvider(
            Options.Create(CreateValidAcsOptions()),
            sender);

        var result = await provider.SendAsync(
            new EmailDeliveryMessage(
                Guid.NewGuid(),
                "campaign-email:tenant:notification:attempt",
                "researcher@example.com",
                "Invitation",
                "Open your study link.",
                "https://app.validatedscale.com/r/inv_example/unsubscribe"),
            CancellationToken.None);

        var request = Assert.Single(sender.Requests);
        Assert.Equal(EmailDeliveryProviderNames.AzureCommunicationEmail, provider.Provider);
        Assert.Equal("no-reply@validatedscale.com", request.SenderAddress);
        Assert.Equal("researcher@example.com", request.RecipientAddress);
        Assert.Equal("Invitation", request.Subject);
        Assert.Equal("Open your study link.", request.BodyText);
        Assert.True(request.DisableUserEngagementTracking);
        Assert.Equal(
            "campaign-email:tenant:notification:attempt",
            request.Headers["X-Platform-Delivery-Key"]);
        Assert.Equal(
            "<https://app.validatedscale.com/r/inv_example/unsubscribe>",
            request.Headers["List-Unsubscribe"]);
        Assert.Equal(EmailDeliveryProviderNames.AzureCommunicationEmail, result.Provider);
        Assert.Equal("acs-message-123", result.ProviderMessageId);
        Assert.Equal(acceptedAt, result.SentAt);
    }

    [Fact]
    public async Task Acs_provider_rejects_unsafe_delivery_correlation_key_before_send()
    {
        var sender = new CapturingAzureCommunicationEmailSender(
            new AzureCommunicationEmailSendResult("acs-message-123", DateTimeOffset.UtcNow));
        var provider = new AzureCommunicationEmailDeliveryProvider(
            Options.Create(CreateValidAcsOptions()),
            sender);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.SendAsync(
                new EmailDeliveryMessage(
                    Guid.NewGuid(),
                    "campaign-email:tenant\r\nx-unsafe",
                    "researcher@example.com",
                    "Invitation",
                    "Open your study link.",
                    "https://app.validatedscale.com/r/inv_example/unsubscribe"),
                CancellationToken.None));

        Assert.Empty(sender.Requests);
        Assert.Equal("Email delivery correlation key is invalid.", exception.Message);
    }

    [Fact]
    public void Delivery_message_contract_has_no_raw_token_or_path_property()
    {
        Assert.DoesNotContain(
            typeof(EmailDeliveryMessage).GetProperties(),
            property =>
                property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Delivery_message_contract_can_carry_unsubscribe_url_without_raw_token_property()
    {
        var message = new EmailDeliveryMessage(
            Guid.NewGuid(),
            "campaign-email:tenant:notification:attempt",
            "researcher@example.com",
            "Invitation",
            "Open your study link.",
            "https://app.example.test/r/inv_example/unsubscribe");

        Assert.Equal("https://app.example.test/r/inv_example/unsubscribe", message.UnsubscribeUrl);
    }

    private static EmailDeliveryOptions CreateValidAcsOptions()
    {
        return new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.AzureCommunicationEmail,
            SenderDomainVerified = true,
            VerifiedSenderDomain = "validatedscale.com",
            FromAddress = "no-reply@validatedscale.com",
            PublicAppBaseUrl = "https://app.validatedscale.com",
            InvitationFooterText = "ValidatedScale research invitation.",
            AzureCommunicationServices = new AzureCommunicationServicesEmailDeliveryOptions
            {
                ConnectionString = "endpoint=https://validatedscale.communication.azure.com/;accesskey=abcdefghijklmnopqrstuvwxyz0123456789",
                EventGridWebhookSecret = "abcdefghijklmnopqrstuvwxyz012345"
            }
        };
    }

    private sealed class CapturingAzureCommunicationEmailSender(
        AzureCommunicationEmailSendResult result) : IAzureCommunicationEmailSender
    {
        public List<AzureCommunicationEmailSendRequest> Requests { get; } = [];

        public Task<AzureCommunicationEmailSendResult> SendAsync(
            AzureCommunicationEmailSendRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }
}
