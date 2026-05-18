using Microsoft.Extensions.Options;
using Platform.Application.Features.System.GetHealth;

namespace Platform.Infrastructure.Notifications;

public sealed class EmailDeliveryConfigurationHealthCheck(
    IOptions<EmailDeliveryOptions> options) : IPlatformHealthCheck
{
    public string Name => "email_delivery_configuration";

    public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            options.Value.EnsureValidProviderConfiguration();
            return Task.FromResult(PlatformHealthCheckResult.Ok(Name));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(PlatformHealthCheckResult.Unready(Name));
        }
    }
}
