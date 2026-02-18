# Use HttpClient (same as .NET app) with TLS 1.3 support
Add-Type -AssemblyName System.Net.Http

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.ServerCertificateCustomValidationCallback = { $true }
$handler.SslProtocols = [System.Security.Authentication.SslProtocols]::Tls12

$client = New-Object System.Net.Http.HttpClient($handler)
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36")
$client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
$client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5")
$client.DefaultRequestHeaders.Add("Accept-Encoding", "identity")
$client.Timeout = [TimeSpan]::FromSeconds(30)

try {
    Write-Host "Trying ipostatus.kfintech.com..."
    $response = $client.GetAsync("https://ipostatus.kfintech.com/").Result
    Write-Host "Status: $($response.StatusCode)"
    $content = $response.Content.ReadAsStringAsync().Result
    Write-Host "Length: $($content.Length)"
    Write-Host $content.Substring(0, [Math]::Min(3000, $content.Length))
} catch {
    Write-Host "Error 1: $($_.Exception.InnerException.Message)"
    try {
        Write-Host "`nTrying kprism..."
        $response = $client.GetAsync("https://kprism.kfintech.com/ipostatus/").Result
        Write-Host "Status: $($response.StatusCode)"
        $content = $response.Content.ReadAsStringAsync().Result
        Write-Host "Length: $($content.Length)"
        Write-Host $content.Substring(0, [Math]::Min(3000, $content.Length))
    } catch {
        Write-Host "Error 2: $($_.Exception.InnerException.Message)"
    }
} finally {
    $client.Dispose()
}
