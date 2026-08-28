using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Application.Items;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Items;

public sealed class ItemService(IKeepwiseDbContext db, ICurrentUser currentUser, IClock clock, CoverageFactory coverageFactory)
{
    public async Task<PagedResult<ItemSummaryDto>> SearchAsync(ItemListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = db.Items.AsNoTracking()
            .Where(i => i.UserId == currentUser.UserId && i.DeletedAtUtc == null);

        if (query.Archived is not null)
        {
            items = items.Where(i => i.IsArchived == query.Archived);
        }

        if (query.CategoryId is not null)
        {
            items = items.Where(i => i.CategoryId == query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            items = items.Where(i => i.Brand != null && i.Brand.ToLower().Contains(query.Brand.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            items = items.Where(i =>
                i.Name.ToLower().Contains(term) ||
                (i.Brand != null && i.Brand.ToLower().Contains(term)) ||
                (i.ModelNumber != null && i.ModelNumber.ToLower().Contains(term)) ||
                (i.SerialNumber != null && i.SerialNumber.ToLower().Contains(term)) ||
                (i.VendorName != null && i.VendorName.ToLower().Contains(term)));
        }

        items = items.Include(i => i.Category).Include(i => i.Coverages.Where(c => c.DeletedAtUtc == null));

        if (query.WarrantyStatus is not null)
        {
            items = items.Where(i => i.Coverages.Any(c =>
                c.Kind == CoverageKind.Warranty && c.Status == query.WarrantyStatus && c.DeletedAtUtc == null));
        }

        items = query.Sort switch
        {
            "name" => items.OrderBy(i => i.Name),
            "purchaseDate" => items.OrderByDescending(i => i.PurchaseDate),
            "created" => items.OrderByDescending(i => i.CreatedAtUtc),
            _ => items.OrderByDescending(i => i.UpdatedAtUtc)
        };

        var total = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ItemSummaryDto>(pageItems.Select(MapSummary).ToList(), total, page, pageSize);
    }

    public async Task<ItemDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await LoadOwnedAsync(id, cancellationToken);
        return MapDetail(item);
    }

    public async Task<ItemDetailDto> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppValidationException("Item name is required.");
        }

        var item = new Item
        {
            UserId = currentUser.UserId,
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            ItemTypeId = request.ItemTypeId,
            Brand = TrimToNull(request.Brand),
            ModelNumber = TrimToNull(request.ModelNumber),
            SerialNumber = TrimToNull(request.SerialNumber),
            PurchaseDate = request.PurchaseDate,
            PurchasePrice = request.PurchasePrice,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(),
            VendorName = TrimToNull(request.VendorName),
            VendorContact = TrimToNull(request.VendorContact),
            Notes = TrimToNull(request.Notes)
        };

        if (request.Warranty is not null)
        {
            var start = request.Warranty.StartDate ?? request.PurchaseDate
                ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
            item.Coverages.Add(coverageFactory.Create(item, request.Warranty with { StartDate = start }));
        }

        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(item.Id, cancellationToken);
    }

    public async Task<ItemDetailDto> UpdateAsync(Guid id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await LoadOwnedAsync(id, cancellationToken);
        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.ItemTypeId = request.ItemTypeId;
        item.Brand = TrimToNull(request.Brand);
        item.ModelNumber = TrimToNull(request.ModelNumber);
        item.SerialNumber = TrimToNull(request.SerialNumber);
        item.PurchaseDate = request.PurchaseDate;
        item.PurchasePrice = request.PurchasePrice;
        item.Currency = string.IsNullOrWhiteSpace(request.Currency) ? item.Currency : request.Currency.Trim().ToUpperInvariant();
        item.VendorName = TrimToNull(request.VendorName);
        item.VendorContact = TrimToNull(request.VendorContact);
        item.Notes = TrimToNull(request.Notes);
        item.IsArchived = request.IsArchived;
        item.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return MapDetail(item);
    }

    public async Task ArchiveAsync(Guid id, bool archived, CancellationToken cancellationToken)
    {
        var item = await LoadOwnedAsync(id, cancellationToken);
        item.IsArchived = archived;
        item.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await LoadOwnedAsync(id, cancellationToken);
        item.DeletedAtUtc = clock.UtcNow;
        var coverageIds = item.Coverages.Select(c => c.Id).ToList();
        foreach (var coverage in item.Coverages)
        {
            coverage.DeletedAtUtc = clock.UtcNow;
        }

        var pending = await db.ReminderOccurrences
            .Where(o => coverageIds.Contains(o.CoverageId) && o.Status == OccurrenceStatus.Scheduled)
            .ToListAsync(cancellationToken);
        foreach (var occurrence in pending)
        {
            occurrence.Status = OccurrenceStatus.Cancelled;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Item> LoadOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Items
            .Include(i => i.Category)
            .Include(i => i.ItemType)
            .Include(i => i.Coverages.Where(c => c.DeletedAtUtc == null))
                .ThenInclude(c => c.ReminderRules)
            .Include(i => i.Attachments.Where(a => a.DeletedAtUtc == null))
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == currentUser.UserId && i.DeletedAtUtc == null, cancellationToken);

        return item ?? throw new NotFoundException("Item was not found.");
    }

    private static ItemSummaryDto MapSummary(Item item)
    {
        var warranty = item.Coverages.Where(c => c.Kind == CoverageKind.Warranty).OrderByDescending(c => c.EndDate).FirstOrDefault();
        var maintenance = item.Coverages.Where(c => c.Kind == CoverageKind.Maintenance).OrderBy(c => c.NextDueDate).FirstOrDefault();
        return new ItemSummaryDto(
            item.Id,
            item.Name,
            item.Brand,
            item.Category?.Name,
            item.PurchaseDate,
            warranty?.Status,
            warranty?.EndDate,
            maintenance?.NextDueDate,
            item.IsArchived);
    }

    private static ItemDetailDto MapDetail(Item item) => new(
        item.Id,
        item.Name,
        item.CategoryId,
        item.Category?.Name,
        item.ItemTypeId,
        item.ItemType?.Name,
        item.Brand,
        item.ModelNumber,
        item.SerialNumber,
        item.PurchaseDate,
        item.PurchasePrice,
        item.Currency,
        item.VendorName,
        item.VendorContact,
        item.Notes,
        item.IsArchived,
        item.Coverages.OrderBy(c => c.Kind).Select(CoverageMapper.Map).ToList(),
        item.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.CreatedAtUtc)).ToList());

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
