using Keepwise.Application.Abstractions;
using Keepwise.Application.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Dashboard;

public sealed class DashboardService(IKeepwiseDbContext db, ICurrentUser currentUser, IClock clock)
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var soon = today.AddDays(30);

        var items = db.Items.Where(i => i.UserId == currentUser.UserId && i.DeletedAtUtc == null && !i.IsArchived);
        var coverages = db.Coverages.Where(c =>
            c.Item.UserId == currentUser.UserId && c.DeletedAtUtc == null && !c.Item.IsArchived && c.Item.DeletedAtUtc == null);

        var totalItems = await items.CountAsync(cancellationToken);
        var activeWarranties = await coverages.CountAsync(
            c => c.Kind == CoverageKind.Warranty && (c.Status == CoverageStatus.Active || c.Status == CoverageStatus.Extended),
            cancellationToken);
        var expiringSoon = await coverages.CountAsync(
            c => c.Kind == CoverageKind.Warranty && c.Status == CoverageStatus.ExpiringSoon,
            cancellationToken);
        var upcomingMaintenance = await coverages.CountAsync(
            c => c.Kind == CoverageKind.Maintenance && c.NextDueDate != null && c.NextDueDate >= today && c.NextDueDate <= soon,
            cancellationToken);
        var upcomingRenewals = await coverages.CountAsync(
            c => c.Kind == CoverageKind.Renewal && c.EndDate >= today && c.EndDate <= soon && !c.IsCancelled,
            cancellationToken);
        var expiredItems = await items.CountAsync(
            i => i.Coverages.Any(c => c.Kind == CoverageKind.Warranty && c.Status == CoverageStatus.Expired && c.DeletedAtUtc == null),
            cancellationToken);

        var upcoming = await coverages
            .Where(c =>
                (c.Kind == CoverageKind.Maintenance && c.NextDueDate != null && c.NextDueDate >= today && c.NextDueDate <= soon) ||
                (c.Kind != CoverageKind.Maintenance && c.EndDate >= today && c.EndDate <= soon && !c.IsCancelled))
            .OrderBy(c => c.Kind == CoverageKind.Maintenance ? c.NextDueDate : c.EndDate)
            .Take(10)
            .Select(c => new UpcomingEventDto(
                c.ItemId,
                c.Id,
                c.Item.Name,
                c.Kind,
                c.Kind == CoverageKind.Maintenance ? c.NextDueDate!.Value : c.EndDate,
                c.Status))
            .ToListAsync(cancellationToken);

        var recent = await items
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(5)
            .Select(i => new ItemSummaryLite(i.Id, i.Name, i.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            totalItems,
            activeWarranties,
            expiringSoon,
            upcomingMaintenance,
            upcomingRenewals,
            expiredItems,
            upcoming,
            recent);
    }
}
