# Testing guidelines

- **Backend unit:** xUnit + FluentAssertions on Domain calculators.
- **Application:** Coverage factory / service behavior.
- **API:** `WebApplicationFactory` + PostgreSQL database `keepwise_test`.
- **Shared:** Vitest.
- **E2E:** Playwright smoke against a running web+API (optional locally).
- Naming: `Method_condition_expected`. AAA structure.
- Coverage priority: Domain date/reminder/idempotency logic (~90% of that project). Do not chase 100% overall.
- CI runs `dotnet test` and `pnpm` web/shared checks. Failures fail the build.
