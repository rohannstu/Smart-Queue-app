namespace SmartQueue.Domain.Entities;

public sealed class Organization : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
