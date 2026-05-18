namespace Platform.Domain.Subjects;

public sealed class SubjectGroup
{
    private SubjectGroup()
    {
    }

    public SubjectGroup(
        Guid id,
        Guid tenantId,
        string type,
        string name,
        Guid? workspaceId = null,
        Guid? parentGroupId = null,
        string attributes = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        Type = type;
        Name = name;
        ParentGroupId = parentGroupId;
        Attributes = SubjectJson.RequireObject(attributes, nameof(attributes));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? WorkspaceId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid? ParentGroupId { get; private set; }

    public string Attributes { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
}
