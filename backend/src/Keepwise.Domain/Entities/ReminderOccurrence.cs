namespace Keepwise.Domain.Entities;

public sealed class ReminderOccurrence : AuditedEntity
{
    public Guid UserId { get; set; }
    public Guid CoverageId { get; set; }
    public Coverage Coverage { get; set; } = null!;
    public Guid ReminderRuleId { get; set; }
    public ReminderRule ReminderRule { get; set; } = null!;
    public NotificationChannel Channel { get; set; }
    public DateOnly ScheduledLocalDate { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public string OccurrenceKey { get; set; } = string.Empty;
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Scheduled;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
}
