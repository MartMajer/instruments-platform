using System.Text;
using System.Text.Json;

namespace Platform.Domain.Outbox;

public static class OutboxPayload
{
    public const int MaxPayloadBytes = 64 * 1024;

    public static JsonDocument Create(IReadOnlyDictionary<string, object?> values)
    {
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(values);
        EnsureWithinLimit(payloadBytes.Length, nameof(values));

        return JsonDocument.Parse(payloadBytes);
    }

    public static void EnsureWithinLimit(JsonDocument payload, string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        EnsureWithinLimit(
            Encoding.UTF8.GetByteCount(payload.RootElement.GetRawText()),
            parameterName);
    }

    private static void EnsureWithinLimit(int payloadBytes, string? parameterName)
    {
        if (payloadBytes > MaxPayloadBytes)
        {
            throw new ArgumentException(
                $"Outbox payload exceeds the {MaxPayloadBytes} byte M1 limit. Store large data elsewhere and publish a pointer payload.",
                parameterName);
        }
    }
}
