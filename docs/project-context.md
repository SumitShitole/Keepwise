# Keepwise — project context

Keepwise is a personal ownership assistant. It tracks assets, purchases, warranties, maintenance, insurance, and return dates, and it can extract purchase candidates from receipts, PDFs, and shared text for user confirmation.

**Target users:** Household owners and vehicle owners in India (English UI, INR, Asia/Kolkata default).

**Platforms:** Responsive web (`apps/web`) and Android (`apps/mobile`). Shared TypeScript lives in `packages/shared`. API is ASP.NET Core 10 (`backend/`).

**MVP:** Passwordless login, items (assets), warranties, maintenance, renewals, reminders, documents, **purchase ingestion** (upload/text → candidate review → confirm), return windows, attention dashboard, privacy/AI toggles.

**Later:** Email forwarding/OAuth, Android share sheet, barcode, product intelligence. **Never:** SMS inbox read, WhatsApp inbox read.

**Business rules:** Warranty expiry is explicit vendor date if provided, otherwise start + duration. Reminder date is target minus offset. Notifications are unique on occurrence key (user, coverage, rule, channel, local date). Backend is source of truth for dates.

**Status:** Foundation plus a working item/warranty/dashboard slice with tests and CI.
