# Starts Keepwise locally: Postgres (if needed), API, and web.
# Usage (from repo root):
#   powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
#
# Web  http://127.0.0.1:43123
# API  http://127.0.0.1:43124

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet not found. Install the .NET 10 SDK and ensure it is on PATH (often C:\Program Files\dotnet)."
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

function Ensure-Postgres {
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Starting Postgres via Docker Compose..."
        & docker compose up -d postgres
        for ($i = 0; $i -lt 20; $i++) {
            if (Test-KeepwiseDb 5432) { return 5432 }
            Start-Sleep -Seconds 1
        }
    }

    if (Test-KeepwiseDb 5432) { return 5432 }

    if (-not (Get-Command initdb -ErrorAction SilentlyContinue)) {
        throw @"
Postgres is not ready on 127.0.0.1:5432 as user/database keepwise (password keepwise_dev).
Install Docker Desktop and run: docker compose up -d
Or create the role/database (see README), or install PostgreSQL so initdb is on PATH.
"@
    }

    $dataDir = Join-Path $env:LOCALAPPDATA "Keepwise\pgdata"
    $logFile = Join-Path $env:LOCALAPPDATA "Keepwise\postgres.log"
    New-Item -ItemType Directory -Force -Path (Split-Path $dataDir) | Out-Null

    if (-not (Test-Path (Join-Path $dataDir "PG_VERSION"))) {
        Write-Host "Initializing user-local Postgres cluster (port 5433)..."
        & initdb -D $dataDir -U keepwise --auth=trust --encoding=UTF8 --locale=C
        if ($LASTEXITCODE -ne 0) { throw "initdb failed ($LASTEXITCODE)" }
    }

    & pg_ctl -D $dataDir status 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Starting user-local Postgres on 5433..."
        & pg_ctl -D $dataDir -l $logFile -o "-p 5433" start
        if ($LASTEXITCODE -ne 0) { throw "pg_ctl start failed ($LASTEXITCODE)" }
        Start-Sleep -Seconds 2
    }

    & createdb -h 127.0.0.1 -p 5433 -U keepwise keepwise 2>$null | Out-Null
    if (-not (Test-KeepwiseDb 5433)) {
        throw "Could not connect to user-local Postgres on 5433."
    }
    return 5433
}

Write-Host "Installing JavaScript workspace packages..."
Invoke-Pnpm install

$pgPort = Ensure-Postgres
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__Keepwise = "Host=127.0.0.1;Port=$pgPort;Database=keepwise;Username=keepwise;Password=keepwise_dev"
Write-Host "Using Postgres on 127.0.0.1:$pgPort"

$shell = (Get-Process -Id $PID).Path
$conn = $env:ConnectionStrings__Keepwise
$pathEscaped = $env:Path.Replace("'", "''")
$rootEscaped = $Root.Replace("'", "''")

function Start-DevWindow([string]$Title, [string]$Command) {
    $wrapped = @"
`$Host.UI.RawUI.WindowTitle = '$Title'
`$env:Path = '$pathEscaped'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ConnectionStrings__Keepwise = '$conn'
Set-Location '$rootEscaped'
$Command
"@
    Start-Process -FilePath $shell -WorkingDirectory $Root -ArgumentList @(
        "-NoExit", "-NoProfile", "-Command", $wrapped
    ) | Out-Null
}

if (Test-TcpPort 43124) {
    Write-Host "API already listening on http://127.0.0.1:43124"
} else {
    Write-Host "Starting API..."
    Start-DevWindow "Keepwise API" "dotnet run --project backend/src/Keepwise.Api --urls http://127.0.0.1:43124"
}

$webCommand = if (Get-Command pnpm -ErrorAction SilentlyContinue) {
    "pnpm --filter @keepwise/web dev"
} else {
    "corepack pnpm --filter @keepwise/web dev"
}

if (Test-TcpPort 43123) {
    Write-Host "Web already listening on http://127.0.0.1:43123"
} else {
    Write-Host "Starting web..."
    Start-DevWindow "Keepwise web" $webCommand
}

Write-Host ""
Write-Host "Web  http://127.0.0.1:43123"
Write-Host "API  http://127.0.0.1:43124  (health: /health, hangfire: /hangfire)"
Write-Host "Android: powershell -ExecutionPolicy Bypass -File .\scripts\android.ps1"
Write-Host "Close the API and web terminal windows to stop those processes."
