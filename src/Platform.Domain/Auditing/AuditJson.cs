using System.Text.Json;

namespace Platform.Domain.Auditing;

public static class AuditJson
{
    public static JsonDocument Create(IReadOnlyDictionary<string, object?> values)
    {
        return JsonSerializer.SerializeToDocument(values);
    }
}
