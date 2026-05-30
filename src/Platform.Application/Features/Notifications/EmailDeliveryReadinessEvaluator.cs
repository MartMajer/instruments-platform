using System.Net;
using System.Net.Mail;

namespace Platform.Application.Features.Notifications;

public sealed record EmailDeliveryReadinessConfiguration(
    string? Provider,
    string? SenderDomainVerified,
    string? VerifiedSenderDomain,
    string? FromAddress,
    string? PublicAppBaseUrl,
    string? InvitationFooterText,
    string? AzureCommunicationServicesConnectionString = null,
    string? AzureCommunicationServicesEndpoint = null,
    string? AzureCommunicationServicesAccessKey = null,
    string? AzureCommunicationServicesEventGridWebhookSecret = null);

public static class EmailDeliveryReadinessEvaluator
{
    public const string BlockingSeverity = "blocking";

    public static EmailDeliveryReadinessResponse Create(EmailDeliveryReadinessConfiguration configuration)
    {
        var provider = NormalizeProvider(configuration.Provider);
        var mode = provider switch
        {
            EmailDeliveryProviderNames.LocalDev => "local_dev",
            EmailDeliveryProviderNames.AzureCommunicationEmail => "azure_communication_email",
            _ => "unknown"
        };
        var issues = new List<EmailDeliveryReadinessIssueResponse>();

        if (provider == EmailDeliveryProviderNames.LocalDev)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.local_dev",
                "This environment uses local development email proof mode. It will not send real respondent email.",
                "info"));
        }
        else if (provider == EmailDeliveryProviderNames.AzureCommunicationEmail)
        {
            AddAzureCommunicationEmailReadinessIssues(configuration, issues);
        }
        else
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.provider_unknown",
                "EmailDelivery:Provider must be local-dev or azure-communication-email.",
                BlockingSeverity));
        }

        var eventGridWebhookSecret = configuration.AzureCommunicationServicesEventGridWebhookSecret?.Trim();
        var webhookConfigured = provider == EmailDeliveryProviderNames.AzureCommunicationEmail &&
            eventGridWebhookSecret is { Length: >= 32 };
        var canSendRealEmail = provider == EmailDeliveryProviderNames.AzureCommunicationEmail &&
            issues.All(issue => issue.Severity != BlockingSeverity);

        return new EmailDeliveryReadinessResponse(
            provider,
            mode,
            canSendRealEmail,
            webhookConfigured,
            issues);
    }

    private static void AddAzureCommunicationEmailReadinessIssues(
        EmailDeliveryReadinessConfiguration configuration,
        ICollection<EmailDeliveryReadinessIssueResponse> issues)
    {
        if (!IsExplicitTrue(configuration.SenderDomainVerified))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.sender_domain_unverified",
                "Verify sender-domain authentication with Azure Communication Services before real email sends.",
                BlockingSeverity));
        }

        var verifiedSenderDomain = NormalizeSenderDomain(configuration.VerifiedSenderDomain);
        if (verifiedSenderDomain is null)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.verified_sender_domain_missing",
                "Set EmailDelivery:VerifiedSenderDomain to the domain authenticated with Azure Communication Services.",
                BlockingSeverity));
        }

        if (string.IsNullOrWhiteSpace(configuration.FromAddress) ||
            !IsValidEmailAddress(configuration.FromAddress))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.from_address_missing",
                "A valid EmailDelivery:FromAddress is required before Azure Communication Services sends.",
                BlockingSeverity));
        }
        else if (verifiedSenderDomain is not null &&
            !SenderAddressUsesVerifiedDomain(configuration.FromAddress, verifiedSenderDomain))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.from_address_domain_mismatch",
                "EmailDelivery:FromAddress must use EmailDelivery:VerifiedSenderDomain.",
                BlockingSeverity));
        }

        if (string.IsNullOrWhiteSpace(configuration.PublicAppBaseUrl) ||
            !IsSafePublicAppBaseUrl(configuration.PublicAppBaseUrl))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.public_app_base_url_missing",
                "A safe HTTPS EmailDelivery:PublicAppBaseUrl is required so invitation links are absolute.",
                BlockingSeverity));
        }

        if (string.IsNullOrWhiteSpace(configuration.InvitationFooterText))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.invitation_footer_missing",
                "EmailDelivery:InvitationFooterText is required before Azure Communication Services sends.",
                BlockingSeverity));
        }
        else if (configuration.InvitationFooterText.Length > 2000)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.invitation_footer_too_long",
                "EmailDelivery:InvitationFooterText must be 2000 characters or less.",
                BlockingSeverity));
        }

        AddAzureCommunicationServicesCredentialIssues(configuration, issues);
        AddAzureCommunicationServicesEventGridIssues(configuration, issues);
    }

    private static void AddAzureCommunicationServicesCredentialIssues(
        EmailDeliveryReadinessConfiguration configuration,
        ICollection<EmailDeliveryReadinessIssueResponse> issues)
    {
        var connectionString = configuration.AzureCommunicationServicesConnectionString?.Trim();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (IsSafeAzureCommunicationServicesConnectionString(connectionString))
            {
                return;
            }

            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.acs_credentials_missing",
                "Configure a safe Azure Communication Services connection string, or endpoint plus access key.",
                BlockingSeverity));
            return;
        }

        var endpoint = configuration.AzureCommunicationServicesEndpoint?.Trim();
        var accessKey = configuration.AzureCommunicationServicesAccessKey?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.acs_credentials_missing",
                "Configure a safe Azure Communication Services connection string, or endpoint plus access key.",
                BlockingSeverity));
            return;
        }

        if (!IsSafeAzureCommunicationServicesEndpoint(endpoint))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.acs_endpoint_invalid",
                "EmailDelivery:AzureCommunicationServices:Endpoint must be a safe HTTPS Azure Communication Services endpoint.",
                BlockingSeverity));
        }
    }

    private static void AddAzureCommunicationServicesEventGridIssues(
        EmailDeliveryReadinessConfiguration configuration,
        ICollection<EmailDeliveryReadinessIssueResponse> issues)
    {
        var secret = configuration.AzureCommunicationServicesEventGridWebhookSecret?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.acs_event_grid_webhook_secret_missing",
                "Configure EmailDelivery:AzureCommunicationServices:EventGridWebhookSecret before Azure Communication Services sends.",
                BlockingSeverity));
        }
        else if (secret.Length < 32)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.acs_event_grid_webhook_secret_unsafe",
                "EmailDelivery:AzureCommunicationServices:EventGridWebhookSecret must be at least 32 characters.",
                BlockingSeverity));
        }
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? EmailDeliveryProviderNames.LocalDev
            : provider.Trim().ToLowerInvariant();
    }

    private static bool IsExplicitTrue(string? value)
    {
        var normalized = value?.Trim();
        return string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidEmailAddress(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool SenderAddressUsesVerifiedDomain(
        string fromAddress,
        string verifiedSenderDomain)
    {
        try
        {
            var address = new MailAddress(fromAddress);
            return string.Equals(
                address.Host,
                verifiedSenderDomain,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NormalizeSenderDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var domain = value.Trim().ToLowerInvariant();
        if (domain.Length == 0 ||
            domain.Length != value.Length ||
            domain.Length > 253 ||
            domain.StartsWith(".", StringComparison.Ordinal) ||
            domain.EndsWith(".", StringComparison.Ordinal) ||
            domain.Contains("@", StringComparison.Ordinal) ||
            domain.Contains("/", StringComparison.Ordinal) ||
            domain.Contains("\\", StringComparison.Ordinal) ||
            !domain.Contains(".", StringComparison.Ordinal) ||
            IsLocalhost(domain) ||
            IPAddress.TryParse(domain, out _) ||
            Uri.CheckHostName(domain) != UriHostNameType.Dns)
        {
            return null;
        }

        foreach (var character in domain)
        {
            if (char.IsControl(character) ||
                char.IsWhiteSpace(character))
            {
                return null;
            }
        }

        return domain;
    }

    private static bool IsSafePublicAppBaseUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == "https" &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrWhiteSpace(uri.UserInfo) &&
            (string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/") &&
            string.IsNullOrWhiteSpace(uri.Query) &&
            string.IsNullOrWhiteSpace(uri.Fragment) &&
            !IsLocalhost(uri.Host);
    }

    private static bool IsSafeAzureCommunicationServicesConnectionString(string value)
    {
        if (value.Length != value.Trim().Length ||
            value.Length > 2048 ||
            value.Any(char.IsControl))
        {
            return false;
        }

        var parts = value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(
                part => part[0],
                part => part[1],
                StringComparer.OrdinalIgnoreCase);

        return parts.TryGetValue("endpoint", out var endpoint) &&
            IsSafeAzureCommunicationServicesEndpoint(endpoint) &&
            parts.TryGetValue("accesskey", out var accessKey) &&
            !string.IsNullOrWhiteSpace(accessKey) &&
            accessKey.Length <= 512 &&
            !accessKey.Any(char.IsControl);
    }

    private static bool IsSafeAzureCommunicationServicesEndpoint(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            uri.Host.EndsWith(".communication.azure.com", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(uri.AbsolutePath) &&
            (uri.AbsolutePath == "/" || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/'))) &&
            string.IsNullOrWhiteSpace(uri.UserInfo) &&
            string.IsNullOrWhiteSpace(uri.Query) &&
            string.IsNullOrWhiteSpace(uri.Fragment) &&
            !IsLocalhost(uri.Host);
    }

    private static bool IsLocalhost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }
}
