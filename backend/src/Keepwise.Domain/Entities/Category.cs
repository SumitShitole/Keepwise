namespace Keepwise.Domain.Entities;

public sealed class Category : AuditedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<ItemType> ItemTypes { get; set; } = new List<ItemType>();
}

public sealed class ItemType : AuditedEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
