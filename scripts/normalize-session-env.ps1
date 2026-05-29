param()

$ErrorActionPreference = "Stop"

function Get-RegistryPathValue {
    param(
        [string]$Key,
        [string]$Name
    )

    $output = & reg.exe query $Key /v $Name 2>$null
    $line = $output | Where-Object { $_ -match "\s$Name\s+REG_\w+\s+" } | Select-Object -First 1
    if ($line -match "REG_\w+\s+(.*)$") {
        return $Matches[1]
    }

    return $null
}

$machinePath = Get-RegistryPathValue "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"
$userPath = Get-RegistryPathValue "HKCU\Environment" "Path"

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
