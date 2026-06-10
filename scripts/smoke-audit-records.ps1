param(
    [string]$BaseUrl = "http://localhost:5025",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111",
    [int]$RequestTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

$Headers = @{
    "X-RCA-TenantId" = $TenantId
    "X-RCA-UserId" = "quality"
    "X-RCA-Roles" = "Quality,Supervisor,Administrator"
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
    param(
        [string]$Uri
    )

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

$base = $BaseUrl.TrimEnd("/")
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "Ishikawa RCA audit records smoke test"
Write-Host "Base URL: $base"
Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

$incident = Invoke-JsonPost "$base/api/v1/rca/incidents" @{
    tenantId = $TenantId
    sourceSystem = "AUDIT_SMOKE"
    externalTaskId = "AUDIT-$timestamp"
    title = "Audit smoke RCA $timestamp"
    problemDescription = "Validacion de consulta de auditoria RCA."
    severity = "Medium"
    occurredAt = (Get-Date).ToString("o")
    reportedBy = "audit-smoke"
}
Assert-Success $incident "Create incident"
$incidentId = $incident.data.id

$action = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions" @{
    title = "Audit smoke corrective action"
    description = "Accion para generar auditoria de cambio de estado."
    actionType = "Corrective"
    resolutionScope = "RootCause"
    assignedToUserId = "maintenance"
    dueDate = (Get-Date).AddDays(1).ToString("o")
}
Assert-Success $action "Add corrective action"

$actionStatus = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions/$($action.data.id)/status" @{
    status = "Completed"
    completedByUserId = "quality"
    validationNotes = "Audit smoke: accion completada para validar auditoria."
}
Assert-Success $actionStatus "Complete corrective action"

$audit = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/audit"
Assert-Success $audit "List audit records"

if ($audit.data.Count -lt 1) {
    throw "List audit records failed: expected at least one audit record"
}

$statusAudit = $audit.data | Where-Object { $_.action -eq "CorrectiveActionStatusChanged" } | Select-Object -First 1
if ($null -eq $statusAudit) {
    throw "List audit records failed: expected CorrectiveActionStatusChanged"
}

if ($statusAudit.entityType -ne "CorrectiveAction") {
    throw "List audit records failed: expected entityType CorrectiveAction"
}

if ($statusAudit.userId -ne "quality") {
    throw "List audit records failed: expected userId quality"
}

if ($statusAudit.summary -notmatch "Completed") {
    throw "List audit records failed: expected summary to include Completed"
}

Write-Host "Validated incident audit records endpoint."
Write-Host "Audit records smoke test completed successfully."
