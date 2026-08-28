namespace Keepwise.Domain.Entities;

public sealed class Coverage : SoftDeletableEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public CoverageKind Kind { get; set; }
    public string? Title { get; set; }
    public string? Provider { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public int? DurationValue { get; set; }
    public DurationUnit? DurationUnit { get; set; }
    public DateOnly? ExplicitEndDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsExtended { get; set; }
    public CoverageStatus Status { get; set; }
    public int? RecurrenceValue { get; set; }
    public DurationUnit? RecurrenceUnit { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal? Premium { get; set; }
    public string? Notes { get; set; }

    public ICollection<ReminderRule> ReminderRules { get; set; } = new List<ReminderRule>();
    public ICollection<MaintenanceEvent> MaintenanceEvents { get; set; } = new List<MaintenanceEvent>();
}
