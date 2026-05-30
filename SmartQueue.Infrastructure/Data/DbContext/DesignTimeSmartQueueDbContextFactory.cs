using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartQueue.Infrastructure.Interceptors;
using SmartQueue.Infrastructure.Services;
using SmartQueue.Infrastructure.Interfaces;

namespace SmartQueue.Infrastructure.Data.Context;

public sealed class DesignTimeSmartQueueDbContextFactory : IDesignTimeDbContextFactory<SmartQueueDbContext>
{
    public SmartQueueDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTQUEUE_DEFAULT_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=smartqueue;Username=postgres;Password=postgres;";

        var optionsBuilder = new DbContextOptionsBuilder<SmartQueueDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var tenantProvider = new DesignTimeTenantProvider();
        var tenantInterceptor = new TenantInterceptor(tenantProvider);

        return new SmartQueueDbContext(optionsBuilder.Options, tenantProvider, tenantInterceptor);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid GetOrganizationId() => Guid.Empty;
        public string GetSlug() => string.Empty;
        public bool IsSuperAdmin() => true;
    }
}
