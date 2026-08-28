namespace Keepwise.Domain.Entities;

public abstract class AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public uint RowVersion { get; set; }
}

public abstract class SoftDeletableEntity : AuditedEntity
{
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted => DeletedAtUtc is not null;
}
