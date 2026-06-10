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
    param(
        [string]$Uri
    )

    Invoke-RestMethod `
        -Method Get `
        -Uri $Uri `
        -Headers $Headers `
        -TimeoutSec $RequestTimeoutSeconds
}

function Invoke-MultipartPost {
    param(
        [string]$Uri,
        [hashtable]$Fields,
        [string]$FilePath,
        [string]$FileFieldName = "Attachment",
        [string]$FileContentType = "text/plain"
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
        foreach ($key in $Fields.Keys) {
            if ($null -ne $Fields[$key]) {
                $content.Add([System.Net.Http.StringContent]::new([string]$Fields[$key]), $key)
            }
        }

        $stream = [System.IO.File]::OpenRead($FilePath)
        $fileContent = [System.Net.Http.StreamContent]::new($stream)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($FileContentType)
        $content.Add($fileContent, $FileFieldName, [System.IO.Path]::GetFileName($FilePath))

        $response = $client.PostAsync($Uri, $content).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            throw "Multipart post failed: $($response.StatusCode) $body"
        }

        return $body | ConvertFrom-Json
    }
    finally {
        if ($null -ne $stream) {
            $stream.Dispose()
        }

        $content.Dispose()
        $client.Dispose()
    }
}

function Invoke-BinaryGet {
    param(
        [string]$Uri
    )

    Add-Type -AssemblyName System.Net.Http

    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
    foreach ($key in $Headers.Keys) {
        $client.DefaultRequestHeaders.Add($key, [string]$Headers[$key])
    }

    try {
        $response = $client.GetAsync($Uri).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            throw "Binary get failed: $($response.StatusCode) $([System.Text.Encoding]::UTF8.GetString($body))"
        }

        return [pscustomobject]@{
            Bytes = $body
            ContentType = $response.Content.Headers.ContentType.MediaType
            ContentDisposition = [string]$response.Content.Headers.ContentDisposition
        }
    }
    finally {
        $client.Dispose()
    }
}

function Assert-ByteArrayEqual {
    param(
        [byte[]]$Actual,
        [byte[]]$Expected,
        [string]$Step
    )

    if ($Actual.Length -ne $Expected.Length) {
        throw "$Step failed: expected $($Expected.Length) bytes, got $($Actual.Length)"
    }

    for ($i = 0; $i -lt $Expected.Length; $i++) {
        if ($Actual[$i] -ne $Expected[$i]) {
            throw "$Step failed: byte mismatch at offset $i"
        }
    }
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
Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

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

$wizardProblem = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Problem"
    completedByUserId = "quality"
    notes = "Problema definido por smoke test."
}
Assert-Success $wizardProblem "Complete wizard problem step"

$canvas = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/canvas"
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

$wizardCauses = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Causes"
    completedByUserId = "quality"
    notes = "Causas iniciales cargadas."
}
Assert-Success $wizardCauses "Complete wizard causes step"

$subCauseBody = @{
    branchId = $branchId
    parentCauseId = $cause.data.id
    title = "Smoke subcause"
    description = "Subcausa creada por smoke test."
    probabilityScore = 2
    impactScore = 3
    frequencyScore = 2
    isRootCause = $false
    evidenceSummary = "Subcausa asociada a la causa principal."
}

$subCause = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/causes" $subCauseBody
Assert-Success $subCause "Add subcause"
if ($subCause.data.parentCauseId -ne $cause.data.id) {
    throw "Add subcause failed: expected parentCauseId to match parent cause"
}
Write-Host "Added subcause: $($subCause.data.id)"

$evidenceBody = @{
    causeId = $cause.data.id
    title = "Smoke evidence"
    evidenceType = "Observation"
    source = "Manual"
    summary = "Registro de evidencia creado por smoke test."
    referenceUri = "https://example.com/evidence/smoke"
    capturedByUserId = "quality"
    validationStatus = "Validated"
    validatedByUserId = "quality"
    validationNotes = "Evidencia validada por smoke test."
}

$evidence = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/evidence" $evidenceBody
Assert-Success $evidence "Add evidence"
Write-Host "Added evidence: $($evidence.data.id)"

$artifactDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) "artifacts"
if (-not (Test-Path $artifactDir)) {
    New-Item -ItemType Directory -Path $artifactDir | Out-Null
}

$evidenceFilePath = Join-Path $artifactDir "smoke-evidence-$timestamp.txt"
Set-Content -Path $evidenceFilePath -Value "Smoke evidence attachment $timestamp" -Encoding UTF8
$fileEvidence = Invoke-MultipartPost `
    -Uri "$base/api/v1/rca/incidents/$incidentId/evidence-files" `
    -Fields @{
        Title = "Smoke evidence file"
        EvidenceType = "Document"
        Source = "Manual"
        Summary = "Archivo adjunto creado por smoke test."
        CauseId = $cause.data.id
        CapturedByUserId = "quality"
        ValidationStatus = "Validated"
        ValidatedByUserId = "quality"
        ValidationNotes = "Adjunto validado por smoke test."
    } `
    -FilePath $evidenceFilePath `
    -FileContentType "text/plain"
Assert-Success $fileEvidence "Add evidence file"
if ([string]::IsNullOrWhiteSpace($fileEvidence.data.attachmentStorageKey)) {
    throw "Add evidence file failed: expected attachmentStorageKey"
}

$downloadedEvidenceFile = Invoke-BinaryGet "$base/api/v1/rca/incidents/$incidentId/evidence/$($fileEvidence.data.id)/attachment"
$expectedEvidenceBytes = [System.IO.File]::ReadAllBytes($evidenceFilePath)
Assert-ByteArrayEqual $downloadedEvidenceFile.Bytes $expectedEvidenceBytes "Download evidence attachment"
if ($downloadedEvidenceFile.ContentType -ne "text/plain") {
    throw "Download evidence attachment failed: expected content-type text/plain, got $($downloadedEvidenceFile.ContentType)"
}

$expectedEvidenceFileName = [System.IO.Path]::GetFileName($evidenceFilePath)
if ($downloadedEvidenceFile.ContentDisposition -notmatch [regex]::Escape($expectedEvidenceFileName)) {
    throw "Download evidence attachment failed: expected content-disposition to include $expectedEvidenceFileName"
}
Write-Host "Added evidence file: $($fileEvidence.data.id)"
Write-Host "Downloaded evidence file through controlled endpoint."

$wizardEvidence = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Evidence"
    completedByUserId = "quality"
    notes = "Evidencia registrada."
}
Assert-Success $wizardEvidence "Complete wizard evidence step"

$evidenceList = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/evidence"
Assert-Success $evidenceList "List evidence"
if ($evidenceList.data.Count -lt 1) {
    throw "List evidence failed: expected at least one evidence record"
}

$actionBody = @{
    causeId = $cause.data.id
    title = "Smoke corrective action"
    description = "Accion creada por smoke test."
    actionType = "Corrective"
    resolutionScope = "RootCause"
    assignedToUserId = "maintenance"
    dueDate = (Get-Date).AddDays(2).ToString("o")
}

$action = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions" $actionBody
Assert-Success $action "Add corrective action"
Write-Host "Added action: $($action.data.id)"

$recurrenceActionBody = @{
    causeId = $cause.data.id
    title = "Smoke recurrence prevention"
    description = "Accion preventiva de recurrencia creada por smoke test."
    actionType = "RecurrencePreventive"
    resolutionScope = "RootCause"
    assignedToUserId = "quality"
    dueDate = (Get-Date).AddDays(3).ToString("o")
}

$recurrenceAction = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions" $recurrenceActionBody
Assert-Success $recurrenceAction "Add recurrence preventive action"
Write-Host "Added recurrence action: $($recurrenceAction.data.id)"

$wizardActions = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Actions"
    completedByUserId = "quality"
    notes = "Accion correctiva registrada."
}
Assert-Success $wizardActions "Complete wizard actions step"

$actionStatusBody = @{
    status = "Completed"
    completedByUserId = "quality"
    validationNotes = "Validacion smoke: accion completada con evidencia registrada."
}

$actionStatus = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions/$($action.data.id)/status" $actionStatusBody
Assert-Success $actionStatus "Complete corrective action"
if ($actionStatus.data.status -ne "Completed") {
    throw "Complete corrective action failed: expected Completed status"
}
Write-Host "Completed action: $($actionStatus.data.id)"

$recurrenceActionStatusBody = @{
    status = "Completed"
    completedByUserId = "quality"
    validationNotes = "Validacion smoke: prevencion de recurrencia completada."
}

$recurrenceActionStatus = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/actions/$($recurrenceAction.data.id)/status" $recurrenceActionStatusBody
Assert-Success $recurrenceActionStatus "Complete recurrence preventive action"
if ($recurrenceActionStatus.data.status -ne "Completed") {
    throw "Complete recurrence preventive action failed: expected Completed status"
}
Write-Host "Completed recurrence action: $($recurrenceActionStatus.data.id)"

$wizardValidation = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Validation"
    completedByUserId = "quality"
    notes = "Accion validada."
}
Assert-Success $wizardValidation "Complete wizard validation step"

$escalationBody = @{
    escalatedByUserId = "quality"
    escalationReason = "Smoke escalation: RCA requiere seguimiento 8D formal."
}

$escalatedIncident = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/escalate-8d" $escalationBody
Assert-Success $escalatedIncident "Escalate incident to 8D"
if (-not $escalatedIncident.data.escalatedTo8D) {
    throw "Escalate incident to 8D failed: expected escalatedTo8D true"
}
Write-Host "Escalated incident to 8D: $($escalatedIncident.data.id)"

$closeBody = @{
    closedByUserId = "quality"
    closureSummary = "Smoke RCA cerrado con causa raiz, subcausa, evidencia y accion completada."
}

$closedIncident = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/close" $closeBody
Assert-Success $closedIncident "Close incident"
if ($closedIncident.data.status -ne "Closed") {
    throw "Close incident failed: expected Closed status"
}
Write-Host "Closed incident: $($closedIncident.data.id)"

$wizardClosed = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/wizard/step" @{
    step = "Closed"
    completedByUserId = "quality"
    notes = "RCA cerrado formalmente."
}
Assert-Success $wizardClosed "Complete wizard closed step"

$snapshot = Invoke-JsonGet "$base/api/v1/integrations/rca/incidents/$incidentId/snapshot"
Assert-Success $snapshot "Get integration snapshot"
Write-Host "Snapshot root cause: $($snapshot.data.rootCauseTitle)"
if ($snapshot.data.status -ne "Closed") {
    throw "Get integration snapshot failed: expected Closed status"
}
if (-not $snapshot.data.escalatedTo8D) {
    throw "Get integration snapshot failed: expected escalatedTo8D true"
}
if ($snapshot.data.wizardStep -ne "Closed") {
    throw "Get integration snapshot failed: expected wizardStep Closed"
}
if ($snapshot.data.evidenceCount -lt 1) {
    throw "Get integration snapshot failed: expected evidenceCount >= 1"
}
if ($snapshot.data.causeCount -lt 2) {
    throw "Get integration snapshot failed: expected causeCount >= 2"
}
if ($snapshot.data.openCorrectiveActionsCount -ne 0) {
    throw "Get integration snapshot failed: expected openCorrectiveActionsCount = 0"
}

$events = Invoke-JsonGet "$base/api/v1/integrations/rca/events?incidentId=$incidentId"
Assert-Success $events "Get integration events"
Write-Host "Events returned: $($events.data.Count)"
if (-not ($events.data | Where-Object { $_.type -eq "RcaEvidenceAttached" })) {
    throw "Get integration events failed: expected RcaEvidenceAttached event"
}
if (-not ($events.data | Where-Object { $_.type -eq "RcaCorrectiveActionCompleted" })) {
    throw "Get integration events failed: expected RcaCorrectiveActionCompleted event"
}
if (-not ($events.data | Where-Object { $_.type -eq "RcaEscalatedTo8D" })) {
    throw "Get integration events failed: expected RcaEscalatedTo8D event"
}
if (-not ($events.data | Where-Object { $_.type -eq "RcaWizardStepCompleted" })) {
    throw "Get integration events failed: expected RcaWizardStepCompleted event"
}
if (-not ($events.data | Where-Object { $_.type -eq "RcaClosed" })) {
    throw "Get integration events failed: expected RcaClosed event"
}

$aiSummary = Invoke-JsonPost "$base/api/v1/rca/incidents/$incidentId/ai/summarize" @{}
Assert-Success $aiSummary "AI summarize"
Write-Host "AI provider: $($aiSummary.data.metadata.provider)"

Write-Host "Smoke test completed successfully."
