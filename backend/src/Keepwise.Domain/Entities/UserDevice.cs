namespace Keepwise.Domain.Entities;

public sealed class UserDevice : AuditedEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string PushToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
