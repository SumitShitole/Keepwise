using System.Text.Json;
using Keepwise.Domain;

namespace Keepwise.Application.Ingestion;

public sealed class CandidatePayload
{
    public bool IsPurchase { get; set; }
    public string? Vendor { get; set; }
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? OrderNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? WarrantyDurationMonths { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? SerialNumber { get; set; }
    public string? Gstin { get; set; }
    public string? UpiReference { get; set; }
    public int? ReturnWindowDays { get; set; }
    public FieldProvenance WarrantyProvenance { get; set; } = FieldProvenance.Estimated;
    public double OverallConfidence { get; set; }
    public Dictionary<string, double> FieldConfidence { get; set; } = [];

    public static CandidatePayload From(ExtractedPurchase extracted) => new()
    {
        IsPurchase = extracted.IsPurchase,
        Vendor = extracted.Vendor,
        ProductName = extracted.ProductName,
        Brand = extracted.Brand,
        Model = extracted.Model,
        PurchaseDate = extracted.PurchaseDate,
        Amount = extracted.Amount,
        Currency = extracted.Currency,
        OrderNumber = extracted.OrderNumber,
        InvoiceNumber = extracted.InvoiceNumber,
        WarrantyDurationMonths = extracted.WarrantyDurationMonths,
        WarrantyEndDate = extracted.WarrantyEndDate,
        SerialNumber = extracted.SerialNumber,
        Gstin = extracted.Gstin,
        UpiReference = extracted.UpiReference,
        ReturnWindowDays = extracted.ReturnWindowDays,
        WarrantyProvenance = extracted.WarrantyProvenance,
        OverallConfidence = extracted.OverallConfidence,
        FieldConfidence = new Dictionary<string, double>(extracted.FieldConfidence)
    };

    public static CandidatePayload Parse(string json) =>
        JsonSerializer.Deserialize<CandidatePayload>(json, JsonOptions) ?? new CandidatePayload();

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

public sealed record IngestionSettingsDto(
    bool ReceiptScanningEnabled,
    bool SharedTextEnabled,
    bool EmailScanningEnabled,
    bool SmsImportEnabled,
    bool WhatsAppImportEnabled,
    bool AiProcessingEnabled);

public sealed record PrivacySummaryDto(
    IngestionSettingsDto Ingestion,
    int PendingCandidates,
    int ImportedDocuments,
    bool AiProcessingEnabled);

public sealed record PurchaseCandidateDto(
    Guid Id,
    CandidateStatus Status,
    IngestionSourceType SourceType,
    double OverallConfidence,
    Guid? DuplicateOfId,
    Guid? ConfirmedItemId,
    CandidatePayload Payload,
    DateTimeOffset CreatedAtUtc);

public sealed record IngestAcceptedDto(Guid JobId, Guid? CandidateId, CandidateStatus Status);
