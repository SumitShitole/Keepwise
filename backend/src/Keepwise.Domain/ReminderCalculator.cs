namespace Keepwise.Domain;

/// <summary>
/// ReminderDate = TargetDate - ReminderOffset, evaluated in the user's local calendar.
/// </summary>
public static class ReminderCalculator
{
    public static DateOnly ResolveReminderDate(
        DateOnly targetDate,
        int offsetValue,
        DurationUnit offsetUnit)
    {
        if (offsetValue < 0)
        {
            throw new DomainException("invalid_offset", "Reminder offset cannot be negative.");
        }

        return DateMath.Subtract(targetDate, offsetValue, offsetUnit);
    }

    public static DateTimeOffset ToUtcInstant(DateOnly localDate, TimeZoneInfo timeZone, TimeOnly localTime)
    {
        var localDateTime = DateTime.SpecifyKind(localDate.ToDateTime(localTime), DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localDateTime))
        {
            localDateTime = localDateTime.AddHours(1);
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            var utc = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        var converted = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        return new DateTimeOffset(converted, TimeSpan.Zero);
    }
}
