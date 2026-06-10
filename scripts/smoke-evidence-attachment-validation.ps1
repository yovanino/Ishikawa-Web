param(
    [string]$BaseUrl = "http://localhost:5025",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111",
    [int]$RequestTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

$Headers = @{
    "X-RCA-TenantId" = $TenantId
    "X-RCA-UserId" = "quality"
    "X-RCA-Roles" = "Quality,Supervisor,Maintenance,Administrator"
}

function Invoke-JsonPost {
    param(
        [string]$Uri,
        [object]$Body
    )

    Invoke-RestMethod `
        -Method Post `
        -Uri $Uri `
        -TimeoutSec $RequestTimeoutSeconds `
        -Headers $Headers `
        -ContentType "application/json" `
        -Body ($Body | ConvertTo-Json -Depth 10)
}

function Assert-Success {
    param(
        [object]$Result,
        [string]$Step
    )

    if (-not $Result.success) {
        throw "$Step failed: $($Result.message)"
    }
}

function Assert-InvalidAttachmentUpload {
    param(
        [string]$Uri,
        [string]$FilePath
    )

    Add-Type -AssemblyName System.Net.Http

    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
    foreach ($key in $Headers.Keys) {
        $client.DefaultRequestHeaders.Add($key, [string]$Headers[$key])
    }

    $content = [System.Net.Http.MultipartFormDataContent]::new()
    $stream = $null

    try {
        $content.Add([System.Net.Http.StringContent]::new("Invalid executable smoke"), "Title")
        $content.Add([System.Net.Http.StringContent]::new("Document"), "EvidenceType")
        $content.Add([System.Net.Http.StringContent]::new("Manual"), "Source")
        $content.Add([System.Net.Http.StringContent]::new("Archivo no permitido para validar hardening."), "Summary")
        $content.Add([System.Net.Http.StringContent]::new("quality"), "CapturedByUserId")

        $stream = [System.IO.File]::OpenRead($FilePath)
        $fileContent = [System.Net.Http.StreamContent]::new($stream)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")
        $content.Add($fileContent, "Attachment", [System.IO.Path]::GetFileName($FilePath))

        $response = $client.PostAsync($Uri, $content).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if ([int]$response.StatusCode -ne 400) {
            throw "Expected HTTP 400, got $([int]$response.StatusCode). Body: $body"
        }

        if ($body -notmatch '"success"\s*:\s*false') {
            throw "Expected ApiResult success=false. Body: $body"
        }

        if ($body -notmatch '"code"\s*:\s*"INVALID_ATTACHMENT"') {
            throw "Expected INVALID_ATTACHMENT. Body: $body"
        }
    }
    finally {
        if ($null -ne $stream) {
            $stream.Dispose()
        }

        $content.Dispose()
        $client.Dispose()
    }
}

$base = $BaseUrl.TrimEnd("/")
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "Ishikawa RCA evidence attachment validation smoke test"
Write-Host "Base URL: $base"
Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

$incident = Invoke-JsonPost "$base/api/v1/rca/incidents" @{
    tenantId = $TenantId
    sourceSystem = "SMOKE_ATTACHMENTS"
    externalTaskId = "ATTACH-$timestamp"
    title = "Evidence attachment validation smoke $timestamp"
    problemDescription = "Validacion de rechazo de adjuntos no permitidos."
    severity = "Medium"
    occurredAt = (Get-Date).ToString("o")
    reportedBy = "smoke-attachments"
}
Assert-Success $incident "Create incident"
$incidentId = $incident.data.id
Write-Host "Created incident: $incidentId"

$artifactDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) "artifacts"
if (-not (Test-Path $artifactDir)) {
    New-Item -ItemType Directory -Path $artifactDir | Out-Null
}

$invalidFile = Join-Path $artifactDir "invalid-evidence-$timestamp.exe"
Set-Content -Path $invalidFile -Value "not really executable" -Encoding ASCII

try {
    Assert-InvalidAttachmentUpload `
        -Uri "$base/api/v1/rca/incidents/$incidentId/evidence-files" `
        -FilePath $invalidFile
}
finally {
    Remove-Item $invalidFile -ErrorAction SilentlyContinue
}

Write-Host "Validated 400/INVALID_ATTACHMENT for disallowed evidence extension."
Write-Host "Evidence attachment validation smoke test completed successfully."
