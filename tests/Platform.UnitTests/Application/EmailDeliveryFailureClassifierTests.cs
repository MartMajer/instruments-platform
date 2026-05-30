using Platform.Application.Features.Notifications;

namespace Platform.UnitTests.Application;

public sealed class EmailDeliveryFailureClassifierTests
{
    [Fact]
    public void Classify_identifies_acs_authentication_failures_without_message_leakage()
    {
        var exception = new InvalidOperationException(
            "Azure Communication Services request failed with status code 401 because the access key is invalid.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            EmailDeliveryProviderNames.AzureCommunicationEmail);

        Assert.Equal("azure_communication_email_auth_failed", failureClass);
    }

    [Fact]
    public void Classify_identifies_acs_rate_limit_failures()
    {
        var exception = new InvalidOperationException(
            "Azure Communication Services request failed with status code 429. Too many requests.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            EmailDeliveryProviderNames.AzureCommunicationEmail);

        Assert.Equal("azure_communication_email_rate_limited", failureClass);
    }

    [Fact]
    public void Classify_keeps_unmatched_acs_failures_generic()
    {
        var exception = new InvalidOperationException("Azure Communication Services email send failed.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            EmailDeliveryProviderNames.AzureCommunicationEmail);

        Assert.Equal("azure_communication_email_unknown", failureClass);
    }
}
