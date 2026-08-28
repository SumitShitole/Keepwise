namespace Keepwise.Domain.Entities;

public sealed class Purchase : AuditedEntity
{
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public Guid? CandidateId { get; set; }
    public string? VendorName { get; set; }
    public string? OrderNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? PurchasedOn { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Gstin { get; set; }
    public string? UpiReference { get; set; }
    public DateOnly? ReturnBy { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
}

public sealed class UserIngestionSettings : AuditedEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool ReceiptScanningEnabled { get; set; } = true;
    public bool SharedTextEnabled { get; set; } = true;
    public bool EmailScanningEnabled { get; set; }
    public bool SmsImportEnabled { get; set; }
    public bool WhatsAppImportEnabled { get; set; }
    public bool AiProcessingEnabled { get; set; }
}

public sealed class IngestionJob : AuditedEntity
{
    public Guid UserId { get; set; }
    public IngestionSourceType SourceType { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Queued;
    public string? ContentType { get; set; }
    public string? StorageKey { get; set; }
    public string? Sha256 { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public Guid? CandidateId { get; set; }
    public int OcrRequests { get; set; }
    public int LlmRequests { get; set; }
}

public sealed class PurchaseCandidate : SoftDeletableEntity
{
    public Guid UserId { get; set; }
    public Guid? JobId { get; set; }
    public IngestionSourceType SourceType { get; set; }
    public CandidateStatus Status { get; set; } = CandidateStatus.Processing;
    public string? StorageKey { get; set; }
    public string? Sha256 { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public Guid? DuplicateOfId { get; set; }
    public Guid? ConfirmedItemId { get; set; }
    public double OverallConfidence { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
