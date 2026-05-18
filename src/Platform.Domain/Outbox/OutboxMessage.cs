using System.Text.Json;

namespace Platform.Domain.Outbox;

public sealed record OutboxMessage(
    Guid AggregateId,
    string AggregateType,
    string EventType,
    JsonDocument Payload);
