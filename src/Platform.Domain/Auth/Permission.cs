namespace Platform.Domain.Auth;

public sealed class Permission
{
    private Permission()
    {
    }

    public Permission(Guid id, string code)
    {
        Id = id;
        Code = code;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;
}
