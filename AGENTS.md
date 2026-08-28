# Keepwise

Personal ownership assistant (assets, purchases, warranties, maintenance, insurance, reminders). Confirm-first capture and reminder reliability are non-negotiable.

Before changing behavior, read `docs/project-context.md`, matching files under `.cursor/rules/`, and existing code.

- **Backend:** ASP.NET Core 10 modular monolith in `backend/` (`Keepwise.Domain` / `Application` / `Infrastructure` / `Api`). Dates are computed on the server.
- **Web:** Next.js 15 in `apps/web`. **Android:** Expo 57 stub in `apps/mobile`. **Shared:** `@keepwise/shared`.
- **Plans:** markdown under `docs/plans/` only. **ADRs:** `docs/decisions/` — do not silently rewrite accepted ADRs.
- Never auto-create an Item from a purchase candidate. Never add SMS or WhatsApp inbox read. No passwords, extra UI libraries, or microservices.
- Users may only access their own data. Do not commit secrets.
