# PowerShell Script to Debug Schema Initialization Error
# Run this to diagnose why schema initialization is failing

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Schema Initialization Debug Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# TiDB Configuration
$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"
$TIDB_DB = "Tech"

# Required tables
$REQUIRED_TABLES = @("users", "groups", "subuser", "machines", "audit_reports", "sessions", "logs", "commands")

Write-Host "Step 1: Checking mysql client..." -ForegroundColor Yellow
$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue

if (-not $mysqlPath) {
    Write-Host "❌ mysql client not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install using:" -ForegroundColor Yellow
    Write-Host "   winget install Oracle.MySQL" -ForegroundColor Gray
    Write-Host "   choco install mysql" -ForegroundColor Gray
    exit 1
}

Write-Host "✅ mysql client found" -ForegroundColor Green
Write-Host ""

# Create temporary credentials file to avoid password warning
$tempDir = $env:TEMP
$myCnfPath = Join-Path $tempDir "mysql_temp.cnf"

$myCnfContent = @"
[client]
host=$TIDB_HOST
port=$TIDB_PORT
user=$TIDB_USER
password=$TIDB_PASS
ssl-mode=REQUIRED
"@

Write-Host "Creating temporary config file..." -ForegroundColor Gray
Set-Content -Path $myCnfPath -Value $myCnfContent -NoNewline

Write-Host "Step 2: Checking if database exists..." -ForegroundColor Yellow

$checkDbQuery = "SHOW DATABASES LIKE '$TIDB_DB'"
$mysqlCheckDbArgs = @(
    "--defaults-extra-file=$myCnfPath",
    "-s",
    "-N",
    "-e", $checkDbQuery
)

try {
    $dbExists = & mysql $mysqlCheckDbArgs 2>&1 | Select-Object -First 1

    if ($dbExists -eq $TIDB_DB) {
        Write-Host "✅ Database '$TIDB_DB' exists" -ForegroundColor Green
    } else {
  Write-Host "❌ Database '$TIDB_DB' does NOT exist!" -ForegroundColor Red
     Write-Host ""
     Write-Host "Creating database..." -ForegroundColor Yellow
        
        $createDbQuery = "CREATE DATABASE IF NOT EXISTS ``$TIDB_DB`` CHARACTER SET utf8mb4 COLLATE utf8mb4_bin"
     $mysqlCreateArgs = @(
 "--defaults-extra-file=$myCnfPath",
   "-e", $createDbQuery
      )
  
        $result = & mysql $mysqlCreateArgs 2>&1
        if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database '$TIDB_DB' created successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to create database!" -ForegroundColor Red
            Write-Host "Error: $result" -ForegroundColor Red
      Remove-Item -Path $myCnfPath -ErrorAction SilentlyContinue
            exit 1
        }
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item -Path $myCnfPath -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "Step 3: Testing TiDB connection..." -ForegroundColor Yellow

$testQuery = "SELECT 1 AS test"
$mysqlArgs = @(
    "--defaults-extra-file=$myCnfPath",
    "-D", $TIDB_DB,
    "-s",
  "-N",
    "-e", $testQuery
)

try {
  $result = & mysql $mysqlArgs 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Connection successful!" -ForegroundColor Green
    } else {
     Write-Host "❌ Connection failed!" -ForegroundColor Red
   Write-Host "Error: $result" -ForegroundColor Red
        Remove-Item -Path $myCnfPath -ErrorAction SilentlyContinue
        exit 1
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item -Path $myCnfPath -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "Step 4: Checking existing tables..." -ForegroundColor Yellow

$showTablesQuery = "SHOW TABLES"
$mysqlShowArgs = @(
    "--defaults-extra-file=$myCnfPath",
    "-D", $TIDB_DB,
    "-s",
    "-N",
    "-e", $showTablesQuery
)

try {
    $existingTables = & mysql $mysqlShowArgs 2>&1 | Where-Object { $_ -match '\S' }

    if ($existingTables) {
        Write-Host "📋 Found $($existingTables.Count) existing tables:" -ForegroundColor Yellow
        foreach ($table in $existingTables) {
       Write-Host "   - $table" -ForegroundColor Gray
   }
    } else {
  Write-Host "ℹ️  No tables found in database" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not list tables: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 5: Checking required tables..." -ForegroundColor Yellow

$missingTables = @()
$existingRequired = @()

foreach ($table in $REQUIRED_TABLES) {
    $checkQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$TIDB_DB' AND table_name = '$table'"
    $mysqlCheckArgs = @(
   "--defaults-extra-file=$myCnfPath",
        "-s",
        "-N",
        "-e", $checkQuery
    )
    
    try {
        $count = & mysql $mysqlCheckArgs 2>&1
        if ($count -eq "1") {
      Write-Host " ✅ $table - EXISTS" -ForegroundColor Green
$existingRequired += $table
        } else {
            Write-Host "   ❌ $table - MISSING" -ForegroundColor Red
      $missingTables += $table
        }
    } catch {
        Write-Host "   ⚠️  $table - UNKNOWN" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "   Required tables: $($REQUIRED_TABLES.Count)" -ForegroundColor White
Write-Host "   Existing: $($existingRequired.Count)" -ForegroundColor Green
Write-Host "   Missing: $($missingTables.Count)" -ForegroundColor $(if ($missingTables.Count -gt 0) {"Red"} else {"Green"})

Write-Host ""
Write-Host "Step 6: Checking user permissions..." -ForegroundColor Yellow

$grantsQuery = "SHOW GRANTS FOR CURRENT_USER()"
$mysqlGrantsArgs = @(
    "--defaults-extra-file=$myCnfPath",
    "-e", $grantsQuery
)

Write-Host "   User permissions:" -ForegroundColor Gray
& mysql $mysqlGrantsArgs 2>&1

Write-Host ""
Write-Host "Step 7: Checking main database configuration..." -ForegroundColor Yellow

# Get API URL from user
$API_URL = Read-Host "Enter API URL (default: http://localhost:5000)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "http://localhost:5000"
}

$TOKEN = Read-Host "Enter JWT token (or press Enter to skip)"

if (-not [string]::IsNullOrWhiteSpace($TOKEN)) {
    Write-Host ""
    Write-Host "Checking API configuration..." -ForegroundColor Gray
    
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
    }
    
    try {
     $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
  -Method GET `
          -Headers $headers `
            -ErrorAction Stop
        
        Write-Host "✅ Configuration found:" -ForegroundColor Green
        Write-Host "   User Email: $($configResponse.userEmail)" -ForegroundColor Gray
        Write-Host "   Database: $($configResponse.databaseName)" -ForegroundColor Gray
     Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor $(if($configResponse.schemaInitialized) {"Green"} else {"Red"})
        Write-Host "Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
    } catch {
      Write-Host "⚠️  Could not fetch configuration from API" -ForegroundColor Yellow
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

# Cleanup temp config file
Remove-Item -Path $myCnfPath -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "DIAGNOSIS COMPLETE" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Determine the issue
if ($missingTables.Count -eq $REQUIRED_TABLES.Count) {
    Write-Host "🔍 DIAGNOSIS: No tables created yet" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "RECOMMENDED FIX:" -ForegroundColor Green
    Write-Host "1. Run table creation script:" -ForegroundColor White
    Write-Host "   .\create-tidb-table.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Or manually:" -ForegroundColor White
    Write-Host "   Run: .\fix-database-not-found.ps1" -ForegroundColor Gray
    Write-Host "   Then: mysql ... < create_private_cloud_databases_tidb.sql" -ForegroundColor Gray
}
elseif ($missingTables.Count -gt 0 -and $missingTables.Count -lt $REQUIRED_TABLES.Count) {
    Write-Host "🔍 DIAGNOSIS: Partial schema creation (some tables missing)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Missing tables: $($missingTables -join ', ')" -ForegroundColor Red
    Write-Host ""
    Write-Host "RECOMMENDED FIX:" -ForegroundColor Green
    Write-Host "1. Drop all existing tables and recreate:" -ForegroundColor White
    Write-Host ""
    Write-Host "   SET FOREIGN_KEY_CHECKS = 0;" -ForegroundColor Gray
    foreach ($table in $existingRequired) {
        Write-Host "   DROP TABLE IF EXISTS $table;" -ForegroundColor Gray
    }
    Write-Host "   SET FOREIGN_KEY_CHECKS = 1;" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Then run creation script again" -ForegroundColor White
}
elseif ($missingTables.Count -eq 0) {
    Write-Host "✅ DIAGNOSIS: All required tables exist!" -ForegroundColor Green
    Write-Host ""
    Write-Host "If you're still seeing errors, check:" -ForegroundColor Yellow
Write-Host "1. Table structure matches expected schema" -ForegroundColor White
    Write-Host "2. Foreign key relationships are correct" -ForegroundColor White
    Write-Host "3. Application has correct connection string" -ForegroundColor White
    Write-Host ""
  Write-Host "To verify schema:" -ForegroundColor Yellow
    Write-Host "   curl -X POST $API_URL/api/PrivateCloud/validate-schema -H 'Authorization: Bearer YOUR_TOKEN'" -ForegroundColor Gray
}

Write-Host ""
Write-Host "For detailed logs, check:" -ForegroundColor Yellow
Write-Host "   Application logs: logs\app-*.log" -ForegroundColor Gray
Write-Host "   Search for: 'schema', 'CreateDatabaseSchema', 'InitializeDatabaseSchema'" -ForegroundColor Gray
Write-Host ""
Write-Host "Run this command to see errors:" -ForegroundColor Yellow
Write-Host "   Get-Content logs\app-*.log -Tail 100 | Select-String 'error|exception|schema' -CaseSensitive:`$false" -ForegroundColor Gray
