# Keepwise — project context

Keepwise is a personal ownership assistant. It tracks assets, purchases, warranties, maintenance, insurance, and return dates, and it can extract purchase candidates from receipts, PDFs, and shared text for user confirmation.

**Target users:** Household owners and vehicle owners in India (English UI, INR, Asia/Kolkata default).

**Platforms:** Responsive web (`apps/web`) and Android (`apps/mobile`). Shared TypeScript lives in `packages/shared`. API is ASP.NET Core 10 (`backend/`).

**Shipped:** Passwordless **dev** login (Firebase ID tokens in production when configured), items (assets), warranties/maintenance/renewals/return windows, reminders, documents, **purchase ingestion Phase 1** (upload or paste → candidate inbox → confirm/ignore), attention dashboard, privacy/ingestion toggles, tests and CI.

**Thin / later:** Android covers the same destinations as web (dashboard, inbox, items, add item, settings, details) with a simpler UI; receipt file upload stays on web. Live OCR/LLM, email forwarding/OAuth, Android share sheet, barcode, product intelligence, production notification channels. Hangfire dashboard is Development-only.

**Never:** SMS inbox read, WhatsApp inbox read, password authentication, microservices.

**Business rules:** Warranty expiry is explicit vendor date if provided, otherwise start + duration. Reminder date is target minus offset. Notifications are unique on occurrence key (user, coverage, rule, channel, local date). Backend is source of truth for dates. A candidate is never turned into an Item without user confirm.

**Layout**

| Path | Role |
| --- | --- |
| `apps/web` | Next.js 15: `/`, `/dashboard`, `/items`, `/inbox`, `/settings` |
| `apps/mobile` | Expo 57: sign-in plus dashboard, inbox, items, settings |
| `packages/shared` | Types and API client (`@keepwise/shared`) |
| `backend/` | Domain, Application, Infrastructure, Api, tests |
| `docs/` | Living docs; ADRs in `docs/decisions/`; plans in `docs/plans/` |
| `.cursor/rules/` | Cursor agent rules |

Local API `http://127.0.0.1:43124`, web `http://127.0.0.1:43123`. PostgreSQL 16. API migrates and seeds catalog on startup.
