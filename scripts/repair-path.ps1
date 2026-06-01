param(
    [switch]$Machine
)

$ErrorActionPreference = "Stop"

function Normalize-PathList {
    param([string]$Value)

    $seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    $parts = New-Object "System.Collections.Generic.List[string]"

    foreach ($part in ($Value -split ";")) {
        $trimmed = $part.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        $dedupeKey = $trimmed.TrimEnd("\")
        if ($seen.Add($dedupeKey)) {
            $parts.Add($trimmed)
        }
    }

    return ($parts -join ";")
}

function Repair-PathRegistryValue {
    param(
        [Microsoft.Win32.RegistryHive]$Hive,
        [string]$SubKey,
        [string]$Label
    )

    $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey($Hive, [Microsoft.Win32.RegistryView]::Default)
    $key = $baseKey.OpenSubKey($SubKey, $true)
    if ($null -eq $key) {
        Write-Host "$Label Path key not found."
        return
    }

    try {
        $names = @($key.GetValueNames())
        $canonicalName = $names | Where-Object { $_ -ceq "Path" } | Select-Object -First 1
        $upperName = $names | Where-Object { $_ -ceq "PATH" } | Select-Object -First 1

        $pathName = if ($canonicalName) { $canonicalName } elseif ($upperName) { $upperName } else { $null }
        if (-not $pathName) {
            Write-Host "$Label Path value not found."
            return
        }

        $kind = $key.GetValueKind($pathName)
        $pathValue = [string]$key.GetValue($pathName, "", [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $normalized = Normalize-PathList $pathValue

        if ($pathName -cne "Path") {
            $key.SetValue("Path", $normalized, $kind)
            $key.DeleteValue($pathName, $false)
        }
        else {
            $key.SetValue("Path", $normalized, $kind)
        }

        if ($upperName -and $upperName -cne "Path") {
            $key.DeleteValue($upperName, $false)
        }

        Write-Host "$Label Path repaired."
    }
    finally {
        $key.Dispose()
        $baseKey.Dispose()
    }
}

Repair-PathRegistryValue `
    -Hive ([Microsoft.Win32.RegistryHive]::CurrentUser) `
    -SubKey "Environment" `
    -Label "User"

if ($Machine) {
    Repair-PathRegistryValue `
        -Hive ([Microsoft.Win32.RegistryHive]::LocalMachine) `
        -SubKey "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" `
        -Label "Machine"
}

& (Join-Path $PSScriptRoot "normalize-session-env.ps1")
