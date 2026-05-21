using System.Net.Mail;
using System.Text;

namespace Platform.Application.Features.Notifications;

public static class EmailDeliveryFailureClassifier
{
    public const string SmtpUnknown = "smtp_unknown";
    public const string SmtpAuthFailed = "smtp_auth_failed";
    public const string SmtpTlsFailed = "smtp_tls_failed";
    public const string SmtpTransactionFailed = "smtp_transaction_failed";
    public const string SesIdentityNotVerified = "ses_identity_not_verified";
    public const string SesSenderIdentityNotVerified = "ses_sender_identity_not_verified";
    public const string SesSandboxRecipientNotVerified = "ses_sandbox_recipient_not_verified";
    public const string SesThrottled = "ses_throttled";

    public static string Classify(
        Exception exception,
        string? provider,
        string? managedProviderName,
        string? fromAddress,
        string? recipient)
    {
        var message = CollectMessages(exception);
        var isAwsSes = IsAwsSes(provider, managedProviderName, message);

        if (isAwsSes && Contains(message, "email address is not verified"))
        {
            if (ContainsAddress(message, fromAddress))
            {
                return SesSenderIdentityNotVerified;
            }

            if (ContainsAddress(message, recipient))
            {
                return SesSandboxRecipientNotVerified;
            }

            return SesIdentityNotVerified;
        }

        if (isAwsSes &&
            (Contains(message, "maximum sending rate exceeded") ||
             Contains(message, "daily message quota exceeded") ||
             Contains(message, "throttl")))
        {
            return SesThrottled;
        }

        if (Contains(message, "authentication") ||
            Contains(message, "authenticate") ||
            Contains(message, "credentials") ||
            Contains(message, "password"))
        {
            return SmtpAuthFailed;
        }

        if (Contains(message, "starttls") ||
            Contains(message, "ssl") ||
            Contains(message, "tls") ||
            Contains(message, "certificate"))
        {
            return SmtpTlsFailed;
        }

        if (exception is SmtpException smtpException &&
            smtpException.StatusCode == SmtpStatusCode.TransactionFailed)
        {
            return SmtpTransactionFailed;
        }

        return SmtpUnknown;
    }

    public static bool IsSafeFailureClass(string? value)
    {
        return value is
            SmtpUnknown or
            SmtpAuthFailed or
            SmtpTlsFailed or
            SmtpTransactionFailed or
            SesIdentityNotVerified or
            SesSenderIdentityNotVerified or
            SesSandboxRecipientNotVerified or
            SesThrottled;
    }

    private static bool IsAwsSes(string? provider, string? managedProviderName, string message)
    {
        return string.Equals(provider, EmailDeliveryProviderNames.Smtp, StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(managedProviderName, "aws-ses", StringComparison.OrdinalIgnoreCase) ||
             Contains(message, "amazon ses") ||
             Contains(message, "amazonses") ||
             Contains(message, "eu-central-1"));
    }

    private static string CollectMessages(Exception exception)
    {
        var builder = new StringBuilder();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(current.Message);
        }

        return builder.ToString();
    }

    private static bool ContainsAddress(string message, string? address)
    {
        var normalizedAddress = NormalizeAddress(address);
        return normalizedAddress is not null && Contains(message, normalizedAddress);
    }

    private static string? NormalizeAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return new MailAddress(value).Address.Trim().ToLowerInvariant();
        }
        catch (FormatException)
        {
            return value.Trim().ToLowerInvariant();
        }
    }

    private static bool Contains(string value, string expected)
    {
        return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
