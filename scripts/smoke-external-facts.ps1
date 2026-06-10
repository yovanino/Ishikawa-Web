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

function Invoke-JsonGet {
    param([string]$Uri)

    Invoke-RestMethod `
        -Method Get `
        -Uri $Uri `
        -Headers $Headers `
        -TimeoutSec $RequestTimeoutSeconds
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

function Assert-BadRequestCode {
    param(
        [string]$Uri,
        [object]$Body,
        [string]$ExpectedErrorCode
    )

    Add-Type -AssemblyName System.Net.Http

    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
    foreach ($key in $Headers.Keys) {
        $client.DefaultRequestHeaders.Add($key, [string]$Headers[$key])
    }

    try {
        $json = $Body | ConvertTo-Json -Depth 10
        $content = [System.Net.Http.StringContent]::new(
            $json,
            [System.Text.Encoding]::UTF8,
            "application/json")
        $response = $client.PostAsync($Uri, $content).GetAwaiter().GetResult()
        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if ([int]$response.StatusCode -ne 400) {
            throw "Expected HTTP 400, got $([int]$response.StatusCode). Body: $responseBody"
        }

        if ($responseBody -notmatch ('"code"\s*:\s*"' + [regex]::Escape($ExpectedErrorCode) + '"')) {
            throw "Expected ApiError code $ExpectedErrorCode. Body: $responseBody"
        }
    }
    finally {
        $client.Dispose()
    }
}

$base = $BaseUrl.TrimEnd("/")
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$externalEventId = "PLC4-ALM-$timestamp"

Write-Host "Ishikawa RCA external facts smoke test"
Write-Host "Base URL: $base"
Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

$incident = Invoke-JsonPost "$base/api/v1/rca/incidents" @{
    tenantId = $TenantId
    sourceSystem = "SMOKE_FACTS"
    externalTaskId = "FACTS-$timestamp"
    title = "External facts smoke RCA $timestamp"
    problemDescription = "Validacion de facts externos por API."
    severity = "Medium"
    occurredAt = (Get-Date).ToString("o")
    machineCode = "PRENSA-4"
    lineCode = "L2"
    workOrderCode = "WO-FACTS-$timestamp"
    reportedBy = "smoke-facts"
}
Assert-Success $incident "Create incident"
$incidentId = $incident.data.id
Write-Host "Created incident: $incidentId"

$factPayload = @{
    title = "Alarma de presion fuera de rango"
    description = "SCADA detecto presion alta durante el ciclo."
    factType = "Alarm"
    source = "SCADA"
    sourceDetail = "Linea L2 / PLC prensa 4"
    factSeverity = "High"
    externalSourceSystem = "SCADA"
    externalEventId = $externalEventId
    externalRecordUri = "scada://linea-2/prensa-4/events/$externalEventId"
    machineCode = "PRENSA-4"
    lineCode = "L2"
    workOrderCode = "WO-FACTS-$timestamp"
    alarmCode = "PRES-HIGH"
    occurredAt = (Get-Date).ToString("o")
    capturedByUserId = "gateway-smoke"
}

$fact = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/facts" $factPayload
Assert-Success $fact "Add external fact"
Write-Host "Added external fact: $($fact.data.id)"

$duplicateFact = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/facts" $factPayload
Assert-Success $duplicateFact "Add duplicate external fact"

if ($duplicateFact.data.id -ne $fact.data.id) {
    throw "External fact idempotency failed: expected $($fact.data.id), got $($duplicateFact.data.id)"
}

if ($duplicateFact.message -ne "Hecho externo existente.") {
    throw "External fact idempotency message mismatch: $($duplicateFact.message)"
}
Write-Host "Validated external fact idempotency for event: $externalEventId"

$facts = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/facts"
Assert-Success $facts "List facts"
$matchingFacts = @($facts.data | Where-Object { $_.externalSourceSystem -eq "SCADA" -and $_.externalEventId -eq $externalEventId })
if ($matchingFacts.Count -ne 1) {
    throw "Expected exactly one matching external fact, found $($matchingFacts.Count)."
}
Write-Host "Validated external fact list contains one correlated event."

Assert-BadRequestCode `
    -Uri "$base/api/v1/rca/incidents/$incidentId/facts" `
    -Body @{
        title = "Incomplete external fact"
        factType = "Alarm"
        source = "SCADA"
        externalSourceSystem = "SCADA"
    } `
    -ExpectedErrorCode "EXTERNAL_FACT_CORRELATION_INCOMPLETE"
Write-Host "Validated incomplete external fact correlation rejection."

Write-Host "External facts smoke test completed successfully."
