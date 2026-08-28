using Keepwise.Domain;
using FluentAssertions;

namespace Keepwise.Domain.Tests;

public class WarrantyCalculatorTests
{
    [Fact]
    public void Explicit_expiry_wins_over_duration()
    {
        var start = new DateOnly(2027, 3, 15);
        var expiry = WarrantyCalculator.ResolveExpiry(start, 2, DurationUnit.Years, new DateOnly(2028, 1, 1));
        expiry.Should().Be(new DateOnly(2028, 1, 1));
    }

    [Fact]
    public void Two_year_warranty_from_purchase_date()
    {
        var expiry = WarrantyCalculator.ResolveExpiry(new DateOnly(2027, 3, 15), 2, DurationUnit.Years, null);
        expiry.Should().Be(new DateOnly(2029, 3, 15));
    }

    [Fact]
    public void Duration_in_months()
    {
        var expiry = WarrantyCalculator.ResolveExpiry(new DateOnly(2027, 1, 31), 1, DurationUnit.Months, null);
        expiry.Should().Be(new DateOnly(2027, 2, 28));
    }

    [Fact]
    public void Leap_year_february_29_plus_one_year()
    {
        var expiry = WarrantyCalculator.ResolveExpiry(new DateOnly(2028, 2, 29), 1, DurationUnit.Years, null);
        expiry.Should().Be(new DateOnly(2029, 2, 28));
    }

    [Fact]
    public void Expires_today_is_expiring_soon()
    {
        var today = new DateOnly(2027, 3, 15);
        WarrantyCalculator.ResolveStatus(today, today, false, false).Should().Be(CoverageStatus.ExpiringSoon);
    }

    [Fact]
    public void Expires_tomorrow_is_expiring_soon()
    {
        var today = new DateOnly(2027, 3, 15);
        WarrantyCalculator.ResolveStatus(today.AddDays(1), today, false, false).Should().Be(CoverageStatus.ExpiringSoon);
    }

    [Fact]
    public void Expired_yesterday()
    {
        var today = new DateOnly(2027, 3, 15);
        WarrantyCalculator.ResolveStatus(today.AddDays(-1), today, false, false).Should().Be(CoverageStatus.Expired);
    }

    [Fact]
    public void Cancelled_overrides_dates()
    {
        WarrantyCalculator.ResolveStatus(new DateOnly(2030, 1, 1), new DateOnly(2027, 1, 1), true, true)
            .Should().Be(CoverageStatus.Cancelled);
    }

    [Fact]
    public void Extended_when_far_from_expiry()
    {
        WarrantyCalculator.ResolveStatus(new DateOnly(2030, 1, 1), new DateOnly(2027, 1, 1), false, true)
            .Should().Be(CoverageStatus.Extended);
    }

    [Fact]
    public void Rejects_expiry_before_start()
    {
        var act = () => WarrantyCalculator.ResolveExpiry(new DateOnly(2027, 3, 15), null, null, new DateOnly(2027, 3, 1));
        act.Should().Throw<DomainException>().Which.Code.Should().Be("expiry_before_start");
    }

    [Fact]
    public void Requires_duration_or_explicit_expiry()
    {
        var act = () => WarrantyCalculator.ResolveExpiry(new DateOnly(2027, 3, 15), null, null, null);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("duration_or_expiry_required");
    }
}

public class ReminderCalculatorTests
{
    [Fact]
    public void Ninety_days_before_target()
    {
        var reminder = ReminderCalculator.ResolveReminderDate(new DateOnly(2027, 11, 30), 90, DurationUnit.Days);
        reminder.Should().Be(new DateOnly(2027, 9, 1));
    }

    [Fact]
    public void On_target_date_offset_zero()
    {
        var target = new DateOnly(2027, 11, 30);
        ReminderCalculator.ResolveReminderDate(target, 0, DurationUnit.Days).Should().Be(target);
    }

    [Fact]
    public void Weeks_and_months_offsets()
    {
        ReminderCalculator.ResolveReminderDate(new DateOnly(2027, 3, 15), 2, DurationUnit.Weeks)
            .Should().Be(new DateOnly(2027, 3, 1));
        ReminderCalculator.ResolveReminderDate(new DateOnly(2027, 3, 15), 1, DurationUnit.Months)
            .Should().Be(new DateOnly(2027, 2, 15));
    }

    [Fact]
    public void Kolkata_nine_am_converts_to_utc()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        var utc = ReminderCalculator.ToUtcInstant(new DateOnly(2027, 3, 15), tz, new TimeOnly(9, 0));
        utc.Should().Be(new DateTimeOffset(2027, 3, 15, 3, 30, 0, TimeSpan.Zero));
    }
}

public class OccurrenceKeyTests
{
    [Fact]
    public void Same_inputs_produce_same_key()
    {
        var user = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var coverage = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var rule = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var date = new DateOnly(2027, 3, 15);
        OccurrenceKey.Build(user, coverage, rule, NotificationChannel.Email, date)
            .Should().Be(OccurrenceKey.Build(user, coverage, rule, NotificationChannel.Email, date));
        OccurrenceKey.Build(user, coverage, rule, NotificationChannel.Push, date)
            .Should().NotBe(OccurrenceKey.Build(user, coverage, rule, NotificationChannel.Email, date));
    }
}

public class MaintenanceCalculatorTests
{
    [Fact]
    public void Recurring_every_six_months()
    {
        MaintenanceCalculator.NextDueAfter(new DateOnly(2027, 1, 31), 6, DurationUnit.Months)
            .Should().Be(new DateOnly(2027, 7, 31));
    }

    [Fact]
    public void Recurring_every_twelve_months_from_february()
    {
        MaintenanceCalculator.NextDueAfter(new DateOnly(2028, 2, 29), 12, DurationUnit.Months)
            .Should().Be(new DateOnly(2029, 2, 28));
    }
}
