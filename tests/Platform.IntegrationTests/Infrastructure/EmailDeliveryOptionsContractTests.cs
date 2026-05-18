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
    public async Task Email_delivery_configuration_health_check_returns_unready_for_invalid_smtp_config()
    {
        var check = new EmailDeliveryConfigurationHealthCheck(
            Options.Create(new EmailDeliveryOptions
            {
                Provider = EmailDeliveryProviderNames.Smtp,
                FromAddress = "noreply@example.test",
                Smtp = new SmtpEmailDeliveryOptions
                {
                    Host = "smtp.example.test",
                    Port = 25,
                    UserName = "smtp-user"
                }
            }));

        var result = await check.CheckAsync(CancellationToken.None);

        Assert.Equal("email_delivery_configuration", result.Name);
        Assert.Equal(PlatformHealthCheckStatus.Unready, result.Status);
    }

    [Theory]
    [InlineData("smtp example.test")]
    [InlineData("smtp.example.test\r\nBcc: ada@example.test")]
    public void Smtp_options_reject_unsafe_host_values(string host)
    {
        var options = CreateValidSmtpOptions();
        options.Smtp.Host = host;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("EmailDelivery:Smtp:Host", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("smtp-user", null)]
    [InlineData(null, "smtp-password")]
    public void Smtp_options_require_username_and_password_to_be_configured_together(
        string? userName,
        string? password)
    {
        var options = CreateValidSmtpOptions();
        options.Smtp.UserName = userName;
        options.Smtp.Password = password;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("EmailDelivery:Smtp:Credentials", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("smtp-password", exception.Message, StringComparison.Ordinal);
    }

    private static EmailDeliveryOptions CreateValidSmtpOptions()
    {
        return new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.Smtp,
            FromAddress = "noreply@example.test",
            Smtp = new SmtpEmailDeliveryOptions
            {
                Host = "smtp.example.test",
                Port = 25
            }
        };
    }
}
