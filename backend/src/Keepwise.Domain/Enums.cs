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
    Renewal = 2,
    ReturnWindow = 3
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

public enum LifecycleStatus
{
    Purchased = 0,
    Active = 1,
    Sold = 2,
    Replaced = 3,
    Disposed = 4
}

public enum IngestionSourceType
{
    Manual = 0,
    Document = 1,
    SharedText = 2,
    EmailForward = 3,
    WhatsAppShare = 4,
    SmsShare = 5
}

public enum CandidateStatus
{
    Processing = 0,
    PendingReview = 1,
    Confirmed = 2,
    Ignored = 3,
    Failed = 4,
    Duplicate = 5,
    NeedsOcr = 6
}

public enum FieldProvenance
{
    Confirmed = 0,
    UserProvided = 1,
    VendorProvided = 2,
    AiInferred = 3,
    Estimated = 4
}

public enum IngestionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public enum AttachmentOwnerType
{
    Item = 0,
    PurchaseCandidate = 1,
    Purchase = 2,
    Coverage = 3
}
