param(
    [string]$BaseUrl = "http://localhost:5025",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111",
    [int]$StartupTimeoutSeconds = 20,
    [int]$RequestTimeoutSeconds = 10,
    [int]$ShutdownTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

function Get-AntiForgeryToken {
    param([string]$Html)

    $match = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) {
        throw "Antiforgery token not found."
    }

    return $match.Groups[1].Value
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
        -ContentType "application/json" `
        -Body ($Body | ConvertTo-Json -Depth 10)
}

function Invoke-JsonGet {
    param([string]$Uri)

    Invoke-RestMethod `
        -Method Get `
        -Uri $Uri `
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

try {
    & (Join-Path $PSScriptRoot "start-web.ps1") `
        -BaseUrl $base `
        -StartupTimeoutSeconds $StartupTimeoutSeconds

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    Write-Host "External intake review smoke test"
    Write-Host "Base URL: $base"

    $incident = Invoke-JsonPost "$base/api/v1/rca/incidents" @{
        tenantId = $TenantId
        sourceSystem = "REVIEW_SMOKE"
        externalTaskId = "REVIEW-$timestamp"
        title = "Review intake smoke $timestamp"
        problemDescription = "Validacion de revision interna de intake externo."
        severity = "High"
        claimActorType = "Supplier"
        claimOwnerName = "Proveedor smoke"
        occurredAt = (Get-Date).ToString("o")
        reportedBy = "review-smoke"
    }
    Assert-Success $incident "Create incident"

    $incidentId = $incident.data.id
    Write-Host "Created incident: $incidentId"

    $canvas = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/canvas"
    Assert-Success $canvas "Get canvas"
    $branchId = $canvas.data.branches[0].id
    Write-Host "Using branch: $($canvas.data.branches[0].name)"

    $details = Invoke-WebRequest `
        -Uri "$base/Rca/Details/$incidentId" `
        -SessionVariable internalSession `
        -UseBasicParsing `
        -TimeoutSec $RequestTimeoutSeconds

    $internalToken = Get-AntiForgeryToken $details.Content
    $created = Invoke-WebRequest `
        -Method Post `
        -Uri "$base/Rca/CreateExternalIntake/$incidentId" `
        -WebSession $internalSession `
        -UseBasicParsing `
        -Body @{
            "__RequestVerificationToken" = $internalToken
            "ExternalIntake.ActorType" = "Supplier"
            "ExternalIntake.ActorName" = "Proveedor smoke"
            "ExternalIntake.ContactName" = "Calidad proveedor"
            "ExternalIntake.ContactEmail" = "proveedor@example.com"
        } `
        -ContentType "application/x-www-form-urlencoded" `
        -TimeoutSec $RequestTimeoutSeconds

    $linkMatch = [regex]::Match($created.Content, '/external-intake/([^"<]+)')
    if (-not $linkMatch.Success) {
        throw "External intake link not found."
    }

    $token = $linkMatch.Groups[1].Value
    Write-Host "Created external intake token."

    $portal = Invoke-WebRequest `
        -Uri "$base/external-intake/$token" `
        -SessionVariable externalSession `
        -UseBasicParsing `
        -TimeoutSec $RequestTimeoutSeconds

    $externalToken = Get-AntiForgeryToken $portal.Content
    $submitted = Invoke-WebRequest `
        -Method Post `
        -Uri "$base/external-intake/$token" `
        -WebSession $externalSession `
        -UseBasicParsing `
        -Body @{
            "__RequestVerificationToken" = $externalToken
            ContactName = "Calidad proveedor"
            ContactEmail = "proveedor@example.com"
            ClaimReference = "SUP-REVIEW-001"
            MaterialCode = "MAT-SMOKE"
            BatchOrLot = "LOT-SMOKE"
            Description = "Proveedor detecto variacion dimensional en lote smoke."
            ContainmentResponse = "Lote segregado y certificado retenido."
            ProposedRootCause = "Parametro de proceso fuera de ventana en proveedor."
            ProposedCorrectiveAction = "Recalibrar dispositivo de control y enviar certificado."
            EvidenceSummary = "Registro dimensional y foto del calibre."
        } `
        -ContentType "application/x-www-form-urlencoded" `
        -TimeoutSec $RequestTimeoutSeconds

    if ($submitted.Content -notmatch "Respuesta enviada") {
        throw "External submit confirmation not found."
    }

    Write-Host "Submitted external response."

    $detailsAfterSubmit = Invoke-WebRequest `
        -Uri "$base/Rca/Details/$incidentId" `
        -WebSession $internalSession `
        -UseBasicParsing `
        -TimeoutSec $RequestTimeoutSeconds

    $reviewToken = Get-AntiForgeryToken $detailsAfterSubmit.Content
    $intakeMatch = [regex]::Match($detailsAfterSubmit.Content, 'name="intakeId" value="([^"]+)"')
    if (-not $intakeMatch.Success) {
        throw "Submitted intake id not found."
    }

    $intakeId = $intakeMatch.Groups[1].Value
    Invoke-WebRequest `
        -Method Post `
        -Uri "$base/Rca/ReviewExternalIntake/$incidentId" `
        -WebSession $internalSession `
        -UseBasicParsing `
        -Body @{
            "__RequestVerificationToken" = $reviewToken
            intakeId = $intakeId
            branchId = $branchId
            importCause = "true"
            markCauseAsRoot = "false"
            importCorrectiveAction = "true"
            reviewedByUserId = "review-smoke"
        } `
        -ContentType "application/x-www-form-urlencoded" `
        -TimeoutSec $RequestTimeoutSeconds | Out-Null

    $canvasAfter = Invoke-JsonGet "$base/api/v1/rca/incidents/$incidentId/canvas"
    Assert-Success $canvasAfter "Get canvas after review"
    $snapshot = Invoke-JsonGet "$base/api/v1/integrations/rca/incidents/$incidentId/snapshot"
    Assert-Success $snapshot "Get snapshot after review"
    $events = Invoke-JsonGet "$base/api/v1/integrations/rca/events?incidentId=$incidentId"
    Assert-Success $events "Get integration events after review"

    $importedCause = $canvasAfter.data.causes | Where-Object { $_.title -eq "Parametro de proceso fuera de ventana en proveedor." }
    if (-not $importedCause) {
        throw "Imported external cause not found."
    }

    $importedAction = $snapshot.data.openActions | Where-Object { $_.title -eq "Recalibrar dispositivo de control y enviar certificado." }
    if (-not $importedAction) {
        throw "Imported external action not found."
    }

    foreach ($eventType in @("RcaExternalIntakeCreated", "RcaExternalIntakeOpened", "RcaExternalIntakeSubmitted", "RcaExternalIntakeReviewed")) {
        if (-not ($events.data | Where-Object { $_.type -eq $eventType })) {
            throw "Expected integration event not found: $eventType"
        }
    }

    Write-Host "External review smoke completed. Incident=$incidentId Intake=$intakeId"
}
finally {
    & (Join-Path $PSScriptRoot "stop-web.ps1") `
        -ShutdownTimeoutSeconds $ShutdownTimeoutSeconds
}
