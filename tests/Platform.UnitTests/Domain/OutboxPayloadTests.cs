using System.Text.Json;
using Platform.Domain.Outbox;

namespace Platform.UnitTests.Domain;

public sealed class OutboxPayloadTests
{
    [Fact]
    public void Payload_cap_is_64_kib_for_m1_outbox_events()
    {
        Assert.Equal(64 * 1024, OutboxPayload.MaxPayloadBytes);
    }

    [Fact]
    public void Create_accepts_payload_under_cap_and_preserves_json()
    {
        using var payload = OutboxPayload.Create(new Dictionary<string, object?>
        {
            ["schema_version"] = 1,
            ["notification_id"] = Guid.Parse("018f1f7d-1b2c-7a00-9a20-c4cc00000001")
        });

        Assert.Equal(1, payload.RootElement.GetProperty("schema_version").GetInt32());
        Assert.Equal(
            "018f1f7d-1b2c-7a00-9a20-c4cc00000001",
            payload.RootElement.GetProperty("notification_id").GetGuid().ToString());
    }

    [Fact]
    public void Create_rejects_payload_over_cap_without_truncation()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["body"] = new string('x', OutboxPayload.MaxPayloadBytes)
            }));

        Assert.Contains("outbox payload", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(OutboxPayload.MaxPayloadBytes.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Outbox_event_create_rejects_oversized_json_document_payloads()
    {
        using var oversizedPayload = JsonDocument.Parse(
            $$"""{"schema_version":1,"body":"{{new string('x', OutboxPayload.MaxPayloadBytes)}}"}""");

        Assert.Throws<ArgumentException>(() => OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "response_session",
            "ResponseSessionSubmitted",
            oversizedPayload,
            correlationId: null));
    }

    [Fact]
    public void Mark_failed_still_truncates_last_error_independently_of_payload_cap()
    {
        using var payload = OutboxPayload.Create(new Dictionary<string, object?>
        {
            ["schema_version"] = 1
        });
        var outboxEvent = OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            "InvitationEmailQueued",
            payload,
            correlationId: null);

        outboxEvent.MarkFailed(new string('e', OutboxEvent.MaxLastErrorLength + 1), DateTimeOffset.UtcNow);

        Assert.NotNull(outboxEvent.LastError);
        Assert.Equal(OutboxEvent.MaxLastErrorLength, outboxEvent.LastError.Length);
        Assert.DoesNotContain(OutboxEvent.DeadLetterPrefix, outboxEvent.LastError, StringComparison.Ordinal);
    }
}
