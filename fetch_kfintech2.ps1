# Try all TLS protocols
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls11 -bor [Net.SecurityProtocolType]::Tls

# Ignore SSL certificate errors
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$urls = @(
    "https://ipostatus.kfintech.com/static/js/main.26d2acc4.js",
    "https://ipostatus.kfintech.com/",
    "https://kosmic.kfintech.com/ipostatus/",
    "https://kprism.kfintech.com/ipostatus/"
)

foreach ($url in $urls) {
    Write-Host "`n=== Trying: $url ==="
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36" -TimeoutSec 15 -Headers @{
            "Accept" = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
            "Accept-Language" = "en-US,en;q=0.5"
        }
        Write-Host "Status: $($response.StatusCode)"
        $content = $response.Content
        Write-Host "Length: $($content.Length)"

        if ($url -like "*.js") {
            $content | Out-File -FilePath "$env:TEMP\kfintech_main.js" -Encoding utf8
            Write-Host "Saved JS to temp"
        } else {
            # Show first 2000 chars
            Write-Host $content.Substring(0, [Math]::Min(2000, $content.Length))
        }
        break
    } catch {
        Write-Host "Error: $($_.Exception.Message)"
    }
}
