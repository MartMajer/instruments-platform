using Microsoft.Extensions.DependencyInjection;

namespace Platform.Workers.Outbox;

public static class OutboxDispatchingServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxDispatching(this IServiceCollection services)
    {
        services.AddScoped<IOutboxEventHandler, InvitationEmailQueuedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, ReportPdfArtifactTerminalStateReachedOutboxHandler>();
        services.AddScoped<IOutboxEventDispatcher, OutboxEventDispatcher>();

        return services;
    }
}
