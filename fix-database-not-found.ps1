# PowerShell Script to Fix "Unknown database" Error
# This will create the Cloud_Erase database if it doesn't exist

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Database Creation Fix Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# TiDB Configuration
$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"
$TIDB_DB = "Cloud_Erase"

Write-Host "Step 1: Checking mysql client..." -ForegroundColor Yellow
$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue

if (-not $mysqlPath) {
    Write-Host "❌ mysql client not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install MySQL client using one of these commands:" -ForegroundColor Yellow
    Write-Host "   winget install Oracle.MySQL" -ForegroundColor Gray
    Write-Host "   choco install mysql" -ForegroundColor Gray
    exit 1
}

Write-Host "✅ mysql client found: $($mysqlPath.Source)" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Testing TiDB connection (without database)..." -ForegroundColor Yellow

# Test connection without specifying database
$testQuery = "SELECT VERSION() AS version"
$mysqlArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "--ssl-mode=REQUIRED",
    "-s",
"-N",
    "-e", $testQuery
)

try {
    $version = & mysql $mysqlArgs 2>&1 | Select-Object -First 1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Connection successful!" -ForegroundColor Green
        Write-Host "   TiDB Version: $version" -ForegroundColor Gray
    } else {
        Write-Host "❌ Connection failed!" -ForegroundColor Red
        Write-Host "Error: $version" -ForegroundColor Red
   Write-Host ""
        Write-Host "Possible issues:" -ForegroundColor Yellow
        Write-Host "   - Wrong username/password" -ForegroundColor Gray
        Write-Host "   - TiDB cluster not running" -ForegroundColor Gray
     Write-Host "   - Network/firewall blocking connection" -ForegroundColor Gray
   exit 1
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Checking if database exists..." -ForegroundColor Yellow

$checkDbQuery = "SHOW DATABASES LIKE '$TIDB_DB'"
$mysqlCheckArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "--ssl-mode=REQUIRED",
    "-s",
    "-N",
    "-e", $checkDbQuery
)

$dbExists = & mysql $mysqlCheckArgs 2>&1

if ($dbExists -eq $TIDB_DB) {
    Write-Host "✅ Database '$TIDB_DB' already exists" -ForegroundColor Green
} else {
  Write-Host "⚠️  Database '$TIDB_DB' does not exist" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Creating database..." -ForegroundColor Yellow
    
    $createDbQuery = "CREATE DATABASE IF NOT EXISTS ``$TIDB_DB`` CHARACTER SET utf8mb4 COLLATE utf8mb4_bin"
    $mysqlCreateArgs = @(
        "-h", $TIDB_HOST,
        "-P", $TIDB_PORT,
        "-u", $TIDB_USER,
   "-p$TIDB_PASS",
        "--ssl-mode=REQUIRED",
   "-e", $createDbQuery
    )
    
    try {
        $result = & mysql $mysqlCreateArgs 2>&1
        if ($LASTEXITCODE -eq 0) {
     Write-Host "✅ Database '$TIDB_DB' created successfully!" -ForegroundColor Green
        } else {
    Write-Host "❌ Failed to create database!" -ForegroundColor Red
         Write-Host "Error: $result" -ForegroundColor Red
   
            Write-Host ""
            Write-Host "Possible reasons:" -ForegroundColor Yellow
            Write-Host "   - User doesn't have CREATE DATABASE permission" -ForegroundColor Gray
            Write-Host "   - Database name conflicts with reserved words" -ForegroundColor Gray
          exit 1
        }
    } catch {
   Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Step 4: Verifying database..." -ForegroundColor Yellow

$verifyQuery = "SELECT 1"
$mysqlVerifyArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
  "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-s",
    "-N",
    "-e", $verifyQuery
)

try {
    $result = & mysql $mysqlVerifyArgs 2>&1
if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database '$TIDB_DB' is accessible!" -ForegroundColor Green
    } else {
     Write-Host "❌ Cannot access database!" -ForegroundColor Red
        Write-Host "Error: $result" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 5: Listing all databases..." -ForegroundColor Yellow

$listDbQuery = "SHOW DATABASES"
$mysqlListArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "--ssl-mode=REQUIRED",
    "-e", $listDbQuery
)

Write-Host "Available databases:" -ForegroundColor Gray
& mysql $mysqlListArgs 2>&1

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ DATABASE FIX COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database '$TIDB_DB' is ready to use!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run the debug script again:" -ForegroundColor White
Write-Host "   .\debug-schema-init.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Or create tables directly:" -ForegroundColor White
Write-Host "   .\create-tidb-table.ps1" -ForegroundColor Gray
Write-Host ""
