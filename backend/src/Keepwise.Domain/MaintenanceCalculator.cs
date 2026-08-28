namespace Keepwise.Domain;

public static class MaintenanceCalculator
{
    public static DateOnly NextDueAfter(DateOnly fromDate, int recurrenceValue, DurationUnit recurrenceUnit)
    {
        if (recurrenceValue <= 0)
        {
            throw new DomainException("invalid_recurrence", "Maintenance recurrence must be greater than zero.");
        }

        return DateMath.Add(fromDate, recurrenceValue, recurrenceUnit);
    }
}
