#!/usr/bin/env bash
# Builds current JS and runs Keepwise on an Android emulator (Expo Go).
# Usage (from repo root): ./scripts/android.sh
#
# Optional: ANDROID_AVD=Keepwise_Pixel ./scripts/android.sh
# API  http://127.0.0.1:43124  (started if not already listening)

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export PATH="${HOME}/.dotnet:${PATH}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export EXPO_NO_TELEMETRY="${EXPO_NO_TELEMETRY:-1}"

if [ -z "${ANDROID_HOME:-}" ] && [ -d "${HOME}/Android/Sdk" ]; then
  export ANDROID_HOME="${HOME}/Android/Sdk"
fi
if [ -n "${ANDROID_HOME:-}" ]; then
  export ANDROID_SDK_ROOT="$ANDROID_HOME"
  export PATH="${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/emulator:${ANDROID_HOME}/cmdline-tools/latest/bin:${PATH}"
fi

if ! command -v adb >/dev/null 2>&1; then
  echo "adb not found. Install the Android SDK (platform-tools) and set ANDROID_HOME." >&2
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

tcp_ok() {
  bash -c "echo >/dev/tcp/127.0.0.1/$1" >/dev/null 2>&1
}

pg_ok() {
  local port="$1"
  command -v psql >/dev/null 2>&1 || return 1
  PGPASSWORD=keepwise_dev PGCONNECT_TIMEOUT=3 \
    psql --no-password -h 127.0.0.1 -p "$port" -U keepwise -d keepwise -c "SELECT 1;" >/dev/null 2>&1
}

stop_port() {
  local port="$1"
  if command -v lsof >/dev/null 2>&1; then
    local pids
    pids="$(lsof -tiTCP:"$port" -sTCP:LISTEN || true)"
    if [ -n "$pids" ]; then
      echo "Stopping process on port $port..."
      # shellcheck disable=SC2086
      kill $pids 2>/dev/null || true
      sleep 1
    fi
  fi
}

wait_boot() {
  echo "Waiting for Android emulator..."
  for _ in $(seq 1 80); do
    if adb devices 2>/dev/null | grep -qE $'\tdevice$'; then
      if [ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; then
        return
      fi
    fi
    sleep 3
  done
  echo "Emulator did not finish booting." >&2
  exit 1
}

ensure_emulator() {
  if adb devices | grep -qE $'\tdevice$'; then
    wait_boot
    return
  fi
  if pgrep -f "qemu-system" >/dev/null 2>&1; then
    echo "Emulator process already running; waiting for adb..."
    wait_boot
    return
  fi
  if ! command -v emulator >/dev/null 2>&1; then
    echo "No Android device and emulator not on PATH. Set ANDROID_HOME." >&2
    exit 1
  fi
  avds=()
  while IFS= read -r line; do
    [ -n "$line" ] && avds+=("$line")
  done <<EOF
$(emulator -list-avds)
EOF
  if [ "${#avds[@]}" -eq 0 ]; then
    echo "No Android Virtual Devices. Create one in Android Studio." >&2
    exit 1
  fi
  avd="${ANDROID_AVD:-}"
  if [ -z "$avd" ]; then
    avd="Keepwise_Pixel"
    found=""
    for name in "${avds[@]}"; do
      if [ "$name" = "$avd" ]; then found=1; break; fi
    done
    if [ -z "$found" ]; then
      avd="${avds[0]}"
    fi
  fi
  echo "Starting emulator $avd..."
  emulator -avd "$avd" -no-metrics >/dev/null 2>&1 &
  wait_boot
}

ensure_api() {
  if tcp_ok 43124; then
    echo "API already listening on http://127.0.0.1:43124"
    return
  fi
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "API is not running and dotnet was not found." >&2
    exit 1
  fi
  pg_port=""
  if pg_ok 5432; then pg_port=5432; elif pg_ok 5433; then pg_port=5433; fi
  if [ -z "$pg_port" ]; then
    echo "Postgres keepwise database is not ready. Run ./scripts/dev.sh or docker compose up -d first." >&2
    exit 1
  fi
  export ConnectionStrings__Keepwise="Host=127.0.0.1;Port=${pg_port};Database=keepwise;Username=keepwise;Password=keepwise_dev"
  echo "Starting API (Postgres ${pg_port})..."
  dotnet run --project backend/src/Keepwise.Api --urls http://127.0.0.1:43124 &
  for _ in $(seq 1 60); do
    if tcp_ok 43124; then return; fi
    sleep 2
  done
  echo "API did not start on port 43124." >&2
  exit 1
}

lan_ip() {
  if command -v ip >/dev/null 2>&1; then
    ip -4 route get 1.1.1.1 2>/dev/null | awk '{for (i=1;i<=NF;i++) if ($i=="src") {print $(i+1); exit}}'
  elif command -v ipconfig >/dev/null 2>&1; then
    ipconfig getifaddr en0 2>/dev/null || true
  fi
}

echo "Installing JavaScript workspace packages..."
"${PNPM[@]}" install

ensure_emulator
ensure_api

echo "Forwarding emulator port 43124 (API)..."
adb reverse tcp:43124 tcp:43124 >/dev/null

stop_port 8081

LAN="$(lan_ip)"
LAN="${LAN:-127.0.0.1}"
EXPO_URL="exp://${LAN}:8081"
echo "Expo URL for emulator: ${EXPO_URL}"

(
  for _ in $(seq 1 90); do
    if tcp_ok 8081; then break; fi
    sleep 1
  done
  adb reverse tcp:43124 tcp:43124 >/dev/null
  sleep 2
  adb shell am start -a android.intent.action.VIEW -d "${EXPO_URL}" >/dev/null
) &

echo "Starting Expo with a clean bundle on Android..."
"${PNPM[@]}" --filter @keepwise/mobile android
