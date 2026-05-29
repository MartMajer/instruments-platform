namespace Platform.Domain.DirectoryImports;

public sealed class DirectoryImportRule
{
    private DirectoryImportRule()
    {
    }

    public DirectoryImportRule(
        Guid id,
        Guid tenantId,
        Guid connectionId,
        string name,
        string criteriaJson,
        string fieldSelectionJson,
        bool mirrorMode = false,
        DateTimeOffset? mirrorConfirmedAt = null)
    {
        if (mirrorMode && !mirrorConfirmedAt.HasValue)
        {
            throw new ArgumentException("Mirror mode requires explicit confirmation.", nameof(mirrorConfirmedAt));
        }

        Id = id;
        TenantId = RequireNonEmpty(tenantId, nameof(tenantId));
        ConnectionId = RequireNonEmpty(connectionId, nameof(connectionId));
        Name = NormalizeRequired(name, nameof(name));
        CriteriaJson = DirectoryImportJson.RequireObject(criteriaJson, nameof(criteriaJson));
        FieldSelectionJson = DirectoryImportJson.RequireObject(fieldSelectionJson, nameof(fieldSelectionJson));
        MirrorMode = mirrorMode;
        MirrorConfirmedAt = mirrorConfirmedAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ConnectionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string CriteriaJson { get; private set; } = "{}";

    public string FieldSelectionJson { get; private set; } = "{}";

    public bool MirrorMode { get; private set; }

    public DateTimeOffset? MirrorConfirmedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void EnableMirrorMode(DateTimeOffset confirmedAt)
    {
        MirrorMode = true;
        MirrorConfirmedAt = confirmedAt;
        UpdatedAt = confirmedAt;
    }

    public void ReplaceCriteria(string criteriaJson, string fieldSelectionJson, DateTimeOffset updatedAt)
    {
        CriteriaJson = DirectoryImportJson.RequireObject(criteriaJson, nameof(criteriaJson));
        FieldSelectionJson = DirectoryImportJson.RequireObject(fieldSelectionJson, nameof(fieldSelectionJson));
        UpdatedAt = updatedAt;
    }

    private static Guid RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value must not be an empty GUID.", parameterName);
        }

        return value;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
