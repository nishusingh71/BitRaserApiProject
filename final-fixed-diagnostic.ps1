# Fixed Diagnostic Script - Resolves Type Already Exists Error
# Works with PowerShell 5.1+ and handles 404 errors

$ErrorActionPreference = "Continue"

Write-Host "🔍 ===== FIXED DIAGNOSTIC FOR devste@gmail.com =====" -ForegroundColor Cyan
Write-Host ""

# Configuration
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZXZzdGVAZ21haWwuY29tIiwianRpIjoiZmE3NDExZDEtMTVkZC00ZGIxLTg4NmMtNWQ5MjAyNjA1MWUwIiwiZXhwIjoxNzY0MDYxMjA3LCJpc3MiOiJEaHJ1dkFwaUlzc3VlciIsImF1ZCI6IkRocnV2QXBpQXVkaWVuY2UifQ.EHp_Aun2L1iuwCjXYw5Fmfwcdr5A_msFzqtKOm-YMbg"
$baseUrl = "https://localhost:44316"
$connectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;"

Write-Host "📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "   User: devste@gmail.com"
Write-Host "   Base URL: $baseUrl"
Write-Host "   Database: Cloud_Erase"
Write-Host ""

# ✅ FIX: Check if type already exists before adding
if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
    Write-Host "Adding SSL certificate bypass..." -ForegroundColor Yellow
    Add-Type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
       public bool CheckValidationResult(
    ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
   return true;
   }
   }
"@
    Write-Host "✅ SSL certificate policy added" -ForegroundColor Green
} else {
 Write-Host "✅ SSL certificate policy already exists" -ForegroundColor Green
}

[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

Write-Host ""

# ✅ FIX: Check if API is running first
Write-Host "0️⃣ Checking if API is running..." -ForegroundColor Cyan
try {
    $apiCheck = Test-NetConnection -ComputerName localhost -Port 44316 -WarningAction SilentlyContinue
    
    if ($apiCheck.TcpTestSucceeded) {
        Write-Host "   ✅ API is running on port 44316" -ForegroundColor Green
    } else {
        Write-Host "   ❌ API is NOT running on port 44316!" -ForegroundColor Red
        Write-Host ""
        Write-Host "   💡 SOLUTION:" -ForegroundColor Yellow
        Write-Host "   1. Start your API project in Visual Studio" -ForegroundColor White
        Write-Host "   2. Press F5 or click 'Run'" -ForegroundColor White
    Write-Host "   3. Wait for 'https://localhost:44316' to open" -ForegroundColor White
  Write-Host "   4. Then run this script again" -ForegroundColor White
  Write-Host ""
        exit 1
    }
} catch {
    Write-Host "   ⚠️  Cannot test port: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Test 1: Check Private Cloud Access
Write-Host "1️⃣ Checking Private Cloud Access..." -ForegroundColor Cyan
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    $access = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/check-access" `
      -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
Write-Host "   ✅ Access Check Response:" -ForegroundColor Green
    $access | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
    Write-Host ""
    
    if (-not $access.hasPrivateCloudAccess) {
        Write-Host "   ❌ PROBLEM: User does not have private cloud access!" -ForegroundColor Red
 Write-Host ""
 Write-Host "   💡 SOLUTION:" -ForegroundColor Yellow
    Write-Host "   Run this SQL in Main Database:" -ForegroundColor Yellow
     Write-Host "   UPDATE users SET is_private_cloud = TRUE WHERE user_email = 'devste@gmail.com';" -ForegroundColor White
        Write-Host ""
        exit 1
    }
    
    Write-Host "   ✅ User has private cloud access" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Check for 404 error
    if ($_.Exception.Message -like "*404*" -or $_.Exception.Message -like "*Not Found*") {
        Write-Host ""
        Write-Host "   🔍 404 ERROR DETECTED!" -ForegroundColor Red
Write-Host ""
        Write-Host "   Possible causes:" -ForegroundColor Yellow
        Write-Host "   1. PrivateCloudController not properly registered" -ForegroundColor White
        Write-Host "   2. API routing configuration issue" -ForegroundColor White
        Write-Host " 3. Controller not compiled in current build" -ForegroundColor White
        Write-Host ""
    Write-Host "   💡 SOLUTIONS:" -ForegroundColor Yellow
        Write-Host "   1. Rebuild the project (Ctrl+Shift+B)" -ForegroundColor White
        Write-Host "   2. Check that PrivateCloudController.cs exists" -ForegroundColor White
     Write-Host "   3. Restart the API application" -ForegroundColor White
        Write-Host "   4. Check Swagger UI: https://localhost:44316/swagger" -ForegroundColor White
     Write-Host "      Look for 'PrivateCloud' endpoints" -ForegroundColor White
   Write-Host ""
    }
    
    if ($_.ErrorDetails.Message) {
        Write-Host "   Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""

# Test 2: Attempt Complete Setup
Write-Host "2️⃣ Attempting Complete Setup..." -ForegroundColor Cyan
Write-Host "   📦 Preparing request..."

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
    notes = "Cloud_Erase database - Fixed diagnostic"
    migrateExistingData = $false
} | ConvertTo-Json

Write-Host "   ✅ Request prepared"
Write-Host ""
Write-Host "   🚀 Sending POST request..."
Write-Host ""

try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/complete-setup" `
        -Method Post `
-Headers $headers `
        -Body $setupBody `
        -ErrorAction Stop
    
 Write-Host ""
    Write-Host "   📨 SETUP RESPONSE:" -ForegroundColor Cyan
    Write-Host ""
  $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
    Write-Host ""
    
    if ($response.success) {
        Write-Host "   🎉 SETUP SUCCESSFUL!" -ForegroundColor Green
        Write-Host ""
        Write-Host "   ✅ Completed Steps:" -ForegroundColor Green
        foreach ($step in $response.steps) {
            if ($step.success) {
       Write-Host "  $($step.step). $($step.name): $($step.status)" -ForegroundColor Green
    } else {
        Write-Host "      $($step.step). $($step.name): $($step.status)" -ForegroundColor Red
 }
        }
    } else {
      Write-Host "   ⚠️  SETUP FAILED!" -ForegroundColor Red
        Write-Host ""
        Write-Host "   ❌ Failed Steps:" -ForegroundColor Red
        foreach ($step in $response.steps) {
       if (-not $step.success) {
Write-Host "   Step $($step.step): $($step.name)" -ForegroundColor Red
      Write-Host "   Status: $($step.status)" -ForegroundColor Red
      if ($step.error) {
 Write-Host "      Error: $($step.error)" -ForegroundColor Yellow
   } else {
         Write-Host "  Error: (empty error message)" -ForegroundColor Yellow
    }
   Write-Host ""
     }
        }
        
        if ($response.summary.error) {
   Write-Host "   📝 Summary Error:" -ForegroundColor Yellow
            Write-Host "      $($response.summary.error)" -ForegroundColor White
    }
    }
    
} catch {
    Write-Host ""
    Write-Host "   ❌ EXCEPTION!" -ForegroundColor Red
  Write-Host ""
    Write-Host "   Message: $($_.Exception.Message)" -ForegroundColor Yellow
    
    # Check for 404
    if ($_.Exception.Message -like "*404*" -or $_.Exception.Message -like "*Not Found*") {
        Write-Host ""
        Write-Host "   🔍 404 ERROR - Endpoint Not Found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "   💡 Quick Fix:" -ForegroundColor Yellow
        Write-Host "   1. Open Visual Studio" -ForegroundColor White
   Write-Host "   2. Build > Rebuild Solution (Ctrl+Shift+B)" -ForegroundColor White
        Write-Host "   3. Stop the API (Shift+F5)" -ForegroundColor White
        Write-Host "   4. Start the API (F5)" -ForegroundColor White
      Write-Host "   5. Run this script again" -ForegroundColor White
        Write-Host ""
    }
    
    if ($_.ErrorDetails.Message) {
     Write-Host ""
        Write-Host "   Error Details:" -ForegroundColor Yellow
    try {
  $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
   $errorJson | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor White
        } catch {
            Write-Host $_.ErrorDetails.Message -ForegroundColor White
    }
    }
}

Write-Host ""
Write-Host "===== DIAGNOSTIC COMPLETE =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Quick Checklist:" -ForegroundColor Yellow
Write-Host "✅ SSL certificate bypass: Working" -ForegroundColor Green
Write-Host "✅ API running check: $(if ($apiCheck.TcpTestSucceeded) { 'Passed' } else { 'Failed' })" -ForegroundColor $(if ($apiCheck.TcpTestSucceeded) { 'Green' } else { 'Red' })
Write-Host ""
Write-Host "If you see 404 errors:" -ForegroundColor Yellow
Write-Host "1. Rebuild project in Visual Studio" -ForegroundColor White
Write-Host "2. Check Swagger UI: https://localhost:44316/swagger" -ForegroundColor White
Write-Host "3. Look for '/api/PrivateCloud' endpoints" -ForegroundColor White
Write-Host ""
