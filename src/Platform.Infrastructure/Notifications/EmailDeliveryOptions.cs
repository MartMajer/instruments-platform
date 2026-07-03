using Platform.Application.Features.Notifications;
using System.Globalization;

namespace Platform.Infrastructure.Notifications;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public string Provider { get; set; } = EmailDeliveryProviderNames.LocalDev;

    public string? ManagedProviderName { get; set; }

    public bool SenderDomainVerified { get; set; }

    public string? VerifiedSenderDomain { get; set; }

    public string? FromAddress { get; set; }

    public string? PublicAppBaseUrl { get; set; }

    public string? InvitationFooterText { get; set; }

    public string? ProviderWebhookSecret { get; set; }

    public SmtpEmailDeliveryOptions Smtp { get; set; } = new();

    public AwsSesEmailDeliveryOptions AwsSes { get; set; } = new();

    public AzureCommunicationServicesEmailDeliveryOptions AzureCommunicationServices { get; set; } = new();

    public void EnsureValidProviderConfiguration()
    {
        var readiness = EmailDeliveryReadinessEvaluator.Create(new EmailDeliveryReadinessConfiguration(
            Provider,
            ManagedProviderName,
            SenderDomainVerified.ToString(CultureInfo.InvariantCulture),
            VerifiedSenderDomain,
            FromAddress,
            PublicAppBaseUrl,
            InvitationFooterText,
            Smtp.Host,
            Smtp.Port.ToString(CultureInfo.InvariantCulture),
            Smtp.EnableSsl.ToString(CultureInfo.InvariantCulture),
            Smtp.UserName,
            Smtp.Password,
            ProviderWebhookSecret,
            AwsSes.SnsTopicArn,
            AzureCommunicationServices.ConnectionString,
            AzureCommunicationServices.Endpoint,
            AzureCommunicationServices.AccessKey,
            AzureCommunicationServices.EventGridWebhookSecret));
        var blockers = readiness.Issues
            .Where(issue => issue.Severity == EmailDeliveryReadinessEvaluator.BlockingSeverity)
            .Select(issue => issue.Code)
            .ToArray();
        if (blockers.Length > 0)
        {
            throw new InvalidOperationException(
                $"Email delivery provider has blocking configuration issues: {string.Join(", ", blockers)}.");
        }
    }
}

public sealed class SmtpEmailDeliveryOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 25;

    public bool EnableSsl { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }
}

public sealed class AwsSesEmailDeliveryOptions
{
    public string? SnsTopicArn { get; set; }
}

public sealed class AzureCommunicationServicesEmailDeliveryOptions
{
    public string? ConnectionString { get; set; }

    public string? Endpoint { get; set; }

    public string? AccessKey { get; set; }

    public string? EventGridWebhookSecret { get; set; }

    public bool DisableUserEngagementTracking { get; set; } = true;
}
