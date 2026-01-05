# Quick Endpoint Checker - Checks if PrivateCloud endpoints exist
# Fixes both SSL and 404 errors

$ErrorActionPreference = "Continue"

Write-Host "🔍 Quick Endpoint Checker" -ForegroundColor Cyan
Write-Host ""

# Fix SSL - Check if type exists first
if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
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
}

[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

$baseUrl = "https://localhost:44316"
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZXZzdGVAZ21haWwuY29tIiwianRpIjoiZmE3NDExZDEtMTVkZC00ZGIxLTg4NmMtNWQ5MjAyNjA1MWUwIiwiZXhwIjoxNzY0MDYxMjA3LCJpc3MiOiJEaHJ1dkFwaUlzc3VlciIsImF1ZCI6IkRocnV2QXBpQXVkaWVuY2UifQ.EHp_Aun2L1iuwCjXYw5Fmfwcdr5A_msFzqtKOm-YMbg"

# Check if API is running
Write-Host "1. Checking if API is running..." -ForegroundColor Yellow
$portTest = Test-NetConnection -ComputerName localhost -Port 44316 -WarningAction SilentlyContinue

if ($portTest.TcpTestSucceeded) {
    Write-Host "   ✅ API is running on port 44316" -ForegroundColor Green
} else {
 Write-Host "   ❌ API is NOT running!" -ForegroundColor Red
    Write-Host "Start your API in Visual Studio (F5)" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Check Swagger endpoint
Write-Host "2. Checking Swagger..." -ForegroundColor Yellow
try {
    $swagger = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -ErrorAction Stop
    Write-Host "   ✅ Swagger is accessible" -ForegroundColor Green
    
    # Check if PrivateCloud endpoints exist
    $privateCloudPaths = $swagger.paths.Keys | Where-Object { $_ -like "*PrivateCloud*" }
    
    if ($privateCloudPaths.Count -gt 0) {
   Write-Host "   ✅ Found $($privateCloudPaths.Count) PrivateCloud endpoints:" -ForegroundColor Green
        foreach ($path in $privateCloudPaths) {
            Write-Host "      - $path" -ForegroundColor Gray
}
    } else {
        Write-Host "   ❌ No PrivateCloud endpoints found in Swagger!" -ForegroundColor Red
        Write-Host "   This means controller is not registered" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Cannot access Swagger: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Try to call check-access endpoint
Write-Host "3. Testing /api/PrivateCloud/check-access..." -ForegroundColor Yellow
try {
  $headers = @{
     "Authorization" = "Bearer $token"
    }
    
    $access = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/check-access" `
 -Method Get `
        -Headers $headers `
   -ErrorAction Stop
    
    Write-Host "   ✅ Endpoint is working!" -ForegroundColor Green
    Write-Host "   Has Access: $($access.hasPrivateCloudAccess)" -ForegroundColor Gray
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    
    if ($statusCode -eq 404) {
     Write-Host "   ❌ 404 NOT FOUND!" -ForegroundColor Red
Write-Host ""
      Write-Host "   💡 FIX:" -ForegroundColor Yellow
        Write-Host "   1. Open Visual Studio" -ForegroundColor White
        Write-Host "   2. Build > Rebuild Solution" -ForegroundColor White
Write-Host "   3. Stop API (Shift+F5)" -ForegroundColor White
        Write-Host "   4. Start API (F5)" -ForegroundColor White
  Write-Host "   5. Check Swagger: https://localhost:44316/swagger" -ForegroundColor White
    } elseif ($statusCode -eq 401) {
        Write-Host "   ⚠️  401 Unauthorized (Token might be expired)" -ForegroundColor Yellow
 } else {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "===== SUMMARY =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "If you see 404 errors, the controller is not loaded." -ForegroundColor Yellow
Write-Host "Solution: Rebuild and restart your API project." -ForegroundColor White
Write-Host ""
Write-Host "Check Swagger UI manually:" -ForegroundColor Yellow
Write-Host "https://localhost:44316/swagger" -ForegroundColor White
Write-Host ""
