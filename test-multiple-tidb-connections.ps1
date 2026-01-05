# Test Multiple TiDB Connections

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Testing Multiple TiDB Database Connections" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# TiDB Configuration
$TIDB_HOST = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$TIDB_PORT = 4000
$TIDB_USER = "2tdeFNZMcsWKkDR.root"

Write-Host "Enter TiDB password:" -NoNewline
$TIDB_PASS_SECURE = Read-Host -AsSecureString
$TIDB_PASS = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($TIDB_PASS_SECURE))

if ([string]::IsNullOrWhiteSpace($TIDB_PASS)) {
    $TIDB_PASS = "76wtaj1GZkg7Qhek"
}

Write-Host ""

# Define databases to test
$databases = @(
    @{ Name = "Cloud_Erase__App"; Description = "Main Application Database" },
    @{ Name = "Cloud_Erase"; Description = "Cloud Erase Database" },
    @{ Name = "Analytics_DB"; Description = "Analytics Database" },
    @{ Name = "Reporting_DB"; Description = "Reporting Database" },
    @{ Name = "Logs_DB"; Description = "Logs Database" }
)

$successCount = 0
$failCount = 0

Write-Host "Starting connection tests..." -ForegroundColor Yellow
Write-Host ""

foreach ($db in $databases) {
    Write-Host "Testing: $($db.Name)" -ForegroundColor Yellow
    Write-Host "   Description: $($db.Description)" -ForegroundColor Gray
    Write-Host "   Connecting..." -NoNewline
    
    $testQuery = "SELECT DATABASE() as current_db, VERSION() as version, NOW() as server_time"
    
    try {
        $result = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS `
        -D $db.Name --ssl-mode=REQUIRED -s -N -e $testQuery 2>&1
        
     if ($LASTEXITCODE -eq 0) {
            Write-Host " ✅ SUCCESS" -ForegroundColor Green
            
     $fields = $result -split "`t"
      Write-Host "   Current Database: $($fields[0])" -ForegroundColor Green
         Write-Host "   Version: $($fields[1])" -ForegroundColor Gray
       Write-Host "   Server Time: $($fields[2])" -ForegroundColor Gray
    
   # Test table count
      $tableCountQuery = "SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = '$($db.Name)'"
      $tableResult = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS `
-D $db.Name --ssl-mode=REQUIRED -s -N -e $tableCountQuery 2>&1
       
            if ($LASTEXITCODE -eq 0) {
             Write-Host "   Tables: $tableResult" -ForegroundColor Gray
            }
            
     $successCount++
        } else {
 Write-Host " ❌ FAILED" -ForegroundColor Red
      Write-Host "   Error: $result" -ForegroundColor Red
 $failCount++
        }
    } catch {
        Write-Host " ❌ ERROR" -ForegroundColor Red
        Write-Host "   Exception: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
    
    Write-Host ""
}

# Summary
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Total Databases Tested: $($databases.Count)" -ForegroundColor White
Write-Host "Successful Connections: $successCount" -ForegroundColor Green
Write-Host "Failed Connections: $failCount" -ForegroundColor $(if($failCount -gt 0) {"Red"} else {"Green"})
Write-Host ""

if ($successCount -eq $databases.Count) {
    Write-Host "🎉 All connections successful!" -ForegroundColor Green
} elseif ($successCount -gt 0) {
    Write-Host "⚠️  Some connections failed" -ForegroundColor Yellow
} else {
    Write-Host "❌ All connections failed" -ForegroundColor Red
}

Write-Host ""

# Generate connection strings
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Connection Strings for appsettings.json" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

foreach ($db in $databases) {
    $connStr = "Server=$TIDB_HOST;Port=$TIDB_PORT;Database=$($db.Name);User=$TIDB_USER;Password=***HIDDEN***;SslMode=Required;"
    Write-Host """$($db.Name)Connection"": ""$connStr""," -ForegroundColor Gray
}

Write-Host ""

# Test cross-database query
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Testing Cross-Database Query" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$crossDbQuery = @"
SELECT 
    'Cloud_Erase__App' as DatabaseName,
    (SELECT COUNT(*) FROM Cloud_Erase__App.information_schema.tables WHERE table_schema = 'Cloud_Erase__App') as TableCount
UNION ALL
SELECT 
    'Cloud_Erase',
    (SELECT COUNT(*) FROM Cloud_Erase.information_schema.tables WHERE table_schema = 'Cloud_Erase')
UNION ALL
SELECT 
    'Analytics_DB',
    (SELECT COUNT(*) FROM Analytics_DB.information_schema.tables WHERE table_schema = 'Analytics_DB')
UNION ALL
SELECT 
    'Reporting_DB',
    (SELECT COUNT(*) FROM Reporting_DB.information_schema.tables WHERE table_schema = 'Reporting_DB')
UNION ALL
SELECT 
    'Logs_DB',
    (SELECT COUNT(*) FROM Logs_DB.information_schema.tables WHERE table_schema = 'Logs_DB')
"@

try {
    Write-Host "Executing cross-database query..." -ForegroundColor Yellow
    
    $crossResult = mysql -h $TIDB_HOST -P $TIDB_PORT -u $TIDB_USER -p$TIDB_PASS `
 --ssl-mode=REQUIRED -e $crossDbQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
      Write-Host "✅ Cross-database query successful!" -ForegroundColor Green
        Write-Host ""
        Write-Host $crossResult
    } else {
        Write-Host "❌ Cross-database query failed" -ForegroundColor Red
     Write-Host $crossResult
    }
} catch {
    Write-Host "❌ Error executing cross-database query" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Update appsettings.json with connection strings" -ForegroundColor White
Write-Host "2. Create DbContext classes for each database" -ForegroundColor White
Write-Host "3. Register DbContexts in Program.cs" -ForegroundColor White
Write-Host "4. Use multiple contexts in your controllers/services" -ForegroundColor White
Write-Host ""

Write-Host "Documentation:" -ForegroundColor Yellow
Write-Host "- See MULTIPLE-TIDB-CONNECTIONS-GUIDE.md for implementation details" -ForegroundColor White
Write-Host "- Run create-multiple-tidb-databases.sql to create test tables" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Testing complete!" -ForegroundColor Green
