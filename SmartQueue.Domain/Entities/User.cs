namespace SmartQueue.Domain.Entities;

public sealed class User : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = "Operator";
    public bool IsActive { get; set; } = true;
}
