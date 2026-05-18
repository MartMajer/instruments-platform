namespace Platform.Application.Tenancy;

public sealed class CurrentTenant : ICurrentTenant
{
    private Guid? _tenantId;
    private string? _source;

    public bool HasTenant => _tenantId.HasValue;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException("Tenant context has not been resolved.");

    public string Source => _source
        ?? throw new InvalidOperationException("Tenant context has not been resolved.");

    public void SetTenant(Guid tenantId, string source)
    {
        if (_tenantId.HasValue)
        {
            if (_tenantId.Value != tenantId || _source != source)
            {
                throw new InvalidOperationException("Tenant context is immutable once resolved.");
            }

            return;
        }

        _tenantId = tenantId;
        _source = source;
    }
}
