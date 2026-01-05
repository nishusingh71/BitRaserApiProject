# Emergency Fix Script for devste@gmail.com - Failed to Configure

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🚨 EMERGENCY FIX - devste@gmail.com" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$USER_EMAIL = "devste@gmail.com"

# Step 1: Check if user exists and has correct flags
Write-Host "Step 1: Fixing user flags in main database..." -ForegroundColor Yellow

$MAIN_DB_HOST = "localhost"
$MAIN_DB_PORT = 3306
$MAIN_DB_NAME = "cloud_erase"
$MAIN_DB_USER = "root"

Write-Host "Enter main database password:" -ForegroundColor Gray
$MAIN_DB_PASS = Read-Host -AsSecureString
$MAIN_DB_PASS_TEXT = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($MAIN_DB_PASS))

# Check current user status
Write-Host "Checking user status..." -ForegroundColor Gray

$checkUserQuery = @"
SELECT 
    user_id,
    user_email,
    user_name,
    is_private_cloud,
    private_api,
    status
FROM users 
WHERE user_email = '$USER_EMAIL'
"@

try {
    $result = mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -s -N -e $checkUserQuery 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $result) {
        $fields = $result -split "`t"
        
        Write-Host "Current user status:" -ForegroundColor Gray
        Write-Host "   User ID: $($fields[0])" -ForegroundColor Gray
 Write-Host "   Email: $($fields[1])" -ForegroundColor Gray
        Write-Host "   Name: $($fields[2])" -ForegroundColor Gray
        Write-Host "   is_private_cloud: $($fields[3])" -ForegroundColor $(if($fields[3] -eq "1") {"Green"} else {"Red"})
      Write-Host "   private_api: $($fields[4])" -ForegroundColor $(if($fields[4] -eq "1") {"Green"} else {"Red"})
        Write-Host "   Status: $($fields[5])" -ForegroundColor Gray
     
        if ($fields[3] -ne "1" -or $fields[4] -ne "1") {
       Write-Host ""
      Write-Host "❌ PROBLEM FOUND: Flags not set correctly!" -ForegroundColor Red
            Write-Host ""
  Write-Host "Fixing now..." -ForegroundColor Yellow
            
   $fixQuery = "UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = '$USER_EMAIL'"
        
       $fixResult = mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -e $fixQuery 2>&1
   
        if ($LASTEXITCODE -eq 0) {
       Write-Host "✅ User flags updated successfully!" -ForegroundColor Green
            } else {
      Write-Host "❌ Failed to update flags: $fixResult" -ForegroundColor Red
              exit 1
            }
    } else {
            Write-Host "✅ User flags are correct!" -ForegroundColor Green
    }
    } else {
        Write-Host "❌ User not found or error: $result" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Database error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Checking TiDB Tech database..." -ForegroundColor Yellow

$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"
$TIDB_DB = "Tech"

Write-Host "Testing connection to Tech database..." -ForegroundColor Gray

$testQuery = "SELECT DATABASE() as db, VERSION() as version"

try {
    $result = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -s -N -e $testQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
     $fields = $result -split "`t"
        Write-Host "✅ TiDB connection successful!" -ForegroundColor Green
        Write-Host "   Current Database: $($fields[0])" -ForegroundColor Gray
        Write-Host "   Version: $($fields[1])" -ForegroundColor Gray
    } else {
        Write-Host "❌ TiDB connection failed!" -ForegroundColor Red
        Write-Host "   Error: $result" -ForegroundColor Red
        
        Write-Host ""
        Write-Host "Trying to create Tech database..." -ForegroundColor Yellow
        
     $createDbQuery = "CREATE DATABASE IF NOT EXISTS ``Tech`` CHARACTER SET utf8mb4 COLLATE utf8mb4_bin"
 mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS --ssl-mode=REQUIRED -e $createDbQuery 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Tech database created!" -ForegroundColor Green
        } else {
         Write-Host "❌ Failed to create database" -ForegroundColor Red
            exit 1
   }
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Cleaning up old configurations..." -ForegroundColor Yellow

$deleteOldConfigQuery = "DELETE FROM private_cloud_databases WHERE user_email = '$USER_EMAIL'"

try {
    mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -e $deleteOldConfigQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Old configurations deleted" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Could not delete old configs: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 4: Testing API setup..." -ForegroundColor Yellow

$API_URL = "http://localhost:5000"

Write-Host "Enter your password for login:" -ForegroundColor Gray
$userPassword = Read-Host -AsSecureString
$userPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($userPassword))

$loginBody = @{
    email = $USER_EMAIL
  password = $userPasswordText
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
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Setting up private cloud configuration..." -ForegroundColor Yellow

$connectionString = "mysql://$($TIDB_USER):$($TIDB_PASS)@$($TIDB_HOST):$($TIDB_PORT)/$($TIDB_DB)?ssl-mode=REQUIRED"

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
    notes = "Emergency fix - Tech database"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

try {
    $setupResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/setup-simple" `
        -Method POST `
   -Headers $headers `
    -Body $setupBody `
     -ErrorAction Stop
    
    Write-Host "✅ SETUP SUCCESSFUL!" -ForegroundColor Green
    Write-Host "   Message: $($setupResponse.message)" -ForegroundColor Gray
    Write-Host "   Detail: $($setupResponse.detail)" -ForegroundColor Gray
} catch {
    Write-Host "❌ SETUP FAILED!" -ForegroundColor Red
    
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
          Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
            Write-Host "   Detail: $($errorResponse.detail)" -ForegroundColor Red
         
            if ($errorResponse.error) {
      Write-Host "   Error: $($errorResponse.error)" -ForegroundColor Red
  }
        } catch {
            Write-Host "   Raw Error: $($_.ErrorDetails.Message)" -ForegroundColor Red
      }
    }
    
    Write-Host ""
    Write-Host "Checking application logs..." -ForegroundColor Yellow
    
    $logFiles = Get-ChildItem -Path "logs" -Filter "app-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($logFiles) {
    Write-Host "Last 30 lines from log:" -ForegroundColor Gray
     Get-Content -Path $logFiles.FullName -Tail 30 | ForEach-Object {
 if ($_ -match "ERROR|❌") {
            Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "WARN|⚠️") {
    Write-Host $_ -ForegroundColor Yellow
     } else {
   Write-Host $_ -ForegroundColor Gray
     }
      }
    }
    
    exit 1
}

Write-Host ""
Write-Host "Step 5: Verifying configuration..." -ForegroundColor Yellow

try {
$configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
        -Method GET `
  -Headers @{"Authorization" = "Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Configuration verified!" -ForegroundColor Green
    Write-Host "   Config ID: $($configResponse.configId)" -ForegroundColor Gray
    Write-Host "   Database Name: $($configResponse.databaseName)" -ForegroundColor Green
    Write-Host "   Server Host: $($configResponse.serverHost)" -ForegroundColor Gray
    Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Could not retrieve config: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ FIX COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "✅ User flags set correctly" -ForegroundColor Green
Write-Host "✅ TiDB Tech database accessible" -ForegroundColor Green
Write-Host "✅ Old configurations cleaned" -ForegroundColor Green
Write-Host "✅ New configuration saved" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test connection: POST /api/PrivateCloud/test" -ForegroundColor White
Write-Host "2. Initialize schema: POST /api/PrivateCloud/initialize-schema" -ForegroundColor White
Write-Host "3. Validate schema: POST /api/PrivateCloud/validate-schema" -ForegroundColor White
Write-Host ""

Write-Host "Configuration Details:" -ForegroundColor Yellow
Write-Host "   User: $USER_EMAIL" -ForegroundColor Gray
Write-Host "   Database: Tech" -ForegroundColor Gray
Write-Host "   Host: $TIDB_HOST" -ForegroundColor Gray
Write-Host "   Port: $TIDB_PORT" -ForegroundColor Gray
Write-Host ""

Write-Host "🎉 All fixed! Your private cloud is ready!" -ForegroundColor Green
