namespace Keepwise.Domain.Entities;

public sealed class AuditEvent : AuditedEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Metadata { get; set; }
}
