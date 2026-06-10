param(
    [string]$BaseUrl = "http://localhost:5025",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111",
    [int]$RequestTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$base = $BaseUrl.TrimEnd("/")
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
$client.DefaultRequestHeaders.Add("X-RCA-TenantId", $TenantId)
$client.DefaultRequestHeaders.Add("X-RCA-UserId", "quality")
$client.DefaultRequestHeaders.Add("X-RCA-Roles", "Quality,Supervisor,Maintenance,Administrator")

try {
    Write-Host "Ishikawa RCA API model validation smoke test"
    Write-Host "Base URL: $base"
    Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

    $payload = @{
        tenantId = $TenantId
        sourceSystem = "SMOKE_VALIDATION"
        title = "Invalid model-state smoke"
        problemDescription = "Payload intentionally uses an invalid occurredAt value."
        severity = "Medium"
        occurredAt = "not-a-date"
        reportedBy = "smoke-validation"
    } | ConvertTo-Json -Depth 10

    $content = [System.Net.Http.StringContent]::new(
        $payload,
        [System.Text.Encoding]::UTF8,
        "application/json")
    $response = $client.PostAsync("$base/api/v1/rca/incidents", $content).GetAwaiter().GetResult()
    $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if ([int]$response.StatusCode -ne 400) {
        throw "Expected HTTP 400, got $([int]$response.StatusCode). Body: $body"
    }

    if ($body -notmatch '"success"\s*:\s*false') {
        throw "Expected ApiResult success=false. Body: $body"
    }

    if ($body -notmatch '"code"\s*:\s*"MODEL_VALIDATION_ERROR"') {
        throw "Expected MODEL_VALIDATION_ERROR. Body: $body"
    }

    if ($body -notmatch '"correlationId"\s*:') {
        throw "Expected correlationId in API validation error. Body: $body"
    }

    Write-Host "Validated 400/MODEL_VALIDATION_ERROR for invalid incident payload."
    Write-Host "API model validation smoke test completed successfully."
}
finally {
    $client.Dispose()
}
