using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Application.Items;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Items;

public sealed class CoverageFactory(IClock clock)
{
    public Coverage Create(Item item, CreateCoverageRequest request)
    {
        var start = request.StartDate ?? item.PurchaseDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        DateOnly end;
        DateOnly? nextDue = null;

        if (request.Kind == CoverageKind.Maintenance)
        {
            nextDue = request.ExplicitEndDate ?? start;
            if (request.RecurrenceValue is > 0 && request.RecurrenceUnit is not null && request.ExplicitEndDate is null)
            {
                nextDue = start;
            }

            end = nextDue ?? start;
        }
        else
        {
            end = WarrantyCalculator.ResolveExpiry(start, request.DurationValue, request.DurationUnit, request.ExplicitEndDate);
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var coverage = new Coverage
        {
            ItemId = item.Id,
            Item = item,
            Kind = request.Kind,
            Title = request.Title,
            Provider = request.Provider,
            ReferenceNumber = request.ReferenceNumber,
            StartDate = start,
            DurationValue = request.DurationValue,
            DurationUnit = request.DurationUnit,
            ExplicitEndDate = request.ExplicitEndDate,
            EndDate = end,
            RecurrenceValue = request.RecurrenceValue,
            RecurrenceUnit = request.RecurrenceUnit,
            NextDueDate = request.Kind == CoverageKind.Maintenance ? nextDue : null,
            Premium = request.Premium,
            Notes = request.Notes,
            Status = WarrantyCalculator.ResolveStatus(end, today, false, false)
        };

        var offsets = request.ReminderOffsetsDays is { Count: > 0 }
            ? request.ReminderOffsetsDays
            : DefaultReminderOffsets.DaysBefore;

        foreach (var days in offsets.Distinct().OrderByDescending(d => d))
        {
            coverage.ReminderRules.Add(new ReminderRule
            {
                OffsetValue = days,
                OffsetUnit = DurationUnit.Days,
                IsEnabled = true
            });
        }

        return coverage;
    }
}

public static class CoverageMapper
{
    public static CoverageDto Map(Coverage coverage) => new(
        coverage.Id,
        coverage.Kind,
        coverage.Title,
        coverage.Provider,
        coverage.ReferenceNumber,
        coverage.StartDate,
        coverage.EndDate,
        coverage.ExplicitEndDate,
        coverage.DurationValue,
        coverage.DurationUnit,
        coverage.Status,
        coverage.IsCancelled,
        coverage.IsExtended,
        coverage.RecurrenceValue,
        coverage.RecurrenceUnit,
        coverage.NextDueDate,
        coverage.Premium,
        coverage.Notes,
        coverage.ReminderRules.OrderByDescending(r => r.OffsetValue)
            .Select(r => new ReminderRuleDto(r.Id, r.OffsetValue, r.OffsetUnit, r.IsEnabled))
            .ToList());
}

public sealed class CoverageService(IKeepwiseDbContext db, ICurrentUser currentUser, IClock clock, CoverageFactory factory)
{
    public async Task<CoverageDto> AddAsync(Guid itemId, CreateCoverageRequest request, CancellationToken cancellationToken)
    {
        var item = await RequireItemAsync(itemId, cancellationToken);
        var coverage = factory.Create(item, request);
        db.Coverages.Add(coverage);
        await db.SaveChangesAsync(cancellationToken);
        return CoverageMapper.Map(coverage);
    }

    public async Task<CoverageDto> UpdateAsync(Guid coverageId, UpdateCoverageRequest request, CancellationToken cancellationToken)
    {
        var coverage = await RequireCoverageAsync(coverageId, cancellationToken);
        coverage.Title = request.Title;
        coverage.Provider = request.Provider;
        coverage.ReferenceNumber = request.ReferenceNumber;
        coverage.StartDate = request.StartDate;
        coverage.DurationValue = request.DurationValue;
        coverage.DurationUnit = request.DurationUnit;
        coverage.ExplicitEndDate = request.ExplicitEndDate;
        coverage.RecurrenceValue = request.RecurrenceValue;
        coverage.RecurrenceUnit = request.RecurrenceUnit;
        coverage.Premium = request.Premium;
        coverage.Notes = request.Notes;
        coverage.IsCancelled = request.IsCancelled;

        if (coverage.Kind == CoverageKind.Maintenance)
        {
            coverage.NextDueDate = request.ExplicitEndDate ?? coverage.NextDueDate ?? request.StartDate;
            coverage.EndDate = coverage.NextDueDate ?? request.StartDate;
        }
        else
        {
            coverage.EndDate = WarrantyCalculator.ResolveExpiry(
                request.StartDate,
                request.DurationValue,
                request.DurationUnit,
                request.ExplicitEndDate);
        }

        RefreshStatus(coverage);
        coverage.UpdatedAtUtc = clock.UtcNow;

        var pending = db.ReminderOccurrences.Where(o =>
            o.CoverageId == coverage.Id && o.Status == OccurrenceStatus.Scheduled);
        foreach (var occurrence in pending)
        {
            occurrence.Status = OccurrenceStatus.Cancelled;
        }

        await db.SaveChangesAsync(cancellationToken);
        return CoverageMapper.Map(coverage);
    }

    public async Task<CoverageDto> ExtendWarrantyAsync(Guid coverageId, int durationValue, DurationUnit unit, CancellationToken cancellationToken)
    {
        var coverage = await RequireCoverageAsync(coverageId, cancellationToken);
        if (coverage.Kind != CoverageKind.Warranty && coverage.Kind != CoverageKind.Renewal)
        {
            throw new AppValidationException("Only warranties and renewals can be extended.");
        }

        coverage.EndDate = DateMath.Add(coverage.EndDate, durationValue, unit);
        coverage.IsExtended = true;
        coverage.UpdatedAtUtc = clock.UtcNow;
        RefreshStatus(coverage);

        var pending = db.ReminderOccurrences.Where(o =>
            o.CoverageId == coverage.Id && o.Status == OccurrenceStatus.Scheduled);
        foreach (var occurrence in pending)
        {
            occurrence.Status = OccurrenceStatus.Cancelled;
        }

        await db.SaveChangesAsync(cancellationToken);
        return CoverageMapper.Map(coverage);
    }

    public async Task CompleteMaintenanceAsync(Guid coverageId, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        var coverage = await RequireCoverageAsync(coverageId, cancellationToken);
        EnsureMaintenance(coverage);
        coverage.MaintenanceEvents.Add(new MaintenanceEvent
        {
            Kind = MaintenanceEventKind.Completed,
            EventDate = request.EventDate,
            PreviousDueDate = coverage.NextDueDate,
            Notes = request.Notes
        });

        if (coverage.RecurrenceValue is > 0 && coverage.RecurrenceUnit is not null)
        {
            coverage.NextDueDate = MaintenanceCalculator.NextDueAfter(
                request.EventDate,
                coverage.RecurrenceValue.Value,
                coverage.RecurrenceUnit.Value);
        }
        else
        {
            coverage.NextDueDate = null;
        }

        coverage.EndDate = coverage.NextDueDate ?? request.EventDate;
        RefreshStatus(coverage);
        await CancelPendingAsync(coverage);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SkipMaintenanceAsync(Guid coverageId, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        var coverage = await RequireCoverageAsync(coverageId, cancellationToken);
        EnsureMaintenance(coverage);
        coverage.MaintenanceEvents.Add(new MaintenanceEvent
        {
            Kind = MaintenanceEventKind.Skipped,
            EventDate = request.EventDate,
            PreviousDueDate = coverage.NextDueDate,
            Notes = request.Notes
        });

        if (coverage.RecurrenceValue is > 0 && coverage.RecurrenceUnit is not null && coverage.NextDueDate is not null)
        {
            coverage.NextDueDate = MaintenanceCalculator.NextDueAfter(
                coverage.NextDueDate.Value,
                coverage.RecurrenceValue.Value,
                coverage.RecurrenceUnit.Value);
            coverage.EndDate = coverage.NextDueDate.Value;
        }

        RefreshStatus(coverage);
        await CancelPendingAsync(coverage);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RescheduleMaintenanceAsync(Guid coverageId, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        var coverage = await RequireCoverageAsync(coverageId, cancellationToken);
        EnsureMaintenance(coverage);
        if (request.NewDueDate is null)
        {
            throw new AppValidationException("New due date is required to reschedule.");
        }

        coverage.MaintenanceEvents.Add(new MaintenanceEvent
        {
            Kind = MaintenanceEventKind.Rescheduled,
            EventDate = request.EventDate,
            PreviousDueDate = coverage.NextDueDate,
            Notes = request.Notes
        });
        coverage.NextDueDate = request.NewDueDate;
        coverage.EndDate = request.NewDueDate.Value;
        RefreshStatus(coverage);
        await CancelPendingAsync(coverage);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void RefreshStatus(Coverage coverage)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var target = coverage.Kind == CoverageKind.Maintenance ? coverage.NextDueDate : coverage.EndDate;
        coverage.Status = WarrantyCalculator.ResolveStatus(target, today, coverage.IsCancelled, coverage.IsExtended);
    }

    private async Task CancelPendingAsync(Coverage coverage)
    {
        var pending = await db.ReminderOccurrences
            .Where(o => o.CoverageId == coverage.Id && o.Status == OccurrenceStatus.Scheduled)
            .ToListAsync();
        foreach (var occurrence in pending)
        {
            occurrence.Status = OccurrenceStatus.Cancelled;
        }
    }

    private static void EnsureMaintenance(Coverage coverage)
    {
        if (coverage.Kind != CoverageKind.Maintenance)
        {
            throw new AppValidationException("This action is only valid for maintenance schedules.");
        }
    }

    private async Task<Item> RequireItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await db.Items.FirstOrDefaultAsync(
            i => i.Id == itemId && i.UserId == currentUser.UserId && i.DeletedAtUtc == null,
            cancellationToken);
        return item ?? throw new NotFoundException("Item was not found.");
    }

    private async Task<Coverage> RequireCoverageAsync(Guid coverageId, CancellationToken cancellationToken)
    {
        var coverage = await db.Coverages
            .Include(c => c.Item)
            .Include(c => c.ReminderRules)
            .Include(c => c.MaintenanceEvents)
            .FirstOrDefaultAsync(c => c.Id == coverageId && c.DeletedAtUtc == null, cancellationToken);

        if (coverage is null || coverage.Item.UserId != currentUser.UserId || coverage.Item.DeletedAtUtc is not null)
        {
            throw new NotFoundException("Coverage was not found.");
        }

        return coverage;
    }
}
