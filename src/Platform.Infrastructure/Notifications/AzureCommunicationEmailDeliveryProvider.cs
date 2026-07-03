using System.Net.Mail;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class AzureCommunicationEmailDeliveryProvider(
    IOptions<EmailDeliveryOptions> options,
    IAzureCommunicationEmailSender sender) : IEmailDeliveryProvider
{
    public string Provider => EmailDeliveryProviderNames.AzureCommunicationEmail;

    public async Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        var deliveryOptions = options.Value;
        deliveryOptions.EnsureValidProviderConfiguration();

        var fromAddress = CreateSafeMailAddress(
            deliveryOptions.FromAddress,
            requireAddressOnly: false,
            invalidMessage: "Email delivery sender address is invalid.");
        var recipientAddress = CreateSafeMailAddress(
            message.Recipient,
            requireAddressOnly: true,
            invalidMessage: "Email delivery recipient address is invalid.");

        if (!IsSafeSubject(message.Subject))
        {
            throw new InvalidOperationException("Email delivery subject is invalid.");
        }

        if (!IsSafeBodyText(message.BodyText))
        {
            throw new InvalidOperationException("Email delivery body is invalid.");
        }

        if (!IsSafeHeaderValue(message.DeliveryAttemptKey))
        {
            throw new InvalidOperationException("Email delivery correlation key is invalid.");
        }

        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Platform-Delivery-Key"] = message.DeliveryAttemptKey
        };
        if (IsSafeListUnsubscribeUrl(message.UnsubscribeUrl))
        {
            headers["List-Unsubscribe"] = $"<{message.UnsubscribeUrl!.Trim()}>";
        }

        var sendResult = await sender.SendAsync(
            new AzureCommunicationEmailSendRequest(
                fromAddress.Address,
                recipientAddress.Address,
                message.Subject,
                message.BodyText,
                headers,
                deliveryOptions.AzureCommunicationServices.DisableUserEngagementTracking),
            cancellationToken);

        return new EmailDeliveryResult(
            Provider,
            sendResult.MessageId,
            sendResult.AcceptedAt);
    }

    private static bool IsSafeListUnsubscribeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != value.Trim().Length ||
            value.Any(char.IsControl))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrWhiteSpace(uri.Fragment);
    }

    private static bool IsSafeHeaderValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length == value.Trim().Length &&
            value.Length <= 500 &&
            !value.Any(char.IsControl);
    }

    private static bool IsSafeSubject(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length == value.Trim().Length &&
            value.Length <= 200 &&
            !value.Any(char.IsControl);
    }

    private static bool IsSafeBodyText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 10000 &&
            !value.Any(character => char.IsControl(character) &&
                character is not '\r' and not '\n' and not '\t');
    }

    private static MailAddress CreateSafeMailAddress(
        string? value,
        bool requireAddressOnly,
        string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != value.Trim().Length ||
            value.Any(char.IsControl))
        {
            throw new InvalidOperationException(invalidMessage);
        }

        MailAddress address;
        try
        {
            address = new MailAddress(value);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(invalidMessage);
        }

        if (string.IsNullOrWhiteSpace(address.Address) ||
            address.Address.Any(char.IsControl) ||
            (requireAddressOnly && !string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(invalidMessage);
        }

        return address;
    }
}

public interface IAzureCommunicationEmailSender
{
    Task<AzureCommunicationEmailSendResult> SendAsync(
        AzureCommunicationEmailSendRequest request,
        CancellationToken cancellationToken);
}

public sealed record AzureCommunicationEmailSendRequest(
    string SenderAddress,
    string RecipientAddress,
    string Subject,
    string BodyText,
    IReadOnlyDictionary<string, string> Headers,
    bool DisableUserEngagementTracking);

public sealed record AzureCommunicationEmailSendResult(
    string MessageId,
    DateTimeOffset AcceptedAt);

public sealed class AzureCommunicationEmailSdkSender(
    IOptions<EmailDeliveryOptions> options) : IAzureCommunicationEmailSender
{
    public async Task<AzureCommunicationEmailSendResult> SendAsync(
        AzureCommunicationEmailSendRequest request,
        CancellationToken cancellationToken)
    {
        var client = CreateClient(options.Value.AzureCommunicationServices);
        var content = new EmailContent(request.Subject)
        {
            PlainText = request.BodyText
        };
        var recipients = new EmailRecipients(
        [
            new EmailAddress(request.RecipientAddress)
        ]);
        var message = new EmailMessage(request.SenderAddress, recipients, content)
        {
            UserEngagementTrackingDisabled = request.DisableUserEngagementTracking
        };
        foreach (var (name, value) in request.Headers)
        {
            message.Headers.Add(name, value);
        }

        var operation = await client.SendAsync(
            WaitUntil.Started,
            message,
            cancellationToken);

        return new AzureCommunicationEmailSendResult(
            operation.Id,
            DateTimeOffset.UtcNow);
    }

    private static EmailClient CreateClient(AzureCommunicationServicesEmailDeliveryOptions options)
    {
        var clientOptions = new EmailClientOptions(EmailClientOptions.ServiceVersion.V2025_09_01);
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new EmailClient(options.ConnectionString.Trim(), clientOptions);
        }

        return new EmailClient(
            new Uri(options.Endpoint!.Trim(), UriKind.Absolute),
            new AzureKeyCredential(options.AccessKey!.Trim()),
            clientOptions);
    }
}
