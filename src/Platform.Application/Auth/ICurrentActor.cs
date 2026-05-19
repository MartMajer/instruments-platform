namespace Platform.Application.Auth;

public interface ICurrentActor
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? TenantId { get; }

    string? Email { get; }

    bool EmailVerificationRequired { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
