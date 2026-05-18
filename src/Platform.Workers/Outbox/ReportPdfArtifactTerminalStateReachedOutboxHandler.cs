using System.Text.Json;
using Platform.Application.Features.Notifications;
using Platform.Domain.Outbox;
using Platform.Domain.Reports;

namespace Platform.Workers.Outbox;

public sealed class ReportPdfArtifactTerminalStateReachedOutboxHandler(
    IOperationalNotificationStore operationalNotificationStore) : IOutboxEventHandler
{
    public const string EventTypeName = "ReportPdfArtifactTerminalStateReached";

    public string EventType => EventTypeName;

    public async Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);
        var payload = ValidateReportPdfArtifactTerminalStateReachedIntent(outboxEvent);

        var recorded = await operationalNotificationStore.RecordReportPdfArtifactTerminalStateAsync(
            outboxEvent.TenantId,
            payload.ExportArtifactId,
            payload.CampaignSeriesId,
            payload.Status,
            payload.FailureReasonCode,
            cancellationToken);

        if (recorded.IsFailure)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} operational notification routing failed with code '{OutboxTextSafety.SafeIdentifierForDiagnostics(recorded.Error.Code)}'.");
        }
    }

    private static ReportPdfArtifactTerminalStateReachedPayload ValidateReportPdfArtifactTerminalStateReachedIntent(
        OutboxEvent outboxEvent)
    {
        if (outboxEvent.EventType != EventTypeName)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} handler cannot process event type '{OutboxTextSafety.SafeIdentifierForDiagnostics(outboxEvent.EventType)}'.");
        }

        var root = outboxEvent.Payload.RootElement;
        RequireSchemaVersion(root);
        var exportArtifactId = RequireGuid(root, "export_artifact_id");
        var campaignSeriesId = RequireGuid(root, "campaign_series_id");
        RequireStringValue(root, "artifact_type", ExportArtifactTypes.CampaignSeriesReportPdf);
        RequireStringValue(root, "target_kind", ExportArtifactTargetKinds.CampaignSeries);
        RequireStringValue(root, "format", ExportArtifactFormats.Pdf);
        var status = RequireTerminalStatus(root);
        var failureReasonCode = ValidateFailureReason(root, status);

        if (OutboxTextSafety.ContainsSensitiveValue(root.GetRawText()))
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload contains unsafe report artifact completion values.");
        }

        return new ReportPdfArtifactTerminalStateReachedPayload(
            exportArtifactId,
            campaignSeriesId,
            status,
            failureReasonCode);
    }

    private static void RequireSchemaVersion(JsonElement root)
    {
        if (!root.TryGetProperty("schema_version", out var schemaVersion) ||
            schemaVersion.ValueKind != JsonValueKind.Number ||
            schemaVersion.GetInt32() != 1)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare schema_version 1.");
        }
    }

    private static Guid RequireGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(value.GetString(), out var parsed))
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare {propertyName} as a GUID.");
        }

        return parsed;
    }

    private static void RequireStringValue(JsonElement root, string propertyName, string expectedValue)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            !string.Equals(value.GetString(), expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare {propertyName} '{expectedValue}'.");
        }
    }

    private static string RequireTerminalStatus(JsonElement root)
    {
        if (!root.TryGetProperty("status", out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare terminal status.");
        }

        var status = value.GetString();
        if (status is not (ExportArtifactStatuses.Succeeded or ExportArtifactStatuses.Failed))
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload status must be succeeded or failed.");
        }

        return status;
    }

    private static string? ValidateFailureReason(JsonElement root, string status)
    {
        if (status == ExportArtifactStatuses.Succeeded)
        {
            if (root.TryGetProperty("failure_reason_code", out var succeededFailureReason) &&
                succeededFailureReason.ValueKind != JsonValueKind.Null)
            {
                throw new InvalidOperationException(
                    $"{EventTypeName} payload must not include failure_reason_code for succeeded artifacts.");
            }

            return null;
        }

        if (!root.TryGetProperty("failure_reason_code", out var failureReason) ||
            failureReason.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare failure_reason_code for failed artifacts.");
        }

        try
        {
            OutboxTextSafety.EnsureSafeIdentifier(failureReason.GetString(), "failure_reason_code");
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} payload must declare a safe failure_reason_code.",
                exception);
        }

        return failureReason.GetString();
    }

    private sealed record ReportPdfArtifactTerminalStateReachedPayload(
        Guid ExportArtifactId,
        Guid CampaignSeriesId,
        string Status,
        string? FailureReasonCode);
}
