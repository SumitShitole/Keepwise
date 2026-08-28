# Keepwise

Keepwise is a personal ownership assistant. Track assets, warranties, maintenance, and renewals — or import a receipt / order text, review the candidate in the inbox, confirm, and let reminders run.

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

Create the Postgres role and database (skip this if you use `docker compose up -d`):

```bash
# Linux
sudo -u postgres psql -c "CREATE USER keepwise WITH PASSWORD 'keepwise_dev';"
sudo -u postgres psql -c "CREATE DATABASE keepwise OWNER keepwise;"
```

```sql
-- Windows (pgAdmin / psql as a superuser)
CREATE USER keepwise WITH PASSWORD 'keepwise_dev';
CREATE DATABASE keepwise OWNER keepwise;
```

API migrations run on startup. Install JS deps with `pnpm install` (or `corepack pnpm install` if `pnpm` is not on PATH). On Windows, add `C:\Program Files\dotnet` to PATH if `dotnet` is not found.

Copy `backend/src/Keepwise.Api/appsettings.example.json` when configuring production. Do not commit secrets.

Environment:

- `ConnectionStrings__Keepwise` — PostgreSQL
- `Auth__AllowDevLogin` — `true` locally
- `Auth__FirebaseProjectId` — production Firebase
- `NEXT_PUBLIC_API_URL` / `EXPO_PUBLIC_API_URL` — API base URL

## Run

One command (installs JS deps, starts Postgres if needed, then API + web):

```powershell
# Windows
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
```

```bash
# macOS / Linux
chmod +x scripts/dev.sh   # once
./scripts/dev.sh
```

Postgres order: Docker Compose on port 5432, then an existing `keepwise` database on 5432, then (Windows script only) a user-local cluster on **5433** under `%LOCALAPPDATA%\Keepwise`.

Android (installs JS deps, starts the API if needed, boots an emulator, forwards the API port, clean Expo bundle on the emulator):

```powershell
# Windows
powershell -ExecutionPolicy Bypass -File .\scripts\android.ps1
```

```bash
# macOS / Linux
chmod +x scripts/android.sh   # once
./scripts/android.sh
```

Needs `adb` and an AVD (optional `ANDROID_AVD`; defaults to `Keepwise_Pixel` when that AVD exists). Expo Go on the emulator loads `apps/mobile`.

Or start the processes yourself:

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

See [docs/project-context.md](docs/project-context.md), [docs/plans/](docs/plans/), and [docs/decisions/README.md](docs/decisions/README.md).

## License

Proprietary — all rights reserved unless a license file is added.
