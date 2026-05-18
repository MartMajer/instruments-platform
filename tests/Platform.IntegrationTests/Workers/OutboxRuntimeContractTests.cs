using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.Domain.Outbox;
using Platform.Workers.Outbox;

namespace Platform.IntegrationTests.Workers;

public sealed class OutboxRuntimeContractTests
{
    [Fact]
    public void Dispatcher_rejects_unsafe_handler_event_type_values()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new OutboxEventDispatcher(
        [
            new RecordingOutboxEventHandler("InvitationEmailQueued:/r/inv_secret:ada@example.test")
        ]));

        Assert.Contains("unsafe_event_type", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handler_not_found_exception_sanitizes_event_type_in_message()
    {
        var exception = new OutboxEventHandlerNotFoundException(
            "InvitationEmailQueued:/r/inv_secret:ada@example.test");

        Assert.Contains("unsafe_event_type", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddOutboxRelayWorker_rejects_oversized_batch_size()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OutboxRelay:BatchSize"] = "501",
                ["OutboxRelay:PollIntervalSeconds"] = "1"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOutboxRelayWorker(configuration);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OutboxRelayWorkerOptions>>().Value);

        Assert.Contains("BatchSize", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OutboxRelay_rejects_oversized_batch_size_before_database_access()
    {
        var relay = new OutboxRelay(null!, null!, null!, null!);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => relay.ProcessDueAsync(501));

        Assert.Contains("500", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingOutboxEventHandler(string eventType) : IOutboxEventHandler
    {
        public string EventType { get; } = eventType;

        public Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
