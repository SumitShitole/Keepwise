# Keepwise — project context

Keepwise helps people track purchases and important dates (warranty, maintenance, insurance, AMC, subscriptions) and notifies them before those dates.

**Target users:** Household owners and vehicle owners in India (English UI, INR, Asia/Kolkata default).

**Platforms:** Responsive web (`apps/web`) and Android (`apps/mobile`). Shared TypeScript lives in `packages/shared`. API is ASP.NET Core 10 (`backend/`).

**MVP:** Passwordless dev login (Firebase in production), profile, categories as data, items, warranties (duration or explicit expiry), basic recurring maintenance, renewals as a coverage type, reminder engine, Hangfire, push+email abstractions (logging providers in dev), dashboard, search, private local document storage.

**Out of MVP:** SMS, WhatsApp, OCR, family sharing, iOS, full calendar UI, km-based vehicle service.

**Business rules:** Warranty expiry is explicit vendor date if provided, otherwise start + duration. Reminder date is target minus offset. Notifications are unique on occurrence key (user, coverage, rule, channel, local date). Backend is source of truth for dates.

**Status:** Foundation plus a working item/warranty/dashboard slice with tests and CI.
