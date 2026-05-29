param(
    [string]$HostName = "127.0.0.1",
    [int]$Port = 5025,
    [int]$TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $asyncResult = $client.BeginConnect($HostName, $Port, $null, $null)
        if ($asyncResult.AsyncWaitHandle.WaitOne(500) -and $client.Connected) {
            $client.EndConnect($asyncResult)
            Write-Host "Port $Port is open on $HostName."
            exit 0
        }
    }
    catch {
        Start-Sleep -Milliseconds 500
    }
    finally {
        $client.Close()
    }

    Start-Sleep -Milliseconds 500
}

throw "Timeout waiting for $HostName`:$Port after $TimeoutSeconds seconds."
