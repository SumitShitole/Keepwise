# Keepwise

Keepwise is a personal ownership assistant. Track assets, warranties, maintenance, and renewals — or import a receipt / order text, confirm the extracted purchase, and let reminders run.

## Platforms

- Web (Next.js)
- Android (Expo)
- ASP.NET Core 10 API

## Stack

.NET 10, EF Core 10, PostgreSQL, Hangfire, Next.js, Expo, pnpm workspaces, xUnit, Vitest.

## Repository

- `apps/web` — Keepwise web app
- `apps/mobile` — Keepwise Android app
- `packages/shared` — types and API client
- `backend/` — API, domain, infrastructure, tests
- `docs/` — architecture and ADRs
- `.cursor/rules/` — Cursor rules

## Prerequisites

- .NET 10 SDK
- Node.js 22 + pnpm
- PostgreSQL 16
- Expo tooling for Android builds

## Local setup

```bash
sudo -u postgres psql -c "CREATE USER keepwise WITH PASSWORD 'keepwise_dev';"
sudo -u postgres psql -c "CREATE DATABASE keepwise OWNER keepwise;"
export PATH="$HOME/.dotnet:$PATH"
dotnet ef database update --project backend/src/Keepwise.Infrastructure --startup-project backend/src/Keepwise.Api
pnpm install
```

Copy `backend/src/Keepwise.Api/appsettings.example.json` when configuring production. Do not commit secrets.

Environment:

- `ConnectionStrings__Keepwise` — PostgreSQL
- `Auth__AllowDevLogin` — `true` locally
- `Auth__FirebaseProjectId` — production Firebase
- `NEXT_PUBLIC_API_URL` / `EXPO_PUBLIC_API_URL` — API base URL

## Run

```bash
# API  http://127.0.0.1:43124
dotnet run --project backend/src/Keepwise.Api --urls http://127.0.0.1:43124

# Web  http://127.0.0.1:43123
pnpm --filter @keepwise/web dev

# Android
pnpm --filter @keepwise/mobile start
```

Health: `GET /health`. Hangfire dashboard (dev): `/hangfire`.

## Tests

```bash
dotnet test backend/Keepwise.slnx
pnpm --filter @keepwise/shared test
```

Create `keepwise_test` for API tests (same credentials as local).

## Docs

See [docs/project-context.md](docs/project-context.md) and [docs/decisions/README.md](docs/decisions/README.md).

## License

Proprietary — all rights reserved unless a license file is added.
