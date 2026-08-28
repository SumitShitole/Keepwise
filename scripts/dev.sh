#!/usr/bin/env bash
# Starts Keepwise locally: Postgres (if needed), API, and web.
# Usage (from repo root): ./scripts/dev.sh
#
# Web  http://127.0.0.1:43123
# API  http://127.0.0.1:43124

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export PATH="${HOME}/.dotnet:${PATH}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install the .NET 10 SDK." >&2
  exit 1
fi

if command -v pnpm >/dev/null 2>&1; then
  PNPM=(pnpm)
elif command -v corepack >/dev/null 2>&1; then
  PNPM=(corepack pnpm)
else
  echo "pnpm not found. Enable it with: corepack enable" >&2
  exit 1
fi

pg_ok() {
  local port="$1"
  command -v psql >/dev/null 2>&1 || return 1
  PGPASSWORD=keepwise_dev PGCONNECT_TIMEOUT=3 \
    psql --no-password -h 127.0.0.1 -p "$port" -U keepwise -d keepwise -c "SELECT 1;" >/dev/null 2>&1
}

ensure_postgres() {
  if command -v docker >/dev/null 2>&1; then
    echo "Starting Postgres via Docker Compose..."
    docker compose up -d postgres || echo "Docker Compose did not start Postgres (is port 5432 already in use?)."
    for _ in $(seq 1 20); do
      if pg_ok 5432; then
        echo 5432
        return
      fi
      sleep 1
    done
  fi

  if pg_ok 5432; then
    echo 5432
    return
  fi

  echo "Postgres is not ready on 127.0.0.1:5432 as user/database keepwise (password keepwise_dev)." >&2
  echo "Start it with: docker compose up -d" >&2
  echo "Or: create user keepwise / database keepwise (see README)." >&2
  exit 1
}

echo "Installing JavaScript workspace packages..."
"${PNPM[@]}" install

PG_PORT="$(ensure_postgres)"
export ConnectionStrings__Keepwise="Host=127.0.0.1;Port=${PG_PORT};Database=keepwise;Username=keepwise;Password=keepwise_dev"
echo "Using Postgres on 127.0.0.1:${PG_PORT}"

cleanup() {
  kill "${API_PID:-}" "${WEB_PID:-}" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Starting API..."
dotnet run --project backend/src/Keepwise.Api --urls http://127.0.0.1:43124 &
API_PID=$!

echo "Starting web..."
"${PNPM[@]}" --filter @keepwise/web dev &
WEB_PID=$!

echo
echo "Web  http://127.0.0.1:43123"
echo "API  http://127.0.0.1:43124  (health: /health, hangfire: /hangfire)"
echo "Android: ${PNPM[*]} --filter @keepwise/mobile start"
echo "Ctrl+C stops both processes."

wait
