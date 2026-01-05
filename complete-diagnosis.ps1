# Complete Diagnostic Script for Private Cloud Setup Failure

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "COMPLETE PRIVATE CLOUD SETUP DIAGNOSTICS" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$USER_EMAIL = "devste@gmail.com"

Write-Host "🔍 DIAGNOSIS FOR: $USER_EMAIL" -ForegroundColor Yellow
Write-Host ""

# Step 1: Check main database
Write-Host "Step 1: Checking main database..." -ForegroundColor Yellow

$MAIN_DB_HOST = Read-Host "Enter main database host (default: localhost)"
if ([string]::IsNullOrWhiteSpace($MAIN_DB_HOST)) {
    $MAIN_DB_HOST = "localhost"
}

$MAIN_DB_PORT = Read-Host "Enter main database port (default: 3306)"
if ([string]::IsNullOrWhiteSpace($MAIN_DB_PORT)) {
    $MAIN_DB_PORT = 3306
}

$MAIN_DB_NAME = Read-Host "Enter main database name (default: cloud_erase)"
if ([string]::IsNullOrWhiteSpace($MAIN_DB_NAME)) {
    $MAIN_DB_NAME = "cloud_erase"
}

$MAIN_DB_USER = Read-Host "Enter main database username (default: root)"
if ([string]::IsNullOrWhiteSpace($MAIN_DB_USER)) {
    $MAIN_DB_USER = "root"
}

$MAIN_DB_PASS = Read-Host "Enter main database password" -AsSecureString
$MAIN_DB_PASS_TEXT = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($MAIN_DB_PASS))

Write-Host ""
Write-Host "Checking if user exists in main database..." -ForegroundColor Gray

$checkUserQuery = "SELECT user_id, user_email, user_name, is_private_cloud, private_api, status FROM users WHERE user_email = '$USER_EMAIL'"

try {
    $userResult = mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -s -N -e $checkUserQuery 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $userResult) {
        Write-Host "✅ User found in main database" -ForegroundColor Green
        
        $userFields = $userResult -split "`t"
        Write-Host "   User ID: $($userFields[0])" -ForegroundColor Gray
        Write-Host "   Email: $($userFields[1])" -ForegroundColor Gray
        Write-Host "   Name: $($userFields[2])" -ForegroundColor Gray
   Write-Host "   is_private_cloud: $($userFields[3])" -ForegroundColor $(if($userFields[3] -eq "1") {"Green"} else {"Red"})
        Write-Host "   private_api: $($userFields[4])" -ForegroundColor $(if($userFields[4] -eq "1") {"Green"} else {"Red"})
        Write-Host "   Status: $($userFields[5])" -ForegroundColor Gray
  
        if ($userFields[3] -ne "1") {
            Write-Host ""
       Write-Host "❌ PROBLEM: is_private_cloud is NOT enabled!" -ForegroundColor Red
   Write-Host ""
          Write-Host "FIX: Run this command:" -ForegroundColor Yellow
            Write-Host "mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p -D $MAIN_DB_NAME -e `"UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = '$USER_EMAIL';`"" -ForegroundColor Gray
    Write-Host ""
     
       $response = Read-Host "Do you want me to fix it now? (yes/no)"
 if ($response -eq "yes") {
       $fixQuery = "UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = '$USER_EMAIL'"
       mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -e $fixQuery 2>&1
                
      if ($LASTEXITCODE -eq 0) {
           Write-Host "✅ User flags updated successfully!" -ForegroundColor Green
                } else {
    Write-Host "❌ Failed to update user flags" -ForegroundColor Red
      exit 1
     }
          }
        }
    } else {
        Write-Host "❌ User NOT found in main database!" -ForegroundColor Red
        Write-Host ""
     Write-Host "FIX: User must exist first. Create user or check email spelling." -ForegroundColor Yellow
        exit 1
  }
} catch {
    Write-Host "❌ Error checking main database: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Checking private_cloud_databases table..." -ForegroundColor Yellow

$checkConfigQuery = "SELECT config_id, user_email, database_name, test_status, schema_initialized FROM private_cloud_databases WHERE user_email = '$USER_EMAIL'"

try {
 $configResult = mysql -h $MAIN_DB_HOST -P $MAIN_DB_PORT -u $MAIN_DB_USER -p$MAIN_DB_PASS_TEXT -D $MAIN_DB_NAME -s -N -e $checkConfigQuery 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $configResult) {
        Write-Host "✅ Configuration exists" -ForegroundColor Green
        
  $configFields = $configResult -split "`t"
        Write-Host "   Config ID: $($configFields[0])" -ForegroundColor Gray
        Write-Host "   User Email: $($configFields[1])" -ForegroundColor Gray
    Write-Host "   Database: $($configFields[2])" -ForegroundColor Gray
        Write-Host "   Test Status: $($configFields[3])" -ForegroundColor Gray
        Write-Host "   Schema Initialized: $($configFields[4])" -ForegroundColor $(if($configFields[4] -eq "1") {"Green"} else {"Yellow"})
    } else {
      Write-Host "ℹ️  No configuration found (this is normal for first-time setup)" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not check configuration: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 3: Checking TiDB database..." -ForegroundColor Yellow

$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"
$TIDB_DB = "Tech"

Write-Host "Testing TiDB connection..." -ForegroundColor Gray

$tidbTestQuery = "SELECT 1 AS test"

try {
    $tidbResult = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -s -N -e $tidbTestQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ TiDB connection successful!" -ForegroundColor Green
        Write-Host "   Host: $TIDB_HOST" -ForegroundColor Gray
        Write-Host "   Port: $TIDB_PORT" -ForegroundColor Gray
        Write-Host "   Database: $TIDB_DB" -ForegroundColor Gray
    } else {
Write-Host "❌ TiDB connection failed!" -ForegroundColor Red
        Write-Host "   Error: $tidbResult" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 4: Checking API logs..." -ForegroundColor Yellow

$logFiles = Get-ChildItem -Path "logs" -Filter "app-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($logFiles) {
    Write-Host "Latest log file: $($logFiles.Name)" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Recent setup-related logs:" -ForegroundColor Gray
    Get-Content -Path $logFiles.FullName -Tail 200 | Select-String "SETUP|setup|Private|private" -CaseSensitive:$false | Select-Object -Last 10 | ForEach-Object {
    Write-Host "   $_" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Recent errors:" -ForegroundColor Gray
    Get-Content -Path $logFiles.FullName -Tail 200 | Select-String "ERROR|❌" -CaseSensitive:$false | Select-Object -Last 5 | ForEach-Object {
     Write-Host "   $_" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  No log files found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "DIAGNOSIS COMPLETE" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "1. Check if is_private_cloud = 1 ✓" -ForegroundColor White
Write-Host "2. Check if TiDB database 'Tech' exists ✓" -ForegroundColor White
Write-Host "3. Check if API can connect to TiDB ✓" -ForegroundColor White
Write-Host "4. Check application logs for errors ✓" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Ensure is_private_cloud = 1 for user" -ForegroundColor White
Write-Host "2. Ensure database 'Tech' exists in TiDB" -ForegroundColor White
Write-Host "3. Restart API application" -ForegroundColor White
Write-Host "4. Try setup again via API" -ForegroundColor White
Write-Host ""

Write-Host "Test setup with:" -ForegroundColor Yellow
Write-Host "POST http://localhost:5000/api/PrivateCloud/setup-simple" -ForegroundColor Gray
Write-Host "Body:" -ForegroundColor Gray
Write-Host '{' -ForegroundColor Gray
Write-Host '  "connectionString": "mysql://2tdeFNZMcsWKkDR.root:76wtaj1GZkg7Qhek@gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000/Tech?ssl-mode=REQUIRED",' -ForegroundColor Gray
Write-Host '  "databaseType": "mysql"' -ForegroundColor Gray
Write-Host '}' -ForegroundColor Gray
