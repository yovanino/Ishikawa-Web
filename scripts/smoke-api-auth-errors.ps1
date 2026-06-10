param(
    [string]$BaseUrl = "http://localhost:5025",
    [int]$RequestTimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

function New-JsonContent {
    param([string]$Json)

    return [System.Net.Http.StringContent]::new(
        $Json,
        [System.Text.Encoding]::UTF8,
        "application/json")
}

function Assert-ApiError {
    param(
        [System.Net.Http.HttpResponseMessage]$Response,
        [int]$ExpectedStatusCode,
        [string]$ExpectedErrorCode
    )

    $body = $Response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if ([int]$Response.StatusCode -ne $ExpectedStatusCode) {
        throw "Expected HTTP $ExpectedStatusCode, got $([int]$Response.StatusCode). Body: $body"
    }

    if ($body -notmatch '"success"\s*:\s*false') {
        throw "Expected ApiResult success=false. Body: $body"
    }

    if ($body -notmatch ('"code"\s*:\s*"' + [regex]::Escape($ExpectedErrorCode) + '"')) {
        throw "Expected ApiError code $ExpectedErrorCode. Body: $body"
    }

    if ($body -notmatch '"correlationId"\s*:') {
        throw "Expected correlationId in API error. Body: $body"
    }
}

Write-Host "Ishikawa RCA API auth error smoke test"
Write-Host "Base URL: $BaseUrl"
Write-Host "Request timeout: $RequestTimeoutSeconds seconds"

$closeUrl = "$BaseUrl/api/v1/rca/incidents/00000000-0000-0000-0000-000000000001/close"
$payload = '{"closureSummary":"auth validation smoke"}'

$forbiddenClient = [System.Net.Http.HttpClient]::new()
$forbiddenClient.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
$forbiddenClient.DefaultRequestHeaders.Add("X-RCA-TenantId", "11111111-1111-1111-1111-111111111111")
$forbiddenClient.DefaultRequestHeaders.Add("X-RCA-UserId", "operator")
$forbiddenClient.DefaultRequestHeaders.Add("X-RCA-Roles", "Operator")
$forbiddenResponse = $forbiddenClient.PostAsync($closeUrl, (New-JsonContent $payload)).GetAwaiter().GetResult()
Assert-ApiError `
    -Response $forbiddenResponse `
    -ExpectedStatusCode 403 `
    -ExpectedErrorCode "FORBIDDEN"
Write-Host "Validated 403/FORBIDDEN for insufficient role."

$challengeClient = [System.Net.Http.HttpClient]::new()
$challengeClient.Timeout = [TimeSpan]::FromSeconds($RequestTimeoutSeconds)
$challengeClient.DefaultRequestHeaders.Add("X-RCA-TenantId", "not-a-guid")
$challengeClient.DefaultRequestHeaders.Add("X-RCA-UserId", "operator")
$challengeClient.DefaultRequestHeaders.Add("X-RCA-Roles", "Operator")
$challengeResponse = $challengeClient.PostAsync($closeUrl, (New-JsonContent $payload)).GetAwaiter().GetResult()
Assert-ApiError `
    -Response $challengeResponse `
    -ExpectedStatusCode 401 `
    -ExpectedErrorCode "AUTHENTICATION_REQUIRED"
Write-Host "Validated 401/AUTHENTICATION_REQUIRED for invalid authentication context."

Write-Host "API auth error smoke test completed successfully."
