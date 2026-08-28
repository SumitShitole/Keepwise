namespace Keepwise.Domain;

/// <summary>
/// WarrantyExpiry = explicit vendor date when provided; otherwise start + duration.
/// </summary>
public static class WarrantyCalculator
{
    public static DateOnly ResolveExpiry(
        DateOnly startDate,
        int? durationValue,
        DurationUnit? durationUnit,
        DateOnly? explicitExpiry)
    {
        if (explicitExpiry is not null)
        {
            if (explicitExpiry.Value < startDate)
            {
                throw new DomainException(
                    "expiry_before_start",
                    "Warranty expiry cannot be earlier than the start date.");
            }

            return explicitExpiry.Value;
        }

        if (durationValue is null || durationUnit is null)
        {
            throw new DomainException(
                "duration_or_expiry_required",
                "Provide a warranty duration or an explicit expiry date.");
        }

        if (durationValue.Value <= 0)
        {
            throw new DomainException("invalid_duration", "Warranty duration must be greater than zero.");
        }

        return DateMath.Add(startDate, durationValue.Value, durationUnit.Value);
    }

    public static CoverageStatus ResolveStatus(
        DateOnly? expiryDate,
        DateOnly today,
        bool isCancelled,
        bool isExtended,
        int expiringSoonDays = 30)
    {
        if (isCancelled)
        {
            return CoverageStatus.Cancelled;
        }

        if (expiryDate is null)
        {
            return CoverageStatus.Active;
        }

        if (expiryDate.Value < today)
        {
            return CoverageStatus.Expired;
        }

        if (expiryDate.Value <= today.AddDays(expiringSoonDays))
        {
            return CoverageStatus.ExpiringSoon;
        }

        return isExtended ? CoverageStatus.Extended : CoverageStatus.Active;
    }
}
