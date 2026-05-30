using System.Text;

namespace Platform.Application.Features.Notifications;

public static class EmailDeliveryFailureClassifier
{
    public const string AzureCommunicationEmailUnknown = "azure_communication_email_unknown";
    public const string AzureCommunicationEmailAuthFailed = "azure_communication_email_auth_failed";
    public const string AzureCommunicationEmailRateLimited = "azure_communication_email_rate_limited";
    public const string AzureCommunicationEmailSenderDomainRejected = "azure_communication_email_sender_domain_rejected";
    public const string EmailDeliveryUnknown = "email_delivery_unknown";

    public static string Classify(Exception exception, string? provider)
    {
        var message = CollectMessages(exception);
        if (!string.Equals(provider, EmailDeliveryProviderNames.AzureCommunicationEmail, StringComparison.OrdinalIgnoreCase))
        {
            return EmailDeliveryUnknown;
        }

        if (Contains(message, "401") ||
            Contains(message, "403") ||
            Contains(message, "authentication") ||
            Contains(message, "authorization") ||
            Contains(message, "unauthorized") ||
            Contains(message, "forbidden") ||
            Contains(message, "credential") ||
            Contains(message, "access key"))
        {
            return AzureCommunicationEmailAuthFailed;
        }

        if (Contains(message, "429") ||
            Contains(message, "too many requests") ||
            Contains(message, "rate limit") ||
            Contains(message, "throttl"))
        {
            return AzureCommunicationEmailRateLimited;
        }

        if ((Contains(message, "sender") || Contains(message, "domain")) &&
            (Contains(message, "verify") || Contains(message, "verified") || Contains(message, "not allowed")))
        {
            return AzureCommunicationEmailSenderDomainRejected;
        }

        return AzureCommunicationEmailUnknown;
    }

    public static bool IsSafeFailureClass(string? value)
    {
        return value is
            AzureCommunicationEmailUnknown or
            AzureCommunicationEmailAuthFailed or
            AzureCommunicationEmailRateLimited or
            AzureCommunicationEmailSenderDomainRejected or
            EmailDeliveryUnknown;
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

    private static bool Contains(string value, string expected)
    {
        return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
