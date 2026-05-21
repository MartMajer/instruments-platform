using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class AwsSnsSubscriptionConfirmer(HttpClient httpClient) : IAwsSnsSubscriptionConfirmer
{
    public async Task<bool> ConfirmAsync(
        string subscribeUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(subscribeUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        using var response = await httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }
}
