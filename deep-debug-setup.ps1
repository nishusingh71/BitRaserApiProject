# Deep Debug Script - Why Configuration Not Saving

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "DEEP DEBUG: Why Configuration Not Saving" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# API Configuration
$API_URL = "http://localhost:5000"
$USER_EMAIL = "devste@gmail.com"

Write-Host "🔍 Debugging for: $USER_EMAIL" -ForegroundColor Yellow
Write-Host ""

# Get JWT token
Write-Host "Step 1: Getting JWT token..." -ForegroundColor Yellow
$email = Read-Host "Enter email for login (default: devste@gmail.com)"
if ([string]::IsNullOrWhiteSpace($email)) {
    $email = "devste@gmail.com"
}

$password = Read-Host "Enter password" -AsSecureString
$passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

$loginBody = @{
    email = $email
    password = $passwordText
} | ConvertTo-Json

Write-Host "Logging in..." -ForegroundColor Gray

try {
    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $TOKEN = $loginResponse.token
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "   Token: $($TOKEN.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Checking user access..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $TOKEN"
}

try {
    $accessResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/check-access" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Host "✅ Access check successful" -ForegroundColor Green
    Write-Host "   Has Access: $($accessResponse.hasPrivateCloudAccess)" -ForegroundColor $(if($accessResponse.hasPrivateCloudAccess) {"Green"} else {"Red"})
 Write-Host "   Is Configured: $($accessResponse.isConfigured)" -ForegroundColor Gray
    Write-Host "   Schema Initialized: $($accessResponse.isSchemaInitialized)" -ForegroundColor Gray
    Write-Host "   Current User: $($accessResponse.currentUser)" -ForegroundColor Gray
    
    if (-not $accessResponse.hasPrivateCloudAccess) {
        Write-Host ""
     Write-Host "❌ CRITICAL: User does NOT have private cloud access!" -ForegroundColor Red
        Write-Host ""
    Write-Host "FIX THIS FIRST:" -ForegroundColor Yellow
        Write-Host "UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = '$($accessResponse.currentUser)';" -ForegroundColor Gray
     exit 1
    }
} catch {
    Write-Host "❌ Access check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Getting database name from user..." -ForegroundColor Yellow

$databaseName = Read-Host "Enter TiDB database name (e.g., Tech, Cloud_Erase)"

if ([string]::IsNullOrWhiteSpace($databaseName)) {
    Write-Host "❌ Database name is required!" -ForegroundColor Red
    exit 1
}

Write-Host "   Database name: $databaseName" -ForegroundColor Green

Write-Host ""
Write-Host "Step 4: Testing TiDB connection..." -ForegroundColor Yellow

$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"

Write-Host "Testing connection to database: $databaseName" -ForegroundColor Gray

$testQuery = "SELECT DATABASE() as db, VERSION() as version"

try {
    $result = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $databaseName --ssl-mode=REQUIRED -s -N -e $testQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
   Write-Host "✅ TiDB connection successful!" -ForegroundColor Green
        $fields = $result -split "`t"
        Write-Host "   Current Database: $($fields[0])" -ForegroundColor Gray
 Write-Host "   Version: $($fields[1])" -ForegroundColor Gray
    } else {
  Write-Host "❌ TiDB connection failed!" -ForegroundColor Red
      Write-Host "   Error: $result" -ForegroundColor Red
        Write-Host ""
        Write-Host "Possible fixes:" -ForegroundColor Yellow
 Write-Host "1. Database '$databaseName' doesn't exist - create it first" -ForegroundColor White
    Write-Host "2. Check credentials" -ForegroundColor White
    Write-Host "3. Check network/firewall" -ForegroundColor White
        
    $createDb = Read-Host "Do you want to create database '$databaseName'? (yes/no)"
        if ($createDb -eq "yes") {
         $createQuery = "CREATE DATABASE IF NOT EXISTS ``$databaseName`` CHARACTER SET utf8mb4 COLLATE utf8mb4_bin"
            mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS --ssl-mode=REQUIRED -e $createQuery 2>&1
            
   if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Database created successfully!" -ForegroundColor Green
            } else {
     Write-Host "❌ Failed to create database" -ForegroundColor Red
       exit 1
 }
        } else {
            exit 1
        }
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 5: Setting up private cloud configuration..." -ForegroundColor Yellow

$connectionString = "mysql://$($TIDB_USER):$($TIDB_PASS)@$($TIDB_HOST):$($TIDB_PORT)/$($databaseName)?ssl-mode=REQUIRED"

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
    notes = "Setup via debug script - Database: $databaseName"
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Gray
Write-Host $setupBody -ForegroundColor Gray
Write-Host ""

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host "Sending setup request..." -ForegroundColor Gray

try {
    $setupResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/setup-simple" `
        -Method POST `
        -Headers $headers `
        -Body $setupBody `
        -ErrorAction Stop

    Write-Host "✅ SETUP SUCCESSFUL!" -ForegroundColor Green
    Write-Host "   Message: $($setupResponse.message)" -ForegroundColor Gray
    Write-Host "   Detail: $($setupResponse.detail)" -ForegroundColor Gray
    Write-Host "   User Email: $($setupResponse.userEmail)" -ForegroundColor Gray
} catch {
    Write-Host "❌ SETUP FAILED!" -ForegroundColor Red
  Write-Host ""
    
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "Error Response:" -ForegroundColor Red
            Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
     Write-Host "   Detail: $($errorResponse.detail)" -ForegroundColor Red
            Write-Host "   User Email: $($errorResponse.userEmail)" -ForegroundColor Red
  Write-Host "   Database Type: $($errorResponse.databaseType)" -ForegroundColor Red
      Write-Host "   Server Host: $($errorResponse.serverHost)" -ForegroundColor Red
 
    if ($errorResponse.error) {
    Write-Host "   Error: $($errorResponse.error)" -ForegroundColor Red
            }
   if ($errorResponse.stackTrace) {
    Write-Host "   Stack Trace:" -ForegroundColor Red
         Write-Host $errorResponse.stackTrace -ForegroundColor DarkRed
 }
} catch {
            Write-Host "Raw Error:" -ForegroundColor Red
            Write-Host $_.ErrorDetails.Message -ForegroundColor Red
        }
    } else {
        Write-Host "Exception Message:" -ForegroundColor Red
   Write-Host $_.Exception.Message -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "CHECKING APPLICATION LOGS FOR DETAILED ERROR..." -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
 Write-Host ""
 
    $logFiles = Get-ChildItem -Path "logs" -Filter "app-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($logFiles) {
        Write-Host "Latest log file: $($logFiles.Name)" -ForegroundColor Gray
  Write-Host ""
   Write-Host "Last 50 lines:" -ForegroundColor Yellow
        Get-Content -Path $logFiles.FullName -Tail 50 | ForEach-Object {
 if ($_ -match "ERROR|❌|EXCEPTION") {
         Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "WARN|⚠️") {
  Write-Host $_ -ForegroundColor Yellow
            } else {
        Write-Host $_ -ForegroundColor Gray
       }
        }
    } else {
        Write-Host "⚠️  No log files found" -ForegroundColor Yellow
  }
    
    exit 1
}

Write-Host ""
Write-Host "Step 6: Verifying configuration was saved..." -ForegroundColor Yellow

try {
    $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
        -Method GET `
        -Headers @{"Authorization" = "Bearer $TOKEN"} `
        -ErrorAction Stop

    Write-Host "✅ Configuration retrieved successfully!" -ForegroundColor Green
    Write-Host "   Config ID: $($configResponse.configId)" -ForegroundColor Gray
    Write-Host "   User Email: $($configResponse.userEmail)" -ForegroundColor Gray
    Write-Host "   Database Type: $($configResponse.databaseType)" -ForegroundColor Gray
    Write-Host "   Database Name: $($configResponse.databaseName)" -ForegroundColor $(if($configResponse.databaseName -eq $databaseName) {"Green"} else {"Red"})
    Write-Host "   Server Host: $($configResponse.serverHost)" -ForegroundColor Gray
    Write-Host "   Server Port: $($configResponse.serverPort)" -ForegroundColor Gray
    Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
 Write-Host "   Is Active: $($configResponse.isActive)" -ForegroundColor Gray
    Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor Gray
    
    if ($configResponse.databaseName -ne $databaseName) {
  Write-Host ""
        Write-Host "⚠️  WARNING: Database name mismatch!" -ForegroundColor Yellow
  Write-Host "   Expected: $databaseName" -ForegroundColor Yellow
        Write-Host "   Got: $($configResponse.databaseName)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Could not retrieve configuration" -ForegroundColor Yellow
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 7: Testing connection via API..." -ForegroundColor Yellow

try {
    $testResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/test" `
  -Method POST `
   -Headers @{"Authorization" = "Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Connection test successful!" -ForegroundColor Green
    Write-Host "   Success: $($testResponse.success)" -ForegroundColor Gray
    Write-Host "   Message: $($testResponse.message)" -ForegroundColor Gray
    Write-Host "   Server Version: $($testResponse.serverVersion)" -ForegroundColor Gray
  Write-Host "   Response Time: $($testResponse.responseTimeMs) ms" -ForegroundColor Gray
    Write-Host "   Schema Exists: $($testResponse.schemaExists)" -ForegroundColor Gray
    Write-Host "   Missing Tables: $($testResponse.missingTables.Count)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Connection test failed" -ForegroundColor Yellow
  Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ DEBUG COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "✅ User has private cloud access" -ForegroundColor Green
Write-Host "✅ Database '$databaseName' is accessible" -ForegroundColor Green
Write-Host "✅ Configuration saved successfully" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Initialize schema: POST $API_URL/api/PrivateCloud/initialize-schema" -ForegroundColor White
Write-Host "2. Validate schema: POST $API_URL/api/PrivateCloud/validate-schema" -ForegroundColor White
Write-Host ""

Write-Host "Configuration Details:" -ForegroundColor Yellow
Write-Host "   Database Name: $databaseName" -ForegroundColor Gray
Write-Host "   Host: $TIDB_HOST" -ForegroundColor Gray
Write-Host "   Port: $TIDB_PORT" -ForegroundColor Gray
Write-Host "   User: $TIDB_USER" -ForegroundColor Gray
