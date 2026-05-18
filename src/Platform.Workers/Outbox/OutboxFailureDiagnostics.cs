using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

internal static class OutboxFailureDiagnostics
{
    public static string DispatchFailed(OutboxEvent outboxEvent, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            " ",
            "DISPATCH_FAILED",
            $"event_type={OutboxTextSafety.SafeIdentifierForDiagnostics(outboxEvent.EventType)}",
            $"aggregate_type={OutboxTextSafety.SafeIdentifierForDiagnostics(outboxEvent.AggregateType)}",
            $"exception_type={OutboxTextSafety.SafeIdentifierForDiagnostics(exception.GetType().Name)}");
    }
}
