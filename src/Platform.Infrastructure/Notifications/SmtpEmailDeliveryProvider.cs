using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class SmtpEmailDeliveryProvider(
    IOptions<EmailDeliveryOptions> options) : IEmailDeliveryProvider
{
    public string Provider => EmailDeliveryProviderNames.Smtp;

    public async Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        var deliveryOptions = options.Value;
        deliveryOptions.EnsureValidProviderConfiguration();

        using var smtp = new SmtpClient(deliveryOptions.Smtp.Host!, deliveryOptions.Smtp.Port)
        {
            EnableSsl = deliveryOptions.Smtp.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(deliveryOptions.Smtp.UserName))
        {
            smtp.Credentials = new NetworkCredential(
                deliveryOptions.Smtp.UserName,
                deliveryOptions.Smtp.Password);
        }

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

        using var mailMessage = new MailMessage(fromAddress, recipientAddress)
        {
            Subject = message.Subject,
            Body = message.BodyText,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8
        };
        if (!IsSafeHeaderValue(message.DeliveryAttemptKey))
        {
            throw new InvalidOperationException("Email delivery correlation key is invalid.");
        }

        mailMessage.Headers.Add("X-Platform-Delivery-Key", message.DeliveryAttemptKey);
        if (IsSafeListUnsubscribeUrl(message.UnsubscribeUrl))
        {
            mailMessage.Headers.Add("List-Unsubscribe", $"<{message.UnsubscribeUrl!.Trim()}>");
        }

        await smtp.SendMailAsync(mailMessage, cancellationToken);

        var sentAt = DateTimeOffset.UtcNow;
        return new EmailDeliveryResult(
            Provider,
            ProviderMessageId: null,
            sentAt);
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
