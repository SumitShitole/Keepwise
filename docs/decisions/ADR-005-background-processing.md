# ADR-005 Background processing

- **Context:** Reminder generation, dispatch, retries, history.
- **Options:** BackgroundService only; Quartz.NET; Azure Functions; Hangfire.
- **Decision:** Hangfire with PostgreSQL storage, in-process, Cloud Run min instances = 1 in production.
- **Reason:** Dashboard, retries, persistence, .NET-native. Scale-to-zero would stall reminders.
- **Trade-off:** Always-on compute cost. Testing host skips Hangfire.
