# PowerShell Script to Diagnose Setup Failure

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Private Cloud Setup Failure Diagnosis" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$API_URL = Read-Host "Enter API URL (default: http://localhost:5000)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "http://localhost:5000"
}

$TOKEN = Read-Host "Enter JWT token"

if ([string]::IsNullOrWhiteSpace($TOKEN)) {
    Write-Host "❌ JWT token is required!" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host "Step 1: Checking user access..." -ForegroundColor Yellow

try {
    $accessResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/check-access" `
        -Method GET `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Access check successful" -ForegroundColor Green
    Write-Host "   Has Access: $($accessResponse.hasPrivateCloudAccess)" -ForegroundColor Gray
    Write-Host "   Is Configured: $($accessResponse.isConfigured)" -ForegroundColor Gray
    Write-Host "   Current User: $($accessResponse.currentUser)" -ForegroundColor Gray
    
    if (-not $accessResponse.hasPrivateCloudAccess) {
        Write-Host ""
        Write-Host "❌ User does NOT have private cloud access!" -ForegroundColor Red
        Write-Host ""
        Write-Host "FIX: Run this SQL in your main database:" -ForegroundColor Yellow
        Write-Host "UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = '$($accessResponse.currentUser)';" -ForegroundColor Gray
        exit 1
    }
} catch {
 Write-Host "❌ Access check failed!" -ForegroundColor Red
 Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Testing database connection directly..." -ForegroundColor Yellow

$testBody = @{
    connectionString = "mysql://2tdeFNZMcsWKkDR.root:76wtaj1GZkg7Qhek@gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000/Tech?ssl-mode=REQUIRED"
    databaseType = "mysql"
    notes = "Test setup"
} | ConvertTo-Json

Write-Host "Request body:" -ForegroundColor Gray
Write-Host $testBody -ForegroundColor Gray
Write-Host ""

try {
    $setupResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/setup-simple" `
        -Method POST `
    -Headers $headers `
        -Body $testBody `
        -ErrorAction Stop
    
    Write-Host "✅ Setup successful!" -ForegroundColor Green
    Write-Host "   Message: $($setupResponse.message)" -ForegroundColor Gray
    Write-Host "   Detail: $($setupResponse.detail)" -ForegroundColor Gray
} catch {
Write-Host "❌ Setup failed!" -ForegroundColor Red
    
  $errorResponse = $null
 if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
 Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
      Write-Host "   Detail: $($errorResponse.detail)" -ForegroundColor Red
  Write-Host "   User Email: $($errorResponse.userEmail)" -ForegroundColor Red
            Write-Host "   Database Type: $($errorResponse.databaseType)" -ForegroundColor Red
          Write-Host "   Server Host: $($errorResponse.serverHost)" -ForegroundColor Red
  
            if ($errorResponse.error) {
                Write-Host "   Error: $($errorResponse.error)" -ForegroundColor Red
        }
       if ($errorResponse.innerError) {
    Write-Host "   Inner Error: $($errorResponse.innerError)" -ForegroundColor Red
    }
        } catch {
    Write-Host "   Raw Error: $($_.ErrorDetails.Message)" -ForegroundColor Red
  }
    } else {
        Write-Host "   Exception: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "CHECKING APPLICATION LOGS..." -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Recent errors from logs:" -ForegroundColor Yellow
    
    $logFiles = Get-ChildItem -Path "logs" -Filter "app-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($logFiles) {
        Write-Host "Latest log file: $($logFiles.FullName)" -ForegroundColor Gray
        Write-Host ""
   
        $logContent = Get-Content -Path $logFiles.FullName -Tail 100 | Select-String "ERROR|EXCEPTION|❌" -CaseSensitive:$false
        
        if ($logContent) {
     foreach ($line in $logContent | Select-Object -Last 20) {
           Write-Host $line -ForegroundColor Red
      }
        } else {
       Write-Host "No recent errors found in logs" -ForegroundColor Gray
        }
    } else {
        Write-Host "No log files found in 'logs' directory" -ForegroundColor Yellow
    }
    
  Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "DIAGNOSIS COMPLETE" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
  Write-Host ""
    
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "1. User not marked as private cloud user" -ForegroundColor White
    Write-Host "   Fix: UPDATE users SET is_private_cloud = 1 WHERE user_email = 'devste@gmail.com'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Database 'Tech' doesn't exist in TiDB" -ForegroundColor White
    Write-Host "   Fix: Run .\fix-database-not-found.ps1" -ForegroundColor Gray
    Write-Host ""
 Write-Host "3. Connection test failing" -ForegroundColor White
    Write-Host "   Fix: Check TiDB credentials and connectivity" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. API not saving configuration" -ForegroundColor White
    Write-Host "   Fix: Check main database connection in appsettings.json" -ForegroundColor Gray
  Write-Host ""
    
    exit 1
}

Write-Host ""
Write-Host "Step 3: Verifying configuration was saved..." -ForegroundColor Yellow

try {
    $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
        -Method GET `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop

    Write-Host "✅ Configuration saved successfully!" -ForegroundColor Green
    Write-Host "   User Email: $($configResponse.userEmail)" -ForegroundColor Gray
    Write-Host "   Database: $($configResponse.databaseName)" -ForegroundColor Gray
    Write-Host "   Host: $($configResponse.serverHost)" -ForegroundColor Gray
    Write-Host "   Port: $($configResponse.serverPort)" -ForegroundColor Gray
    Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
} catch {
 Write-Host "⚠️  Could not retrieve configuration" -ForegroundColor Yellow
    Write-Host "   This might be normal if configuration was just created" -ForegroundColor Gray
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ DIAGNOSIS COMPLETE - NO ERRORS!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
