using Platform.Domain.Responses;
using Platform.Domain.Campaigns;

namespace Platform.Infrastructure.Data.Interceptors;

internal static class AuditSnapshotRedactionPolicy
{
    public const string RedactedValue = "[redacted]";

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.Ordinal)
    {
        "AssignmentId",
        "CodeSalt",
        "ConsentRecordId",
        "DeliveryAttemptId",
        "Hash",
        "InviteTokenId",
        "IpHash",
        "NotificationId",
        "ParticipantCodeId",
        "ProviderEventId",
        "ProviderDeliveryKey",
        "ProviderMessageId",
        "PublicHandleHash",
        "Recipient",
        "RespondentSubjectId",
        "SubjectId",
        "TargetSubjectId",
        "TokenHash",
        "UserAgentHash"
    };

    public static object? Redact(Type entityType, string propertyName, object? value)
    {
        if (entityType == typeof(EmailSuppression) &&
            propertyName is nameof(EmailSuppression.Note) or nameof(EmailSuppression.ReleaseReason))
        {
            return RedactedValue;
        }

        if (entityType == typeof(NotificationDeliveryEvent) &&
            propertyName == nameof(NotificationDeliveryEvent.Reason))
        {
            return RedactedValue;
        }

        if (entityType == typeof(RespondentRule) && propertyName == nameof(RespondentRule.Rule))
        {
            return RedactedValue;
        }

        if (entityType == typeof(Answer) &&
            propertyName is nameof(Answer.Value) or nameof(Answer.Comment))
        {
            return RedactedValue;
        }

        return SensitivePropertyNames.Contains(propertyName)
            ? RedactedValue
            : value;
    }
}
