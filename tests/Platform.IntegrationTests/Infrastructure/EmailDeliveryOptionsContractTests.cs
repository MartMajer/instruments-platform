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

    [Fact]
    public void Smtp_options_accept_managed_provider_with_verified_sender_domain()
    {
        var options = CreateValidSmtpOptions();

        options.EnsureValidProviderConfiguration();
    }

    [Fact]
    public void Smtp_options_require_managed_provider_attestation()
    {
        var options = CreateValidSmtpOptions();
        options.ManagedProviderName = null;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.managed_provider_missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Smtp_options_require_verified_sender_domain_attestation()
    {
        var options = CreateValidSmtpOptions();
        options.SenderDomainVerified = false;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.sender_domain_unverified", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Smtp_options_require_verified_sender_domain_value()
    {
        var options = CreateValidSmtpOptions();
        options.VerifiedSenderDomain = null;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.verified_sender_domain_missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Smtp_options_require_from_address_to_match_verified_sender_domain()
    {
        var options = CreateValidSmtpOptions();
        options.FromAddress = "noreply@other.example.test";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.from_address_domain_mismatch", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("other.example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public void Smtp_options_require_safe_provider_webhook_secret(string? providerWebhookSecret)
    {
        var options = CreateValidSmtpOptions();
        options.ProviderWebhookSecret = providerWebhookSecret;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.provider_webhook", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("too-short", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://app.example.test")]
    [InlineData("https://user:password@app.example.test")]
    [InlineData("https://app.example.test/path")]
    [InlineData("https://app.example.test?return=/r/inv_secret")]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    public void Smtp_options_require_safe_https_public_app_origin(string publicAppBaseUrl)
    {
        var options = CreateValidSmtpOptions();
        options.PublicAppBaseUrl = publicAppBaseUrl;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.public_app_base_url_missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("password", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inv_secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("smtp")]
    [InlineData("smtp example.test")]
    [InlineData("smtp.example.test\r\nBcc: ada@example.test")]
    [InlineData("localhost")]
    [InlineData("smtp.localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.25")]
    [InlineData("http://smtp.example.test")]
    [InlineData("smtp.example.test:587")]
    public void Smtp_options_reject_unsafe_host_values(string host)
    {
        var options = CreateValidSmtpOptions();
        options.Smtp.Host = host;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.smtp_host_missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Smtp_options_require_aws_ses_endpoint_when_managed_provider_is_aws_ses()
    {
        var options = CreateValidSmtpOptions();
        ConfigureAwsSes(options);
        options.Smtp.Host = "smtp.example.test";

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.smtp_host_provider_mismatch", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("smtp.example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("arn:aws:sns:eu-central-1:12345678901:ses-events")]
    [InlineData("arn:aws:sqs:eu-central-1:123456789012:ses-events")]
    [InlineData("arn:aws:sns:eu-central-1:123456789012:ses/events")]
    public void Smtp_options_require_safe_aws_ses_sns_topic_arn(string? snsTopicArn)
    {
        var options = CreateValidSmtpOptions();
        ConfigureAwsSes(options);
        options.AwsSes.SnsTopicArn = snsTopicArn;

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("email_delivery.aws_ses_sns_topic_missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("123456789012", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("email-smtp.eu-central-1.amazonaws.com")]
    [InlineData("email-smtp.us-east-1.api.aws")]
    [InlineData("email-smtp-fips.us-east-1.amazonaws.com")]
    public void Smtp_options_accept_aws_ses_smtp_endpoint_shape_after_native_event_recording_exists(string host)
    {
        var options = CreateValidSmtpOptions();
        ConfigureAwsSes(options);
        options.Smtp.Host = host;

        options.EnsureValidProviderConfiguration();
    }

    [Fact]
    public void Smtp_options_do_not_require_canonical_webhook_secret_for_aws_ses_native_events()
    {
        var options = CreateValidSmtpOptions();
        ConfigureAwsSes(options);
        options.ProviderWebhookSecret = null;

        options.EnsureValidProviderConfiguration();
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

        Assert.Contains("email_delivery.smtp_credentials_incomplete", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("smtp-password", exception.Message, StringComparison.Ordinal);
    }

    private static EmailDeliveryOptions CreateValidSmtpOptions()
    {
        return new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.Smtp,
            ManagedProviderName = "postmark",
            SenderDomainVerified = true,
            VerifiedSenderDomain = "example.test",
            FromAddress = "noreply@example.test",
            PublicAppBaseUrl = "https://app.example.test",
            InvitationFooterText = "You received this study invitation from the configured workspace.",
            ProviderWebhookSecret = "test-provider-webhook-secret-32-chars",
            Smtp = new SmtpEmailDeliveryOptions
            {
                Host = "smtp.example.test",
                Port = 25,
                EnableSsl = true
            }
        };
    }

    private static void ConfigureAwsSes(EmailDeliveryOptions options)
    {
        options.ManagedProviderName = "aws-ses";
        options.Smtp.Host = "email-smtp.eu-central-1.amazonaws.com";
        options.AwsSes.SnsTopicArn = "arn:aws:sns:eu-central-1:123456789012:ses-events";
    }
}
