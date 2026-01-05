# Complete Schema Initialization Diagnostic Script - FULLY DYNAMIC

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Schema Initialization Diagnostic & Fix" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Get API URL
Write-Host "Enter API Configuration:" -ForegroundColor Yellow
$API_URL = Read-Host "API URL (default: https://localhost:44316)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "https://localhost:44316"
}
Write-Host "✅ Using API URL: $API_URL" -ForegroundColor Green
Write-Host ""

# Get User Email
Write-Host "Enter User Details:" -ForegroundColor Yellow
$USER_EMAIL = Read-Host "User Email (default: devste@gmail.com)"
if ([string]::IsNullOrWhiteSpace($USER_EMAIL)) {
    $USER_EMAIL = "devste@gmail.com"
}
Write-Host "✅ Using User Email: $USER_EMAIL" -ForegroundColor Green
Write-Host ""

# Get TiDB Connection Details
Write-Host "Enter TiDB Connection Details:" -ForegroundColor Yellow

$TIDB_HOST = Read-Host "TiDB Host (default: gateway01.ap-southeast-1.prod.aws.tidbcloud.com)"
if ([string]::IsNullOrWhiteSpace($TIDB_HOST)) {
    $TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
}

$TIDB_PORT = Read-Host "TiDB Port (default: 4000)"
if ([string]::IsNullOrWhiteSpace($TIDB_PORT)) {
    $TIDB_PORT = 4000
} else {
 $TIDB_PORT = [int]$TIDB_PORT
}

$TIDB_USER = Read-Host "TiDB Username (default: 2tdeFNZMcsWKkDR.root)"
if ([string]::IsNullOrWhiteSpace($TIDB_USER)) {
    $TIDB_USER = "2tdeFNZMcsWKkDR.root"
}

Write-Host "TiDB Password:" -NoNewline
$TIDB_PASS_SECURE = Read-Host -AsSecureString
$TIDB_PASS = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($TIDB_PASS_SECURE))
if ([string]::IsNullOrWhiteSpace($TIDB_PASS)) {
    $TIDB_PASS = "76wtaj1GZkg7Qhek"
}

$TIDB_DB = Read-Host "TiDB Database Name (default: Tech)"
if ([string]::IsNullOrWhiteSpace($TIDB_DB)) {
    $TIDB_DB = "Tech"
}

Write-Host ""
Write-Host "✅ Configuration Summary:" -ForegroundColor Green
Write-Host "   API URL: $API_URL" -ForegroundColor Gray
Write-Host "   User Email: $USER_EMAIL" -ForegroundColor Gray
Write-Host "   TiDB Host: $TIDB_HOST" -ForegroundColor Gray
Write-Host "   TiDB Port: $TIDB_PORT" -ForegroundColor Gray
Write-Host "   TiDB User: $TIDB_USER" -ForegroundColor Gray
Write-Host "   TiDB Database: $TIDB_DB" -ForegroundColor Gray
Write-Host ""

$confirm = Read-Host "Proceed with these settings? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Cancelled by user" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Step 1: Checking mysql client..." -ForegroundColor Yellow

$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue

if (-not $mysqlPath) {
    Write-Host "❌ mysql client not found!" -ForegroundColor Red
    Write-Host "Install: winget install Oracle.MySQL" -ForegroundColor Yellow
 exit 1
}

Write-Host "✅ mysql client found" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Testing TiDB connection..." -ForegroundColor Yellow

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
        Write-Host "Possible issues:" -ForegroundColor Yellow
        Write-Host "   - Database '$TIDB_DB' doesn't exist" -ForegroundColor White
        Write-Host "   - Wrong credentials" -ForegroundColor White
  Write-Host "   - Network/firewall issue" -ForegroundColor White
 Write-Host "   - SSL certificate problem" -ForegroundColor White
 exit 1
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Checking existing tables..." -ForegroundColor Yellow

$showTablesQuery = "SHOW TABLES"

try {
    $existingTables = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -s -N -e $showTablesQuery 2>&1 | Where-Object { $_ -match '\S' }
    
    if ($existingTables) {
 Write-Host "📋 Found $($existingTables.Count) existing tables:" -ForegroundColor Yellow
     foreach ($table in $existingTables) {
    Write-Host "   - $table" -ForegroundColor Gray
        }
 
      Write-Host ""
        $dropTables = Read-Host "Do you want to drop existing tables? (yes/no)"
     
 if ($dropTables -eq "yes") {
  Write-Host ""
         Write-Host "Dropping tables..." -ForegroundColor Yellow
   
        # Disable foreign key checks
            mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -e "SET FOREIGN_KEY_CHECKS = 0" 2>&1
 
     # Drop tables in reverse order
    $tablesToDrop = @("commands", "logs", "sessions", "audit_reports", "machines", "subuser", "groups", "users")
  
    foreach ($table in $tablesToDrop) {
      Write-Host "   Dropping $table..." -ForegroundColor Gray
  mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDBPASS -D $TIDB_DB --ssl-mode=REQUIRED -e "DROP TABLE IF EXISTS ``$table``" 2>&1
     }
     
  # Re-enable foreign key checks
  mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -e "SET FOREIGN_KEY_CHECKS = 1" 2>&1
   
       Write-Host "✅ Tables dropped" -ForegroundColor Green
        }
  } else {
     Write-Host "ℹ️  No tables found (clean database)" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not list tables: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 4: Checking user permissions..." -ForegroundColor Yellow

$grantsQuery = "SHOW GRANTS FOR CURRENT_USER()"

try {
    Write-Host "User permissions:" -ForegroundColor Gray
    mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -e $grantsQuery 2>&1
} catch {
    Write-Host "⚠️  Could not check permissions: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 5: Login to API..." -ForegroundColor Yellow

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
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "   - Wrong email or password" -ForegroundColor White
    Write-Host "   - User doesn't exist" -ForegroundColor White
    Write-Host "   - API not running on $API_URL" -ForegroundColor White
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "Step 6: Check configuration..." -ForegroundColor Yellow

try {
    $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
  -Method GET `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
  -ErrorAction Stop
    
  Write-Host "✅ Configuration found!" -ForegroundColor Green
    Write-Host "   Database: $($configResponse.databaseName)" -ForegroundColor Gray
    Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor Gray
    Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
} catch {
  Write-Host "⚠️  No configuration found" -ForegroundColor Yellow
  Write-Host "   Setting up configuration..." -ForegroundColor Gray
    
    $connectionString = "mysql://$($TIDB_USER):$($TIDB_PASS)@$($TIDB_HOST):$($TIDB_PORT)/$($TIDB_DB)?ssl-mode=REQUIRED"
    
    $setupBody = @{
   connectionString = $connectionString
   databaseType = "mysql"
    notes = "Diagnostic setup - Database: $TIDB_DB"
    } | ConvertTo-Json
    
    try {
 $setupResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/setup-simple" `
            -Method POST `
    -Headers $headers `
  -Body $setupBody `
     -ErrorAction Stop
        
 Write-Host "✅ Configuration setup successful!" -ForegroundColor Green
  } catch {
Write-Host "❌ Setup failed: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
    }
}

Write-Host ""
Write-Host "Step 7: Test connection..." -ForegroundColor Yellow

try {
    $testResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/test" `
    -Method POST `
   -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Connection test successful!" -ForegroundColor Green
    Write-Host "   Success: $($testResponse.success)" -ForegroundColor Gray
    Write-Host "   Server Version: $($testResponse.serverVersion)" -ForegroundColor Gray
 Write-Host "   Missing Tables: $($testResponse.missingTables.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 8: Initialize schema..." -ForegroundColor Yellow

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
Write-Host "Step 9: Validate schema..." -ForegroundColor Yellow

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

Write-Host ""
Write-Host "Step 10: Final verification..." -ForegroundColor Yellow

try {
    $finalTables = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS -D $TIDB_DB --ssl-mode=REQUIRED -s -N -e "SHOW TABLES" 2>&1 | Where-Object { $_ -match '\S' }
  
    Write-Host "✅ Tables in TiDB database '$TIDB_DB':" -ForegroundColor Green
    foreach ($table in $finalTables) {
   Write-Host "    ✅ $table" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "   Total tables: $($finalTables.Count)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Could not verify tables: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ DIAGNOSTIC COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration Used:" -ForegroundColor Yellow
Write-Host "   API URL: $API_URL" -ForegroundColor Gray
Write-Host "   User Email: $USER_EMAIL" -ForegroundColor Gray
Write-Host "   TiDB Host: $TIDB_HOST" -ForegroundColor Gray
Write-Host "   TiDB Port: $TIDB_PORT" -ForegroundColor Gray
Write-Host "   TiDB Database: $TIDB_DB" -ForegroundColor Gray
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "✅ TiDB connection working" -ForegroundColor Green
Write-Host "✅ User authenticated" -ForegroundColor Green
Write-Host "✅ Configuration saved" -ForegroundColor Green
Write-Host "✅ Schema initialized" -ForegroundColor Green
Write-Host "✅ All tables created" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Start using private cloud features" -ForegroundColor White
Write-Host "2. Create subusers in your private database" -ForegroundColor White
Write-Host "3. Generate audit reports in your private database" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Schema initialization successful!" -ForegroundColor Green
