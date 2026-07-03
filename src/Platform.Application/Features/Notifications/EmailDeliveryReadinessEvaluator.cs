using System.Net.Mail;
using System.Net;

namespace Platform.Application.Features.Notifications;

public sealed record EmailDeliveryReadinessConfiguration(
    string? Provider,
    string? ManagedProviderName,
    string? SenderDomainVerified,
    string? VerifiedSenderDomain,
    string? FromAddress,
    string? PublicAppBaseUrl,
    string? InvitationFooterText,
    string? SmtpHost,
    string? SmtpPort,
    string? SmtpEnableSsl,
    string? SmtpUserName,
    string? SmtpPassword,
    string? ProviderWebhookSecret,
    string? AwsSesSnsTopicArn,
    string? AzureCommunicationServicesConnectionString = null,
    string? AzureCommunicationServicesEndpoint = null,
    string? AzureCommunicationServicesAccessKey = null,
    string? AzureCommunicationServicesEventGridWebhookSecret = null);

public static class EmailDeliveryReadinessEvaluator
{
    public const string BlockingSeverity = "blocking";
    private static readonly HashSet<string> ManagedSmtpProviderNames = new(StringComparer.Ordinal)
    {
        "postmark",
        "aws-ses",
        "sendgrid",
        "mailgun",
        "other-managed"
    };

    public static EmailDeliveryReadinessResponse Create(EmailDeliveryReadinessConfiguration configuration)
    {
        var provider = NormalizeProvider(configuration.Provider);
        var mode = provider switch
        {
            EmailDeliveryProviderNames.LocalDev => "local_dev",
            EmailDeliveryProviderNames.Smtp => "smtp",
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
        else if (provider == EmailDeliveryProviderNames.Smtp)
        {
            AddSmtpReadinessIssues(configuration, issues);
        }
        else if (provider == EmailDeliveryProviderNames.AzureCommunicationEmail)
        {
            AddAzureCommunicationEmailReadinessIssues(configuration, issues);
        }
        else
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.provider_unknown",
                "EmailDelivery:Provider must be local-dev, smtp, or azure-communication-email.",
                BlockingSeverity));
        }

        bool webhookConfigured;
        if (provider == EmailDeliveryProviderNames.Smtp)
        {
            var webhookSecret = configuration.ProviderWebhookSecret?.Trim();
            var hasWebhookSecret = !string.IsNullOrWhiteSpace(webhookSecret);
            webhookConfigured = webhookSecret is { Length: >= 32 };
            var usesNativeAwsSes = IsAwsSesProvider(configuration.ManagedProviderName);
            if (!webhookConfigured && !usesNativeAwsSes)
            {
                issues.Add(new EmailDeliveryReadinessIssueResponse(
                    hasWebhookSecret
                        ? "email_delivery.provider_webhook_secret_unsafe"
                        : "email_delivery.provider_webhook_disabled",
                    hasWebhookSecret
                        ? "Provider event webhook secret is configured but too short. Use at least 32 characters before real SMTP sends."
                        : "Provider event webhook intake must be configured before real SMTP sends.",
                    BlockingSeverity));
            }
        }
        else if (provider == EmailDeliveryProviderNames.AzureCommunicationEmail)
        {
            var eventGridSecret = configuration.AzureCommunicationServicesEventGridWebhookSecret?.Trim();
            webhookConfigured = eventGridSecret is { Length: >= 32 };
        }
        else
        {
            webhookConfigured = false;
        }

        var canSendRealEmail = (provider == EmailDeliveryProviderNames.Smtp ||
            provider == EmailDeliveryProviderNames.AzureCommunicationEmail) &&
            issues.All(issue => issue.Severity != BlockingSeverity);

        return new EmailDeliveryReadinessResponse(
            provider,
            mode,
            canSendRealEmail,
            webhookConfigured,
            issues);
    }

    private static void AddSmtpReadinessIssues(
        EmailDeliveryReadinessConfiguration configuration,
        ICollection<EmailDeliveryReadinessIssueResponse> issues)
    {
        if (!IsKnownManagedSmtpProvider(configuration.ManagedProviderName))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.managed_provider_missing",
                "Choose a managed email provider for SMTP sends. Do not send respondent email directly from the app server or an unmanaged mailbox.",
                BlockingSeverity));
        }

        if (!IsExplicitTrue(configuration.SenderDomainVerified))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.sender_domain_unverified",
                "Verify sender-domain authentication with the managed provider before SMTP sends.",
                BlockingSeverity));
        }

        var verifiedSenderDomain = NormalizeSenderDomain(configuration.VerifiedSenderDomain);
        if (verifiedSenderDomain is null)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.verified_sender_domain_missing",
                "Set EmailDelivery:VerifiedSenderDomain to the domain authenticated with the managed provider.",
                BlockingSeverity));
        }

        if (string.IsNullOrWhiteSpace(configuration.FromAddress) ||
            !IsValidEmailAddress(configuration.FromAddress))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.from_address_missing",
                "A valid EmailDelivery:FromAddress is required before SMTP sends.",
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
                "EmailDelivery:InvitationFooterText is required before SMTP sends.",
                BlockingSeverity));
        }
        else if (configuration.InvitationFooterText.Length > 2000)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.invitation_footer_too_long",
                "EmailDelivery:InvitationFooterText must be 2000 characters or less.",
                BlockingSeverity));
        }

        var hasSafeSmtpHost = !string.IsNullOrWhiteSpace(configuration.SmtpHost) &&
            IsSafeSmtpHost(configuration.SmtpHost);
        if (!hasSafeSmtpHost)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.smtp_host_missing",
                "A safe EmailDelivery:Smtp:Host is required before SMTP sends.",
                BlockingSeverity));
        }
        else if (!IsCompatibleManagedProviderSmtpHost(
            configuration.ManagedProviderName,
            configuration.SmtpHost!))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.smtp_host_provider_mismatch",
                "EmailDelivery:Smtp:Host must match the configured managed provider.",
                BlockingSeverity));
        }

        if (IsAwsSesProvider(configuration.ManagedProviderName) &&
            !IsSafeAwsSesSnsTopicArn(configuration.AwsSesSnsTopicArn))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.aws_ses_sns_topic_missing",
                "EmailDelivery:AwsSes:SnsTopicArn is required before AWS SES sends.",
                BlockingSeverity));
        }

        var smtpPort = 25;
        if (!string.IsNullOrWhiteSpace(configuration.SmtpPort) &&
            !int.TryParse(configuration.SmtpPort, out smtpPort))
        {
            smtpPort = 0;
        }

        if (smtpPort is < 1 or > 65535)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.smtp_port_invalid",
                "EmailDelivery:Smtp:Port must be between 1 and 65535.",
                BlockingSeverity));
        }

        if (!IsExplicitTrue(configuration.SmtpEnableSsl))
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.smtp_tls_disabled",
                "EmailDelivery:Smtp:EnableSsl must be true before SMTP sends.",
                BlockingSeverity));
        }

        var hasUserName = !string.IsNullOrWhiteSpace(configuration.SmtpUserName);
        var hasPassword = !string.IsNullOrWhiteSpace(configuration.SmtpPassword);
        if (hasUserName != hasPassword)
        {
            issues.Add(new EmailDeliveryReadinessIssueResponse(
                "email_delivery.smtp_credentials_incomplete",
                "SMTP username and password must be both configured or both omitted.",
                BlockingSeverity));
        }
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

    private static bool IsKnownManagedSmtpProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        return ManagedSmtpProviderNames.Contains(provider.Trim().ToLowerInvariant());
    }

    private static bool IsCompatibleManagedProviderSmtpHost(
        string? provider,
        string smtpHost)
    {
        var normalizedProvider = provider?.Trim().ToLowerInvariant();
        var normalizedHost = smtpHost.Trim().ToLowerInvariant();

        return normalizedProvider switch
        {
            "aws-ses" => IsAwsSesSmtpHost(normalizedHost),
            _ => true
        };
    }

    private static bool IsAwsSesProvider(string? provider)
    {
        return string.Equals(provider?.Trim(), "aws-ses", StringComparison.OrdinalIgnoreCase);
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

    private static bool IsSafeSmtpHost(string host)
    {
        var normalized = host.Trim();
        if (normalized.Length == 0 ||
            normalized.Length != host.Length ||
            normalized.Length > 253)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (char.IsControl(character) ||
                char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return !normalized.StartsWith(".", StringComparison.Ordinal) &&
            !normalized.EndsWith(".", StringComparison.Ordinal) &&
            !normalized.Contains("..", StringComparison.Ordinal) &&
            !normalized.Contains("@", StringComparison.Ordinal) &&
            !normalized.Contains("/", StringComparison.Ordinal) &&
            !normalized.Contains("\\", StringComparison.Ordinal) &&
            !normalized.Contains(":", StringComparison.Ordinal) &&
            normalized.Contains(".", StringComparison.Ordinal) &&
            !IsLocalhost(normalized) &&
            !IPAddress.TryParse(normalized, out _) &&
            Uri.CheckHostName(normalized) == UriHostNameType.Dns;
    }

    private static bool IsAwsSesSmtpHost(string host)
    {
        var labels = host.Split('.');
        if (labels.Length is < 4 or > 5 ||
            labels[0] is not ("email-smtp" or "email-smtp-fips"))
        {
            return false;
        }

        if (labels[^2] == "amazonaws" &&
            labels[^1] == "com")
        {
            return labels.Length == 4 && labels[1].Length > 0;
        }

        return labels[^2] == "api" &&
            labels[^1] == "aws" &&
            labels.Length == 4 &&
            labels[1].Length > 0;
    }

    private static bool IsSafeAwsSesSnsTopicArn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var arn = value.Trim();
        if (arn.Length != value.Length ||
            arn.Length > 256)
        {
            return false;
        }

        foreach (var character in arn)
        {
            if (char.IsControl(character) ||
                char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        var parts = arn.Split(':', 6);
        if (parts.Length != 6 ||
            parts[0] != "arn" ||
            parts[2] != "sns" ||
            string.IsNullOrWhiteSpace(parts[3]) ||
            parts[4].Length != 12 ||
            parts[4].Any(character => character is < '0' or > '9') ||
            string.IsNullOrWhiteSpace(parts[5]))
        {
            return false;
        }

        if (parts[1] is not ("aws" or "aws-us-gov" or "aws-cn"))
        {
            return false;
        }

        return IsSafeAwsRegion(parts[3]) && IsSafeAwsSnsTopicName(parts[5]);
    }

    private static bool IsSafeAwsRegion(string region)
    {
        return region.Length is >= 5 and <= 32 &&
            region.All(character =>
                character is >= 'a' and <= 'z' ||
                character is >= '0' and <= '9' ||
                character == '-');
    }

    private static bool IsSafeAwsSnsTopicName(string topicName)
    {
        return topicName.Length is >= 1 and <= 256 &&
            topicName.All(character =>
                character is >= 'a' and <= 'z' ||
                character is >= 'A' and <= 'Z' ||
                character is >= '0' and <= '9' ||
                character is '-' or '_' or '.');
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
