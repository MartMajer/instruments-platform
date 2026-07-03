using System.Net.Mail;
using Platform.Application.Features.Notifications;

namespace Platform.UnitTests.Application;

public sealed class EmailDeliveryFailureClassifierTests
{
    [Fact]
    public void Classify_identifies_ses_sandbox_recipient_identity_rejection()
    {
        var exception = new SmtpException(
            SmtpStatusCode.TransactionFailed,
            "Message rejected: Email address is not verified. The following identities failed the check in region EU-CENTRAL-1: majeric.martin@gmail.com");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "smtp",
            managedProviderName: "aws-ses",
            fromAddress: "no-reply@validatedscale.croat.dev",
            recipient: "majeric.martin@gmail.com");

        Assert.Equal("ses_sandbox_recipient_not_verified", failureClass);
    }

    [Fact]
    public void Classify_identifies_ses_sender_identity_rejection()
    {
        var exception = new SmtpException(
            SmtpStatusCode.TransactionFailed,
            "Message rejected: Email address is not verified. The following identities failed the check in region EU-CENTRAL-1: no-reply@validatedscale.croat.dev");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "smtp",
            managedProviderName: "aws-ses",
            fromAddress: "no-reply@validatedscale.croat.dev",
            recipient: "majeric.martin@gmail.com");

        Assert.Equal("ses_sender_identity_not_verified", failureClass);
    }

    [Fact]
    public void Classify_keeps_unmatched_ses_identity_rejection_generic()
    {
        var exception = new SmtpException(
            SmtpStatusCode.TransactionFailed,
            "Message rejected: Email address is not verified. The following identities failed the check in region EU-CENTRAL-1: other@example.test");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "smtp",
            managedProviderName: "aws-ses",
            fromAddress: "no-reply@validatedscale.croat.dev",
            recipient: "majeric.martin@gmail.com");

        Assert.Equal("ses_identity_not_verified", failureClass);
    }

    [Fact]
    public void Classify_identifies_smtp_authentication_failures_without_message_leakage()
    {
        var exception = new SmtpException(
            SmtpStatusCode.GeneralFailure,
            "Authentication failed for user smtp-secret-user.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "smtp",
            managedProviderName: "aws-ses",
            fromAddress: "no-reply@validatedscale.croat.dev",
            recipient: "majeric.martin@gmail.com");

        Assert.Equal("smtp_auth_failed", failureClass);
    }

    [Fact]
    public void Classify_identifies_azure_communication_email_auth_failures()
    {
        var exception = new InvalidOperationException("The request is not authorized. Check the access key.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "azure-communication-email",
            managedProviderName: null,
            fromAddress: "no-reply@validatedscale.com",
            recipient: "researcher@example.com");

        Assert.Equal("azure_communication_email_auth_failed", failureClass);
    }

    [Fact]
    public void Classify_identifies_azure_communication_email_rate_limiting()
    {
        var exception = new InvalidOperationException("429 Too Many Requests. Rate limit exceeded.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "azure-communication-email",
            managedProviderName: null,
            fromAddress: "no-reply@validatedscale.com",
            recipient: "researcher@example.com");

        Assert.Equal("azure_communication_email_rate_limited", failureClass);
    }

    [Fact]
    public void Classify_keeps_unmatched_azure_communication_email_failure_generic()
    {
        var exception = new InvalidOperationException("Unexpected service failure.");

        var failureClass = EmailDeliveryFailureClassifier.Classify(
            exception,
            provider: "azure-communication-email",
            managedProviderName: null,
            fromAddress: "no-reply@validatedscale.com",
            recipient: "researcher@example.com");

        Assert.Equal("azure_communication_email_unknown", failureClass);
    }
}
