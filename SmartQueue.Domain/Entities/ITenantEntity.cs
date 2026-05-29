namespace SmartQueue.Domain.Entities;

public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
