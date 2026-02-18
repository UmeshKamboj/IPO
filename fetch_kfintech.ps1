[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

try {
    $response = Invoke-WebRequest -Uri "https://ipostatus.kfintech.com/static/js/main.26d2acc4.js" -UseBasicParsing -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    $content = $response.Content
    $content | Out-File -FilePath "$env:TEMP\kfintech_main.js" -Encoding utf8
    Write-Host "Downloaded successfully. Size: $($content.Length) chars"

    # Search for API-related patterns
    $patterns = @("https?://[^\s""']+(?:api|allot|company|ipostatus)[^\s""']*", "fetch\(", "\.post\(", "\.get\(", "baseURL", "REACT_APP", "process\.env")
    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            Write-Host "`n=== Pattern: $pattern ==="
            foreach ($m in $matches) {
                $start = [Math]::Max(0, $m.Index - 50)
                $len = [Math]::Min(200, $content.Length - $start)
                $context = $content.Substring($start, $len)
                Write-Host "  Found: $($m.Value)"
                Write-Host "  Context: $context"
                Write-Host "---"
            }
        }
    }
} catch {
    Write-Host "Error: $_"
}
