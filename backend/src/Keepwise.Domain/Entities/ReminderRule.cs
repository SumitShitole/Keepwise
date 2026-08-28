namespace Keepwise.Domain.Entities;

public sealed class ReminderRule : AuditedEntity
{
    public Guid CoverageId { get; set; }
    public Coverage Coverage { get; set; } = null!;
    public int OffsetValue { get; set; }
    public DurationUnit OffsetUnit { get; set; } = DurationUnit.Days;
    public bool IsEnabled { get; set; } = true;
}
