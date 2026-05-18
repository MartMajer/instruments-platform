using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Notifications;
using Platform.Domain.Outbox;
using Platform.SharedKernel;
using Platform.Workers.Outbox;

namespace Platform.IntegrationTests.Workers;

public sealed class OutboxEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_routes_matching_event_type_to_registered_handler()
    {
        var handler = new RecordingOutboxEventHandler("InvitationEmailQueued");
        var dispatcher = new OutboxEventDispatcher([handler]);
        var outboxEvent = CreateOutboxEvent("InvitationEmailQueued");

        await dispatcher.DispatchAsync(outboxEvent);

        Assert.Equal([outboxEvent.Id], handler.HandledEventIds);
    }

    [Fact]
    public async Task DispatchAsync_throws_when_event_type_has_no_registered_handler()
    {
        var dispatcher = new OutboxEventDispatcher([new RecordingOutboxEventHandler("InvitationEmailQueued")]);
        var outboxEvent = CreateOutboxEvent("UnknownEvent");

        var exception = await Assert.ThrowsAsync<OutboxEventHandlerNotFoundException>(
            () => dispatcher.DispatchAsync(outboxEvent));

        Assert.Equal("UnknownEvent", exception.EventType);
        Assert.Contains("No outbox event handler registered for event type 'UnknownEvent'.", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_duplicate_event_type_handlers()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new OutboxEventDispatcher(
        [
            new RecordingOutboxEventHandler("InvitationEmailQueued"),
            new RecordingOutboxEventHandler("InvitationEmailQueued")
        ]));

        Assert.Contains(
            "Duplicate outbox event handler registered for event type 'InvitationEmailQueued'.",
            exception.Message);
    }

    [Fact]
    public async Task InvitationEmailQueued_handler_is_explicit_known_noop_handler()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateOutboxEvent("InvitationEmailQueued");

        await handler.HandleAsync(outboxEvent);

        Assert.Equal("InvitationEmailQueued", handler.EventType);
    }

    [Fact]
    public async Task AddOutboxDispatching_registers_dispatcher_and_known_m1_handlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOperationalNotificationStore, RecordingOperationalNotificationStore>();
        services.AddOutboxDispatching();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxEventDispatcher>();

        await dispatcher.DispatchAsync(CreateOutboxEvent("InvitationEmailQueued"));
        await dispatcher.DispatchAsync(CreateReportPdfArtifactTerminalStateReachedEvent());
        await Assert.ThrowsAsync<OutboxEventHandlerNotFoundException>(
            () => dispatcher.DispatchAsync(CreateOutboxEvent("UnknownEvent")));
    }

    private static OutboxEvent CreateOutboxEvent(string eventType)
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            eventType,
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["notification_id"] = Guid.NewGuid()
            }),
            null);
    }

    private static OutboxEvent CreateReportPdfArtifactTerminalStateReachedEvent()
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "export_artifact",
            "ReportPdfArtifactTerminalStateReached",
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["export_artifact_id"] = Guid.NewGuid(),
                ["campaign_series_id"] = Guid.NewGuid(),
                ["artifact_type"] = "campaign_series_report_pdf",
                ["target_kind"] = "campaign_series",
                ["format"] = "pdf",
                ["status"] = "succeeded",
                ["failure_reason_code"] = null
            }),
            null);
    }

    private sealed class RecordingOutboxEventHandler(string eventType) : IOutboxEventHandler
    {
        private readonly List<Guid> _handledEventIds = [];

        public string EventType { get; } = eventType;

        public IReadOnlyList<Guid> HandledEventIds => _handledEventIds;

        public Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            _handledEventIds.Add(outboxEvent.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOperationalNotificationStore : IOperationalNotificationStore
    {
        public Task<Result<OperationalNotificationResponse>> RecordReportPdfArtifactTerminalStateAsync(
            Guid tenantId,
            Guid exportArtifactId,
            Guid campaignSeriesId,
            string status,
            string? failureReasonCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new OperationalNotificationResponse(
                Guid.NewGuid(),
                "report_pdf_artifact_terminal",
                status == "failed" ? "warning" : "info",
                "unread",
                exportArtifactId,
                "ReportPdfArtifactTerminalStateReached",
                DateTimeOffset.UtcNow)));
        }

        public Task<Result<ListOperationalNotificationsResponse>> ListOperationalNotificationsAsync(
            Guid tenantId,
            int limit,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<OperationalNotificationSummaryResponse>> GetOperationalNotificationSummaryAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<OperationalNotificationResponse>> MarkOperationalNotificationReadAsync(
            Guid tenantId,
            Guid notificationId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<MarkAllOperationalNotificationsReadResponse>> MarkAllOperationalNotificationsReadAsync(
            Guid tenantId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
