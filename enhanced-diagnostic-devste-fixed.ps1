# Fixed Enhanced Diagnostic Script for devste@gmail.com
# Compatible with all PowerShell versions (5.1+)

$ErrorActionPreference = "Continue"

Write-Host "🔍 ===== ENHANCED DIAGNOSTIC FOR devste@gmail.com =====" -ForegroundColor Cyan
Write-Host ""

# Configuration
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZXZzdGVAZ21haWwuY29tIiwianRpIjoiZmE3NDExZDEtMTVkZC00ZGIxLTg4NmMtNWQ5MjAyNjA1MWUwIiwiZXhwIjoxNzY0MDYxMjA3LCJpc3MiOiJEaHJ1dkFwaUlzc3VlciIsImF1ZCI6IkRocnV2QXBpQXVkaWVuY2UifQ.EHp_Aun2L1iuwCjXYw5Fmfwcdr5A_msFzqtKOm-YMbg"
$baseUrl = "https://localhost:44316"
$connectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;"

Write-Host "📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "   User: devste@gmail.com"
Write-Host "   Database: Cloud_Erase"
Write-Host "   Server: gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000"
Write-Host ""

# Disable SSL certificate validation for development (PowerShell 5.1 compatible)
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
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

Write-Host "✅ SSL certificate validation disabled for localhost testing" -ForegroundColor Green
Write-Host ""

# Test 1: Check Private Cloud Access
Write-Host "1️⃣ Checking Private Cloud Access..." -ForegroundColor Cyan
try {
    $headers = @{
 "Authorization" = "Bearer $token"
    }
    
    $access = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/check-access" `
        -Method Get `
 -Headers $headers
    
    Write-Host "✅ Access Check Response:" -ForegroundColor Green
    $access | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
    
    if (-not $access.hasPrivateCloudAccess) {
        Write-Host ""
        Write-Host "   ❌ PROBLEM FOUND: User does not have private cloud access!" -ForegroundColor Red
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
    if ($_.ErrorDetails.Message) {
        Write-Host "   Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""

# Test 2: Attempt Complete Setup with Enhanced Error Tracking
Write-Host "2️⃣ Attempting Complete Setup..." -ForegroundColor Cyan
Write-Host "   📦 Preparing request..."

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
    notes = "Cloud_Erase database - Enhanced diagnostic test"
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
    
    # Make the request
    $response = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/complete-setup" `
        -Method Post `
        -Headers $headers `
        -Body $setupBody `
-Verbose
    
    Write-Host ""
    Write-Host "   📨 SETUP RESPONSE RECEIVED:" -ForegroundColor Cyan
    Write-Host ""
    $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
    Write-Host ""
    
    # Analyze response
    if ($response.success) {
        Write-Host "   🎉 SETUP SUCCESSFUL!" -ForegroundColor Green
        Write-Host ""
        Write-Host "   ✅ All steps completed:" -ForegroundColor Green
        foreach ($step in $response.steps) {
   Write-Host "      $($step.step). $($step.name): $($step.status)" -ForegroundColor Green
        }
 } else {
  Write-Host "   ⚠️  SETUP FAILED!" -ForegroundColor Red
        Write-Host ""
        Write-Host "   ❌ Failed Steps:" -ForegroundColor Red
        foreach ($step in $response.steps) {
         if (-not $step.success) {
              Write-Host "      Step $($step.step): $($step.name)" -ForegroundColor Red
      Write-Host "    Status: $($step.status)" -ForegroundColor Red
          if ($step.error) {
        Write-Host "Error: $($step.error)" -ForegroundColor Yellow
         } else {
             Write-Host "      Error: (empty error message)" -ForegroundColor Yellow
              }
                Write-Host ""
            }
 }
        
   if ($response.summary.error) {
Write-Host "   📝 Summary Error:" -ForegroundColor Yellow
            Write-Host "    $($response.summary.error)" -ForegroundColor White
        } else {
  Write-Host "   ⚠️  Summary error is empty!" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host ""
    Write-Host "   ❌ EXCEPTION OCCURRED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Exception Type: $($_.Exception.GetType().Name)" -ForegroundColor Yellow
    Write-Host "   Message: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host ""
    
    if ($_.ErrorDetails.Message) {
      Write-Host "   📝 Error Details:" -ForegroundColor Yellow
      try {
      $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            $errorJson | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
       Write-Host ""
            
            # Check for empty error messages
            if ($errorJson.steps) {
    foreach ($step in $errorJson.steps) {
        if (-not $step.success) {
  Write-Host "   🔍 Failed Step Analysis:" -ForegroundColor Cyan
       Write-Host "      Step: $($step.step) - $($step.name)" -ForegroundColor White
               Write-Host "      Success: $($step.success)" -ForegroundColor White
         Write-Host "      Status: $($step.status)" -ForegroundColor White
          if ($step.error) {
             Write-Host "Error: $($step.error)" -ForegroundColor White
       } else {
          Write-Host "      Error: (EMPTY - This is the problem!)" -ForegroundColor Red
     }
      Write-Host ""
           }
  }
            }
            
     if ($errorJson.summary -and $errorJson.summary.error) {
    Write-Host "   Summary Error: $($errorJson.summary.error)" -ForegroundColor White
     } else {
        Write-Host "   ⚠️  Summary error is EMPTY!" -ForegroundColor Yellow
         }
      
        } catch {
    Write-Host $_.ErrorDetails.Message -ForegroundColor White
    }
    }
  
    Write-Host ""
    Write-Host "   📊 HTTP Response Details:" -ForegroundColor Cyan
    if ($_.Exception.Response) {
        Write-Host "      Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor White
        Write-Host "      Status: $($_.Exception.Response.StatusDescription)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "===== DIAGNOSTIC ANALYSIS =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔍 Common Reasons for Empty Error Messages:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Exception thrown but not captured properly" -ForegroundColor White
Write-Host "   Solution: Check PrivateCloudService logs"
Write-Host ""
Write-Host "2. Connection string parsing failed" -ForegroundColor White
Write-Host "   Solution: Verify connection string format"
Write-Host ""
Write-Host "3. User validation failed" -ForegroundColor White
Write-Host "   Solution: Check user exists and is_private_cloud=TRUE"
Write-Host ""
Write-Host "4. Database doesn't exist" -ForegroundColor White
Write-Host "   Solution: Verify 'Cloud_Erase' database exists"
Write-Host ""

Write-Host ""
Write-Host "📝 Recommended Actions:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Check Application Logs:" -ForegroundColor White
Write-Host "   Look for 'PrivateCloudService' logs"
Write-Host "   Look for 'SetupPrivateDatabaseFromConnectionStringAsync' logs"
Write-Host ""
Write-Host "2. Verify User in Database:" -ForegroundColor White
Write-Host "   SELECT user_id, user_email, is_private_cloud"
Write-Host "   FROM users"
Write-Host "   WHERE user_email = 'devste@gmail.com';"
Write-Host ""
Write-Host "3. Check Database Exists:" -ForegroundColor White
Write-Host "   SHOW DATABASES LIKE 'Cloud_Erase';"
Write-Host ""
Write-Host "4. Test Connection Manually:" -ForegroundColor White
Write-Host "   mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com \"
Write-Host "     -P 4000 \"
Write-Host "     -u 4WScT7meioLLU3B.root \"
Write-Host "     -p \"
Write-Host "     -D Cloud_Erase \"
Write-Host "     --ssl-mode=REQUIRED"
Write-Host ""

Write-Host "===== DIAGNOSTIC COMPLETE =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Next Step: Check application logs for detailed error messages" -ForegroundColor Yellow
