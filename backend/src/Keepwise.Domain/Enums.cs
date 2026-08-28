namespace Keepwise.Domain;

public enum DurationUnit
{
    Days = 0,
    Weeks = 1,
    Months = 2,
    Years = 3
}

public enum CoverageKind
{
    Warranty = 0,
    Maintenance = 1,
    Renewal = 2
}

public enum CoverageStatus
{
    Active = 0,
    ExpiringSoon = 1,
    Expired = 2,
    Extended = 3,
    Cancelled = 4
}

public enum MaintenanceEventKind
{
    Completed = 0,
    Skipped = 1,
    Rescheduled = 2
}

public enum NotificationChannel
{
    Push = 0,
    Email = 1,
    Sms = 2,
    WhatsApp = 3
}

public enum OccurrenceStatus
{
    Scheduled = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4,
    DeadLettered = 5
}
