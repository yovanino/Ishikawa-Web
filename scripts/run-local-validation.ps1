param(
    [string]$BaseUrl = "http://localhost:5025",
    [int]$StartupTimeoutSeconds = 20,
    [int]$RequestTimeoutSeconds = 10,
    [int]$ShutdownTimeoutSeconds = 10,
    [switch]$Build
)

$ErrorActionPreference = "Stop"

try {
    & (Join-Path $PSScriptRoot "start-web.ps1") `
        -BaseUrl $BaseUrl `
        -StartupTimeoutSeconds $StartupTimeoutSeconds `
        -Build:$Build

    & (Join-Path $PSScriptRoot "smoke-test.ps1") `
        -BaseUrl $BaseUrl `
        -RequestTimeoutSeconds $RequestTimeoutSeconds

    & (Join-Path $PSScriptRoot "smoke-api-auth-errors.ps1") `
        -BaseUrl $BaseUrl `
        -RequestTimeoutSeconds $RequestTimeoutSeconds

    & (Join-Path $PSScriptRoot "smoke-external-facts.ps1") `
        -BaseUrl $BaseUrl `
        -RequestTimeoutSeconds $RequestTimeoutSeconds
}
finally {
    & (Join-Path $PSScriptRoot "stop-web.ps1") `
        -ShutdownTimeoutSeconds $ShutdownTimeoutSeconds
}
