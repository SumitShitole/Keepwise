# Android emulator run script

Status: shipped.

## Goal

One command that installs JS deps, starts the API if needed, boots an Android emulator, forwards Metro and API ports, and launches the latest Expo bundle in Expo Go.

## Approach

- Windows: `scripts/android.ps1`. macOS/Linux: `scripts/android.sh`.
- Prefer an existing `adb` device. Otherwise start `ANDROID_AVD`, or `Keepwise_Pixel`, or the first AVD.
- `adb reverse` `8081` and `43124` so `EXPO_PUBLIC_API_URL` / `http://127.0.0.1:43124` works on the emulator.
- Metro: `expo start --clear --android --host lan` so the emulator loads current `apps/mobile` over the LAN IPv4 address (Windows Metro `--host localhost` binds IPv6-only `::1`, which `adb reverse` cannot use).
- `adb reverse` `43124` so `http://127.0.0.1:43124` in the app reaches the host API.
