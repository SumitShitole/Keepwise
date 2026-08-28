namespace Keepwise.Domain.Entities;

public sealed class Attachment : SoftDeletableEntity
{
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public AttachmentOwnerType OwnerType { get; set; } = AttachmentOwnerType.Item;
    public Guid? OwnerId { get; set; }
}
