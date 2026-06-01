param()

$ErrorActionPreference = "Stop"

function Get-RegistryPathValue {
    param(
        [Microsoft.Win32.RegistryHive]$Hive,
        [string]$SubKey
    )

    $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey($Hive, [Microsoft.Win32.RegistryView]::Default)
    $key = $baseKey.OpenSubKey($SubKey, $false)
    if ($null -eq $key) {
        return $null
    }

    try {
        $pathName = $key.GetValueNames() | Where-Object { $_ -ceq "Path" } | Select-Object -First 1
        if ($pathName) {
            return [string]$key.GetValue($pathName, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        }

        $pathName = $key.GetValueNames() | Where-Object { $_ -ceq "PATH" } | Select-Object -First 1
        if ($pathName) {
            return [string]$key.GetValue($pathName, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        }

        return $null
    }
    finally {
        $key.Dispose()
        $baseKey.Dispose()
    }
}

$machinePath = Get-RegistryPathValue `
    -Hive ([Microsoft.Win32.RegistryHive]::LocalMachine) `
    -SubKey "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
$userPath = Get-RegistryPathValue `
    -Hive ([Microsoft.Win32.RegistryHive]::CurrentUser) `
    -SubKey "Environment"

$pathParts = @($machinePath, $userPath) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object { $_.Trim(";") } |
    Where-Object { $_ }

$normalizedPath = $pathParts -join ";"

if ([string]::IsNullOrWhiteSpace($normalizedPath)) {
    $normalizedPath = [System.Environment]::GetEnvironmentVariable("Path", "Process")
}

$normalizedPath = [System.Environment]::ExpandEnvironmentVariables($normalizedPath)

[System.Environment]::SetEnvironmentVariable("PATH", $null, "Process")
[System.Environment]::SetEnvironmentVariable("Path", $normalizedPath, "Process")

Write-Host "Session Path normalized."
