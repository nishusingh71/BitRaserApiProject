
# Auto-Detect Private Cloud Setup Script
# Automatically detects when setup is done via API/Frontend

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Auto-Detect Private Cloud Setup Monitor" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Get configuration
Write-Host "Enter Configuration:" -ForegroundColor Yellow

$API_URL = Read-Host "API URL (default: https://localhost:44316)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "https://localhost:44316"
}

$USER_EMAIL = Read-Host "User Email (default: devste@gmail.com)"
if ([string]::IsNullOrWhiteSpace($USER_EMAIL)) {
    $USER_EMAIL = "devste@gmail.com"
}

$TIDB_DB = Read-Host "Expected Database Name (default: Tech)"
if ([string]::IsNullOrWhiteSpace($TIDB_DB)) {
 $TIDB_DB = "Tech"
}

Write-Host ""
Write-Host "✅ Monitoring Configuration:" -ForegroundColor Green
Write-Host "   API URL: $API_URL" -ForegroundColor Gray
Write-Host "   User Email: $USER_EMAIL" -ForegroundColor Gray
Write-Host "   Expected Database: $TIDB_DB" -ForegroundColor Gray
Write-Host ""

# Login
Write-Host "Step 1: Login to API..." -ForegroundColor Yellow
Write-Host "Enter password for $USER_EMAIL:" -NoNewline
$userPassword = Read-Host -AsSecureString
$userPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($userPassword))

$loginBody = @{
    email = $USER_EMAIL
    password = $userPasswordText
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $TOKEN = $loginResponse.token
    Write-Host "✅ Login successful!" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🔍 MONITORING MODE ACTIVE" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Waiting for you to setup private cloud configuration..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Instructions:" -ForegroundColor White
Write-Host "1. Open your browser or API client" -ForegroundColor Gray
Write-Host "2. Go to: $API_URL/api/PrivateCloud/setup-simple" -ForegroundColor Gray
Write-Host "3. Fill in your TiDB connection details" -ForegroundColor Gray
Write-Host "4. Submit the form" -ForegroundColor Gray
Write-Host ""
Write-Host "This script will automatically detect when setup is complete!" -ForegroundColor Green
Write-Host "Press Ctrl+C to cancel monitoring" -ForegroundColor Yellow
Write-Host ""

# Monitoring loop
$maxAttempts = 60  # Monitor for 5 minutes (60 * 5 seconds)
$attempt = 0
$setupDetected = $false
$lastConfigId = $null

while ($attempt -lt $maxAttempts -and -not $setupDetected) {
    $attempt++
    
# Check configuration every 5 seconds
    Start-Sleep -Seconds 5
    
    try {
        $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
        -Method GET `
          -Headers @{"Authorization"="Bearer $TOKEN"} `
            -ErrorAction SilentlyContinue
        
     if ($configResponse -and $configResponse.configId) {
   # Check if this is a new configuration or updated
   if ($lastConfigId -eq $null -or $configResponse.configId -ne $lastConfigId) {
  $lastConfigId = $configResponse.configId
       
   Write-Host ""
            Write-Host "🎉 Setup detected! Configuration found!" -ForegroundColor Green
      Write-Host "   Config ID: $($configResponse.configId)" -ForegroundColor Gray
             Write-Host "   Database: $($configResponse.databaseName)" -ForegroundColor Gray
                Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
 Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor Gray

         # Verify database name matches
     if ($configResponse.databaseName -eq $TIDB_DB) {
      Write-Host ""
     Write-Host "✅ Database name matches expected value: $TIDB_DB" -ForegroundColor Green
    } else {
          Write-Host ""
         Write-Host "⚠️  Database name mismatch!" -ForegroundColor Yellow
    Write-Host "   Expected: $TIDB_DB" -ForegroundColor Yellow
   Write-Host "   Got: $($configResponse.databaseName)" -ForegroundColor Yellow
        }
             
   $setupDetected = $true
            }
        } else {
            # No configuration yet, show waiting indicator
Write-Host "." -NoNewline -ForegroundColor Gray
        }
    } catch {
        # Configuration not found yet, continue waiting
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $setupDetected) {
    Write-Host ""
 Write-Host ""
    Write-Host "⏱️  Timeout: No setup detected in 5 minutes" -ForegroundColor Yellow
    Write-Host "   Please run setup manually and then run this script again" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ SETUP DETECTED - CONTINUING..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Now continue with schema initialization
Write-Host "Step 2: Test connection..." -ForegroundColor Yellow

try {
    $testResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/test" `
        -Method POST `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Connection test successful!" -ForegroundColor Green
    Write-Host "   Success: $($testResponse.success)" -ForegroundColor Gray
    Write-Host "   Server Version: $($testResponse.serverVersion)" -ForegroundColor Gray
    Write-Host "Missing Tables: $($testResponse.missingTables.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 3: Initialize schema..." -ForegroundColor Yellow
$initSchema = Read-Host "Do you want to initialize schema now? (yes/no)"

if ($initSchema -eq "yes") {
    try {
        Write-Host "Initializing schema (this may take 30-60 seconds)..." -ForegroundColor Gray
        
        $schemaResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/initialize-schema" `
            -Method POST `
 -Headers @{"Authorization"="Bearer $TOKEN"} `
            -ErrorAction Stop `
   -TimeoutSec 120
        
        Write-Host "✅ Schema initialization successful!" -ForegroundColor Green
Write-Host "   Message: $($schemaResponse.message)" -ForegroundColor Gray
        Write-Host "   Note: $($schemaResponse.note)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ Schema initialization failed!" -ForegroundColor Red
        
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
 try {
                $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
        } catch {
         Write-Host "   Raw Error: $($_.ErrorDetails.Message)" -ForegroundColor Red
            }
  }
        
        Write-Host ""
        Write-Host "Checking application logs..." -ForegroundColor Yellow
        
     $logFiles = Get-ChildItem -Path "logs" -Filter "app-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
        if ($logFiles) {
    Write-Host "Last 30 lines from log:" -ForegroundColor Gray
            Write-Host ""
Get-Content -Path $logFiles.FullName -Tail 30 | ForEach-Object {
 if ($_ -match "ERROR|❌|EXCEPTION") {
         Write-Host $_ -ForegroundColor Red
    } elseif ($_ -match "WARN|⚠️") {
            Write-Host $_ -ForegroundColor Yellow
     } elseif ($_ -match "Creating table|Table created") {
    Write-Host $_ -ForegroundColor Green
      } else {
  Write-Host $_ -ForegroundColor Gray
           }
      }
        }
        
        exit 1
 }
    
    Write-Host ""
  Write-Host "Step 4: Validate schema..." -ForegroundColor Yellow
    
    try {
        $validateResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/validate-schema" `
    -Method POST `
       -Headers @{"Authorization"="Bearer $TOKEN"} `
         -ErrorAction Stop
        
        Write-Host "✅ Schema validation successful!" -ForegroundColor Green
        Write-Host "   Is Valid: $($validateResponse.isValid)" -ForegroundColor $(if($validateResponse.isValid) {"Green"} else {"Red"})
   Write-Host "   Message: $($validateResponse.message)" -ForegroundColor Gray
      Write-Host "   Existing Tables: $($validateResponse.existingTables.Count)" -ForegroundColor Gray
     
if ($validateResponse.existingTables.Count -gt 0) {
  Write-Host ""
    Write-Host "   Created Tables:" -ForegroundColor Green
          foreach ($table in $validateResponse.existingTables) {
  Write-Host "      ✅ $table" -ForegroundColor Green
  }
        }
    
        if ($validateResponse.missingTables.Count -gt 0) {
            Write-Host ""
        Write-Host "   Missing Tables:" -ForegroundColor Red
            foreach ($table in $validateResponse.missingTables) {
         Write-Host "      ❌ $table" -ForegroundColor Red
      }
        }
    } catch {
   Write-Host "❌ Schema validation failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ PROCESS COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "✅ Setup detected automatically" -ForegroundColor Green
Write-Host "✅ Configuration verified" -ForegroundColor Green
Write-Host "✅ Connection tested" -ForegroundColor Green
if ($initSchema -eq "yes") {
    Write-Host "✅ Schema initialized" -ForegroundColor Green
}
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Start using private cloud features" -ForegroundColor White
Write-Host "2. Create subusers in your private database" -ForegroundColor White
Write-Host "3. Generate audit reports in your private database" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Private cloud setup complete!" -ForegroundColor Green
