using System.Text.Json;

namespace Platform.Domain.Operations;

public sealed class OperationalNotification
{
    public const string ReportPdfArtifactTerminalNotificationType = "report_pdf_artifact_terminal";
    public const string WithdrawalRequestCreatedNotificationType = "withdrawal_request_created";
    public const string WithdrawalRequestTerminalNotificationType = "withdrawal_request_terminal";
    public const string SourceAggregateTypeExportArtifact = "export_artifact";
    public const string SourceAggregateTypeWithdrawalRequest = "withdrawal_request";
    public const string SourceEventTypeReportPdfArtifactTerminalStateReached = "ReportPdfArtifactTerminalStateReached";
    public const string SourceEventTypeWithdrawalRequestCreated = "WithdrawalRequestCreated";
    public const string SourceEventTypeWithdrawalRequestTerminal = "WithdrawalRequestTerminal";
    public const string SeverityInfo = "info";
    public const string SeverityWarning = "warning";
    public const string StatusUnread = "unread";
    public const string StatusRead = "read";
    public const int MaxFailureReasonCodeLength = 128;

    private OperationalNotification()
    {
    }

    public OperationalNotification(
        Guid id,
        Guid tenantId,
        string notificationType,
        string severity,
        string status,
        Guid sourceAggregateId,
        string sourceAggregateType,
        string sourceEventType,
        string payloadJson,
        DateTimeOffset? createdAt = null)
    {
        Id = id;
        TenantId = tenantId;
        NotificationType = NormalizeRequired(notificationType, nameof(notificationType));
        Severity = NormalizeKnownSeverity(severity, nameof(severity));
        Status = NormalizeKnownStatus(status, nameof(status));
        SourceAggregateId = sourceAggregateId;
        SourceAggregateType = NormalizeRequired(sourceAggregateType, nameof(sourceAggregateType));
        SourceEventType = NormalizeRequired(sourceEventType, nameof(sourceEventType));
        PayloadJson = RequireObject(payloadJson, nameof(payloadJson));
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string NotificationType { get; private set; } = string.Empty;

    public string Severity { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public Guid SourceAggregateId { get; private set; }

    public string SourceAggregateType { get; private set; } = string.Empty;

    public string SourceEventType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static OperationalNotification CreateReportPdfArtifactTerminal(
        Guid id,
        Guid tenantId,
        Guid exportArtifactId,
        string status,
        string payloadJson,
        DateTimeOffset? createdAt = null)
    {
        return new OperationalNotification(
            id,
            tenantId,
            ReportPdfArtifactTerminalNotificationType,
            string.Equals(status, "failed", StringComparison.Ordinal)
                ? SeverityWarning
                : SeverityInfo,
            StatusUnread,
            exportArtifactId,
            SourceAggregateTypeExportArtifact,
            SourceEventTypeReportPdfArtifactTerminalStateReached,
            payloadJson,
            createdAt);
    }

    public static OperationalNotification CreateWithdrawalRequestCreated(
        Guid id,
        Guid tenantId,
        Guid withdrawalRequestId,
        string payloadJson,
        DateTimeOffset? createdAt = null)
    {
        return new OperationalNotification(
            id,
            tenantId,
            WithdrawalRequestCreatedNotificationType,
            SeverityWarning,
            StatusUnread,
            withdrawalRequestId,
            SourceAggregateTypeWithdrawalRequest,
            SourceEventTypeWithdrawalRequestCreated,
            payloadJson,
            createdAt);
    }

    public static OperationalNotification CreateWithdrawalRequestTerminal(
        Guid id,
        Guid tenantId,
        Guid withdrawalRequestId,
        string status,
        string payloadJson,
        DateTimeOffset? createdAt = null)
    {
        return new OperationalNotification(
            id,
            tenantId,
            WithdrawalRequestTerminalNotificationType,
            string.Equals(status, "failed", StringComparison.Ordinal)
                ? SeverityWarning
                : SeverityInfo,
            StatusUnread,
            withdrawalRequestId,
            SourceAggregateTypeWithdrawalRequest,
            SourceEventTypeWithdrawalRequestTerminal,
            payloadJson,
            createdAt);
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string NormalizeKnownSeverity(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();

        return normalized is SeverityInfo or SeverityWarning
            ? normalized
            : throw new ArgumentException("Unknown operational notification severity.", parameterName);
    }

    private static string NormalizeKnownStatus(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();

        return normalized is StatusUnread or StatusRead
            ? normalized
            : throw new ArgumentException("Unknown operational notification status.", parameterName);
    }

    public void MarkRead(DateTimeOffset readAt)
    {
        if (Status == StatusRead)
        {
            return;
        }

        Status = StatusRead;
        ReadAt = readAt;
        UpdatedAt = readAt;
    }

    private static string RequireObject(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        using var payload = JsonDocument.Parse(value);
        if (payload.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Operational notification payload must be a JSON object.", parameterName);
        }

        return value;
    }
}
