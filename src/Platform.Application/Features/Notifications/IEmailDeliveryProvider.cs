namespace Platform.Application.Features.Notifications;

public interface IEmailDeliveryProvider
{
    string Provider { get; }

    Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryMessage message,
        CancellationToken cancellationToken);
}
