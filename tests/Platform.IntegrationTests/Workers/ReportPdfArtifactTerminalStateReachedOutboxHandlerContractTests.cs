using Platform.Domain.Outbox;
using Platform.Domain.Reports;
using Platform.Application.Features.Notifications;
using Platform.SharedKernel;
using Platform.Workers.Outbox;

namespace Platform.IntegrationTests.Workers;

public sealed class ReportPdfArtifactTerminalStateReachedOutboxHandlerContractTests
{
    [Fact]
    public async Task Handler_rejects_direct_calls_for_other_event_types()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var outboxEvent = CreateEvent("OtherEvent", ValidPayload());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("ReportPdfArtifactTerminalStateReached", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_schema_version()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload.Remove("schema_version");
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("schema_version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_rejects_unsupported_schema_version()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload["schema_version"] = 2;
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("schema_version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_export_artifact_id()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload.Remove("export_artifact_id");
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("export_artifact_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_campaign_series_id()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload.Remove("campaign_series_id");
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("campaign_series_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_campaign_series_report_pdf_artifact_type()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload["artifact_type"] = ExportArtifactTypes.CampaignSeriesReportHtml;
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("artifact_type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_terminal_status()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload["status"] = ExportArtifactStatuses.Rendering;
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("status", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_rejects_failed_status_without_safe_failure_reason()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload["status"] = ExportArtifactStatuses.Failed;
        payload["failure_reason_code"] = "/r/wdr_sensitive_token";
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("failure_reason_code", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("wdr_", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handler_rejects_sensitive_payload_values()
    {
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(new RecordingOperationalNotificationStore());
        var payload = ValidPayload();
        payload["storage_key"] = "tenants/abc/reports/wdr_sensitive_token.pdf";
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("payload", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wdr_", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenants/abc", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handler_records_succeeded_operational_notification()
    {
        var store = new RecordingOperationalNotificationStore();
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(store);
        var payload = ValidPayload();
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        await handler.HandleAsync(outboxEvent);

        var recorded = Assert.Single(store.Recorded);
        Assert.Equal(outboxEvent.TenantId, recorded.TenantId);
        Assert.Equal((Guid)payload["export_artifact_id"]!, recorded.ExportArtifactId);
        Assert.Equal((Guid)payload["campaign_series_id"]!, recorded.CampaignSeriesId);
        Assert.Equal(ExportArtifactStatuses.Succeeded, recorded.Status);
        Assert.Null(recorded.FailureReasonCode);
    }

    [Fact]
    public async Task Handler_records_failed_operational_notification_with_safe_reason()
    {
        var store = new RecordingOperationalNotificationStore();
        var handler = new ReportPdfArtifactTerminalStateReachedOutboxHandler(store);
        var payload = ValidPayload();
        payload["status"] = ExportArtifactStatuses.Failed;
        payload["failure_reason_code"] = "export_artifact.object_store_unavailable";
        var outboxEvent = CreateEvent(
            ReportPdfArtifactTerminalStateReachedOutboxHandler.EventTypeName,
            payload);

        await handler.HandleAsync(outboxEvent);

        var recorded = Assert.Single(store.Recorded);
        Assert.Equal(ExportArtifactStatuses.Failed, recorded.Status);
        Assert.Equal("export_artifact.object_store_unavailable", recorded.FailureReasonCode);
    }

    private static OutboxEvent CreateEvent(
        string eventType,
        IReadOnlyDictionary<string, object?> payload)
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "export_artifact",
            eventType,
            OutboxPayload.Create(payload),
            correlationId: null);
    }

    private static Dictionary<string, object?> ValidPayload()
    {
        return new Dictionary<string, object?>
        {
            ["schema_version"] = 1,
            ["export_artifact_id"] = Guid.NewGuid(),
            ["campaign_series_id"] = Guid.NewGuid(),
            ["artifact_type"] = ExportArtifactTypes.CampaignSeriesReportPdf,
            ["target_kind"] = ExportArtifactTargetKinds.CampaignSeries,
            ["format"] = ExportArtifactFormats.Pdf,
            ["status"] = ExportArtifactStatuses.Succeeded,
            ["failure_reason_code"] = null
        };
    }

    private sealed class RecordingOperationalNotificationStore : IOperationalNotificationStore
    {
        public List<RecordCall> Recorded { get; } = [];

        public Task<Result<OperationalNotificationResponse>> RecordReportPdfArtifactTerminalStateAsync(
            Guid tenantId,
            Guid exportArtifactId,
            Guid campaignSeriesId,
            string status,
            string? failureReasonCode,
            CancellationToken cancellationToken)
        {
            Recorded.Add(new RecordCall(
                tenantId,
                exportArtifactId,
                campaignSeriesId,
                status,
                failureReasonCode));

            return Task.FromResult(Result.Success(new OperationalNotificationResponse(
                Guid.NewGuid(),
                "report_pdf_artifact_terminal",
                status == ExportArtifactStatuses.Failed ? "warning" : "info",
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

    private sealed record RecordCall(
        Guid TenantId,
        Guid ExportArtifactId,
        Guid CampaignSeriesId,
        string Status,
        string? FailureReasonCode);
}
