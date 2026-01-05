# PowerShell Script to Create private_cloud_databases Table in TiDB
# Run this to automatically create the table

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "TiDB Private Cloud Table Creation Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# TiDB Configuration
$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"
$TIDB_PASS = "76wtaj1GZkg7Qhek"
$TIDB_DB = "Cloud_Erase"
$TABLE_NAME = "private_cloud_databases"
$SQL_FILE = "create_private_cloud_databases_tidb.sql"

# Check if mysql command is available
Write-Host "Step 1: Checking for mysql client..." -ForegroundColor Yellow
$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue

if (-not $mysqlPath) {
    Write-Host "❌ mysql client not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install MySQL client:" -ForegroundColor Yellow
  Write-Host "1. Download from: https://dev.mysql.com/downloads/mysql/" -ForegroundColor White
    Write-Host "2. Or use: choco install mysql" -ForegroundColor White
    Write-Host "3. Or use: winget install Oracle.MySQL" -ForegroundColor White
    exit 1
}

Write-Host "✅ mysql client found at: $($mysqlPath.Source)" -ForegroundColor Green
Write-Host ""

# Check if SQL file exists
Write-Host "Step 2: Checking for SQL script file..." -ForegroundColor Yellow
if (-not (Test-Path $SQL_FILE)) {
    Write-Host "❌ SQL file not found: $SQL_FILE" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure '$SQL_FILE' is in the current directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ SQL file found: $SQL_FILE" -ForegroundColor Green
Write-Host ""

# Test TiDB connection
Write-Host "Step 3: Testing TiDB connection..." -ForegroundColor Yellow
$testQuery = "SELECT 1 AS test"
$mysqlTestArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
  "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-e", $testQuery
)

try {
    $result = & mysql $mysqlTestArgs 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ TiDB connection successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ TiDB connection failed!" -ForegroundColor Red
        Write-Host "Error: $result" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error connecting to TiDB: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Check if table already exists
Write-Host "Step 4: Checking if table already exists..." -ForegroundColor Yellow
$checkTableQuery = "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$TIDB_DB' AND table_name = '$TABLE_NAME'"
$mysqlCheckArgs = @(
    "-h", $TIDB_HOST,
 "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-s",
    "-N",
    "-e", $checkTableQuery
)

try {
    $tableExists = & mysql $mysqlCheckArgs 2>&1
    if ($tableExists -eq "1") {
     Write-Host "⚠️  Table '$TABLE_NAME' already exists!" -ForegroundColor Yellow
   Write-Host ""
        $response = Read-Host "Do you want to drop and recreate it? (yes/no)"
        
        if ($response -eq "yes") {
         Write-Host ""
            Write-Host "Dropping existing table..." -ForegroundColor Yellow
            $dropQuery = "DROP TABLE IF EXISTS $TABLE_NAME"
        $mysqlDropArgs = @(
        "-h", $TIDB_HOST,
                "-P", $TIDB_PORT,
     "-u", $TIDB_USER,
  "-p$TIDB_PASS",
                "-D", $TIDB_DB,
       "--ssl-mode=REQUIRED",
      "-e", $dropQuery
            )
         & mysql $mysqlDropArgs 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Table dropped successfully" -ForegroundColor Green
            } else {
        Write-Host "❌ Failed to drop table" -ForegroundColor Red
 exit 1
       }
        } else {
            Write-Host ""
          Write-Host "Operation cancelled. Table not modified." -ForegroundColor Yellow
         exit 0
        }
    } else {
        Write-Host "✅ Table does not exist. Ready to create." -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Could not check if table exists: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Create table from SQL file
Write-Host "Step 5: Creating table from SQL file..." -ForegroundColor Yellow
$mysqlCreateArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED"
)

try {
    Get-Content $SQL_FILE | & mysql $mysqlCreateArgs 2>&1
  
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ SQL script executed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to execute SQL script!" -ForegroundColor Red
    exit 1
    }
} catch {
    Write-Host "❌ Error executing SQL script: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Verify table creation
Write-Host "Step 6: Verifying table creation..." -ForegroundColor Yellow

# Check table exists
$verifyQuery = "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$TIDB_DB' AND table_name = '$TABLE_NAME'"
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
    $tableCount = & mysql $mysqlVerifyArgs 2>&1
    if ($tableCount -eq "1") {
   Write-Host "✅ Table '$TABLE_NAME' exists!" -ForegroundColor Green
    } else {
        Write-Host "❌ Table verification failed!" -ForegroundColor Red
        exit 1
    }
} catch {
 Write-Host "❌ Error verifying table: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Count columns
$columnQuery = "SELECT COUNT(*) as count FROM information_schema.columns WHERE table_schema = '$TIDB_DB' AND table_name = '$TABLE_NAME'"
$mysqlColumnArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-s",
    "-N",
    "-e", $columnQuery
)

try {
    $columnCount = & mysql $mysqlColumnArgs 2>&1
    Write-Host "✅ Column count: $columnCount" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Could not verify column count" -ForegroundColor Yellow
}

# Count indexes
$indexQuery = "SELECT COUNT(DISTINCT index_name) as count FROM information_schema.statistics WHERE table_schema = '$TIDB_DB' AND table_name = '$TABLE_NAME'"
$mysqlIndexArgs = @(
    "-h", $TIDB_HOST,
  "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
    "-p$TIDB_PASS",
    "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-s",
    "-N",
    "-e", $indexQuery
)

try {
    $indexCount = & mysql $mysqlIndexArgs 2>&1
    Write-Host "✅ Index count: $indexCount" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Could not verify index count" -ForegroundColor Yellow
}

Write-Host ""

# Show table structure
Write-Host "Step 7: Showing table structure..." -ForegroundColor Yellow
$descQuery = "DESC $TABLE_NAME"
$mysqlDescArgs = @(
    "-h", $TIDB_HOST,
    "-P", $TIDB_PORT,
    "-u", $TIDB_USER,
"-p$TIDB_PASS",
    "-D", $TIDB_DB,
    "--ssl-mode=REQUIRED",
    "-e", $descQuery
)

Write-Host ""
& mysql $mysqlDescArgs 2>&1

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ TABLE CREATION COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Table Details:" -ForegroundColor Yellow
Write-Host "  Database: $TIDB_DB" -ForegroundColor White
Write-Host "  Table: $TABLE_NAME" -ForegroundColor White
Write-Host "  Host: $TIDB_HOST" -ForegroundColor White
Write-Host "  Port: $TIDB_PORT" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. ✅ Enable private cloud for user:" -ForegroundColor White
Write-Host "   UPDATE users SET is_private_cloud = 1, private_api = 1 WHERE user_email = 'devste@gmail.com';" -ForegroundColor Gray
Write-Host ""
Write-Host "2. ✅ Test API endpoint:" -ForegroundColor White
Write-Host "   GET /api/PrivateCloud/check-access" -ForegroundColor Gray
Write-Host ""
Write-Host "3. ✅ Setup private database via API:" -ForegroundColor White
Write-Host "   POST /api/PrivateCloud/setup-simple" -ForegroundColor Gray
Write-Host ""
Write-Host "Table is ready for use! 🎉" -ForegroundColor Green
