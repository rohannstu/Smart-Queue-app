namespace SmartQueue.Infrastructure.Interfaces;

public interface ITenantProvider
{
    Guid GetOrganizationId();
    string GetSlug();
    bool IsSuperAdmin();
}
