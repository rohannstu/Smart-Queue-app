using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartQueue.Domain.Entities;
using SmartQueue.Infrastructure.Interfaces;

namespace SmartQueue.Infrastructure.Interceptors;

public sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantInterceptor(ITenantProvider tenantProvider) => _tenantProvider = tenantProvider;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                StampAudit(entry);
            }

            if (entry.State is EntityState.Added && entry.Entity is ITenantEntity tenantEntity)
            {
                StampTenantId(tenantEntity);
            }

            if (entry.State == EntityState.Deleted)
            {
                SoftDelete(entry);
            }
        }
    }

    private static void StampAudit(EntityEntry<BaseEntity> entry)
    {
        var now = DateTime.UtcNow;

        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = now;
        }

        entry.Entity.UpdatedAt = now;
    }

    private void StampTenantId(ITenantEntity entity)
    {
        if (_tenantProvider.IsSuperAdmin())
        {
            return;
        }

        if (entity.OrganizationId == Guid.Empty)
        {
            entity.OrganizationId = _tenantProvider.GetOrganizationId();
        }
    }

    private static void SoftDelete(EntityEntry<BaseEntity> entry)
    {
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAt = DateTime.UtcNow;
    }
}
