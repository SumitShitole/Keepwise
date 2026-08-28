namespace Keepwise.Application.Items;

public sealed record ItemListQuery(
    string? Search,
    Guid? CategoryId,
    string? Brand,
    CoverageStatus? WarrantyStatus,
    bool? Archived,
    string? Sort,
    int Page = 1,
    int PageSize = 20);

public sealed record ItemSummaryDto(
    Guid Id,
    string Name,
    string? Brand,
    string? CategoryName,
    DateOnly? PurchaseDate,
    CoverageStatus? WarrantyStatus,
    DateOnly? WarrantyEndDate,
    DateOnly? NextMaintenanceDate,
    bool IsArchived);

public sealed record ItemDetailDto(
    Guid Id,
    string Name,
    Guid? CategoryId,
    string? CategoryName,
    Guid? ItemTypeId,
    string? ItemTypeName,
    string? Brand,
    string? ModelNumber,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string Currency,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    bool IsArchived,
    IReadOnlyList<CoverageDto> Coverages,
    IReadOnlyList<AttachmentDto> Attachments);

public sealed record CoverageDto(
    Guid Id,
    CoverageKind Kind,
    string? Title,
    string? Provider,
    string? ReferenceNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? ExplicitEndDate,
    int? DurationValue,
    DurationUnit? DurationUnit,
    CoverageStatus Status,
    bool IsCancelled,
    bool IsExtended,
    int? RecurrenceValue,
    DurationUnit? RecurrenceUnit,
    DateOnly? NextDueDate,
    decimal? Premium,
    string? Notes,
    IReadOnlyList<ReminderRuleDto> ReminderRules);

public sealed record ReminderRuleDto(Guid Id, int OffsetValue, DurationUnit OffsetUnit, bool IsEnabled);

public sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset CreatedAtUtc);

public sealed record CreateItemRequest(
    string Name,
    Guid? CategoryId,
    Guid? ItemTypeId,
    string? Brand,
    string? ModelNumber,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? Currency,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    CreateCoverageRequest? Warranty);

public sealed record UpdateItemRequest(
    string Name,
    Guid? CategoryId,
    Guid? ItemTypeId,
    string? Brand,
    string? ModelNumber,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? Currency,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    bool IsArchived);

public sealed record CreateCoverageRequest(
    CoverageKind Kind,
    string? Title,
    string? Provider,
    string? ReferenceNumber,
    DateOnly? StartDate,
    int? DurationValue,
    DurationUnit? DurationUnit,
    DateOnly? ExplicitEndDate,
    int? RecurrenceValue,
    DurationUnit? RecurrenceUnit,
    decimal? Premium,
    string? Notes,
    IReadOnlyList<int>? ReminderOffsetsDays);

public sealed record UpdateCoverageRequest(
    string? Title,
    string? Provider,
    string? ReferenceNumber,
    DateOnly StartDate,
    int? DurationValue,
    DurationUnit? DurationUnit,
    DateOnly? ExplicitEndDate,
    int? RecurrenceValue,
    DurationUnit? RecurrenceUnit,
    decimal? Premium,
    string? Notes,
    bool IsCancelled);

public sealed record MaintenanceActionRequest(DateOnly EventDate, string? Notes, DateOnly? NewDueDate);
