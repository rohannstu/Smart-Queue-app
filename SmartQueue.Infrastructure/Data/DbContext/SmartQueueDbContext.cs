using Microsoft.EntityFrameworkCore;
using SmartQueue.Domain.Entities;
using SmartQueue.Infrastructure.Interfaces;
using SmartQueue.Infrastructure.Interceptors;

namespace SmartQueue.Infrastructure.Data.Context;

public sealed class SmartQueueDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    public TenantInterceptor TenantInterceptor { get; }

    public SmartQueueDbContext(DbContextOptions<SmartQueueDbContext> options, ITenantProvider tenantProvider, TenantInterceptor tenantInterceptor)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        TenantInterceptor = tenantInterceptor;
    }

    public DbSet<Organization> Organizations { get; set; } = null!;

    public Guid CurrentOrganizationId => _tenantProvider.GetOrganizationId();
    public bool CurrentUserIsSuperAdmin => _tenantProvider.IsSuperAdmin();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            return;
        }

        optionsBuilder.AddInterceptors(TenantInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartQueueDbContext).Assembly);
        ConfigureQueryFilters(modelBuilder);
    }

    private void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(SmartQueueDbContext)
                .GetMethod(nameof(ApplyBaseEntityFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var genericMethod = method.MakeGenericMethod(entityType.ClrType);
            genericMethod.Invoke(this, new object[] { modelBuilder });
        }
    }

    private void ApplyBaseEntityFilters<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (CurrentUserIsSuperAdmin || !typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)) || EF.Property<Guid>(e, nameof(ITenantEntity.OrganizationId)) == CurrentOrganizationId));
    }
}
