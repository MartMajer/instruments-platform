using Platform.Application.Features.Notifications;
using Platform.Infrastructure.Notifications;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class EmailDeliveryProviderTests
{
    [Fact]
    public async Task Local_dev_provider_returns_provider_name_and_message_id_without_external_io()
    {
        var provider = new LocalDevEmailDeliveryProvider();
        var notificationId = Guid.NewGuid();

        Assert.Equal(EmailDeliveryProviderNames.LocalDev, provider.Provider);

        var result = await provider.SendAsync(
            new EmailDeliveryMessage(
                notificationId,
                "researcher@example.com",
                "Invitation",
                "Open /r/inv_raw-token"),
            CancellationToken.None);

        Assert.Equal(EmailDeliveryProviderNames.LocalDev, result.Provider);
        Assert.StartsWith($"local-dev:{notificationId:N}", result.ProviderMessageId, StringComparison.Ordinal);
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

        Assert.Contains("EmailDelivery:Smtp:Host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("EmailDelivery:FromAddress", exception.Message, StringComparison.Ordinal);
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
}
