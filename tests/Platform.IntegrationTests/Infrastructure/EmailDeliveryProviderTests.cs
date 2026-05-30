using Platform.Application.Features.Notifications;
using Platform.Infrastructure.Notifications;

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
}
