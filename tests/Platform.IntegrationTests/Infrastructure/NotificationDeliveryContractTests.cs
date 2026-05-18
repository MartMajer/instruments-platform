using System.Reflection;
using Platform.Application.Features.Notifications;
using Platform.Domain.Campaigns;
using Platform.Infrastructure.Notifications;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class NotificationDeliveryContractTests
{
    [Fact]
    public void Delivery_error_sanitizer_removes_sensitive_values()
    {
        var sanitized = InvokeStoreContract<string>(
            "SanitizeDeliveryError",
            "SMTP failed for ada@example.test at /r/inv_secret using token inv_secret and password=secret.");

        Assert.Equal("delivery_failed", sanitized);
        Assert.DoesNotContain("ada@example.test", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inv_secret", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delivery_error_sanitizer_uses_stable_fallback_for_blank_errors()
    {
        var sanitized = InvokeStoreContract<string>("SanitizeDeliveryError", "   ");

        Assert.Equal("delivery_failed", sanitized);
    }

    [Fact]
    public void Provider_message_id_sanitizer_redacts_sensitive_values()
    {
        var sanitized = InvokeStoreContract<string>(
            "SanitizeProviderMessageId",
            "smtp:/r/inv_secret:ada@example.test");

        Assert.Equal("redacted", sanitized);
    }

    [Fact]
    public void Provider_message_id_sanitizer_bounds_length()
    {
        var sanitized = InvokeStoreContract<string>(
            "SanitizeProviderMessageId",
            new string('a', 500));

        Assert.True(sanitized.Length <= 200);
    }

    [Fact]
    public void Respondent_path_is_exposed_only_for_local_dev_delivery()
    {
        Assert.Equal(
            "/r/inv_local",
            InvokeStoreContract<string?>(
                "CreateProofRespondentPath",
                EmailDeliveryProviderNames.LocalDev,
                "/r/inv_local"));
        Assert.Null(InvokeStoreContract<string?>(
            "CreateProofRespondentPath",
            EmailDeliveryProviderNames.Smtp,
            "/r/inv_smtp"));
        Assert.Null(InvokeStoreContract<string?>(
            "CreateProofRespondentPath",
            "unknown",
            "/r/inv_unknown"));
    }

    [Fact]
    public void Delivery_batch_size_contract_matches_endpoint_validator()
    {
        Assert.False(InvokeStoreContract<bool>("IsValidBatchSize", 0));
        Assert.True(InvokeStoreContract<bool>("IsValidBatchSize", 1));
        Assert.True(InvokeStoreContract<bool>("IsValidBatchSize", 25));
        Assert.False(InvokeStoreContract<bool>("IsValidBatchSize", 26));
    }

    [Fact]
    public void Delivery_processing_status_contract_requires_live_campaigns()
    {
        Assert.True(InvokeStoreContract<bool>("CanProcessCampaignStatus", CampaignStatuses.Live));
        Assert.False(InvokeStoreContract<bool>("CanProcessCampaignStatus", CampaignStatuses.Draft));
        Assert.False(InvokeStoreContract<bool>("CanProcessCampaignStatus", CampaignStatuses.Closed));
        Assert.False(InvokeStoreContract<bool>("CanProcessCampaignStatus", CampaignStatuses.Cancelled));
    }

    [Fact]
    public void Delivery_provider_name_is_allowlisted()
    {
        Assert.Equal(
            EmailDeliveryProviderNames.LocalDev,
            InvokeStoreContract<string>("SanitizeProvider", EmailDeliveryProviderNames.LocalDev));
        Assert.Equal(
            EmailDeliveryProviderNames.Smtp,
            InvokeStoreContract<string>("SanitizeProvider", EmailDeliveryProviderNames.Smtp));
        Assert.Equal("unknown", InvokeStoreContract<string>("SanitizeProvider", "smtp:/r/inv_secret"));
        Assert.Equal("unknown", InvokeStoreContract<string>("SanitizeProvider", " "));
    }

    [Fact]
    public void Smtp_options_require_valid_from_address()
    {
        var options = new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.Smtp,
            FromAddress = "not-an-email",
            Smtp = new SmtpEmailDeliveryOptions
            {
                Host = "smtp.example.test",
                Port = 25
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("EmailDelivery:FromAddress", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Smtp_options_reject_invalid_port(int port)
    {
        var options = new EmailDeliveryOptions
        {
            Provider = EmailDeliveryProviderNames.Smtp,
            FromAddress = "noreply@example.test",
            Smtp = new SmtpEmailDeliveryOptions
            {
                Host = "smtp.example.test",
                Port = port
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValidProviderConfiguration);

        Assert.Contains("EmailDelivery:Smtp:Port", exception.Message);
    }

    private static T InvokeStoreContract<T>(string name, params object?[] args)
    {
        var method = typeof(NotificationDeliveryStore).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method is null)
        {
            throw new InvalidOperationException($"NotificationDeliveryStore.{name} was not found.");
        }

        return (T)method.Invoke(null, args)!;
    }
}
