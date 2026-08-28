# Notification system

1. `ReminderEngine.GenerateAsync` materializes `reminder_occurrences` for enabled rules and channels within a 120-day horizon.
2. Hangfire runs generate every 5 minutes and dispatch every minute.
3. `OccurrenceKey` = `userId|coverageId|ruleId|channel|yyyy-MM-dd` (unique).
4. Dispatch skips deleted/cancelled items and disabled channels. Failures retry until 5 attempts, then dead-letter.
5. Changing coverage dates or rules cancels scheduled rows so they can be regenerated.

Providers implement `INotificationSender`. Dev: logging email + push. SMS (MSG91) and WhatsApp (Gupshup/Meta) are stubs until DLT/business accounts exist.

Send time is 09:00 in the user's IANA timezone, stored as UTC.
