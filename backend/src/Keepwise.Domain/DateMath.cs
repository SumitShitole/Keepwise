namespace Keepwise.Domain;

public static class DateMath
{
    public static DateOnly Add(DateOnly start, int value, DurationUnit unit) =>
        unit switch
        {
            DurationUnit.Days => start.AddDays(value),
            DurationUnit.Weeks => start.AddDays(checked(value * 7)),
            DurationUnit.Months => start.AddMonths(value),
            DurationUnit.Years => start.AddYears(value),
            _ => throw new DomainException("invalid_duration_unit", $"Unsupported duration unit '{unit}'.")
        };

    public static DateOnly Subtract(DateOnly target, int value, DurationUnit unit) =>
        Add(target, checked(-value), unit);
}
