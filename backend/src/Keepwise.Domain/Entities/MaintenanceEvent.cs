namespace Keepwise.Domain.Entities;

public sealed class MaintenanceEvent : AuditedEntity
{
    public Guid CoverageId { get; set; }
    public Coverage Coverage { get; set; } = null!;
    public MaintenanceEventKind Kind { get; set; }
    public DateOnly EventDate { get; set; }
    public DateOnly? PreviousDueDate { get; set; }
    public string? Notes { get; set; }
}
