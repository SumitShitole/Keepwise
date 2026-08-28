namespace Keepwise.Domain.Entities;

public sealed class Item : SoftDeletableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? ItemTypeId { get; set; }
    public ItemType? ItemType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string Currency { get; set; } = "INR";
    public string? VendorName { get; set; }
    public string? VendorContact { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }

    public ICollection<Coverage> Coverages { get; set; } = new List<Coverage>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
