namespace Platform.Application.Features.Auth.GetCurrentSession;

public sealed record GetCurrentSessionResponse(
    Guid UserId,
    Guid TenantId,
    string? Email,
    bool EmailVerificationRequired,
    string[] Permissions);
