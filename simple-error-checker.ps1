# Simple Error Checker for devste@gmail.com Private Cloud Setup
# PowerShell 5.1+ Compatible

$ErrorActionPreference = "Continue"

Write-Host "🔍 Simple Error Checker" -ForegroundColor Cyan
Write-Host ""

# Disable SSL validation
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

# Config
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZXZzdGVAZ21haWwuY29tIiwianRpIjoiZmE3NDExZDEtMTVkZC00ZGIxLTg4NmMtNWQ5MjAyNjA1MWUwIiwiZXhwIjoxNzY0MDYxMjA3LCJpc3MiOiJEaHJ1dkFwaUlzc3VlciIsImF1ZCI6IkRocnV2QXBpQXVkaWVuY2UifQ.EHp_Aun2L1iuwCjXYw5Fmfwcdr5A_msFzqtKOm-YMbg"
$baseUrl = "https://localhost:44316"
$connectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;"

Write-Host "Testing Private Cloud Setup..." -ForegroundColor Yellow
Write-Host ""

try {
    # Test setup
    $body = @{
        connectionString = $connectionString
        databaseType = "mysql"
        migrateExistingData = $false
    } | ConvertTo-Json
    
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/complete-setup" `
        -Method Post `
      -Headers $headers `
        -Body $body
    
    Write-Host "Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""
    
    if ($response.success) {
   Write-Host "✅ SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "❌ FAILED!" -ForegroundColor Red
        Write-Host ""
    Write-Host "Failed Steps:" -ForegroundColor Yellow
        foreach ($step in $response.steps) {
            if (-not $step.success) {
     Write-Host "  - $($step.name)" -ForegroundColor Red
         if ($step.error) {
 Write-Host "    Error: $($step.error)" -ForegroundColor Yellow
     } else {
            Write-Host "    Error: (empty)" -ForegroundColor Yellow
         }
    }
        }
  
        if ($response.summary.error) {
  Write-Host ""
            Write-Host "Summary Error:" -ForegroundColor Yellow
      Write-Host "  $($response.summary.error)" -ForegroundColor White
        }
    }
 
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host ""
  Write-Host "Error Details:" -ForegroundColor Yellow
        try {
     $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            $errorJson | ConvertTo-Json -Depth 5 | Write-Host
        } catch {
   Write-Host $_.ErrorDetails.Message
        }
    }
}

Write-Host ""
Write-Host "=== Quick Checks ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If error message is empty, check:" -ForegroundColor Yellow
Write-Host "1. User exists: SELECT * FROM users WHERE user_email='devste@gmail.com';"
Write-Host "2. Private cloud enabled: is_private_cloud = TRUE"
Write-Host "3. Database exists: SHOW DATABASES LIKE 'Cloud_Erase';"
Write-Host "4. Application logs for detailed error"
Write-Host ""
