namespace Keepwise.Domain;

/// <summary>
/// A logical notification is uniquely identified by user, coverage, rule, channel, and local scheduled date.
/// </summary>
public static class OccurrenceKey
{
    public static string Build(
        Guid userId,
        Guid coverageId,
        Guid reminderRuleId,
        NotificationChannel channel,
        DateOnly scheduledLocalDate) =>
        $"{userId:N}|{coverageId:N}|{reminderRuleId:N}|{channel}|{scheduledLocalDate:yyyy-MM-dd}";
}
