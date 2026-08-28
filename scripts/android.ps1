# Builds current JS and runs Keepwise on an Android emulator (Expo Go).
# Usage (from repo root):
#   powershell -ExecutionPolicy Bypass -File .\scripts\android.ps1
#
# Optional: $env:ANDROID_AVD = "Keepwise_Pixel"
# API  http://127.0.0.1:43124  (started if not already listening)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Add-PathIfExists([string]$Dir) {
    if ($Dir -and (Test-Path $Dir) -and ($env:Path -notlike "*$Dir*")) {
        $env:Path = "$Dir;" + $env:Path
    }
}

Add-PathIfExists "C:\Program Files\dotnet"
Add-PathIfExists (Join-Path $env:USERPROFILE ".dotnet")
@(16, 17, 18) | ForEach-Object {
    Add-PathIfExists "C:\Program Files\PostgreSQL\$_\bin"
}

if (-not $env:ANDROID_HOME) {
    $defaultSdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    if (Test-Path $defaultSdk) { $env:ANDROID_HOME = $defaultSdk }
}
if ($env:ANDROID_HOME) {
    $env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
    Add-PathIfExists (Join-Path $env:ANDROID_HOME "platform-tools")
    Add-PathIfExists (Join-Path $env:ANDROID_HOME "emulator")
    Add-PathIfExists (Join-Path $env:ANDROID_HOME "cmdline-tools\latest\bin")
}

if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
    throw "adb not found. Install the Android SDK (platform-tools) and set ANDROID_HOME."
}

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    throw "node not found. Install Node.js 22+ (includes corepack)."
}

function Invoke-Pnpm {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$PnpmArgs)
    if (Get-Command pnpm -ErrorAction SilentlyContinue) {
        & pnpm @PnpmArgs
    } elseif (Get-Command corepack -ErrorAction SilentlyContinue) {
        & corepack pnpm @PnpmArgs
    } else {
        throw "pnpm not found. Enable it with: corepack enable"
    }
    if ($LASTEXITCODE -ne 0) { throw "pnpm $($PnpmArgs -join ' ') failed ($LASTEXITCODE)" }
}

function Test-TcpPort([int]$Port) {
    try {
        $client = [System.Net.Sockets.TcpClient]::new()
        $task = $client.ConnectAsync("127.0.0.1", $Port)
        $ok = $task.Wait(500)
        $client.Dispose()
        return $ok -and $task.Status -eq [System.Threading.Tasks.TaskStatus]::RanToCompletion
    } catch {
        return $false
    }
}

function Test-KeepwiseDb([int]$Port) {
    if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { return $false }
    $prev = $env:PGPASSWORD
    $env:PGPASSWORD = "keepwise_dev"
    $env:PGCONNECT_TIMEOUT = "3"
    & psql --no-password -h 127.0.0.1 -p $Port -U keepwise -d keepwise -c "SELECT 1;" 2>$null | Out-Null
    $ok = ($LASTEXITCODE -eq 0)
    $env:PGPASSWORD = $prev
    return $ok
}

function Stop-Listener([int]$Port) {
    $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $conns) { return }
    $conns | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object {
        if ($_ -and $_ -ne 0) {
            Write-Host "Stopping process $_ on port $Port..."
            Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 1
}

function Wait-AdbBoot {
    Write-Host "Waiting for Android emulator..."
    $deadline = (Get-Date).AddMinutes(4)
    do {
        $devices = & adb devices 2>$null
        if ($devices -match "\tdevice") {
            $boot = ((& adb shell getprop sys.boot_completed 2>$null) | Out-String).Trim()
            if ($boot -eq "1") { return }
        }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $deadline)
    throw "Emulator did not finish booting. Open Android Studio AVD Manager and start a device, then re-run."
}

function Ensure-Emulator {
    $devices = & adb devices
    if ($devices -match "\tdevice") {
        Wait-AdbBoot
        return
    }

    $qemu = Get-Process -Name "qemu-system-x86_64" -ErrorAction SilentlyContinue
    if ($qemu) {
        Write-Host "Emulator process already running; waiting for adb..."
        Wait-AdbBoot
        return
    }

    $emulator = Get-Command emulator -ErrorAction SilentlyContinue
    if (-not $emulator) {
        throw "No Android device and emulator.exe not on PATH. Create an AVD or set ANDROID_HOME."
    }

    $avds = @(& emulator -list-avds | Where-Object { $_.Trim() })
    if ($avds.Count -eq 0) {
        throw "No Android Virtual Devices. Create one in Android Studio (for example Keepwise_Pixel)."
    }

    $avd = $env:ANDROID_AVD
    if (-not $avd) {
        $named = $avds | Where-Object { $_ -eq "Keepwise_Pixel" } | Select-Object -First 1
        $avd = if ($named) { $named } else { $avds[0] }
    }

    Write-Host "Starting emulator $avd..."
    Start-Process -FilePath $emulator.Source -ArgumentList @("-avd", $avd, "-no-metrics") | Out-Null
    Wait-AdbBoot
}

function Ensure-Api {
    if (Test-TcpPort 43124) {
        Write-Host "API already listening on http://127.0.0.1:43124"
        return
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "API is not running and dotnet was not found. Start the API or install the .NET 10 SDK."
    }

    $pgPort = $null
    if (Test-KeepwiseDb 5432) { $pgPort = 5432 }
    elseif (Test-KeepwiseDb 5433) { $pgPort = 5433 }
    if (-not $pgPort) {
        throw "Postgres keepwise database is not ready. Run scripts/dev.ps1 or docker compose up -d first."
    }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__Keepwise = "Host=127.0.0.1;Port=$pgPort;Database=keepwise;Username=keepwise;Password=keepwise_dev"
    Write-Host "Starting API (Postgres $pgPort)..."
    Start-Process -FilePath "dotnet" -WorkingDirectory $Root -ArgumentList @(
        "run", "--project", "backend/src/Keepwise.Api", "--urls", "http://127.0.0.1:43124"
    ) -WindowStyle Minimized | Out-Null

    $deadline = (Get-Date).AddMinutes(2)
    do {
        if (Test-TcpPort 43124) { return }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)
    throw "API did not start on port 43124."
}

function Get-LanIPv4 {
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notmatch '^127\.' -and
            $_.IPAddress -notmatch '^169\.254\.'
        } |
        Select-Object -First 1 -ExpandProperty IPAddress
}

Write-Host "Installing JavaScript workspace packages..."
Invoke-Pnpm install

Ensure-Emulator
Ensure-Api

Write-Host "Forwarding emulator port 43124 (API)..."
& adb reverse tcp:43124 tcp:43124 | Out-Null

Stop-Listener 8081

$adbExe = (Get-Command adb).Source
$lan = Get-LanIPv4
if (-not $lan) { $lan = "127.0.0.1" }
$expoUrl = "exp://${lan}:8081"
Write-Host "Expo URL for emulator: $expoUrl"

$helper = @"
`$deadline = (Get-Date).AddMinutes(3)
do {
    try {
        `$client = [System.Net.Sockets.TcpClient]::new()
        `$ok = `$client.ConnectAsync('$lan', 8081).Wait(500)
        `$client.Dispose()
        if (`$ok) { break }
    } catch {}
    Start-Sleep -Seconds 1
} while ((Get-Date) -lt `$deadline)
& '$adbExe' reverse tcp:43124 tcp:43124 | Out-Null
Start-Sleep -Seconds 2
& '$adbExe' shell am start -a android.intent.action.VIEW -d '$expoUrl'
"@
Start-Process -FilePath "powershell" -WindowStyle Hidden -ArgumentList @("-NoProfile", "-Command", $helper) | Out-Null

$env:EXPO_NO_TELEMETRY = "1"
Write-Host "Starting Expo with a clean bundle on Android..."
Invoke-Pnpm --filter @keepwise/mobile android
