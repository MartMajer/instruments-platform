using Platform.Domain.Responses;

namespace Platform.Infrastructure.Data.Interceptors;

internal static class AuditSnapshotRedactionPolicy
{
    public const string RedactedValue = "[redacted]";

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.Ordinal)
    {
        "AssignmentId",
        "CodeSalt",
        "ConsentRecordId",
        "Hash",
        "InviteTokenId",
        "IpHash",
        "ParticipantCodeId",
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
