# Business rules

- **Warranty expiry:** `explicitEndDate` if provided (must be on/after start); otherwise `startDate + duration`.
- **Warranty start:** defaults to purchase date, else today (UTC date). User can override start.
- **Status:** Cancelled wins. Else expired if end/next due < today. Else expiring soon if within 30 days. Else Extended if flagged, else Active.
- **Reminder date:** `targetDate - offset` in the user's local calendar. Offset 0 is the due date.
- **Dedup:** one notification per `(user, coverage, rule, channel, scheduledLocalDate)`.
- **Deleted/archived items:** pending occurrences are cancelled; engine skips them.
- **Disabled channels:** no new occurrences; dispatch cancels if prefs changed.
- **Maintenance recurrence:** next due = event date + interval after complete; skip advances from previous due; reschedule sets an explicit next due.
