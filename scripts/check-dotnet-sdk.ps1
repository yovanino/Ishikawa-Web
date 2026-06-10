param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "dotnet was not found in PATH. Install the .NET SDK requested by global.json."
}

$globalJsonPath = Join-Path $RepoRoot "global.json"
$requiredSdk = $null
if (Test-Path $globalJsonPath) {
    $globalJson = Get-Content $globalJsonPath -Raw | ConvertFrom-Json
    $requiredSdk = $globalJson.sdk.version
}

$sdkOutput = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0) {
    throw "dotnet --list-sdks failed. Install the .NET SDK requested by global.json."
}

$installedSdks = @($sdkOutput | ForEach-Object {
    if ($_ -match "^(\S+)\s+\[") {
        $Matches[1]
    }
})

if ($installedSdks.Count -eq 0) {
    throw "No .NET SDKs are registered. Install SDK $requiredSdk or update global.json to an installed SDK."
}

if ($requiredSdk -and ($installedSdks -notcontains $requiredSdk)) {
    throw "Required .NET SDK $requiredSdk was not found. Installed SDKs: $($installedSdks -join ', ')."
}

Write-Host "Found required .NET SDK $requiredSdk."
