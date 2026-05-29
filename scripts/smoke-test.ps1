param(
    [string]$BaseUrl = "http://localhost:5025",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111"
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Uri,
        [object]$Body
    )

    Invoke-RestMethod `
        -Method Post `
        -Uri $Uri `
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

$base = $BaseUrl.TrimEnd("/")
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "Ishikawa RCA smoke test"
Write-Host "Base URL: $base"

$incidentBody = @{
    tenantId = $TenantId
    sourceSystem = "SMOKE_TEST"
    externalTaskId = "SMOKE-$timestamp"
    title = "Smoke test RCA $timestamp"
    problemDescription = "Validacion end-to-end de API RCA standalone."
    severity = "High"
    occurredAt = (Get-Date).ToString("o")
    machineCode = "TEST-MACHINE"
    lineCode = "TEST-LINE"
    workOrderCode = "TEST-WO"
    reportedBy = "smoke-test"
}

$incident = Invoke-JsonPost "$base/api/v1/rca/incidents" $incidentBody
Assert-Success $incident "Create incident"
$incidentId = $incident.data.id
Write-Host "Created incident: $incidentId"

$canvas = Invoke-RestMethod -Method Get -Uri "$base/api/v1/rca/incidents/$incidentId/canvas"
Assert-Success $canvas "Get canvas"
$branchId = $canvas.data.branches[0].id
Write-Host "Using branch: $($canvas.data.branches[0].name)"

$causeBody = @{
    branchId = $branchId
    title = "Smoke cause"
    description = "Causa creada por smoke test."
    probabilityScore = 3
    impactScore = 4
    frequencyScore = 2
    isRootCause = $true
    evidenceSummary = "Evidencia simulada."
}

$cause = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/causes" $causeBody
Assert-Success $cause "Add cause"
Write-Host "Added cause: $($cause.data.id)"

$actionBody = @{
    causeId = $cause.data.id
    title = "Smoke corrective action"
    description = "Accion creada por smoke test."
    assignedToUserId = "maintenance"
    dueDate = (Get-Date).AddDays(2).ToString("o")
}

$action = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions" $actionBody
Assert-Success $action "Add corrective action"
Write-Host "Added action: $($action.data.id)"

$snapshot = Invoke-RestMethod -Method Get -Uri "$base/api/v1/integrations/rca/incidents/$incidentId/snapshot"
Assert-Success $snapshot "Get integration snapshot"
Write-Host "Snapshot root cause: $($snapshot.data.rootCauseTitle)"

$events = Invoke-RestMethod -Method Get -Uri "$base/api/v1/integrations/rca/events?incidentId=$incidentId"
Assert-Success $events "Get integration events"
Write-Host "Events returned: $($events.data.Count)"

$aiSummary = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/ai/summarize" @{}
Assert-Success $aiSummary "AI summarize"
Write-Host "AI provider: $($aiSummary.data.metadata.provider)"

Write-Host "Smoke test completed successfully."
