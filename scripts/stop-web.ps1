param(
    [int]$ShutdownTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$pidFile = Join-Path $repoRoot "artifacts\ishikawa-web.pid"

if (-not (Test-Path $pidFile)) {
    Write-Host "PID file not found. Nothing to stop."
    exit 0
}

$pidValue = (Get-Content $pidFile | Select-Object -First 1).Trim()
if ([string]::IsNullOrWhiteSpace($pidValue)) {
    Remove-Item $pidFile -Force
    Write-Host "PID file was empty. Removed."
    exit 0
}

$process = Get-Process -Id ([int]$pidValue) -ErrorAction SilentlyContinue
if ($null -eq $process) {
    Remove-Item $pidFile -Force
    Write-Host "Process $pidValue is not running. PID file removed."
    exit 0
}

Stop-Process -Id $process.Id -Force

$deadline = (Get-Date).AddSeconds($ShutdownTimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    if ($null -eq (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
        Remove-Item $pidFile -Force
        Write-Host "Stopped Ishikawa RCA web app. PID=$($process.Id)"
        exit 0
    }

    Start-Sleep -Milliseconds 500
}

throw "Timeout waiting for process $($process.Id) to stop after $ShutdownTimeoutSeconds seconds."
