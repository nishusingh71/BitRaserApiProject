# Quick Test Script for Hybrid Multi-Tenant Architecture

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Hybrid Multi-Tenant Architecture - Quick Test" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$API_URL = Read-Host "API URL (default: https://localhost:44316)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "https://localhost:44316"
}

Write-Host ""
Write-Host "Test Scenario 1: Regular User (Main Database)" -ForegroundColor Yellow
Write-Host "----------------------------------------------" -ForegroundColor Yellow

$regularEmail = Read-Host "Regular user email (default: regular@example.com)"
if ([string]::IsNullOrWhiteSpace($regularEmail)) {
    $regularEmail = "regular@example.com"
}

Write-Host "Enter password for $regularEmail" -NoNewline
$regularPass = Read-Host -AsSecureString
$regularPassText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($regularPass))

# Test Regular User
Write-Host ""
Write-Host "Testing regular user..." -ForegroundColor White

try {
    $loginBody = @{
        email = $regularEmail
        password = $regularPassText
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/Auth/login" `
    -Method POST `
   -ContentType "application/json" `
     -Body $loginBody `
        -ErrorAction Stop
    
 $regularToken = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
    
    # Get audit reports
    $headers = @{ "Authorization" = "Bearer $regularToken" }
    
  Write-Host "Fetching audit reports..." -NoNewline
    $reportsResponse = Invoke-RestMethod -Uri "$API_URL/api/AuditReports" `
      -Method GET `
    -Headers $headers `
        -ErrorAction Stop
    
    Write-Host " ✅ Success" -ForegroundColor Green
    Write-Host "   Database: $($reportsResponse.database)" -ForegroundColor $(if($reportsResponse.database -eq "Main Database") {"Green"} else {"Red"})
    Write-Host "   Is Private Cloud: $($reportsResponse.isPrivateCloud)" -ForegroundColor Gray
    Write-Host "   Report Count: $($reportsResponse.reportCount)" -ForegroundColor Gray
    
    # Create test report
    Write-Host "Creating test audit report..." -NoNewline
    $createReportBody = @{
        reportName = "Test Report - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        erasureMethod = "DoD 5220.22-M"
        reportDetailsJson = '{"drives": 1, "test": true}'
    } | ConvertTo-Json
    
    $createResponse = Invoke-RestMethod -Uri "$API_URL/api/AuditReports" `
        -Method POST `
        -Headers $headers `
        -ContentType "application/json" `
-Body $createReportBody `
  -ErrorAction Stop
    
    Write-Host " ✅ Created" -ForegroundColor Green
    Write-Host "   Report ID: $($createResponse.reportId)" -ForegroundColor Gray
    Write-Host "   Database: $($createResponse.database)" -ForegroundColor Green

} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
 Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Test Scenario 2: Private Cloud User" -ForegroundColor Yellow
Write-Host "----------------------------------------------" -ForegroundColor Yellow

$privateEmail = Read-Host "Private cloud user email (default: private@example.com)"
if ([string]::IsNullOrWhiteSpace($privateEmail)) {
    $privateEmail = "private@example.com"
}

Write-Host "Enter password for $privateEmail" -NoNewline
$privatePass = Read-Host -AsSecureString
$privatePassText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($privatePass))

# Test Private Cloud User
Write-Host ""
Write-Host "Testing private cloud user..." -ForegroundColor White

try {
    $loginBody = @{
   email = $privateEmail
        password = $privatePassText
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/Auth/login" `
        -Method POST `
   -ContentType "application/json" `
        -Body $loginBody `
-ErrorAction Stop
    
    $privateToken = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
    
    # Get audit reports
    $headers = @{ "Authorization" = "Bearer $privateToken" }
    
    Write-Host "Fetching audit reports..." -NoNewline
    $reportsResponse = Invoke-RestMethod -Uri "$API_URL/api/AuditReports" `
  -Method GET `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Host " ✅ Success" -ForegroundColor Green
    Write-Host "   Database: $($reportsResponse.database)" -ForegroundColor $(if($reportsResponse.database -eq "Private Cloud") {"Green"} else {"Yellow"})
    Write-Host "   Is Private Cloud: $($reportsResponse.isPrivateCloud)" -ForegroundColor Green
    Write-Host "   Report Count: $($reportsResponse.reportCount)" -ForegroundColor Gray
    
    # Create test report
    Write-Host "Creating test audit report..." -NoNewline
    $createReportBody = @{
    reportName = "Private Cloud Test - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
  erasureMethod = "NIST 800-88"
    reportDetailsJson = '{"drives": 5, "privateCloud": true}'
    } | ConvertTo-Json
    
    $createResponse = Invoke-RestMethod -Uri "$API_URL/api/AuditReports" `
   -Method POST `
  -Headers $headers `
     -ContentType "application/json" `
      -Body $createReportBody `
        -ErrorAction Stop
    
    Write-Host " ✅ Created" -ForegroundColor Green
    Write-Host "   Report ID: $($createResponse.reportId)" -ForegroundColor Gray
    Write-Host "   Database: $($createResponse.database)" -ForegroundColor $(if($createResponse.database -eq "Private Cloud") {"Green"} else {"Yellow"})
    
    # Test SubUsers
    Write-Host ""
    Write-Host "Testing SubUsers..." -ForegroundColor White
    Write-Host "Fetching subusers..." -NoNewline
    
    $subusersResponse = Invoke-RestMethod -Uri "$API_URL/api/SubUsers" `
    -Method GET `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Host " ✅ Success" -ForegroundColor Green
    Write-Host "Database: $($subusersResponse.database)" -ForegroundColor $(if($subusersResponse.database -eq "Private Cloud") {"Green"} else {"Yellow"})
    Write-Host "   SubUser Count: $($subusersResponse.subuserCount)" -ForegroundColor Gray
  
} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Message -like "*401*") {
        Write-Host ""
   Write-Host "⚠️  Private cloud user might not exist or credentials are wrong" -ForegroundColor Yellow
 Write-Host "   Run this SQL to create a private cloud user:" -ForegroundColor Gray
        Write-Host "   UPDATE users SET is_private_cloud = TRUE WHERE user_email = '$privateEmail';" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ Regular User Test:" -ForegroundColor Yellow
Write-Host "   - Should route to Main Database" -ForegroundColor White
Write-Host "   - database field should be 'Main Database'" -ForegroundColor White
Write-Host "   - isPrivateCloud should be false" -ForegroundColor White
Write-Host ""

Write-Host "✅ Private Cloud User Test:" -ForegroundColor Yellow
Write-Host "   - Should route to Private Cloud Database" -ForegroundColor White
Write-Host "   - database field should be 'Private Cloud'" -ForegroundColor White
Write-Host "   - isPrivateCloud should be true" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Check application logs for database routing details" -ForegroundColor White
Write-Host "2. Verify data in respective databases" -ForegroundColor White
Write-Host "3. Test with more users and scenarios" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Testing complete!" -ForegroundColor Green
