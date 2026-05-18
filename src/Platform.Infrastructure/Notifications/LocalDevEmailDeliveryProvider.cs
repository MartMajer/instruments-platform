using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class LocalDevEmailDeliveryProvider : IEmailDeliveryProvider
{
    public string Provider => EmailDeliveryProviderNames.LocalDev;

    public Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryMessage message,
        CancellationToken cancellationToken)
    {
        var sentAt = DateTimeOffset.UtcNow;

        return Task.FromResult(new EmailDeliveryResult(
            Provider,
            $"{Provider}:{message.NotificationId:N}:{sentAt.ToUnixTimeMilliseconds()}",
            sentAt));
    }
}
