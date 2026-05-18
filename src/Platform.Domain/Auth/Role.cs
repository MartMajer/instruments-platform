namespace Platform.Domain.Auth;

public sealed class Role
{
    private Role()
    {
    }

    public Role(Guid id, Guid? tenantId, string code, string name)
    {
        Id = id;
        TenantId = tenantId;
        Code = code;
        Name = name;
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
}
