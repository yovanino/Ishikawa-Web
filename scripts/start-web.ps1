param(
    [string]$BaseUrl = "http://localhost:5025",
    [int]$StartupTimeoutSeconds = 20,
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$webProject = Join-Path $repoRoot "src\IshikawaRca.Web\IshikawaRca.Web.csproj"
$webDir = Join-Path $repoRoot "src\IshikawaRca.Web"
$webDll = Join-Path $webDir "bin\Debug\net9.0\IshikawaRca.Web.dll"
$pidFile = Join-Path $repoRoot "artifacts\ishikawa-web.pid"

& (Join-Path $PSScriptRoot "normalize-session-env.ps1")

if ($Build) {
    & dotnet build (Join-Path $repoRoot "IshikawaRca.sln") /m:1 --no-restore
}

if (-not (Test-Path $webDll)) {
    throw "Compiled web DLL not found. Run dotnet build IshikawaRca.sln /m:1 --no-restore first."
}

if (-not (Test-Path (Split-Path $pidFile))) {
    New-Item -ItemType Directory -Path (Split-Path $pidFile) | Out-Null
}

$uri = [System.Uri]$BaseUrl
$port = $uri.Port
if ($port -lt 1) {
    throw "BaseUrl must include an explicit port. Received: $BaseUrl"
}

$netstat = Join-Path $env:SystemRoot "System32\netstat.exe"
$existing = & $netstat -ano | Select-String ":$port\s+.*LISTENING"
if ($existing) {
    throw "Port $port is already in use. Stop the existing process before starting the app."
}

$process = Start-Process `
    -FilePath "C:\Program Files\dotnet\dotnet.exe" `
    -ArgumentList @($webDll, "--urls", $BaseUrl) `
    -WorkingDirectory $webDir `
    -PassThru `
    -WindowStyle Hidden

$process.Id | Set-Content $pidFile
Write-Host "Started Ishikawa RCA web app. PID=$($process.Id)"

try {
    & (Join-Path $PSScriptRoot "test-port.ps1") -HostName "127.0.0.1" -Port $port -TimeoutSeconds $StartupTimeoutSeconds
}
catch {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    throw
}

Write-Host "App is ready at $BaseUrl"
