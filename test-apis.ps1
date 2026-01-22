# API Testing Script for PowerShell
# Usage: .\test-apis.ps1 -Token "your-jwt-token"

param(
    [string]$Token = "",
    [string]$BaseUrl = "https://financeapi.ivotiontech.co.in/api",
    [int]$CompanyId = 1,
    [int]$IpoId = 11
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "API Testing Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

function Test-Endpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$Description,
        [object]$Body = $null
    )

    Write-Host "Testing: $Description" -ForegroundColor Yellow
    Write-Host "Endpoint: $Method $Endpoint"

    $headers = @{
        "Content-Type" = "application/json"
    }

    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    try {
        $uri = "$BaseUrl$Endpoint"

        if ($Body) {
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -Body ($Body | ConvertTo-Json) -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -ErrorAction Stop
        }

        Write-Host "✓ Success" -ForegroundColor Green
        Write-Host "Response:" ($response | ConvertTo-Json -Depth 3 -Compress).Substring(0, [Math]::Min(200, ($response | ConvertTo-Json).Length)) "..."
    }
    catch {
        Write-Host "✗ Failed" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)"
    }

    Write-Host ""
}

# Test 1: Get Groups
Write-Host "=== 1. Get Groups ===" -ForegroundColor Cyan
Test-Endpoint -Method "GET" -Endpoint "/ipos/groups?companyId=$CompanyId&ipoId=$IpoId" -Description "Get Groups by Company and IPO"

# Test 2: Get Order List (NO PAGINATION)
Write-Host "=== 2. Get Order List (No Pagination) ===" -ForegroundColor Cyan
Test-Endpoint -Method "GET" -Endpoint "/ipos/$IpoId/order/list" -Description "Get All Orders for IPO $IpoId"

# Test 3: Get Order List with Group Filter
Write-Host "=== 3. Get Order List with Group Filter ===" -ForegroundColor Cyan
Test-Endpoint -Method "GET" -Endpoint "/ipos/$IpoId/order/list?groupId=1" -Description "Get Orders for IPO $IpoId and Group 1"

# Test 4: Get Top 5 Orders
Write-Host "=== 4. Get Top 5 Orders ===" -ForegroundColor Cyan
Test-Endpoint -Method "GET" -Endpoint "/ipos/$IpoId/orders/top5" -Description "Get Top 5 Orders for IPO $IpoId"

# Test 5: Get Order Details (NO PAGINATION)
Write-Host "=== 5. Get Order Details (No Pagination) ===" -ForegroundColor Cyan
Test-Endpoint -Method "GET" -Endpoint "/ipos/$IpoId/order-details?orderType=1" -Description "Get All Order Details for IPO $IpoId (BUY)"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

if (-not $Token) {
    Write-Host "NOTE: Run with -Token parameter to test authenticated endpoints" -ForegroundColor Yellow
    Write-Host "Example: .\test-apis.ps1 -Token 'your-jwt-token'" -ForegroundColor Yellow
}
