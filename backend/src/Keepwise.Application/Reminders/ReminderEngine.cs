using Keepwise.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Keepwise.Application.Reminders;

public sealed class ReminderEngine(
    IKeepwiseDbContext db,
    IClock clock,
    IEnumerable<INotificationSender> senders,
    ILogger<ReminderEngine> logger)
{
    private static readonly TimeOnly LocalSendTime = new(9, 0);

    public async Task GenerateAsync(CancellationToken cancellationToken)
    {
        var horizon = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime).AddDays(120);
        var coverages = await db.Coverages
            .Include(c => c.Item).ThenInclude(i => i.User)
            .Include(c => c.ReminderRules)
            .Where(c => c.DeletedAtUtc == null && !c.IsCancelled && c.Item.DeletedAtUtc == null && !c.Item.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var coverage in coverages)
        {
            var user = coverage.Item.User;
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }

            var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, tz));
            var target = coverage.Kind == CoverageKind.Maintenance
                ? coverage.NextDueDate
                : coverage.EndDate;

            if (target is null)
            {
                continue;
            }

            foreach (var rule in coverage.ReminderRules.Where(r => r.IsEnabled))
            {
                DateOnly reminderLocal;
                try
                {
                    reminderLocal = ReminderCalculator.ResolveReminderDate(target.Value, rule.OffsetValue, rule.OffsetUnit);
                }
                catch (DomainException)
                {
                    continue;
                }

                if (reminderLocal < todayLocal || reminderLocal > horizon)
                {
                    continue;
                }

                foreach (var channel in EnabledChannels(user))
                {
                    var key = OccurrenceKey.Build(user.Id, coverage.Id, rule.Id, channel, reminderLocal);
                    var exists = await db.ReminderOccurrences.AnyAsync(o => o.OccurrenceKey == key, cancellationToken);
                    if (exists)
                    {
                        continue;
                    }

                    db.ReminderOccurrences.Add(new ReminderOccurrence
                    {
                        UserId = user.Id,
                        CoverageId = coverage.Id,
                        ReminderRuleId = rule.Id,
                        Channel = channel,
                        ScheduledLocalDate = reminderLocal,
                        ScheduledAtUtc = ReminderCalculator.ToUtcInstant(reminderLocal, tz, LocalSendTime),
                        OccurrenceKey = key,
                        Status = OccurrenceStatus.Scheduled
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var due = await db.ReminderOccurrences
            .Include(o => o.Coverage).ThenInclude(c => c.Item).ThenInclude(i => i.User)
            .Where(o => o.Status == OccurrenceStatus.Scheduled && o.ScheduledAtUtc <= now)
            .OrderBy(o => o.ScheduledAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var senderMap = senders.ToDictionary(s => s.Channel);

        foreach (var occurrence in due)
        {
            var user = occurrence.Coverage.Item.User;
            if (occurrence.Coverage.DeletedAtUtc is not null ||
                occurrence.Coverage.Item.DeletedAtUtc is not null ||
                occurrence.Coverage.IsCancelled ||
                !IsChannelEnabled(user, occurrence.Channel))
            {
                occurrence.Status = OccurrenceStatus.Cancelled;
                continue;
            }

            if (!senderMap.TryGetValue(occurrence.Channel, out var sender))
            {
                occurrence.Status = OccurrenceStatus.Failed;
                occurrence.LastError = "No provider registered for channel.";
                continue;
            }

            occurrence.Status = OccurrenceStatus.Sending;
            occurrence.AttemptCount++;
            try
            {
                var targetDate = occurrence.Coverage.Kind == CoverageKind.Maintenance
                    ? occurrence.Coverage.NextDueDate
                    : occurrence.Coverage.EndDate;
                await sender.SendAsync(
                    new NotificationMessage(
                        user.Id,
                        occurrence.Channel == NotificationChannel.Email ? user.Email : user.MobileNumber ?? user.Email,
                        $"Keepwise reminder: {occurrence.Coverage.Item.Name}",
                        $"{occurrence.Coverage.Item.Name} is due on {targetDate:yyyy-MM-dd}.",
                        occurrence.Channel),
                    cancellationToken);
                occurrence.Status = OccurrenceStatus.Sent;
                occurrence.SentAtUtc = now;
                occurrence.LastError = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notification failed for {Key}", occurrence.OccurrenceKey);
                occurrence.LastError = ex.Message;
                occurrence.Status = occurrence.AttemptCount >= 5
                    ? OccurrenceStatus.DeadLettered
                    : OccurrenceStatus.Scheduled;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshStatusesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var coverages = await db.Coverages
            .Where(c => c.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var coverage in coverages)
        {
            var target = coverage.Kind == CoverageKind.Maintenance ? coverage.NextDueDate : coverage.EndDate;
            coverage.Status = WarrantyCalculator.ResolveStatus(target, today, coverage.IsCancelled, coverage.IsExtended);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<NotificationChannel> EnabledChannels(User user)
    {
        if (user.PushEnabled)
        {
            yield return NotificationChannel.Push;
        }

        if (user.EmailEnabled)
        {
            yield return NotificationChannel.Email;
        }

        if (user.SmsEnabled)
        {
            yield return NotificationChannel.Sms;
        }

        if (user.WhatsAppEnabled)
        {
            yield return NotificationChannel.WhatsApp;
        }
    }

    private static bool IsChannelEnabled(User user, NotificationChannel channel) =>
        channel switch
        {
            NotificationChannel.Push => user.PushEnabled,
            NotificationChannel.Email => user.EmailEnabled,
            NotificationChannel.Sms => user.SmsEnabled,
            NotificationChannel.WhatsApp => user.WhatsAppEnabled,
            _ => false
        };
}
