using Microsoft.AspNetCore.Http;
using SmartQueue.Infrastructure.Interfaces;

namespace SmartQueue.Infrastructure.Services;

public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Guid GetOrganizationId()
    {
        var orgClaim = GetClaim("org");
        return Guid.TryParse(orgClaim, out var organizationId) ? organizationId : Guid.Empty;
    }

    public string GetSlug() => GetClaim("slug") ?? string.Empty;

    public bool IsSuperAdmin()
    {
        var role = GetClaim("role");
        return string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetClaim(string type)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst(type)?.Value;
    }
}
