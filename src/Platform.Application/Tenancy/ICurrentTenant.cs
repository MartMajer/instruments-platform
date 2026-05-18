namespace Platform.Application.Tenancy;

public interface ICurrentTenant
{
    bool HasTenant { get; }

    Guid TenantId { get; }

    string Source { get; }

    void SetTenant(Guid tenantId, string source);
}
