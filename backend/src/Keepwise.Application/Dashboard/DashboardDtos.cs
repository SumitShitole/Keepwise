namespace Keepwise.Application.Dashboard;

public sealed record DashboardDto(
    int TotalItems,
    int ActiveWarranties,
    int WarrantiesExpiringSoon,
    int UpcomingMaintenance,
    int UpcomingRenewals,
    int ExpiredItems,
    IReadOnlyList<UpcomingEventDto> UpcomingEvents,
    IReadOnlyList<ItemSummaryLite> RecentlyAdded,
    IReadOnlyList<AttentionItemDto> Attention,
    int PendingCandidates);

public sealed record AttentionItemDto(
    string Kind,
    string Title,
    string Detail,
    string? Href,
    int Urgency);

public sealed record UpcomingEventDto(
    Guid ItemId,
    Guid CoverageId,
    string ItemName,
    CoverageKind Kind,
    DateOnly Date,
    CoverageStatus Status);

public sealed record ItemSummaryLite(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public sealed record CategoryDto(Guid Id, string Name, string Slug, IReadOnlyList<ItemTypeDto> ItemTypes);

public sealed record ItemTypeDto(Guid Id, string Name, string Slug);
