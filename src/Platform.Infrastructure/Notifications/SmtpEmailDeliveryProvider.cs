using System.Net;
using System.Net.Mail;
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

        using var mailMessage = new MailMessage(
            deliveryOptions.FromAddress!,
            message.Recipient,
            message.Subject,
            message.BodyText);

        await smtp.SendMailAsync(mailMessage, cancellationToken);

        var sentAt = DateTimeOffset.UtcNow;
        return new EmailDeliveryResult(
            Provider,
            $"{Provider}:{message.NotificationId:N}:{sentAt.ToUnixTimeMilliseconds()}",
            sentAt);
    }
}
