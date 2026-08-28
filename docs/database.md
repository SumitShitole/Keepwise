# Database

PostgreSQL 16 with EF Core 10 (`KeepwiseDbContext`). Tables: `users`, `user_devices`, `categories`, `item_types`, `items`, `coverages`, `reminder_rules`, `reminder_occurrences`, `maintenance_events`, `attachments`, `audit_events`.

- UUID primary keys.
- Soft delete: `DeletedAtUtc` + query filters on users, items, coverages, attachments.
- Concurrency: `xmin` mapped as `RowVersion`.
- Unique `reminder_occurrences.occurrence_key`.
- Migrations in `backend/src/Keepwise.Infrastructure/Persistence/Migrations`.
- Local: user `keepwise` / database `keepwise` (password `keepwise_dev`). Tests: `keepwise_test`.
- Prefer `docker compose up -d` (port 5432). The Windows `scripts/dev.ps1` can fall back to a user-local cluster on port 5433.
