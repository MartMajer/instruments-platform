namespace Platform.Domain.Templates;

public sealed class SurveyTemplate
{
    private SurveyTemplate()
    {
    }

    private SurveyTemplate(
        Guid id,
        Guid? tenantId,
        string name,
        string? description,
        Guid? workspaceId,
        Guid? createdBy)
    {
        Id = id;
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static SurveyTemplate CreateGlobal(Guid id, string name, string? description = null)
    {
        return new SurveyTemplate(id, tenantId: null, name, description, workspaceId: null, createdBy: null);
    }

    public static SurveyTemplate CreateTenant(
        Guid id,
        Guid tenantId,
        string name,
        string? description = null,
        Guid? workspaceId = null,
        Guid? createdBy = null)
    {
        return new SurveyTemplate(id, tenantId, name, description, workspaceId, createdBy);
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
